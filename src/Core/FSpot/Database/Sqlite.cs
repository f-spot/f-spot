//
// Sqlite.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

using MdsSqliteCommand = Microsoft.Data.Sqlite.SqliteCommand;
using MdsSqliteConnection = Microsoft.Data.Sqlite.SqliteConnection;
using MdsSqliteConnectionStringBuilder = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using MdsSqliteDataReader = Microsoft.Data.Sqlite.SqliteDataReader;

namespace FSpot.Database
{
	public interface IDataReader : IDisposable
	{
		bool Read ();
		object this[int i] { get; }
		object this[string columnName] { get; }
		T Get<T> (int i);
		T Get<T> (string columnName);
		object Get (int i, Type asType);
		int FieldCount { get; }
		string[] FieldNames { get; }
	}

	sealed class SqliteBackend : IDisposable
	{
		MdsSqliteConnection sqlite;

		public SqliteBackend (string dbPath)
		{
			sqlite = new MdsSqliteConnection (new MdsSqliteConnectionStringBuilder {
				DataSource = dbPath,
				// Pooled connections keep the file locked after Dispose (breaks Windows File.Delete).
				Pooling = false,
				// Match the old P/Invoke layer: no busy timeout.
				DefaultTimeout = 0
			}.ToString ());
			try {
				sqlite.Open ();
			} catch {
				sqlite.Dispose ();
				sqlite = null;
				throw;
			}
		}

		public IDataReader Query (string sql, object[] args = null)
		{
			var cmd = sqlite.CreateCommand ();
			Bind (cmd, sql, args);
			try {
				return new SqliteResultReader (cmd, cmd.ExecuteReader ());
			} catch {
				cmd.Dispose ();
				throw;
			}
		}

		public object QueryScalar (string sql, object[] args = null)
		{
			using (var cmd = sqlite.CreateCommand ()) {
				Bind (cmd, sql, args);
				return SqliteUtils.NormalizeDbValue (cmd.ExecuteScalar ());
			}
		}

		public long Execute (string sql, object[] args = null)
		{
			using (var cmd = sqlite.CreateCommand ()) {
				Bind (cmd, sql, args);
				cmd.ExecuteNonQuery ();
				cmd.Parameters.Clear ();
				cmd.CommandText = "SELECT last_insert_rowid()";
				return Convert.ToInt64 (cmd.ExecuteScalar ());
			}
		}

		static void Bind (MdsSqliteCommand cmd, string sql, object[] args)
		{
			cmd.CommandText = sql;
			if (args == null)
				return;
			for (int i = 0; i < args.Length; i++) {
				cmd.Parameters.AddWithValue ("@p" + i, ToBindValue (args[i]));
			}
		}

		static object ToBindValue (object value)
		{
			if (value == null)
				return DBNull.Value;
			if (value is DateTime)
				return DateTimeUtil.FromDateTime ((DateTime)value);
			if (value is bool)
				return ((bool)value) ? 1 : 0;
			return value;
		}

		public void Dispose ()
		{
			if (sqlite == null)
				return;
			sqlite.Dispose ();
			sqlite = null;
		}
	}

	sealed class SqliteResultReader : IDataReader
	{
		readonly MdsSqliteCommand command;
		readonly MdsSqliteDataReader reader;
		string[] field_names;
		bool disposed;

		internal SqliteResultReader (MdsSqliteCommand command, MdsSqliteDataReader reader)
		{
			this.command = command;
			this.reader = reader;
		}

		public void Dispose ()
		{
			if (disposed)
				return;
			disposed = true;
			reader.Dispose ();
			command.Dispose ();
		}

		public int FieldCount {
			get { return reader.FieldCount; }
		}

		public string[] FieldNames {
			get {
				if (field_names == null) {
					field_names = new string[FieldCount];
					for (int i = 0; i < field_names.Length; i++) {
						field_names[i] = reader.GetName (i);
					}
				}
				return field_names;
			}
		}

		public bool Read ()
		{
			return reader.Read ();
		}

		public object this[int i] {
			get { return SqliteUtils.NormalizeDbValue (reader.GetValue (i)); }
		}

		public object this[string columnName] {
			get { return this[GetColumnIndex (columnName)]; }
		}

		public T Get<T> (int i)
		{
			return (T)Get (i, typeof (T));
		}

		public object Get (int i, Type asType)
		{
			return SqliteUtils.GetAs (this[i], asType);
		}

		public T Get<T> (string columnName)
		{
			return Get<T> (GetColumnIndex (columnName));
		}

		int GetColumnIndex (string columnName)
		{
			int index = Array.IndexOf (FieldNames, columnName);
			if (index < 0)
				throw new ArgumentException ("columnName");
			return index;
		}
	}
}
