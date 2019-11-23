//
// Sqlite.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
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
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Hyena;

namespace Hyena.Data.Sqlite
{
    public class Connection : IDisposable
    {
        IntPtr ptr;
        internal IntPtr Ptr { get { return ptr; } }

        internal List<Statement> Statements = new List<Statement> ();

        public string DbPath { get; private set; }

        public long LastInsertRowId {
            get { return Native.sqlite3_last_insert_rowid (Ptr); }
        }

        public Connection (string dbPath)
        {
            DbPath = dbPath;
            CheckError (Native.sqlite3_open (Native.GetUtf8Bytes (dbPath), out ptr));
            if (ptr == IntPtr.Zero)
                throw new Exception ("Unable to open connection");

            Native.sqlite3_extended_result_codes (ptr, 1);

            AddFunction<BinaryFunction> ();
            AddFunction<CollationKeyFunction> ();
            AddFunction<SearchKeyFunction> ();
            AddFunction<Md5Function> ();
        }

        public void Dispose ()
        {
            if (ptr != IntPtr.Zero) {
                lock (Statements) {
                    var stmts = Statements.ToArray ();

                    if (stmts.Length > 0)
                        Hyena.Log.DebugFormat ("Connection disposing of {0} remaining statements", stmts.Length);

                    foreach (var stmt in stmts) {
                        stmt.Dispose ();
                    }
                }

                CheckError (Native.sqlite3_close (ptr));
                ptr = IntPtr.Zero;
            }
        }

        ~Connection ()
        {
            Dispose ();
        }

        internal void CheckError (int errorCode)
        {
            CheckError (errorCode, "");
        }

        internal void CheckError (int errorCode, string sql)
        {
            if (errorCode == 0 || errorCode == 100 || errorCode == 101)
                return;

            string errmsg = Native.sqlite3_errmsg16 (Ptr).PtrToString ();
            if (sql != null) {
                errmsg = String.Format ("{0} (SQL: {1})", errmsg, sql);
            }

            throw new SqliteException (errorCode, errmsg);
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
            // TODO
            // * The application must insure that the 1st parameter to sqlite3_exec() is a valid and open database connection.
            // * The application must not close database connection specified by the 1st parameter to sqlite3_exec() while sqlite3_exec() is running.
            // * The application must not modify the SQL statement text passed into the 2nd parameter of sqlite3_exec() while sqlite3_exec() is running. 
            CheckError (Native.sqlite3_exec (Ptr, Native.GetUtf8Bytes (sql), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero), sql);
        }

        // We need to keep a managed ref to the function objects we create so
        // they won't get GC'd
        List<SqliteFunction> functions = new List<SqliteFunction> ();

        const int UTF16 = 4;
        public void AddFunction<T> () where T : SqliteFunction
        {
            var type = typeof (T);
            var pr = (SqliteFunctionAttribute)type.GetCustomAttributes (typeof (SqliteFunctionAttribute), false).First ();
            var f = (SqliteFunction) Activator.CreateInstance (typeof (T));

            f._InvokeFunc = (pr.FuncType == FunctionType.Scalar) ? new SqliteCallback(f.ScalarCallback) : null;
            f._StepFunc = (pr.FuncType == FunctionType.Aggregate) ? new SqliteCallback(f.StepCallback) : null;
            f._FinalFunc = (pr.FuncType == FunctionType.Aggregate) ? new SqliteFinalCallback(f.FinalCallback) : null;
            f._CompareFunc = (pr.FuncType == FunctionType.Collation) ? new SqliteCollation(f.CompareCallback) : null;

            if (pr.FuncType != FunctionType.Collation) {
                CheckError (Native.sqlite3_create_function16 (
                    ptr, pr.Name, pr.Arguments, UTF16, IntPtr.Zero,
                    f._InvokeFunc, f._StepFunc, f._FinalFunc
                ));
            } else {
                CheckError (Native.sqlite3_create_collation16 (
                    ptr, pr.Name, UTF16, IntPtr.Zero, f._CompareFunc
                ));
            }
            functions.Add (f);
        }

