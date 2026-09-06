//
// Sqlite.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MdsSqliteCommand = Microsoft.Data.Sqlite.SqliteCommand;
using MdsSqliteConnection = Microsoft.Data.Sqlite.SqliteConnection;
using MdsSqliteConnectionStringBuilder = Microsoft.Data.Sqlite.SqliteConnectionStringBuilder;
using MdsSqliteDataReader = Microsoft.Data.Sqlite.SqliteDataReader;
using MdsSqliteException = Microsoft.Data.Sqlite.SqliteException;

namespace Hyena.Data.Sqlite
{
	public class Connection : IDisposable
	{
		MdsSqliteConnection sqlite;
		internal List<Statement> Statements = new List<Statement> ();

		// Keep managed refs so function objects are not GC'd while SQLite holds the callbacks.
		List<SqliteFunction> functions = new List<SqliteFunction> ();

		public string DbPath { get; private set; }

		internal MdsSqliteConnection Sqlite {
			get { return sqlite; }
		}

		public long LastInsertRowId {
			get {
				try {
					using (var cmd = sqlite.CreateCommand ()) {
						cmd.CommandText = "SELECT last_insert_rowid()";
						return Convert.ToInt64 (cmd.ExecuteScalar ());
					}
				} catch (MdsSqliteException ex) {
					throw Wrap (ex);
				}
			}
		}

		public Connection (string dbPath)
		{
			DbPath = dbPath;
			sqlite = new MdsSqliteConnection (new MdsSqliteConnectionStringBuilder {
				DataSource = dbPath,
				// Pooled connections keep the file locked after Dispose (breaks Windows File.Delete).
				Pooling = false,
				// Match the old P/Invoke layer: no busy timeout.
				DefaultTimeout = 0
			}.ToString ());
			try {
				sqlite.Open ();
			} catch (MdsSqliteException ex) {
				throw Wrap (ex);
			}

			AddFunction<BinaryFunction> ();
			AddFunction<CollationKeyFunction> ();
			AddFunction<SearchKeyFunction> ();
			AddFunction<Md5Function> ();
		}

		public void Dispose ()
		{
			if (sqlite == null)
				return;

			lock (Statements) {
				var stmts = Statements.ToArray ();

				if (stmts.Length > 0)
					Hyena.Log.DebugFormat ("Connection disposing of {0} remaining statements", stmts.Length);

				foreach (var stmt in stmts) {
					stmt.Dispose ();
				}
			}

			foreach (var f in functions) {
				f.Dispose ();
			}
			functions.Clear ();

			sqlite.Dispose ();
			sqlite = null;
		}

		~Connection ()
		{
			Dispose ();
		}

		public Statement CreateStatement (string sql)
		{
			return new Statement (this, sql);
		}

		public QueryReader Query (string sql)
		{
			return new Statement (this, sql) { ReaderDisposes = true }.Query ();
		}

		public T Query<T> (string sql)
		{
			using (var stmt = new Statement (this, sql)) {
				return stmt.Query<T> ();
			}
		}

		public void Execute (string sql)
		{
			try {
				using (var cmd = sqlite.CreateCommand ()) {
					cmd.CommandText = sql;
					cmd.ExecuteNonQuery ();
				}
			} catch (MdsSqliteException ex) {
				throw Wrap (ex, sql);
			}
		}

		public void AddFunction<T> () where T : SqliteFunction
		{
			var type = typeof (T);
			var pr = (SqliteFunctionAttribute)type.GetCustomAttributes (typeof (SqliteFunctionAttribute), false).First ();
			var f = (SqliteFunction)Activator.CreateInstance (typeof (T));

			try {
				if (pr.FuncType == FunctionType.Collation) {
					sqlite.CreateCollation (pr.Name, f.Compare);
				} else if (pr.FuncType == FunctionType.Scalar) {
					sqlite.CreateFunction (pr.Name, f, (SqliteFunction state, object[] args) => InvokeScalar (state, args));
				} else {
					throw new NotSupportedException (string.Format (
						"Microsoft.Data.Sqlite adapter does not register {0} functions named {1}", pr.FuncType, pr.Name));
				}
			} catch (MdsSqliteException ex) {
				throw Wrap (ex);
			}

			functions.Add (f);
		}

		public void RemoveFunction<T> () where T : SqliteFunction
		{
			var type = typeof (T);
			var pr = (SqliteFunctionAttribute)type.GetCustomAttributes (typeof (SqliteFunctionAttribute), false).First ();
			try {
				if (pr.FuncType == FunctionType.Collation) {
					sqlite.CreateCollation (pr.Name, null);
				} else {
					sqlite.CreateFunction<object> (pr.Name, (Func<object[], object>)null);
				}
			} catch (MdsSqliteException ex) {
				throw Wrap (ex);
			}

			var func = functions.FirstOrDefault (f => f is T);
			if (func != null) {
				functions.Remove (func);
				func.Dispose ();
			}
		}

