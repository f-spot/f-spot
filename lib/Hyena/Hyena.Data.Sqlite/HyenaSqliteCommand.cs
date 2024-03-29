//
// HyenaSqliteCommand.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
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
		object result = null;
		Exception execution_exception = null;
		bool finished = false;

		ManualResetEvent finished_event = new ManualResetEvent (true);
		string command;
		string command_format = null;
		string command_formatted = null;
		int parameter_count = 0;
		object[] current_values;
		int ticks;

		public string Text {
			get { return command; }
		}

		public bool ReaderDisposes { get; set; }

		internal HyenaCommandType CommandType;

		public HyenaSqliteCommand (string command)
		{
			this.command = command;
		}

		public HyenaSqliteCommand (string command, params object[] param_values)
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

		internal HyenaSqliteCommand ApplyValues (params object[] param_values)
		{
			if (command_format == null) {
				CreateParameters ();
			}

			// Special case for if a single null values is the paramter array
			if (parameter_count == 1 && param_values == null) {
				current_values = new object[] { "NULL" };
				command_formatted = null;
				return this;
			}

			if (param_values.Length != parameter_count) {
				throw new ArgumentException (string.Format (
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
				return string.Format ("'{0}'", (o as string).Replace ("'", "''"));
			} else if (o is DateTime) {
				return DateTimeUtil.FromDateTime ((DateTime)o);
			} else if (o is bool) {
				return ((bool)o) ? "1" : "0";
			} else if (o == null) {
				return "NULL";
			} else if (o is byte[]) {
				string hex = BitConverter.ToString (o as byte[]).Replace ("-", "");
				return string.Format ("X'{0}'", hex);
			} else if (o is Array) {
				var sb = new StringBuilder ();
				bool first = true;
				foreach (object i in (o as Array)) {
					if (!first)
						sb.Append (',');
					else
						first = false;

					sb.Append (SqlifyObject (i));
				}
				return sb.ToString ();
			} else {
				return o;
			}
		}

		string CurrentSqlText {
			get {
				if (command_format == null) {
					return command;
				}

				if (command_formatted == null) {
					command_formatted = string.Format (System.Globalization.CultureInfo.InvariantCulture, command_format, current_values);
				}

				return command_formatted;
			}
		}

		void CreateParameters ()
		{
			var sb = new StringBuilder ();
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

		static bool log_all = false;
		public static bool LogAll {
			get { return log_all; }
			set { log_all = value; }
		}

		public delegate void CommandExecutedHandler (object o, CommandExecutedArgs args);
		public static event CommandExecutedHandler CommandExecuted;

		static bool raise_command_executed = false;
		public static bool RaiseCommandExecuted {
			get { return raise_command_executed; }
			set { raise_command_executed = value; }
		}

		static int raise_command_executed_threshold_ms = 400;
		public static int RaiseCommandExecutedThresholdMs {
			get { return raise_command_executed_threshold_ms; }
			set { raise_command_executed_threshold_ms = value; }
		}

		#endregion
	}
}
