//
// SqliteModelCache.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Scott Peterson <lunchtimemama@gmail.com>
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
using System.Collections.Generic;
using System.Text;

using Hyena.Query;

namespace Hyena.Data.Sqlite
{
    public class SqliteModelCache<T> : DictionaryModelCache<T> where T : ICacheableItem, new ()
    {
        private HyenaSqliteConnection connection;
        private ICacheableDatabaseModel model;
        private SqliteModelProvider<T> provider;
        private HyenaSqliteCommand select_range_command;
        private HyenaSqliteCommand select_single_command;
        private HyenaSqliteCommand select_first_command;
        private HyenaSqliteCommand count_command;
        private HyenaSqliteCommand delete_selection_command;
        private HyenaSqliteCommand save_selection_command;
        private HyenaSqliteCommand get_selection_command;
        private HyenaSqliteCommand indexof_command;

        private string select_str;
        private string reload_sql;
        private string last_indexof_where_fragment;
        private long uid;
        private long selection_uid;
        private long rows;
        // private bool warm;
        private long first_order_id;

        public event Action<IDataReader> AggregatesUpdated;

        public SqliteModelCache (HyenaSqliteConnection connection,
                           string uuid,
                           ICacheableDatabaseModel model,
                           SqliteModelProvider<T> provider)
            : base (model)
        {
            this.connection = connection;
            this.model = model;
            this.provider = provider;

            CheckCacheTable ();

            if (model.SelectAggregates != null) {
                if (model.CachesJoinTableEntries) {
                    count_command = new HyenaSqliteCommand (String.Format (@"
                        SELECT count(*), {0} FROM {1}{2} j
                        WHERE j.{4} IN (SELECT ItemID FROM {3} WHERE ModelID = ?)
                        AND {5} = j.{6}",
                        model.SelectAggregates, // eg sum(FileSize), sum(Duration)
                        provider.TableName,     // eg CoreTracks
                        model.JoinFragment,     // eg , CorePlaylistEntries
                        CacheTableName,         // eg CoreCache
                        model.JoinPrimaryKey,   // eg EntryID
                        provider.PrimaryKey,    // eg CoreTracks.TrackID
                        model.JoinColumn        // eg TrackID
                    ));
                } else {
                    count_command = new HyenaSqliteCommand (String.Format (
                        "SELECT count(*), {0} FROM {1} WHERE {2} IN (SELECT ItemID FROM {3} WHERE ModelID = ?)",
                        model.SelectAggregates, provider.TableName, provider.PrimaryKey, CacheTableName
                    ));
                }
            } else {
                count_command = new HyenaSqliteCommand (String.Format (
                    "SELECT COUNT(*) FROM {0} WHERE ModelID = ?", CacheTableName
                ));
            }

            uid = FindOrCreateCacheModelId (String.Format ("{0}-{1}", uuid, typeof(T).Name));

            if (model.Selection != null) {
                selection_uid = FindOrCreateCacheModelId (String.Format ("{0}-{1}-Selection", uuid, typeof(T).Name));
            }

            if (model.CachesJoinTableEntries) {
                select_str = String.Format (
                    @"SELECT {0} {10}, OrderID, {5}.ItemID  FROM {1}
                        INNER JOIN {2}
                            ON {3} = {2}.{4}
                        INNER JOIN {5}
                            ON {2}.{6} = {5}.ItemID {11}
                        WHERE
                            {5}.ModelID = {7} {8} {9}",
                    provider.Select, provider.From,
                    model.JoinTable, provider.PrimaryKey, model.JoinColumn,
                    CacheTableName, model.JoinPrimaryKey, uid,
                    String.IsNullOrEmpty (provider.Where) ? null : "AND",
                    provider.Where, "{0}", "{1}"
                );

                reload_sql = String.Format (@"
                    DELETE FROM {0} WHERE ModelID = {1};
                        INSERT INTO {0} (ModelID, ItemID) SELECT {1}, {2} ",
                    CacheTableName, uid, model.JoinPrimaryKey
                );
            } else if (model.CachesValues) {
                // The duplication of OrderID/ItemID in the select below is intentional!
                // The first is used construct the QueryFilterInfo value, the last internally
                // to this class
                select_str = String.Format (
                    @"SELECT OrderID, ItemID {2}, OrderID, ItemID FROM {0} {3} WHERE {0}.ModelID = {1}",
                    CacheTableName, uid, "{0}", "{1}"
                );

                reload_sql = String.Format (@"
                    DELETE FROM {0} WHERE ModelID = {1};
                    INSERT INTO {0} (ModelID, ItemID) SELECT DISTINCT {1}, {2} ",
                    CacheTableName, uid, provider.Select
                );
            } else {
                select_str = String.Format (
                    @"SELECT {0} {7}, OrderID, {2}.ItemID FROM {1}
                        INNER JOIN {2}
                            ON {3} = {2}.ItemID {8}
                        WHERE
                            {2}.ModelID = {4} {5} {6}",
                    provider.Select, provider.From, CacheTableName,
                    provider.PrimaryKey, uid,
                    String.IsNullOrEmpty (provider.Where) ? String.Empty : "AND",
                    provider.Where, "{0}", "{1}"
                );

                reload_sql = String.Format (@"
                    DELETE FROM {0} WHERE ModelID = {1};
                        INSERT INTO {0} (ModelID, ItemID) SELECT {1}, {2} ",
                    CacheTableName, uid, provider.PrimaryKey
                );
            }

            select_single_command = new HyenaSqliteCommand (
                String.Format (@"
                    SELECT OrderID FROM {0}
                        WHERE
                            ModelID = {1} AND
                            ItemID = ?",
                    CacheTableName, uid
                )
            );

            select_range_command = new HyenaSqliteCommand (
                String.Format (String.Format ("{0} {1}", select_str, "LIMIT ?, ?"), null, null)
            );

            select_first_command = new HyenaSqliteCommand (
                String.Format (
                    "SELECT OrderID FROM {0} WHERE ModelID = {1} LIMIT 1",
                    CacheTableName, uid
                )
            );

            if (model.Selection != null) {
                delete_selection_command = new HyenaSqliteCommand (String.Format (
                    "DELETE FROM {0} WHERE ModelID = {1}", CacheTableName, selection_uid
                ));

                save_selection_command = new HyenaSqliteCommand (String.Format (
                    "INSERT INTO {0} (ModelID, ItemID) SELECT {1}, ItemID FROM {0} WHERE ModelID = {2} LIMIT ?, ?",
                    CacheTableName, selection_uid, uid
                ));

                get_selection_command = new HyenaSqliteCommand (String.Format (
                    "SELECT OrderID FROM {0} WHERE ModelID = {1} AND ItemID IN (SELECT ItemID FROM {0} WHERE ModelID = {2})",
                    CacheTableName, uid, selection_uid
                ));
            }
        }

