//
// HyenaSqliteCommand.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;
using System.Text;
using System.Threading;

namespace Hyena.Data.Sqlite
{
    public class CommandExecutedArgs : EventArgs
    {
        public CommandExecutedArgs (string sql, int ms)
        {
            Sql = sql;
            Ms = ms;
        }

        public string Sql;
        public int Ms;
    }

    public class HyenaSqliteCommand
    {
        private object result = null;
        private Exception execution_exception = null;
        private bool finished = false;

        private ManualResetEvent finished_event = new ManualResetEvent (true);
        private string command;
        private string command_format = null;
        private string command_formatted = null;
        private int parameter_count = 0;
        private object [] current_values;
        private int ticks;

        public string Text {
            get { return command; }
        }

        public bool ReaderDisposes { get; set; }

        internal HyenaCommandType CommandType;

        public HyenaSqliteCommand (string command)
        {
            this.command = command;
        }

        public HyenaSqliteCommand (string command, params object [] param_values)
        {
            this.command = command;
            ApplyValues (param_values);
        }

        internal void Execute (HyenaSqliteConnection hconnection, Connection connection)
        {
            if (finished) {
                throw new Exception ("Command is already set to finished; result needs to be claimed before command can be rerun");
            }

            execution_exception = null;
            result = null;
            int execution_ms = 0;

            string command_text = null;
            try {
                command_text = CurrentSqlText;
                ticks = System.Environment.TickCount;

                switch (CommandType) {
                    case HyenaCommandType.Reader:
                        using (var reader = connection.Query (command_text)) {
                            result = new ArrayDataReader (reader, command_text);
                        }
                        break;

                    case HyenaCommandType.Scalar:
                        result = connection.Query<object> (command_text);
                        break;

                    case HyenaCommandType.Execute:
                    default:
                        connection.Execute (command_text);
                        result = connection.LastInsertRowId;
                        break;
                }

                execution_ms = System.Environment.TickCount - ticks;
                if (log_all) {
                    Log.DebugFormat ("Executed in {0}ms {1}", execution_ms, command_text);
                } else if (Log.Debugging && execution_ms > 500) {
                    Log.WarningFormat ("Executed in {0}ms {1}", execution_ms, command_text);
                }
            } catch (Exception e) {
                Log.DebugFormat ("Exception executing command: {0}", command_text ?? command);
                Log.Exception (e);
                execution_exception = e;
            }

            // capture the text
            string raise_text = null;
            if (raise_command_executed && execution_ms >= raise_command_executed_threshold_ms) {
                raise_text = Text;
            }

            finished_event.Reset ();
            finished = true;

            if (raise_command_executed && execution_ms >= raise_command_executed_threshold_ms) {
                var handler = CommandExecuted;
                if (handler != null) {

                    // Don't raise this on this thread; this thread is dedicated for use by the db connection
                    ThreadAssist.ProxyToMain (delegate {
                        handler (this, new CommandExecutedArgs (raise_text, execution_ms));
                    });
                }
            }
        }

        internal object WaitForResult (HyenaSqliteConnection conn)
        {
            while (!finished) {
                conn.ResultReadySignal.WaitOne ();
            }

            // Reference the results since they could be overwritten
            object ret = result;
            var exception = execution_exception;

            // Reset to false in case run again
            finished = false;

            conn.ClaimResult ();
            finished_event.Set ();

            if (exception != null) {
                throw exception;
            }

            return ret;
        }

        internal void WaitIfNotFinished ()
        {
            finished_event.WaitOne ();
        }

        internal HyenaSqliteCommand ApplyValues (params object [] param_values)
        {
            if (command_format == null) {
                CreateParameters ();
            }

            // Special case for if a single null values is the paramter array
            if (parameter_count == 1 && param_values == null) {
                current_values = new object [] { "NULL" };
                command_formatted = null;
                return this;
            }

            if (param_values.Length != parameter_count) {
                throw new ArgumentException (String.Format (
                    "Command {2} has {0} parameters, but {1} values given.", parameter_count, param_values.Length, command
                ));
            }

            // Transform values as necessary - not needed for numerical types
            for (int i = 0; i < parameter_count; i++) {
                param_values[i] = SqlifyObject (param_values[i]);
            }

            current_values = param_values;
            command_formatted = null;
            return this;
        }

        public static object SqlifyObject (object o)
        {
            if (o is string) {
                return String.Format ("'{0}'", (o as string).Replace ("'", "''"));
            } else if (o is DateTime) {
                return DateTimeUtil.FromDateTime ((DateTime) o);
            } else if (o is bool) {
                return ((bool)o) ? "1" : "0";
            } else if (o == null) {
                return "NULL";
            } else if (o is byte[]) {
                string hex = BitConverter.ToString (o as byte[]).Replace ("-", "");
                return String.Format ("X'{0}'", hex);
            } else if (o is Array) {
                StringBuilder sb = new StringBuilder ();
                bool first = true;
                foreach (object i in (o as Array)) {
                    if (!first)
                        sb.Append (",");
                    else
                        first = false;

                    sb.Append (SqlifyObject (i));
                }
                return sb.ToString ();
            } else {
                return o;
            }
        }

        private string CurrentSqlText {
            get {
                if (command_format == null) {
                    return command;
                }

                if (command_formatted == null) {
                    command_formatted = String.Format (System.Globalization.CultureInfo.InvariantCulture, command_format, current_values);
                }

                return command_formatted;
            }
        }

        private void CreateParameters ()
        {
            StringBuilder sb = new StringBuilder ();
            foreach (char c in command) {
                if (c == '?') {
                    sb.Append ('{');
                    sb.Append (parameter_count++);
                    sb.Append ('}');
                } else {
                    sb.Append (c);
                }
            }
            command_format = sb.ToString ();
        }

        #region Static Debugging Facilities

        private static bool log_all = false;
        public static bool LogAll {
            get { return log_all; }
            set { log_all = value; }
        }

        public delegate void CommandExecutedHandler (object o, CommandExecutedArgs args);
        public static event CommandExecutedHandler CommandExecuted;

        private static bool raise_command_executed = false;
        public static bool RaiseCommandExecuted {
            get { return raise_command_executed; }
            set { raise_command_executed = value; }
        }

        private static int raise_command_executed_threshold_ms = 400;
        public static int RaiseCommandExecutedThresholdMs {
            get { return raise_command_executed_threshold_ms; }
            set { raise_command_executed_threshold_ms = value; }
        }

        #endregion
    }
}