		internal static SqliteException Wrap (MdsSqliteException ex, string sql = null)
		{
			string errmsg = ex.Message;
			if (sql != null) {
				errmsg = string.Format ("{0} (SQL: {1})", errmsg, sql);
			}
			return new SqliteException (ex.SqliteErrorCode, errmsg);
		}

		static object InvokeScalar (SqliteFunction function, object[] args)
		{
			var result = function.Invoke (NormalizeFunctionArgs (args));
			if (result is Exception ex)
				throw ex;
			return SqliteUtils.ToDbFormat (result);
		}

		internal static object[] NormalizeFunctionArgs (object[] args)
		{
			if (args == null)
				return Array.Empty<object> ();

			var normalized = new object[args.Length];
			for (int i = 0; i < args.Length; i++) {
				normalized[i] = NormalizeDbValue (args[i]);
			}
			return normalized;
		}

		internal static object NormalizeDbValue (object value)
		{
			if (value == null || value is DBNull)
				return null;
			if (value is int i)
				return (long)i;
			if (value is uint ui)
				return (long)ui;
			if (value is byte b)
				return (long)b;
			if (value is sbyte sb)
				return (long)sb;
			if (value is short s)
				return (long)s;
			if (value is ushort us)
				return (long)us;
			if (value is float f)
				return (double)f;
			return value;
		}
	}

	public class SqliteException : Exception
	{
		public int ErrorCode { get; private set; }

		public SqliteException (int errorCode, string message) : base (string.Format ("Sqlite error {0}: {1}", errorCode, message))
		{
			ErrorCode = errorCode;
		}
	}

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

	public class Statement : IDisposable, IEnumerable<IDataReader>
	{
		Connection connection;
		MdsSqliteCommand command;
		MdsSqliteDataReader mds_reader;
		QueryReader reader;
		bool bound;
		bool disposed;

		internal bool Reading { get; set; }
		internal bool Bound { get { return bound; } }
		internal Connection Connection { get { return connection; } }

		public bool IsDisposed { get { return disposed; } }

		public string CommandText { get; private set; }
		public int ParameterCount { get; private set; }
		public bool ReaderDisposes { get; internal set; }

		internal event EventHandler Disposed;

		internal Statement (Connection connection, string sql)
		{
			CommandText = sql;
			this.connection = connection;

			if (HasMultipleStatements (sql)) {
				throw new ArgumentException ("sql", string.Format ("This sqlite binding does not support multiple commands in one statement:\n  {0}", sql));
			}

			string rewritten = RewritePositionalParameters (sql, out var parameterCount);
			ParameterCount = parameterCount;

			command = connection.Sqlite.CreateCommand ();
			command.CommandText = rewritten;
			try {
				command.Prepare ();
			} catch (MdsSqliteException ex) {
				command.Dispose ();
				command = null;
				throw Connection.Wrap (ex, sql);
			}

			lock (Connection.Statements) {
				Connection.Statements.Add (this);
			}

			reader = new QueryReader () { Statement = this };
		}

		internal MdsSqliteDataReader CurrentReader {
			get { return mds_reader; }
		}

		internal void CheckReading ()
		{
			CheckDisposed ();
			if (!Reading) {
				throw new InvalidOperationException ("Statement is not readable");
			}
		}

		internal void CheckDisposed ()
		{
			if (disposed) {
				throw new InvalidOperationException ("Statement is disposed");
			}
		}

		public void Dispose ()
		{
			if (disposed)
				return;

			disposed = true;
			CloseReader ();
			if (command != null) {
				command.Dispose ();
				command = null;
			}

			lock (Connection.Statements) {
				Connection.Statements.Remove (this);
			}

			Disposed?.Invoke (this, EventArgs.Empty);
		}

		~Statement ()
		{
			Dispose ();
		}

		static readonly object[] null_val = new object[] { null };
		public Statement Bind (params object[] vals)
		{
			Reset ();

			if (vals == null && ParameterCount == 1)
				vals = null_val;

			if (vals == null || vals.Length != ParameterCount || ParameterCount == 0)
				throw new ArgumentException ("vals", string.Format ("Statement has {0} parameters", ParameterCount));

			command.Parameters.Clear ();
			for (int i = 0; i < vals.Length; i++) {
				var p = command.CreateParameter ();
				p.ParameterName = "$hp" + i;
				p.Value = ToBindValue (SqliteUtils.ToDbFormat (vals[i]));
				command.Parameters.Add (p);
			}

			bound = true;
			return this;
		}

		static object ToBindValue (object o)
		{
			if (o == null)
				return DBNull.Value;
			if (o is ulong ul)
				return unchecked((long)ul);
			if (o is uint ui)
				return unchecked((int)ui);
			if (o is float fl)
				return (double)fl;
			return o;
		}