        public void RemoveFunction<T> () where T : SqliteFunction
        {
            var type = typeof (T);
            var pr = (SqliteFunctionAttribute)type.GetCustomAttributes (typeof (SqliteFunctionAttribute), false).First ();
            if (pr.FuncType != FunctionType.Collation) {
                CheckError (Native.sqlite3_create_function16 (
                    ptr, pr.Name, pr.Arguments, UTF16, IntPtr.Zero,
                    null, null, null
                ));
            } else {
                CheckError (Native.sqlite3_create_collation16 (
                    ptr, pr.Name, UTF16, IntPtr.Zero, null
                ));
            }

            var func = functions.FirstOrDefault (f => f is T);
            if (func != null) {
                functions.Remove (func);
            }
        }
    }

    public class SqliteException : Exception
    {
        public int ErrorCode { get; private set; }

        public SqliteException (int errorCode, string message) : base (String.Format ("Sqlite error {0}: {1}", errorCode, message))
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
        string [] FieldNames { get; }
    }

    public class Statement : IDisposable, IEnumerable<IDataReader>
    {
        IntPtr ptr;
        Connection connection;
        bool bound;
        QueryReader reader;
        bool disposed;

        internal bool Reading { get; set; }
        internal IntPtr Ptr { get { return ptr; } }
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

            IntPtr pzTail = IntPtr.Zero;
            CheckError (Native.sqlite3_prepare16_v2 (connection.Ptr, sql, -1, out ptr, out pzTail));
            
            lock (Connection.Statements) {
                Connection.Statements.Add (this);
            }

            if (pzTail != IntPtr.Zero && Marshal.ReadByte (pzTail) != 0) {
                Dispose ();
                throw new ArgumentException ("sql", String.Format ("This sqlite binding does not support multiple commands in one statement:\n  {0}", sql));
            }

            ParameterCount = Native.sqlite3_bind_parameter_count (ptr);
            reader = new QueryReader () { Statement = this };
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
            if (ptr != IntPtr.Zero) {
                // Don't check for error here, because if the most recent evaluation had an error finalize will return it too
                // See http://sqlite.org/c3ref/finalize.html
                Native.sqlite3_finalize (ptr);

                ptr = IntPtr.Zero;

                lock (Connection.Statements) {
                    Connection.Statements.Remove (this);
                }

                var h = Disposed;
                if (h != null) {
                    h (this, EventArgs.Empty);
                }
            }
        }

        ~Statement ()
        {
            Dispose ();
        }

        object [] null_val = new object [] { null };
        public Statement Bind (params object [] vals)
        {
            Reset ();

            if (vals == null && ParameterCount == 1)
                vals = null_val;

            if (vals == null || vals.Length != ParameterCount || ParameterCount == 0)
                throw new ArgumentException ("vals", String.Format ("Statement has {0} parameters", ParameterCount));

            for (int i = 1; i <= vals.Length; i++) {
                int code = 0;
                object o = SqliteUtils.ToDbFormat (vals[i - 1]);

                if (o == null)
                    code = Native.sqlite3_bind_null (Ptr, i);
                else if (o is double)
                    code = Native.sqlite3_bind_double (Ptr, i, (double)o);
                else if (o is float)
                    code = Native.sqlite3_bind_double (Ptr, i, (double)(float)o);
                else if (o is int)
                    code = Native.sqlite3_bind_int (Ptr, i, (int)o);
                else if (o is uint)
                    code = Native.sqlite3_bind_int (Ptr, i, (int)(uint)o);
                else if (o is long)
                    code = Native.sqlite3_bind_int64 (Ptr, i, (long)o);
                else if (o is ulong)
                    code = Native.sqlite3_bind_int64 (Ptr, i, (long)(ulong)o);
                else if (o is byte[]) {
                    byte [] bytes = o as byte[];
                    code = Native.sqlite3_bind_blob (Ptr, i, bytes, bytes.Length, (IntPtr)(-1));
                } else {
                    // C# strings are UTF-16, so 2 bytes per char
                    // -1 for the last arg is the TRANSIENT destructor type so that sqlite will make its own copy of the string
                    string str = (o as string) ?? o.ToString ();
                    code = Native.sqlite3_bind_text16 (Ptr, i, str, str.Length * 2, (IntPtr)(-1));
                }

                CheckError (code);
            }

            bound = true;
            return this;
        }

        internal void CheckError (int code)
        {
            connection.CheckError (code, CommandText);
        }

