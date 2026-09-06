//
// DatabaseConnection.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace FSpot.Database
{
	public enum DatabaseCommandType
	{
		Reader,
		Scalar,
		Execute,
	}

	public class DatabaseConnection : IDisposable
	{
		SqliteBackend connection;
		string dbpath;

		protected string DbPath { get { return dbpath; } }

		// Parallel queues: a command and its ? placeholder args (null when none).
		Queue<DatabaseCommand> command_queue = new Queue<DatabaseCommand> ();
		Queue<object[]> args_queue = new Queue<object[]> ();

		Thread queue_thread;
		volatile bool dispose_requested = false;
		volatile int results_ready = 0;
		AutoResetEvent queue_signal = new AutoResetEvent (false);
		internal ManualResetEvent ResultReadySignal = new ManualResetEvent (false);

		volatile Thread transaction_thread = null;
		ManualResetEvent transaction_signal = new ManualResetEvent (true);

		public bool InTransaction {
			get { return transaction_thread == Thread.CurrentThread; }
		}

		public string ServerVersion { get { return Query<string> ("SELECT sqlite_version ()"); } }

		public DatabaseConnection (string dbpath)
		{
			this.dbpath = dbpath;
			queue_thread = new Thread (ProcessQueue);
			queue_thread.Name = string.Format ("DatabaseConnection ({0})", dbpath);
			queue_thread.IsBackground = true;
			queue_thread.Start ();
		}

		#region Public Query Methods

		// SELECT multiple column queries
		public IDataReader Query (DatabaseCommand command)
		{
			command.CommandType = DatabaseCommandType.Reader;
			QueueCommand (command);
			return (IDataReader)command.WaitForResult (this);
		}

		public IDataReader Query (DatabaseCommand command, params object[] param_values)
		{
			command.CommandType = DatabaseCommandType.Reader;
			QueueCommand (command, param_values);
			return (IDataReader)command.WaitForResult (this);
		}

		public IDataReader Query (string command_str, params object[] param_values)
		{
			return Query (new DatabaseCommand (command_str, param_values) { ReaderDisposes = true });
		}

		public IDataReader Query (object command)
		{
			return Query (new DatabaseCommand (command.ToString ()) { ReaderDisposes = true });
		}

		// SELECT single column, multiple rows queries
		public IEnumerable<T> QueryEnumerable<T> (DatabaseCommand command)
		{
			Type type = typeof (T);
			using (IDataReader reader = Query (command)) {
				while (reader.Read ()) {
					yield return (T)reader.Get (0, type);
				}
			}
		}

		public IEnumerable<T> QueryEnumerable<T> (DatabaseCommand command, params object[] param_values)
		{
			Type type = typeof (T);
			using (IDataReader reader = Query (command, param_values)) {
				while (reader.Read ()) {
					yield return (T)reader.Get (0, type);
				}
			}
		}

		public IEnumerable<T> QueryEnumerable<T> (string command_str, params object[] param_values)
		{
			return QueryEnumerable<T> (new DatabaseCommand (command_str, param_values) { ReaderDisposes = true });
		}

		public IEnumerable<T> QueryEnumerable<T> (object command)
		{
			return QueryEnumerable<T> (new DatabaseCommand (command.ToString ()) { ReaderDisposes = true });
		}

		// SELECT single column, single row queries
		public T Query<T> (DatabaseCommand command)
		{
			command.CommandType = DatabaseCommandType.Scalar;
			QueueCommand (command);
			object result = command.WaitForResult (this);
			return (T)SqliteUtils.FromDbFormat (typeof (T), result);
		}

		public T Query<T> (DatabaseCommand command, params object[] param_values)
		{
			command.CommandType = DatabaseCommandType.Scalar;
			QueueCommand (command, param_values);
			object result = command.WaitForResult (this);
			return (T)SqliteUtils.FromDbFormat (typeof (T), result);
		}

		public T Query<T> (string command_str, params object[] param_values)
		{
			return Query<T> (new DatabaseCommand (command_str, param_values) { ReaderDisposes = true });
		}

		public T Query<T> (object command)
		{
			return Query<T> (new DatabaseCommand (command.ToString ()) { ReaderDisposes = true });
		}

		// INSERT, UPDATE, DELETE queries
		public long Execute (DatabaseCommand command)
		{
			command.CommandType = DatabaseCommandType.Execute;
			QueueCommand (command);
			return (long)command.WaitForResult (this);
		}

		public long Execute (DatabaseCommand command, params object[] param_values)
		{
			command.CommandType = DatabaseCommandType.Execute;
			QueueCommand (command, param_values);
			return (long)command.WaitForResult (this);
		}

		public long Execute (string command_str, params object[] param_values)
		{
			return Execute (new DatabaseCommand (command_str, param_values) { ReaderDisposes = true });
		}

		public long Execute (object command)
		{
			return Execute (new DatabaseCommand (command.ToString ()) { ReaderDisposes = true });
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

		bool Exists (string type, string name)
		{
			return Exists (type, name, "sqlite_master") || Exists (type, name, "sqlite_temp_master");
		}

		bool Exists (string type, string name, string master)
		{
			return Query<int> (string.Format (
				"SELECT COUNT(*) FROM {0} WHERE Type='{1}' AND Name='{2}'",
				master, type, name)
			) > 0;
		}

		delegate void SchemaHandler (string column);

		void SchemaClosure (string table_name, SchemaHandler code)
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

		static readonly char[] ws_chars = new char[] { ' ', '\t', '\n', '\r' };
		public IDictionary<string, string> GetSchema (string table_name)
		{
			var schema = new Dictionary<string, string> ();
			SchemaClosure (table_name, delegate (string column) {
				schema.Add (column.ToLower (), null);
			});
			return schema;
		}

		#endregion

		#region Private Queue Methods

		void QueueCommand (DatabaseCommand command)
		{
			QueueCommand (command, null);
		}

		void QueueCommand (DatabaseCommand command, object[] args)
		{
			while (true) {
				lock (command_queue) {
					if (dispose_requested) {
						// No point in queueing the command if we're already disposing.
						// This helps avoid using the probably-disposed queue_signal below too
						return;
					} else if (transaction_thread == null || Thread.CurrentThread == transaction_thread) {
						command_queue.Enqueue (command);
						args_queue.Enqueue (args);
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

		void ProcessQueue ()
		{
			if (connection == null) {
				connection = new SqliteBackend (dbpath);
			}

			// Keep handling queries
			while (!dispose_requested) {
				while (command_queue.Count > 0) {
					DatabaseCommand command;
					object[] args;
					lock (command_queue) {
						command = command_queue.Dequeue ();
						args = args_queue.Dequeue ();
					}

					// Ensure the command is not altered while applying values or executing
					lock (command) {
						command.WaitIfNotFinished ();

						if (args != null) {
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

		public void Dispose ()
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
