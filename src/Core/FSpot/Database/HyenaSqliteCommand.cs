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

using Hyena;

namespace FSpot.Database
{
	public class HyenaSqliteCommand
	{
		object result = null;
		Exception execution_exception = null;
		bool finished = false;

		ManualResetEvent finished_event = new ManualResetEvent (true);
		string command;
		string parameterized_sql = null;
		int parameter_count = 0;
		object[] current_values;

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

		internal void Execute (HyenaSqliteConnection hconnection, SqliteBackend connection)
		{
			if (finished) {
				throw new Exception ("Command is already set to finished; result needs to be claimed before command can be rerun");
			}

			execution_exception = null;
			result = null;

			string command_text = null;
			try {
				EnsureParameterized ();
				command_text = parameterized_sql;
				int ticks = System.Environment.TickCount;

				switch (CommandType) {
				case HyenaCommandType.Reader:
					using (var reader = connection.Query (command_text, current_values)) {
						result = new ArrayDataReader (reader, command_text);
					}
					break;

				case HyenaCommandType.Scalar:
					result = connection.QueryScalar (command_text, current_values);
					break;

				case HyenaCommandType.Execute:
				default:
					result = connection.Execute (command_text, current_values);
					break;
				}

				int execution_ms = System.Environment.TickCount - ticks;
				if (log_all) {
					Logger.Log.Debug ("Executed in {0}ms {1}", execution_ms, command_text);
				} else if (ApplicationContext.Debugging && execution_ms > 500) {
					Logger.Log.Warning ("Executed in {0}ms {1}", execution_ms, command_text);
				}
			} catch (Exception e) {
				Logger.Log.Warning (e, "Exception executing command: {0}", command_text ?? command);
				execution_exception = e;
			}

			finished_event.Reset ();
			finished = true;
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
			EnsureParameterized ();

			if (parameter_count == 1 && param_values == null) {
				current_values = new object[] { null };
				return this;
			}

			if (param_values == null || param_values.Length != parameter_count) {
				throw new ArgumentException (string.Format (
					"Command {2} has {0} parameters, but {1} values given.", parameter_count, param_values == null ? 0 : param_values.Length, command
				));
			}

			current_values = param_values;
			return this;
		}

		void EnsureParameterized ()
		{
			if (parameterized_sql != null)
				return;

			var sb = new StringBuilder ();
			foreach (char c in command) {
				if (c == '?') {
					sb.Append ("@p");
					sb.Append (parameter_count++);
				} else {
					sb.Append (c);
				}
			}
			parameterized_sql = sb.ToString ();
		}

		static bool log_all = false;
		public static bool LogAll {
			get { return log_all; }
			set { log_all = value; }
		}
	}
}