        private void Reset ()
        {
            CheckDisposed ();
            if (Reading) {
                throw new InvalidOperationException ("Can't reset statement while it's being read; make sure to Dispose any IDataReaders");
            }

            CheckError (Native.sqlite3_reset (ptr));
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
            using (reader) {
                reader.Read ();
            }
            return this;
        }

        public T Query<T> ()
        {
            Reset ();
            using (reader) {
                return reader.Read () ? reader.Get<T> (0) : (T) SqliteUtils.FromDbFormat <T> (null);
            }
        }

        public QueryReader Query ()
        {
            Reset ();
            return reader;
        }
    }

    public class QueryReader : IDataReader
    {
        Dictionary<string, int> columns;
        int column_count = -1;

        internal Statement Statement { get; set; }
        IntPtr Ptr { get { return Statement.Ptr; } }

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
                    column_count = Native.sqlite3_column_count (Ptr);
                }
                return column_count;
            }
        }

        string [] field_names;
        public string [] FieldNames {
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
            if (Statement.ParameterCount > 0 && !Statement.Bound)
                throw new InvalidOperationException ("Statement not bound");

            int code = Native.sqlite3_step (Ptr);
            if (code == ROW) {
                Statement.Reading = true;
                return true;
            } else {
                Statement.Reading = false;
                Statement.CheckError (code);
                return false;
            }
        }

        public object this[int i] {
            get {
                Statement.CheckReading ();
                int type = Native.sqlite3_column_type (Ptr, i);
                switch (type) {
                    case SQLITE_INTEGER:
                        return Native.sqlite3_column_int64 (Ptr, i);
                    case SQLITE_FLOAT:
                        return Native.sqlite3_column_double (Ptr, i);
                    case SQLITE3_TEXT:
                        return Native.sqlite3_column_text16 (Ptr, i).PtrToString ();
                    case SQLITE_BLOB:
                        int num_bytes = Native.sqlite3_column_bytes (Ptr, i);
                        if (num_bytes == 0)
                            return null;

                        byte [] bytes = new byte[num_bytes];
                        Marshal.Copy (Native.sqlite3_column_blob (Ptr, i), bytes, 0, num_bytes);
                        return bytes;
                    case SQLITE_NULL:
                        return null;
                    default:
                        throw new Exception (String.Format ("Column is of unknown type {0}", type));
                }
            }
        }

        public object this[string columnName] {
            get { return this[GetColumnIndex (columnName)]; }
        }

        public T Get<T> (int i)
        {
            return (T) Get (i, typeof(T));
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
            else if (type == typeof(int))
                o = (int)(long)o;
            else if (type == typeof(uint))
                o = (uint)(long)o;
            else if (type == typeof(ulong))
                o = (ulong)(long)o;
            else if (type == typeof(float))
                o = (float)(double)o;

            if (o != null && o.GetType () == type)
                return o;

            return SqliteUtils.FromDbFormat (type, o);
        }

        public T Get<T> (string columnName)
        {
            return Get<T> (GetColumnIndex (columnName));
        }

        private Dictionary<string, int> Columns {
            get {
                if (columns == null) {
                    columns = new Dictionary<string, int> ();
                    for (int i = 0; i < FieldCount; i++) {
                        columns[Native.sqlite3_column_name16 (Ptr, i).PtrToString ()] = i;
                    }
                }
                return columns;
            }
        }

        private int GetColumnIndex (string columnName)
        {
            Statement.CheckReading ();

            int col = 0;
            if (!Columns.TryGetValue (columnName, out col))
                throw new ArgumentException ("columnName");
            return col;
        }

        const int SQLITE_INTEGER = 1;
        const int SQLITE_FLOAT   = 2;
        const int SQLITE3_TEXT   = 3;
        const int SQLITE_BLOB    = 4;
        const int SQLITE_NULL    = 5;

        const int ROW = 100;
        const int DONE = 101;
    }

    internal static class Native
    {
        const string SQLITE_DLL = "sqlite3";

        // Connection functions
        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_open(byte [] utf8DbPath, out IntPtr db);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_close(IntPtr db);

        [DllImport(SQLITE_DLL)]
        internal static extern long sqlite3_last_insert_rowid (IntPtr db);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_busy_timeout(IntPtr db, int ms);

        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern IntPtr sqlite3_errmsg16(IntPtr db);

        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern int sqlite3_create_function16(IntPtr db, string strName, int nArgs, int eTextRep, IntPtr app, SqliteCallback func, SqliteCallback funcstep, SqliteFinalCallback funcfinal);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_aggregate_count(IntPtr context);

        [DllImport(SQLITE_DLL)]
        internal static extern IntPtr sqlite3_aggregate_context(IntPtr context, int nBytes);

        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern int sqlite3_create_collation16(IntPtr db, string strName, int eTextRep, IntPtr ctx, SqliteCollation fcompare);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_extended_result_codes (IntPtr db, int onoff);

        // Statement functions
        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern int sqlite3_prepare16_v2(IntPtr db, string pSql, int nBytes, out IntPtr stmt, out IntPtr ptrRemain);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_step(IntPtr stmt);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_column_count(IntPtr stmt);

        [DllImport(SQLITE_DLL)]
        internal static extern IntPtr sqlite3_column_name16(IntPtr stmt, int index);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_column_type(IntPtr stmt, int iCol);

        [DllImport(SQLITE_DLL)]
        internal static extern IntPtr sqlite3_column_blob(IntPtr stmt, int iCol);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_column_bytes(IntPtr stmt, int iCol);

        [DllImport(SQLITE_DLL)]
        internal static extern double sqlite3_column_double(IntPtr stmt, int iCol);

        [DllImport(SQLITE_DLL)]
        internal static extern long sqlite3_column_int64(IntPtr stmt, int iCol);

        [DllImport(SQLITE_DLL)]
        internal static extern IntPtr sqlite3_column_text16(IntPtr stmt, int iCol);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_finalize(IntPtr stmt);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_reset(IntPtr stmt);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_exec(IntPtr db, byte [] sql, IntPtr callback, IntPtr cbArg, IntPtr errPtr);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_parameter_index(IntPtr stmt, byte [] paramName);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_parameter_count(IntPtr stmt);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_blob(IntPtr stmt, int param, byte[] val, int nBytes, IntPtr destructorType);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_double(IntPtr stmt, int param, double val);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_int(IntPtr stmt, int param, int val);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_int64(IntPtr stmt, int param, long val);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_bind_null(IntPtr stmt, int param);

        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern int sqlite3_bind_text16 (IntPtr stmt, int param, string val, int numBytes, IntPtr destructorType);

        //DllImport(SQLITE_DLL)]
        //internal static extern int sqlite3_bind_zeroblob(IntPtr stmt, int, int n);

        // Context functions
        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_blob(IntPtr context, byte[] value, int nSize, IntPtr pvReserved);

        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_double(IntPtr context, double value);

        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_error(IntPtr context, byte[] strErr, int nLen);

        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_int(IntPtr context, int value);

        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_int64(IntPtr context, Int64 value);

        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_null(IntPtr context);

        [DllImport(SQLITE_DLL)]
        internal static extern void sqlite3_result_text(IntPtr context, byte[] value, int nLen, IntPtr pvReserved);

        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern void sqlite3_result_error16(IntPtr context, string strName, int nLen);

        [DllImport(SQLITE_DLL, CharSet = CharSet.Unicode)]
        internal static extern void sqlite3_result_text16(IntPtr context, string strName, int nLen, IntPtr pvReserved);

        // Value methods
        [DllImport(SQLITE_DLL)]
        internal static extern IntPtr sqlite3_value_blob(IntPtr p);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_value_bytes(IntPtr p);

        [DllImport(SQLITE_DLL)]
        internal static extern double sqlite3_value_double(IntPtr p);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_value_int(IntPtr p);

        [DllImport(SQLITE_DLL)]
        internal static extern Int64 sqlite3_value_int64(IntPtr p);

        [DllImport(SQLITE_DLL)]
        internal static extern int sqlite3_value_type(IntPtr p);

        [DllImport(SQLITE_DLL)]
        internal static extern IntPtr sqlite3_value_text16(IntPtr p);

        internal static string PtrToString (this IntPtr ptr)
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringUni (ptr);
        }

        internal static byte [] GetUtf8Bytes (string str)
        {
            return Encoding.UTF8.GetBytes (str + '\0');
        }
    }
}