        private bool has_select_all_item = false;
        public bool HasSelectAllItem {
            get { return has_select_all_item; }
            set { has_select_all_item = value; }
        }

        // The idea behind this was we could preserve the CoreCache table across restarts of Banshee,
        // and indicate whether the cache was already primed via this property.  It's not used, and may never be.
        public bool Warm {
            //get { return warm; }
            get { return false; }
        }

        public long Count {
            get { return rows; }
        }

        public long CacheId {
            get { return uid; }
        }

        protected virtual string CacheModelsTableName {
            get { return "HyenaCacheModels"; }
        }

        protected virtual string CacheTableName {
            get { return "HyenaCache"; }
        }

        private long FirstOrderId {
            get {
                lock (this) {
                    if (first_order_id == -1) {
                        first_order_id = connection.Query<long> (select_first_command);
                    }
                    return first_order_id;
                }
             }
        }

        public long IndexOf (string where_fragment, long offset)
        {
            if (String.IsNullOrEmpty (where_fragment)) {
                return -1;
            }

            if (!where_fragment.Equals (last_indexof_where_fragment)) {
                last_indexof_where_fragment = where_fragment;

                if (!where_fragment.Trim ().ToLower ().StartsWith ("and ")) {
                    where_fragment = " AND " + where_fragment;
                }

                string sql = String.Format ("{0} {1} LIMIT ?, 1", select_str, where_fragment);
                indexof_command = new HyenaSqliteCommand (String.Format (sql, null, null));
            }

            lock (this) {
                using (IDataReader reader = connection.Query (indexof_command, offset)) {
                    if (reader.Read ()) {
                        long target_id = (long) reader[reader.FieldCount - 2];
                        if (target_id == 0) {
                            return -1;
                        }
                        return target_id - FirstOrderId;
                    }
                }

                return -1;
            }
        }

