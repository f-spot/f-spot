//
// HyenaSqliteConnection.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading;
using System.Collections.Generic;

namespace Hyena.Data.Sqlite
{
    public class HyenaDataReader : IDisposable
    {
        private IDataReader reader;
        private bool read = false;

        public IDataReader Reader {
            get { return reader; }
        }

        public HyenaDataReader (IDataReader reader)
        {
            this.reader = reader;
        }

        public T Get<T> (int i)
        {
            if (!read) {
                Read ();
            }
            return reader.Get<T> (i);
        }

        public bool Read ()
        {
            read = true;
            return reader.Read ();
        }

        public void Dispose ()
        {
            reader.Dispose ();
            reader = null;
        }
    }

    public enum HyenaCommandType {
        Reader,
        Scalar,
        Execute,
    }

    public class HyenaSqliteConnection : IDisposable
    {
        private Hyena.Data.Sqlite.Connection connection;
        private string dbpath;

        protected string DbPath { get { return dbpath; } }

        // These are 'parallel' queues; that is, when a value is pushed or popped to
        // one, a value is pushed or popped to all three.
        // The 1st contains the command object itself, and the 2nd and 3rd contain the
        // arguments to be applied to that command (filled in for any ? placeholder in the command).
        // The 3rd exists as an optimization to avoid making an object [] for a single arg.
        private Queue<HyenaSqliteCommand> command_queue = new Queue<HyenaSqliteCommand>();
        private Queue<object[]> args_queue = new Queue<object[]>();
        private Queue<object> arg_queue = new Queue<object>();

        private Thread queue_thread;
        private volatile bool dispose_requested = false;
        private volatile int results_ready = 0;
        private AutoResetEvent queue_signal = new AutoResetEvent (false);
        internal ManualResetEvent ResultReadySignal = new ManualResetEvent (false);

        private volatile Thread transaction_thread = null;
        private ManualResetEvent transaction_signal = new ManualResetEvent (true);

        private Thread warn_if_called_from_thread;
        public Thread WarnIfCalledFromThread {
            get { return warn_if_called_from_thread; }
            set { warn_if_called_from_thread = value; }
        }

        public string ServerVersion { get { return Query<string> ("SELECT sqlite_version ()"); } }

        public HyenaSqliteConnection(string dbpath)
        {
            this.dbpath = dbpath;
            queue_thread = new Thread(ProcessQueue);
            queue_thread.Name = string.Format ("HyenaSqliteConnection ({0})", dbpath);
            queue_thread.IsBackground = true;
            queue_thread.Start();
        }

        public void AddFunction<T> () where T : SqliteFunction
        {
            connection.AddFunction<T> ();
        }

        public void RemoveFunction<T> () where T : SqliteFunction
        {
            connection.RemoveFunction<T> ();
        }


#region Public Query Methods

        // TODO special case for single object param to avoid object []

        // SELECT multiple column queries
        public IDataReader Query (HyenaSqliteCommand command)
        {
            command.CommandType = HyenaCommandType.Reader;
            QueueCommand (command);
            return (IDataReader) command.WaitForResult (this);
        }

        public IDataReader Query (HyenaSqliteCommand command, params object [] param_values)
        {
            command.CommandType = HyenaCommandType.Reader;
            QueueCommand (command, param_values);
            return (IDataReader) command.WaitForResult (this);
        }

        public IDataReader Query (string command_str, params object [] param_values)
        {
            return Query (new HyenaSqliteCommand (command_str, param_values) { ReaderDisposes = true });
        }

        public IDataReader Query (object command)
        {
            return Query (new HyenaSqliteCommand (command.ToString ()) { ReaderDisposes = true });
        }

        // SELECT single column, multiple rows queries
        public IEnumerable<T> QueryEnumerable<T> (HyenaSqliteCommand command)
        {
            Type type = typeof (T);
            using (IDataReader reader = Query (command)) {
                while (reader.Read ()) {
                    yield return (T) reader.Get (0, type);
                }
            }
        }

