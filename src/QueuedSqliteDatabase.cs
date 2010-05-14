/***************************************************************************
 *  QueuedSqliteDatabase.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Mono.Data.SqliteClient;

using FSpot.Utils;

namespace Banshee.Database
{
    /// <summary>
    /// A thread-safe wrapper for SQLite.
    /// </summary>
    /// <remarks>
    /// SQLite is not thread-safe by default on Linux. Therefor, we handle 
    /// all SQL queries on a seperate thread.
    /// </remarks>
    public class QueuedSqliteDatabase : IDisposable
    {
        /// <summary>
        /// Holds queries to be executed.
        /// </summary>
        private Queue<QueuedSqliteCommand> command_queue = new Queue<QueuedSqliteCommand>();

        private SqliteConnection connection;
        private int version;
        private Thread queue_thread;
        private volatile bool dispose_requested = false;
        private string dbpath;
        private volatile bool connected;

        /// <summary>
        /// Thread currently executing the transaction.
        /// </summary>
        /// <remarks>
        /// Only queries from this thread are allowed (all threads are allowed
        /// if there is no active transaction. Queries from other threads will
        /// be queued until the transaction ends.
        /// </remarks>
        private volatile Thread current_transaction_thread = null;

        /// <summary>
        /// Threads will sleep on this signal if there's a transaction active.
        /// </summary>
        ///
        /// This works as a mutual exclusion mechanism. If there are no threads
        /// waiting to start a transaction, the transaction_signal will 
        /// immediately continue (note that the initial state is set to
        /// signalled). Otherwise, the thread will wait until signalled.
        private AutoResetEvent transaction_signal = new AutoResetEvent(true);
        
        /// <summary>
        /// Signal used to indicate there is new data in the <see cref="command_queue"/>.
        /// </summary>
        private AutoResetEvent queue_signal = new AutoResetEvent(false);
        
	public delegate void ExceptionThrownHandler (Exception e);
	public event ExceptionThrownHandler ExceptionThrown;

        public QueuedSqliteDatabase(string dbpath)
        {
            this.dbpath = dbpath;

            // Connect
            if(connection == null) {
                version = GetFileVersion(dbpath);
                if (version == 3) {
                    connection = new SqliteConnection("Version=3,URI=file:" + dbpath);
                } else if (version == 2) {
                    connection = new SqliteConnection("Version=2,encoding=UTF-8,URI=file:" + dbpath);
                } else {
                    throw new Exception("Unsupported SQLite database version");
                }
                connection.Open();
                connected = true;
            }
 
            queue_thread = new Thread(ProcessQueue);
            queue_thread.IsBackground = true;
            queue_thread.Start();
        }

        ~QueuedSqliteDatabase ()
        {
            Log.Debug ("Finalizer called on {0}. Should be Disposed", GetType ());
            Dispose (false);
        }
        
        public void Dispose()
        {
            Dispose (true);
            GC.SuppressFinalize (this);
        }

        bool already_disposed = false;
        protected virtual void Dispose (bool is_disposing)
        {
            if (already_disposed)
                return;
            if (is_disposing) { //Free managed resources
                dispose_requested = true;
                queue_signal.Set();
                queue_thread.Join();
            }
            //Free unmanaged resources

            already_disposed = true;
       }
        
        private void WaitForConnection()
        {
            while(!connected);
        }

        private void QueueCommand(QueuedSqliteCommand command)
        {
            // Make queries that happen outside of transactions sleep if there
            // is an active transaction. This uses the same thread queue as
            // threads willing to enter a transaction.
            bool release_thread = false;
            if (current_transaction_thread != Thread.CurrentThread) {
                transaction_signal.WaitOne();
                release_thread = true;
            }

            lock(command_queue) {
                command_queue.Enqueue(command);
            }

            if (release_thread) {
                transaction_signal.Set();
            }

            queue_signal.Set();
        }
        
        public SqliteDataReader Query(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.Reader;
            QueueCommand(command);
            return command.WaitForResult() as SqliteDataReader;
        }
        
        public SqliteDataReader Query(object command)
        {
            return Query(new DbCommand(command.ToString()));
        }

        public bool InTransaction {
            get { return current_transaction_thread != null; }
        }
        
        public void BeginTransaction()
        {
            if (current_transaction_thread == Thread.CurrentThread) {
                throw new Exception("Can't start a recursive transaction");
            }

            transaction_signal.WaitOne();
            current_transaction_thread = Thread.CurrentThread;
            ExecuteNonQuery("BEGIN TRANSACTION");
        }

        public void CommitTransaction()
        {
            if (current_transaction_thread != Thread.CurrentThread) {
                throw new Exception("Can't commit from outside a transaction");
            }

            ExecuteNonQuery("COMMIT TRANSACTION");
            current_transaction_thread = null;
            transaction_signal.Set(); 
        }

        public void RollbackTransaction()
        {
            if (current_transaction_thread != Thread.CurrentThread) {
                throw new Exception("Can't rollback from outside a transaction");
            }

            ExecuteNonQuery("ROLLBACK");
            current_transaction_thread = null;
            transaction_signal.Set(); 
        }

        public object QuerySingle(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.Scalar;
            QueueCommand(command);
            return command.WaitForResult();
        }
                
        public object QuerySingle(object command)
        {
            return QuerySingle(new DbCommand(command.ToString()));
        }
        
        public int Execute(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.Execute;
            QueueCommand(command);
            command.WaitForResult();
            return command.InsertID;
        }
        
        public int Execute(object command)
        {
            return Execute(new DbCommand(command.ToString()));
        }
        
        public void ExecuteNonQuery(DbCommand command)
        {
            WaitForConnection();
            command.Connection = connection;
            command.CommandType = Banshee.Database.CommandType.ExecuteNonQuery;
            QueueCommand(command);
        }
        
        public void ExecuteNonQuery(object command)
        {
            ExecuteNonQuery(new DbCommand(command.ToString()));
        }

        public bool TableExists(string table)
        {
            bool result;
            if (version == 2) {
                // Old SQLite doesn't support selecting from sqlite_master
                try {
                    Execute("SELECT * FROM "+table+" LIMIT 0");
                    result = true;
                } catch (Exception) {
                    result = false;
                }
            } else {
                result = Convert.ToInt32(QuerySingle(String.Format(@"
                    SELECT COUNT(*) 
                        FROM sqlite_master
                        WHERE Type='table' AND Name='{0}'", 
                        table))) > 0;
            }

            return result;
        }

        private void ProcessQueue()
        {         
	    try {
           
            // Keep handling queries
            while(!dispose_requested) {
                while(command_queue.Count > 0) {
                    QueuedSqliteCommand command;
                    lock(command_queue) {
                        command = command_queue.Dequeue();
                    }
		    //Log.Debug (command.CommandText);
                    command.Execute();
                }

                queue_signal.WaitOne();
            }

            // Finish
            connection.Close();
	    } catch (Exception e) {
		    if (ExceptionThrown != null)
			    ExceptionThrown (e);
		    else
			    throw;
	    }
        }

        public int GetFileVersion(string path) 
        {
            if (!File.Exists(path)) {
                return 3;
            }

            using (Stream stream = File.OpenRead (path)) {
                byte [] data = new byte [15];
                stream.Read (data, 0, data.Length);

                string magic = System.Text.Encoding.ASCII.GetString (data, 0, data.Length);

                switch (magic) {
                case "SQLite format 3":
                    return 3;
                case "** This file co":
                    return 2;
                default:
                    return -1;
                }
            }
        }
    }

    public enum CommandType {
        Reader,
        Scalar,
        Execute,
        ExecuteNonQuery
    }

    public class QueuedSqliteCommand
    {
        private CommandType command_type;
        private object result;
        private int insert_id;
        private Exception execution_exception;
        private bool finished = false;
        private Object finishedLock = new Object();

        private SqliteCommand command;

        private const int MAX_RETRIES = 4;
        private const int SLEEP_TIME = 1000; // 1 sec
        private int attempt = 0;
        
        public QueuedSqliteCommand(string query)
        {
            this.command = new SqliteCommand(query);
        }
        
        public QueuedSqliteCommand(SqliteConnection connection, string query, CommandType commandType) 
        {
            this.command_type = commandType;
            this.command = new SqliteCommand(query, connection);
        }
        
        public void Execute()
        {
            if(result != null) {
                throw new ApplicationException("Command has alread been executed");
            }
        
            try {
                switch(command_type) {
                    case Banshee.Database.CommandType.Reader:
                        result = command.ExecuteReader();
                        break;
                    case Banshee.Database.CommandType.Scalar:
                        result = command.ExecuteScalar();
                        break;
                    case Banshee.Database.CommandType.Execute:
                    default:
                        result = command.ExecuteNonQuery();
                        insert_id = command.LastInsertRowID();
                        break;
                }
            } catch (SqliteBusyException) {
                if (attempt > MAX_RETRIES) {
                    throw; // FIXME: show this to the user
                }

                attempt++;
                Thread.Sleep(SLEEP_TIME);
                Execute();
            } catch(Exception e) {
                execution_exception = e;
                if (command_type == Banshee.Database.CommandType.ExecuteNonQuery) {
                    throw execution_exception;
                }
            }
            command.Dispose();
            
            Monitor.Enter(finishedLock);
            finished = true;
            Monitor.Pulse(finishedLock);
            Monitor.Exit(finishedLock);
        }
        
        public object WaitForResult()
        {
            Monitor.Enter(finishedLock);
            while (!finished) {
                Monitor.Wait(finishedLock);
            };
            Monitor.Exit(finishedLock);
            
            if(execution_exception != null) {
                throw execution_exception;
            }
            
            return result;
        }
        
        public object Result {
            get { return result; }
            internal set { result = value; }
        }
        
        public int InsertID {
            get { return insert_id; }
        }
        
        public CommandType CommandType {
            get { return command_type; }
            set { command_type = value; }
        }

        public SqliteConnection Connection {
            set { command.Connection = value; }
        }

        protected SqliteParameterCollection Parameters {
            get { return command.Parameters; }
        }

        public string CommandText {
            get { return command.CommandText; }
        }
    }
    
    public class DbCommand : QueuedSqliteCommand
    {
        public DbCommand(string command) : base(command)
        {
        }
                
        public DbCommand(string command, params object [] parameters) : this(command)
        {
            for(int i = 0; i < parameters.Length;) {
                SqliteParameter param;
                
                if(parameters[i] is SqliteParameter) {
                    param = (SqliteParameter)parameters[i];
                    if(i < parameters.Length - 1 && !(parameters[i + 1] is SqliteParameter)) {
                        param.Value = parameters[i + 1];
                        i += 2;
                    } else {
                        i++;
                    }
                } else {
                    param = new SqliteParameter();
                    param.ParameterName = (string)parameters[i];
                    param.Value = parameters[i + 1];
                    i += 2;
                }
                
                Parameters.Add(param);
            }
        }
        
        public void AddParameter<T>(string name, T value)
        {
            AddParameter<T>(new DbParameter<T>(name), value);
        }
        
        public void AddParameter<T>(DbParameter<T> param, T value)
        {
            param.Value = value;
            Parameters.Add(param);
        }
    }
    
    public class DbParameter<T> : SqliteParameter
    {
        public DbParameter(string name) : base()
        {
            ParameterName = name;
        }
        
        public new T Value {
            get { return (T)base.Value; }
            set { base.Value = value; }
        }
    }
}