        public long IndexOf (ICacheableItem item)
        {
            if (item == null || item.CacheModelId != CacheId) {
                return -1;
            }

            return IndexOf (item.CacheEntryId);
        }

        public long IndexOf (object item_id)
        {
            lock (this) {
                if (rows == 0) {
                    return -1;
                }

                long target_id = connection.Query<long> (select_single_command, item_id);
                if (target_id == 0) {
                    return -1;
                }
                return target_id - FirstOrderId;
            }
        }

        public T GetSingleWhere (string conditionOrderFragment, params object [] args)
        {
            return GetSingle (null, null, conditionOrderFragment, args);
        }

        private HyenaSqliteCommand get_single_command;
        private string last_get_single_select_fragment, last_get_single_condition_fragment, last_get_single_from_fragment;
        public T GetSingle (string selectFragment, string fromFragment, string conditionOrderFragment, params object [] args)
        {
            if (selectFragment != last_get_single_select_fragment
                || conditionOrderFragment != last_get_single_condition_fragment
                ||fromFragment != last_get_single_from_fragment
                || get_single_command == null)
            {
                last_get_single_select_fragment = selectFragment;
                last_get_single_condition_fragment = conditionOrderFragment;
                last_get_single_from_fragment = fromFragment;
                get_single_command = new HyenaSqliteCommand (String.Format (String.Format (
                        "{0} {1} {2}", select_str, conditionOrderFragment, "LIMIT 1"), selectFragment, fromFragment
                ));
            }

            using (IDataReader reader = connection.Query (get_single_command, args)) {
                if (reader.Read ()) {
                    T item = provider.Load (reader);
                    item.CacheEntryId = reader[reader.FieldCount - 1];
                    item.CacheModelId = uid;
                    return item;
                }
            }
            return default(T);
        }

        private HyenaSqliteCommand last_reload_command;
        private string last_reload_fragment;

        public override void Reload ()
        {
            lock (this) {
                if (last_reload_fragment != model.ReloadFragment || last_reload_command == null) {
                    last_reload_fragment = model.ReloadFragment;
                    last_reload_command = new HyenaSqliteCommand (String.Format ("{0}{1}", reload_sql, last_reload_fragment));
                }

                Clear ();
                //Log.DebugFormat ("Reloading {0} with {1}", model, last_reload_command.Text);
                connection.Execute (last_reload_command);
            }
        }

        public override void Clear ()
        {
            lock (this) {
                base.Clear ();
                first_order_id = -1;
            }
        }

        private bool saved_selection = false;
        private ICacheableItem saved_focus_item = null;
        public void SaveSelection ()
        {
            if (model.Selection != null && model.Selection.Count > 0 &&
                !(has_select_all_item && model.Selection.AllSelected))
            {
                connection.Execute (delete_selection_command);
                saved_selection = true;

                if (!has_select_all_item && model.Selection.FocusedIndex != -1) {
                    T item = GetValue (model.Selection.FocusedIndex);
                    if (item != null) {
                        saved_focus_item = GetValue (model.Selection.FocusedIndex);
                    }
                }

                long start, end;
                foreach (Hyena.Collections.RangeCollection.Range range in model.Selection.Ranges) {
                    start = range.Start;
                    end = range.End;

                    // Compensate for the first, fake 'All *' item
                    if (has_select_all_item) {
                        start -= 1;
                        end -= 1;
                    }

                    connection.Execute (save_selection_command, start, end - start + 1);
                }
            } else {
                saved_selection = false;
            }
        }