        public IEnumerable<T> QueryEnumerable<T> (HyenaSqliteCommand command, params object [] param_values)
        {
            Type type = typeof (T);
            using (IDataReader reader = Query (command, param_values)) {
                while (reader.Read ()) {
                    yield return (T) reader.Get (0, type);
                }
            }
        }

        public IEnumerable<T> QueryEnumerable<T> (string command_str, params object [] param_values)
        {
            return QueryEnumerable<T> (new HyenaSqliteCommand (command_str, param_values) { ReaderDisposes = true });
        }

        public IEnumerable<T> QueryEnumerable<T> (object command)
        {
            return QueryEnumerable<T> (new HyenaSqliteCommand (command.ToString ()) { ReaderDisposes = true });
        }

        // SELECT single column, single row queries
        public T Query<T> (HyenaSqliteCommand command)
        {
            command.CommandType = HyenaCommandType.Scalar;
            QueueCommand (command);
            object result = command.WaitForResult (this);
            return (T)SqliteUtils.FromDbFormat (typeof (T), result);
        }

        public T Query<T> (HyenaSqliteCommand command, params object [] param_values)
        {
            command.CommandType = HyenaCommandType.Scalar;
            QueueCommand (command, param_values);
            object result = command.WaitForResult (this);
            return (T)SqliteUtils.FromDbFormat (typeof (T), result);
        }

        public T Query<T> (string command_str, params object [] param_values)
        {
            return Query<T> (new HyenaSqliteCommand (command_str, param_values) { ReaderDisposes = true });
        }

        public T Query<T> (object command)
        {
            return Query<T> (new HyenaSqliteCommand (command.ToString ()) { ReaderDisposes = true });
        }

        // INSERT, UPDATE, DELETE queries
        public long Execute (HyenaSqliteCommand command)
        {
            command.CommandType = HyenaCommandType.Execute;;
            QueueCommand(command);
            return (long)command.WaitForResult (this);
        }

        public long Execute (HyenaSqliteCommand command, params object [] param_values)
        {
            command.CommandType = HyenaCommandType.Execute;;
            QueueCommand(command, param_values);
            return (long)command.WaitForResult (this);
        }

        public long Execute (string command_str, params object [] param_values)
        {
            return Execute (new HyenaSqliteCommand (command_str, param_values) { ReaderDisposes = true });
        }

        public long Execute (object command)
        {
            return Execute (new HyenaSqliteCommand (command.ToString ()) { ReaderDisposes = true });
        }

#endregion

#region Public Utility Methods

        public void BeginTransaction ()
        {
            if (transaction_thread == Thread.CurrentThread) {
                throw new Exception ("Can't start a recursive transaction");
            }

            while (transaction_thread != Thread.CurrentThread) {
                if (transaction_thread != null) {
                    // Wait for the existing transaction to finish before this thread proceeds
                    transaction_signal.WaitOne ();
                }

                lock (command_queue) {
                    if (transaction_thread == null) {
                        transaction_thread = Thread.CurrentThread;
                        transaction_signal.Reset ();
                    }
                }
            }

            Execute ("BEGIN TRANSACTION");
        }

        public void CommitTransaction ()
        {
            if (transaction_thread != Thread.CurrentThread) {
                throw new Exception ("Can't commit from outside a transaction");
            }

            Execute ("COMMIT TRANSACTION");

            lock (command_queue) {
                transaction_thread = null;
                // Let any other threads continue
                transaction_signal.Set ();
            }
        }

        public void RollbackTransaction ()
        {
            if (transaction_thread != Thread.CurrentThread) {
                throw new Exception ("Can't rollback from outside a transaction");
            }

            Execute ("ROLLBACK");

            lock (command_queue) {
                transaction_thread = null;

                // Let any other threads continue
                transaction_signal.Set ();
            }
        }

        public bool TableExists (string tableName)
        {
            return Exists ("table", tableName);
        }

        public bool IndexExists (string indexName)
        {
            return Exists ("index", indexName);
        }

        private bool Exists (string type, string name)
        {
            return Exists (type, name, "sqlite_master") || Exists (type, name, "sqlite_temp_master");
        }

