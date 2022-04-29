//
// SqliteModelProvider.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Hyena.Data.Sqlite
{
    public class SqliteModelProvider<T> where T : new ()
    {
        private readonly List<DatabaseColumn> columns = new List<DatabaseColumn> ();
        private readonly List<DatabaseColumn> select_columns = new List<DatabaseColumn> ();
        private readonly List<VirtualDatabaseColumn> virtual_columns = new List<VirtualDatabaseColumn> ();

        private DatabaseColumn key;
        private int key_select_column_index;
        private HyenaSqliteConnection connection;
        private bool check_table = true;

        private HyenaSqliteCommand create_command;
        private HyenaSqliteCommand insert_command;
        private HyenaSqliteCommand update_command;
        private HyenaSqliteCommand delete_command;
        private HyenaSqliteCommand select_command;
        private HyenaSqliteCommand select_range_command;
        private HyenaSqliteCommand select_single_command;

        private string table_name;
        private string primary_key;
        private string select;
        private string from;
        private string where;

        private const string HYENA_DATABASE_NAME = "hyena_database_master";

        public virtual string TableName { get { return table_name; } }

        protected virtual int ModelVersion { get { return 1; } }

        protected virtual int DatabaseVersion { get { return 1; } }

        protected virtual void MigrateTable (int old_version)
        {
        }

        protected virtual void MigrateDatabase (int old_version)
        {
        }

        protected virtual T MakeNewObject ()
        {
            return new T ();
        }

        protected virtual string HyenaTableName {
            get { return "HyenaModelVersions"; }
        }

        public HyenaSqliteConnection Connection {
            get { return connection; }
        }

        protected SqliteModelProvider (HyenaSqliteConnection connection)
        {
            this.connection = connection;
        }

        public SqliteModelProvider (HyenaSqliteConnection connection, string table_name) : this (connection, table_name, true)
        {
        }

        public SqliteModelProvider (HyenaSqliteConnection connection, string table_name, bool checkTable) : this (connection)
        {
            this.table_name = table_name;
            this.check_table = checkTable;
            Init ();
        }

        protected void Init ()
        {
            foreach (FieldInfo field in typeof(T).GetFields (BindingFlags.Instance | BindingFlags.Public)) {
                foreach (Attribute attribute in field.GetCustomAttributes (true)) {
                    AddColumn (field, attribute);
                }
            }
            foreach (FieldInfo field in typeof(T).GetFields (BindingFlags.Instance | BindingFlags.NonPublic)) {
                foreach (Attribute attribute in field.GetCustomAttributes (true)) {
                    AddColumn (field, attribute);
                }
            }
            foreach (PropertyInfo property in typeof(T).GetProperties (BindingFlags.Instance | BindingFlags.Public)) {
                foreach (Attribute attribute in property.GetCustomAttributes (true)) {
                    AddColumn (property, attribute);
                }
            }
            foreach (PropertyInfo property in typeof(T).GetProperties (BindingFlags.Instance | BindingFlags.NonPublic)) {
                foreach (Attribute attribute in property.GetCustomAttributes (true)) {
                    AddColumn (property, attribute);
                }
            }
            if (key == null) {
                throw new Exception (string.Format ("The {0} table does not have a primary key", TableName));
            }

            key_select_column_index = select_columns.IndexOf (key);

            if (check_table) {
                CheckVersion ();
                CheckTable ();
            }
        }

        protected virtual void CheckVersion ()
        {
            if (connection.TableExists (HyenaTableName)) {
                using (IDataReader reader = connection.Query (SelectVersionSql (TableName))) {
                    if (reader.Read ()) {
                        int table_version = reader.Get<int> (0);
                        if (table_version < ModelVersion) {
                            MigrateTable (table_version);
                            UpdateVersion (TableName, ModelVersion);
                        }
                    } else {
                        InsertVersion (TableName, ModelVersion);
                    }
                }
                int db_version = connection.Query<int> (SelectVersionSql (HYENA_DATABASE_NAME));
                if (db_version < DatabaseVersion) {
                    MigrateDatabase (db_version);
                    UpdateVersion (HYENA_DATABASE_NAME, DatabaseVersion);
                }
            }
            else {
                connection.Execute (string.Format (
                    @"CREATE TABLE {0} (
                        id INTEGER PRIMARY KEY,
                        name TEXT UNIQUE,
                        version INTEGER)",
                    HyenaTableName)
                );

                InsertVersion (HYENA_DATABASE_NAME, DatabaseVersion);
                InsertVersion (TableName, ModelVersion);
            }
        }

        private string SelectVersionSql (string name)
        {
            return string.Format (
                "SELECT version FROM {0} WHERE name='{1}'",
                HyenaTableName, name);
        }

        private void UpdateVersion (string name, int version)
        {
            connection.Execute (string.Format (
                "UPDATE {0} SET version={1} WHERE name='{2}'",
                HyenaTableName, version, name));
        }

        private void InsertVersion (string name, int version)
        {
            connection.Execute (string.Format (
                "INSERT INTO {0} (name, version) VALUES ('{1}', {2})",
                HyenaTableName, name, version));
        }

        protected void CheckTable ()
        {
            //Console.WriteLine ("In {0} checking for table {1}", this, TableName);
            IDictionary<string, string> schema = connection.GetSchema (TableName);
            if (schema.Count > 0) {
                foreach (DatabaseColumn column in columns) {
                    if (!schema.ContainsKey (column.Name.ToLower ())) {
                        AddColumnToTable (column.Schema);
                    }
                    if (column.Index != null && !connection.IndexExists (column.Index)) {
                        connection.Execute (string.Format (
                            "CREATE INDEX {0} ON {1}({2})",
                            column.Index, TableName, column.Name)
                        );
                    }
                }
            } else {
                CreateTable ();
            }
        }

        private void AddColumn (MemberInfo member, Attribute attribute)
        {
            DatabaseColumnAttribute column = attribute as DatabaseColumnAttribute;
            if (column != null) {
                DatabaseColumn c = member is FieldInfo
                    ? new DatabaseColumn ((FieldInfo)member, column)
                    : new DatabaseColumn ((PropertyInfo)member, column);

                AddColumn (c, column.Select);
            }
            VirtualDatabaseColumnAttribute virtual_column = attribute as VirtualDatabaseColumnAttribute;
            if (virtual_column != null) {
                if (member is FieldInfo) {
                    virtual_columns.Add (new VirtualDatabaseColumn ((FieldInfo) member, virtual_column));
                } else {
                    virtual_columns.Add (new VirtualDatabaseColumn ((PropertyInfo) member, virtual_column));
                }
            }
        }

        protected void AddColumn (DatabaseColumn c, bool select)
        {
            foreach (DatabaseColumn col in columns) {
                if (col.Name == c.Name) {
                    throw new Exception (string.Format (
                        "{0} has multiple columns named {1}",
                         TableName, c.Name)
                    );
                }
                if (col.Index != null && col.Index == c.Index) {
                    throw new Exception (string.Format (
                        "{0} has multiple indecies named {1}",
                        TableName, c.Name)
                    );
                }
            }

            columns.Add (c);

            if (select) {
                select_columns.Add (c);
            }

            if ((c.Constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                if (key != null) {
                    throw new Exception (string.Format (
                        "Multiple primary keys in the {0} table", TableName)
                    );
                }
                if (!c.ValueType.IsAssignableFrom (typeof (long))) {
                    throw new Exception (string.Format (
                        "Primary key {0} in the {1} class must be of type 'long'", c.Name, typeof(T))
                    );
                }
                key = c;
            }
        }

        protected virtual void CreateTable ()
        {
            connection.Execute (CreateCommand);
            foreach (DatabaseColumn column in columns) {
                if (column.Index != null) {
                    connection.Execute (string.Format (
                        "CREATE INDEX {0} ON {1}({2})",
                        column.Index, TableName, column.Name)
                    );
                }
            }
        }

        protected void CreateIndex (string name, string columns)
        {
            Connection.Execute (string.Format (
                "CREATE INDEX {0} ON {1} ({2})",
                name, TableName, columns
            ));
        }

        public virtual void Save (T target, bool force_insert)
        {
            try {
                if (Convert.ToInt64 (key.GetRawValue (target)) > 0 && !force_insert) {
                    Update (target);
                } else {
                    key.SetValue (target, Insert (target));
                }
            } catch (Exception e) {
                Hyena.Log.Exception (e);
                Hyena.Log.DebugFormat ("type of key value: {0}", key.GetRawValue (target).GetType ());
                throw;
            }

        }

        public virtual void Save (T target)
        {
            Save (target, false);
        }

        protected virtual object [] GetInsertParams (T target)
        {
            // TODO create an instance variable object array and reuse it? beware threading issues
            object [] values = new object [columns.Count - 1];
            int j = 0;
            for (int i = 0; i < columns.Count; i++) {
                if (columns[i] != key) {
                    values[j] = columns[i].GetValue (target);
                    j++;
                }
            }
            return values;
        }

        protected long Insert (T target)
        {
            return connection.Execute (InsertCommand, GetInsertParams (target));
        }

        protected object [] GetUpdateParams (T target)
        {
            // TODO create an instance variable object array and reuse it? beware threading issues
            object [] values = new object [columns.Count];
            int j = 0;
            for (int i = 0; i < columns.Count; i++) {
                if (columns[i] != key) {
                    values[j] = columns[i].GetValue (target);
                    j++;
                }
            }
            values[j] = key.GetValue (target);
            return values;
        }

        protected void Update (T target)
        {
            connection.Execute (UpdateCommand, GetUpdateParams (target));
        }

        public virtual T Load (IDataReader reader)
        {
            T item = MakeNewObject ();
            Load (reader, item);
            return item;
        }

        public void Load (IDataReader reader, T target)
        {
            int i = 0;

            AbstractDatabaseColumn bad_column = null;
            try {
                foreach (var column in select_columns) {
                    bad_column = column;
                    column.SetValue (target, reader.Get (i++, column.ValueType));
                }

                foreach (var column in virtual_columns) {
                    bad_column = column;
                    column.SetValue (target, reader.Get (i++, column.ValueType));
                }
            } catch (Exception e) {
                Log.Debug (
					string.Format ("Caught exception trying to load database column {0}", bad_column == null ? "[unknown]" : bad_column.Name),
                    e.ToString ()
                );
            }
        }

        public IEnumerable<T> FetchAll ()
        {
            using (IDataReader reader = connection.Query (SelectCommand)) {
                while (reader.Read ()) {
                    yield return Load (reader);
                }
            }
        }

        public T FetchFirstMatching (string condition, params object [] vals)
        {
            foreach (T item in FetchAllMatching (string.Format ("{0} LIMIT 1", condition), vals)) {
                return item;
            }
            return default;
        }

        public IEnumerable<T> FetchAllMatching (string condition, params object [] vals)
        {
            HyenaSqliteCommand fetch_matching_command = CreateFetchCommand (condition);
            using (IDataReader reader = connection.Query (fetch_matching_command, vals)) {
                while (reader.Read ()) {
                    yield return Load (reader);
                }
            }
        }

        public HyenaSqliteCommand CreateFetchCommand (string condition)
        {
            return new HyenaSqliteCommand (string.Format ("{0} AND {1}", SelectCommand.Text, condition));
        }

        public IEnumerable<T> FetchRange (int offset, int limit)
        {
            using (IDataReader reader = connection.Query (SelectRangeCommand, offset, limit)) {
                while (reader.Read ()) {
                    yield return Load (reader);
                }
            }
        }

        public T FetchSingle (int id)
        {
            return FetchSingle ((long) id);
        }

        public virtual T FetchSingle (long id)
        {
            using (IDataReader reader = connection.Query (SelectSingleCommand, id)) {
                if (reader.Read ()) {
                    return Load (reader);
                }
            }
            return default;
        }

        protected long PrimaryKeyFor (T item)
        {
            return Convert.ToInt64 (key.GetValue (item));
        }

        protected long PrimaryKeyFor (IDataReader reader)
        {
            return Convert.ToInt64 (reader[key_select_column_index]);
        }

        public virtual void Delete (long id)
        {
            if (id > 0)
                connection.Execute (DeleteCommand, id);
        }

        public void Delete (T item)
        {
            Delete (PrimaryKeyFor (item));
        }

        public void Delete (string condition, params object [] vals)
        {
            connection.Execute (string.Format ("DELETE FROM {0} WHERE {1}", TableName, condition), vals);
        }

        public virtual void Delete (IEnumerable<T> items)
        {
            List<long> ids = new List<long> ();
            long id;
            foreach (T item in items) {
                id = PrimaryKeyFor (item);
                if (id > 0)
                    ids.Add (id);
            }

            if (ids.Count > 0)
                connection.Execute (DeleteCommand, ids.ToArray ());
        }

        public bool Refresh (T item)
        {
            if (key == null || item == null)
                return false;

            long id = (long) key.GetValue (item);
            if (id < 1)
                return false;

            using (IDataReader reader = connection.Query (SelectSingleCommand, id)) {
                if (reader.Read ()) {
                    Load (reader, item);
                    return true;
                }
            }
            return false;
        }

        public void Copy (T original, T copy)
        {
            foreach (DatabaseColumn column in select_columns) {
                if (column != key) {
                    column.SetValue (copy, column.GetRawValue (original));
                }
            }
            foreach (var column in virtual_columns) {
                column.SetValue (copy, column.GetRawValue (original));
            }
        }

        protected virtual HyenaSqliteCommand CreateCommand {
            get {
                if (create_command == null) {
                    StringBuilder builder = new StringBuilder ();
                    builder.Append ("CREATE TABLE ");
                    builder.Append (TableName);
                    builder.Append ('(');
                    bool first = true;
                    foreach (DatabaseColumn column in columns) {
                        if (first) {
                            first = false;
                        } else {
                            builder.Append (',');
                        }
                        builder.Append (column.Schema);
                    }
                    builder.Append (')');
                    create_command = new HyenaSqliteCommand (builder.ToString ());
                }
                return create_command;
            }
        }

        protected virtual HyenaSqliteCommand InsertCommand {
            get {
                // FIXME can this string building be done more nicely?
                if (insert_command == null) {
                    StringBuilder cols = new StringBuilder ();
                    StringBuilder vals = new StringBuilder ();
                    bool first = true;
                    foreach (DatabaseColumn column in columns) {
                        if (column != key) {
                            if (first) {
                                first = false;
                            } else {
                                cols.Append (',');
                                vals.Append (',');
                            }
                            cols.Append (column.Name);
                            vals.Append ('?');
                        }
                    }

                    insert_command = new HyenaSqliteCommand (string.Format (
                        "INSERT INTO {0} ({1}) VALUES ({2})",
                        TableName, cols.ToString (), vals.ToString ())
                    );
                }
                return insert_command;
            }
        }

        protected virtual HyenaSqliteCommand UpdateCommand {
            get {
                if (update_command == null) {
                    StringBuilder builder = new StringBuilder ();
                    builder.Append ("UPDATE ");
                    builder.Append (TableName);
                    builder.Append (" SET ");
                    bool first = true;
                    foreach (DatabaseColumn column in columns) {
                        if (column != key) {
                            if (first) {
                                first = false;
                            } else {
                                builder.Append (',');
                            }
                            builder.Append (column.Name);
                            builder.Append (" = ?");
                        }
                    }
                    builder.Append (" WHERE ");
                    builder.Append (key.Name);
                    builder.Append (" = ?");
                    update_command = new HyenaSqliteCommand (builder.ToString ());
                }
                return update_command;
            }
        }

        protected virtual HyenaSqliteCommand SelectCommand {
            get {
                if (select_command == null) {
                    select_command = new HyenaSqliteCommand (
						string.Format (
                            "SELECT {0} FROM {1} WHERE {2}",
                            Select, From, string.IsNullOrEmpty (Where) ? "1=1" : Where
                        )
                    );
                }
                return select_command;
            }
        }

        protected virtual HyenaSqliteCommand SelectRangeCommand {
            get {
                if (select_range_command == null) {
                    select_range_command = new HyenaSqliteCommand (
						string.Format (
                            "SELECT {0} FROM {1}{2}{3} LIMIT ?, ?",
                            Select, From,
                            (string.IsNullOrEmpty (Where) ? string.Empty : " WHERE "),
                            Where
                        )
                    );
                }
                return select_range_command;
            }
        }

        protected virtual HyenaSqliteCommand SelectSingleCommand {
            get {
                if (select_single_command == null) {
                    select_single_command = new HyenaSqliteCommand (
						string.Format (
                            "SELECT {0} FROM {1} WHERE {2}{3}{4} = ?",
                            Select, From, Where,
                            (string.IsNullOrEmpty (Where) ? string.Empty : " AND "),
                            PrimaryKey
                        )
                    );
                }
                return select_single_command;
            }
        }

        protected virtual HyenaSqliteCommand DeleteCommand {
            get {
                if (delete_command == null) {
                    delete_command = new HyenaSqliteCommand (string.Format (
                        "DELETE FROM {0} WHERE {1} IN (?)", TableName, PrimaryKey
                    ));
                }
                return delete_command;
            }
        }

        public virtual string Select {
            get {
                if (select == null) {
                    BuildQuerySql ();
                }
                return select;
            }
        }

        public virtual string From {
            get {
                if (from == null) {
                    BuildQuerySql ();
                }
                return from;
            }
        }

        public virtual string Where {
            get {
                if (where == null) {
                    BuildQuerySql ();
                }
                return where;
            }
        }

        public string PrimaryKey {
            get {
                if (primary_key == null) {
                    primary_key = string.Format ("{0}.{1}", TableName, key.Name);
                }
                return primary_key;
            }
            protected set { primary_key = value; }
        }

        private void BuildQuerySql ()
        {
            StringBuilder select_builder = new StringBuilder ();
            bool first = true;
            foreach (DatabaseColumn column in select_columns) {
                if (first) {
                    first = false;
                } else {
                    select_builder.Append (',');
                }
                select_builder.Append (TableName);
                select_builder.Append ('.');
                select_builder.Append (column.Name);
            }

            StringBuilder where_builder = new StringBuilder ();
            Dictionary<string, string> tables = new Dictionary<string,string> (virtual_columns.Count + 1);
            bool first_virtual = true;
            foreach (VirtualDatabaseColumn column in virtual_columns) {
                if (first) {
                    first = false;
                } else {
                    select_builder.Append (',');
                }

                select_builder.Append (column.TargetTable);
                select_builder.Append ('.');
                select_builder.Append (column.Name);

                bool table_not_joined = !tables.ContainsKey (column.TargetTable);
                if (first_virtual) {
                    first_virtual = false;
                } else if (table_not_joined) {
                    where_builder.Append (" AND ");
                }

                if (table_not_joined) {
                    where_builder.Append (column.TargetTable);
                    where_builder.Append ('.');
                    where_builder.Append (column.ForeignKey);
                    where_builder.Append (" = ");
                    where_builder.Append (TableName);
                    where_builder.Append ('.');
                    where_builder.Append (column.LocalKey);

                    tables.Add (column.TargetTable, null);
                }
            }

            StringBuilder from_builder = new StringBuilder ();
            from_builder.Append (TableName);
            foreach (KeyValuePair<string, string> pair in tables) {
                from_builder.Append (',');
                from_builder.Append (pair.Key);
            }

            select = select_builder.ToString ();
            from = from_builder.ToString ();
            where = where_builder.ToString ();
        }

        public U GetProperty <U> (T item, DbColumn column)
        {
            CheckProperty (typeof (U), column);

            return connection.Query<U> (string.Format (
                "SELECT {0} FROM {1} WHERE {2}={3}",
                column.Name, TableName, key.Name, key.GetValue (item)));
        }

        public void SetProperty <U> (T item, U value, DbColumn column)
        {
            CheckProperty (typeof (U), column);

            connection.Execute (string.Format (
                "UPDATE {0} SET {1}='{2}' WHERE {3}={4}",
                TableName, column.Name,
                SqliteUtils.ToDbFormat (typeof (U), value),
                key.Name, key.GetValue (item)));
        }

        public void ClearProperty <U> (DbColumn column)
        {
            if (!connection.ColumnExists (TableName, column.Name)) {
                AddColumnToTable (SqliteUtils.BuildColumnSchema (
                    SqliteUtils.GetType (typeof (U)),
                    column.Name, column.DefaultValue, column.Constraints));
            } else {
                connection.Execute (string.Format (
                    "UPDATE {0} SET {1}='{2}'",
                    TableName, column.Name, column.DefaultValue));
            }
        }

        private void CheckProperty (Type type, DbColumn column)
        {
            if (!connection.ColumnExists (TableName, column.Name)) {
                AddColumnToTable (SqliteUtils.BuildColumnSchema (
                    SqliteUtils.GetType (type),
                    column.Name, column.DefaultValue, column.Constraints));
            }
        }

        private void AddColumnToTable (string column_schema)
        {
            connection.Execute (string.Format (
                "ALTER TABLE {0} ADD {1}",
                TableName, column_schema)
            );
        }
	}
}