		void Reset ()
		{
			CheckDisposed ();
			if (Reading) {
				throw new InvalidOperationException ("Can't reset statement while it's being read; make sure to Dispose any IDataReaders");
			}

			CloseReader ();
		}

		void CloseReader ()
		{
			if (mds_reader != null) {
				mds_reader.Dispose ();
				mds_reader = null;
			}
			Reading = false;
		}

		internal void EnsureReader ()
		{
			if (mds_reader != null)
				return;

			if (ParameterCount > 0 && !Bound)
				throw new InvalidOperationException ("Statement not bound");

			try {
				mds_reader = command.ExecuteReader ();
			} catch (MdsSqliteException ex) {
				throw Connection.Wrap (ex, CommandText);
			}
		}

		public IEnumerator<IDataReader> GetEnumerator ()
		{
			Reset ();
			while (reader.Read ()) {
				yield return reader;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		public Statement Execute ()
		{
			Reset ();
			if (ParameterCount > 0 && !Bound)
				throw new InvalidOperationException ("Statement not bound");

			try {
				command.ExecuteNonQuery ();
			} catch (MdsSqliteException ex) {
				throw Connection.Wrap (ex, CommandText);
			}
			return this;
		}

		public T Query<T> ()
		{
			Reset ();
			using (reader) {
				return reader.Read () ? reader.Get<T> (0) : (T)SqliteUtils.FromDbFormat<T> (null);
			}
		}

		public QueryReader Query ()
		{
			Reset ();
			return reader;
		}

		static bool HasMultipleStatements (string sql)
		{
			if (string.IsNullOrEmpty (sql))
				return false;

			var trimmed = sql.TrimEnd ();
			while (trimmed.EndsWith (";", StringComparison.Ordinal)) {
				trimmed = trimmed.Substring (0, trimmed.Length - 1).TrimEnd ();
			}
			return trimmed.IndexOf (';') >= 0;
		}

		static string RewritePositionalParameters (string sql, out int count)
		{
			var sb = new StringBuilder ();
			count = 0;
			foreach (char c in sql) {
				if (c == '?') {
					sb.Append ("$hp");
					sb.Append (count++);
				} else {
					sb.Append (c);
				}
			}
			return sb.ToString ();
		}
	}

	public class QueryReader : IDataReader
	{
		Dictionary<string, int> columns;
		int column_count = -1;

		internal Statement Statement { get; set; }

		public void Dispose ()
		{
			Statement.Reading = false;
			if (Statement.ReaderDisposes) {
				Statement.Dispose ();
			}
		}

		public int FieldCount {
			get {
				if (column_count == -1) {
					Statement.CheckDisposed ();
					Statement.EnsureReader ();
					column_count = Statement.CurrentReader.FieldCount;
				}
				return column_count;
			}
		}

		string[] field_names;
		public string[] FieldNames {
			get {
				if (field_names == null) {
					field_names = Columns.Keys.OrderBy (f => Columns[f]).ToArray ();
				}
				return field_names;
			}
		}

		public bool Read ()
		{
			Statement.CheckDisposed ();
			Statement.EnsureReader ();

			bool has_row;
			try {
				has_row = Statement.CurrentReader.Read ();
			} catch (MdsSqliteException ex) {
				Statement.Reading = false;
				throw Connection.Wrap (ex, Statement.CommandText);
			}

			Statement.Reading = has_row;
			return has_row;
		}

		public object this[int i] {
			get {
				Statement.CheckReading ();
				var value = Statement.CurrentReader.GetValue (i);
				return Connection.NormalizeDbValue (value);
			}
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
			return GetAs (this[i], asType);
		}

		internal static object GetAs (object o, Type type)
		{
			if (o != null && o.GetType () == type)
				return o;

			if (o == null)
				o = null;
			else if (type == typeof (int))
				o = (int)(long)o;
			else if (type == typeof (uint))
				o = (uint)(long)o;
			else if (type == typeof (ulong))
				o = (ulong)(long)o;
			else if (type == typeof (float))
				o = (float)(double)o;

			if (o != null && o.GetType () == type)
				return o;

			return SqliteUtils.FromDbFormat (type, o);
		}

		public T Get<T> (string columnName)
		{
			return Get<T> (GetColumnIndex (columnName));
		}

		Dictionary<string, int> Columns {
			get {
				if (columns == null) {
					columns = new Dictionary<string, int> ();
					for (int i = 0; i < FieldCount; i++) {
						columns[Statement.CurrentReader.GetName (i)] = i;
					}
				}
				return columns;
			}
		}

		int GetColumnIndex (string columnName)
		{
			Statement.CheckReading ();

			if (!Columns.TryGetValue (columnName, out var col))
				throw new ArgumentException ("columnName");
			return col;
		}
	}
}