        private bool Exists (string type, string name, string master)
        {
            return Query<int> (string.Format (
                "SELECT COUNT(*) FROM {0} WHERE Type='{1}' AND Name='{2}'",
                master, type, name)
            ) > 0;
        }

        private delegate void SchemaHandler (string column);

        private void SchemaClosure (string table_name, SchemaHandler code)
        {
            string sql = Query<string> (string.Format (
                "SELECT sql FROM sqlite_master WHERE Name='{0}'", table_name));
            if (string.IsNullOrEmpty (sql)) {
                return;
            }
            sql = sql.Substring (sql.IndexOf ('(') + 1);
            foreach (string column_def in sql.Split (',')) {
                string column_def_t = column_def.Trim ();
                int ws_index = column_def_t.IndexOfAny (ws_chars);
                code (ws_index == -1 ? column_def_t : column_def_t.Substring (0, ws_index));
            }
        }

        public bool ColumnExists (string tableName, string columnName)
        {
            bool value = false;
            SchemaClosure (tableName, delegate (string column) {
                if (column == columnName) {
                    value = true;
                    return;
                }
            });
            return value;
        }

        private static readonly char [] ws_chars = new char [] { ' ', '\t', '\n', '\r' };
        public IDictionary<string, string> GetSchema (string table_name)
        {
            Dictionary<string, string> schema = new Dictionary<string,string> ();
            SchemaClosure (table_name, delegate (string column) {
                schema.Add (column.ToLower (), null);
            });
            return schema;
        }

#endregion

#region Private Queue Methods

        private void QueueCommand(HyenaSqliteCommand command, object [] args)
        {
            QueueCommand (command, null, args);
        }

        // TODO optimize object vs object [] code paths?
        /*private void QueueCommand(HyenaSqliteCommand command, object arg)
        {
            QueueCommand (command, arg, null);
        }*/

        private void QueueCommand(HyenaSqliteCommand command)
        {
            QueueCommand (command, null, null);
        }

        private void QueueCommand(HyenaSqliteCommand command, object arg, object [] args)
        {
            if (warn_if_called_from_thread != null && Thread.CurrentThread == warn_if_called_from_thread) {
                Hyena.Log.Warning ("HyenaSqliteConnection command issued from the main thread");
            }

            while (true) {
                lock (command_queue) {
                    if (dispose_requested) {
                        // No point in queueing the command if we're already disposing.
                        // This helps avoid using the probably-disposed queue_signal below too
                        return;
                    } else if (transaction_thread == null || Thread.CurrentThread == transaction_thread) {
                        command_queue.Enqueue (command);
                        args_queue.Enqueue (args);
                        arg_queue.Enqueue (arg);
                        break;
                    }
                }

                transaction_signal.WaitOne ();
            }
            queue_signal.Set ();
        }

        internal void ClaimResult ()
        {
            lock (command_queue) {
                results_ready--;
                if (results_ready == 0) {
                    ResultReadySignal.Reset ();
                }
            }
        }

        private void ProcessQueue()
        {
            if (connection == null) {
                connection = new Hyena.Data.Sqlite.Connection (dbpath);
            }

            // Keep handling queries
            while (!dispose_requested) {
                while (command_queue.Count > 0) {
                    HyenaSqliteCommand command;
                    object [] args;
                    object arg;
                    lock (command_queue) {
                        command = command_queue.Dequeue ();
                        args = args_queue.Dequeue ();
                        arg = arg_queue.Dequeue ();
                    }

                    // Ensure the command is not altered while applying values or executing
                    lock (command) {
                        command.WaitIfNotFinished ();

                        if (arg != null) {
                            command.ApplyValues (arg);
                        } else if (args != null) {
                            command.ApplyValues (args);
                        }

                        command.Execute (this, connection);
                    }

                    lock (command_queue) {
                        results_ready++;
                        ResultReadySignal.Set ();
                    }
                }

                queue_signal.WaitOne ();
            }

            // Finish
            connection.Dispose ();
        }

#endregion

        public void Dispose()
        {
            dispose_requested = true;
            queue_signal.Set ();
            queue_thread.Join ();

            queue_signal.Close ();
            ResultReadySignal.Close ();
            transaction_signal.Close ();
        }
    }
}