        public void RestoreSelection ()
        {
            if (saved_selection) {
                long selected_id = -1;
                long first_id = FirstOrderId;

                // Compensate for the first, fake 'All *' item
                if (has_select_all_item) {
                    first_id -= 1;
                }

                model.Selection.Clear (false);

                foreach (long order_id in connection.QueryEnumerable<long> (get_selection_command)) {
                    selected_id = order_id - first_id;
                    model.Selection.QuietSelect ((int)selected_id);
                }

                if (has_select_all_item && model.Selection.Count == 0) {
                    model.Selection.QuietSelect (0);
                } if (saved_focus_item != null) {
                    long i = IndexOf (saved_focus_item);
                    if (i != -1) {
                        // TODO get rid of int cast
                        model.Selection.FocusedIndex = (int)i;
                    }
                }
                saved_selection = false;
                saved_focus_item = null;
            }
        }

        protected override void FetchSet (long offset, long limit)
        {
            lock (this) {
                using (IDataReader reader = connection.Query (select_range_command, offset, limit)) {
                    T item;
                    while (reader.Read ()) {
                        if (!ContainsKey (offset)) {
                            item = provider.Load (reader);
                            item.CacheEntryId = reader[reader.FieldCount - 1];
                            item.CacheModelId = uid;
                            Add (offset, item);
                        }
                        offset++;
                    }
                }
            }
        }

        public void UpdateAggregates ()
        {
            rows = UpdateAggregates (AggregatesUpdated, uid);
        }

        public void UpdateSelectionAggregates (Action<IDataReader> handler)
        {
            SaveSelection ();
            UpdateAggregates (handler, selection_uid);
        }

        private long UpdateAggregates (Action<IDataReader> handler, long model_id)
        {
            long aggregate_rows = 0;
            using (IDataReader reader = connection.Query (count_command, model_id)) {
                if (reader.Read ()) {
                    aggregate_rows = Convert.ToInt64 (reader[0]);

                    if (handler != null) {
                        handler (reader);
                    }
                }
            }

            return aggregate_rows;
        }


        private long FindOrCreateCacheModelId (string id)
        {
            long model_id = connection.Query<long> (String.Format (
                "SELECT CacheID FROM {0} WHERE ModelID = '{1}'",
                CacheModelsTableName, id
            ));

            if (model_id == 0) {
                //Console.WriteLine ("Didn't find existing cache for {0}, creating", id);
                model_id = connection.Execute (new HyenaSqliteCommand (String.Format (
                    "INSERT INTO {0} (ModelID) VALUES (?)", CacheModelsTableName
                    ), id
                ));
            } else {
                //Console.WriteLine ("Found existing cache for {0}: {1}", id, uid);
                //warm = true;
                //Clear ();
                //UpdateAggregates ();
            }

            return model_id;
        }

        private static string checked_cache_table;
        private void CheckCacheTable ()
        {
            if (CacheTableName == checked_cache_table) {
                return;
            }

            if (!connection.TableExists (CacheTableName)) {
                connection.Execute (String.Format (@"
                    CREATE TEMP TABLE {0} (
                        OrderID INTEGER PRIMARY KEY,
                        ModelID INTEGER,
                        ItemID TEXT)", CacheTableName
                ));
            }

            if (!connection.TableExists (CacheModelsTableName)) {
                connection.Execute (String.Format (
                    "CREATE TABLE {0} (CacheID INTEGER PRIMARY KEY, ModelID TEXT UNIQUE)",
                    CacheModelsTableName
                ));
            }

            checked_cache_table = CacheTableName;
        }
    }
}
