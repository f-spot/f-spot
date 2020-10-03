//
// MetaStore.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2006 Gabriel Burt
// Copyright (C) 2009-2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Settings;

using Hyena.Data.Sqlite;

namespace FSpot.Database
{
	public class MetaItem : DbItem
	{

		public string Name { get; set; }
		public string Value { get; set; }

		public int ValueAsInt {
			get => int.Parse (Value);
			set { Value = value.ToString (); }
		}

		public MetaItem (uint id, string name, string data) : base (id)
		{
			Name = name;
			Value = data;
		}
	}

	public class MetaStore : DbStore<MetaItem>
	{
		const string version = "F-Spot Version";
		const string db_version = "F-Spot Database Version";
		const string hidden = "Hidden Tag Id";

		public MetaItem FSpotVersion {
			get { return GetByName (version); }
		}

		public MetaItem DatabaseVersion {
			get { return GetByName (db_version); }
		}

		public MetaItem HiddenTagId {
			get { return GetByName (hidden); }
		}

		MetaItem GetByName (string name)
		{
			foreach (MetaItem i in ItemCache.Values)
				if (i.Name == name)
					return i;

			// Otherwise make it and return it
			return Create (name, null);
		}

		void CreateTable ()
		{
			Database.Execute (
				"CREATE TABLE meta (\n" +
				"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
				"	name	TEXT UNIQUE NOT NULL, \n" +
				"	data	TEXT\n" +
				")");
		}

		void CreateDefaultItems (bool is_new)
		{
			Create (version, FSpotConfiguration.Version);
			Create (db_version, (is_new) ? FSpot.Database.Updater.LatestVersion.ToString () : "0");

			// Get the hidden tag id, if it exists
			string table = Database.Query<string> ("SELECT name FROM sqlite_master WHERE type='table' AND name='tags'");
			if (!string.IsNullOrEmpty (table)) {
				string id = Database.Query<string> ("SELECT id FROM tags WHERE name = 'Hidden'");
				Create (hidden, id);
			}
		}

		void LoadAllItems ()
		{
			Hyena.Data.Sqlite.IDataReader reader = Database.Query ("SELECT id, name, data FROM meta");

			while (reader.Read ()) {
				uint id = Convert.ToUInt32 (reader["id"]);

				string name = reader["name"].ToString ();

				string data = null;
				if (reader["data"] != null)
					data = reader["data"].ToString ();

				MetaItem item = new MetaItem (id, name, data);

				AddToCache (item);
			}

			reader.Dispose ();

			if (FSpotVersion.Value != FSpotConfiguration.Version) {
				FSpotVersion.Value = FSpotConfiguration.Version;
				Commit (FSpotVersion);
			}
		}

		MetaItem Create (string name, string data)
		{

			uint id = (uint)Database.Execute (new HyenaSqliteCommand ("INSERT INTO meta (name, data) VALUES (?, ?)", name, data ?? "NULL"));

			//FIXME This smells bad. This line used to be *before* the
			//Command.executeNonQuery. It smells of a bug, but there might
			//have been a reason for this

			var item = new MetaItem (id, name, data);

			AddToCache (item);
			EmitAdded (item);

			return item;
		}

		public override void Commit (MetaItem item)
		{
			Database.Execute (new HyenaSqliteCommand ("UPDATE meta SET data = ? WHERE name = ?", item.Value, item.Name));

			EmitChanged (item);
		}

		public override MetaItem Get (uint id)
		{
			return LookupInCache (id);
		}

		public override void Remove (MetaItem item)
		{
			RemoveFromCache (item);

			Database.Execute (new HyenaSqliteCommand ("DELETE FROM meta WHERE id = ?", item.Id));

			EmitRemoved (item);
		}

		public MetaStore (IDb db, bool isNew) : base (db, true)
		{
			if (isNew || !Database.TableExists ("meta")) {
				CreateTable ();
				CreateDefaultItems (isNew);
			} else
				LoadAllItems ();
		}
	}
}
