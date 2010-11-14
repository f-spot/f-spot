using Gdk;
using Gtk;
using System.Collections;
using System.IO;
using System;
using FSpot;
using FSpot.Core;
using FSpot.Database;
using Hyena.Data.Sqlite;

namespace FSpot {
public class MetaItem : DbItem {
	private string name;
	public string Name {
		get { return name; }
		set { name = value; }
	}

	private string data;
	public string Value {
		get { return data; }
		set { data = value; }
	}

	public int ValueAsInt {
		get { return System.Int32.Parse (Value); }
		set { Value = value.ToString (); }
	}

	public MetaItem (uint id, string name, string data) : base (id)
	{
		this.name = name;
		this.data = data;
	}
}

public class MetaStore : DbStore<MetaItem> {
	private const string version = "F-Spot Version";
	private const string db_version = "F-Spot Database Version";
	private const string hidden = "Hidden Tag Id";

	public MetaItem FSpotVersion {
		get { return GetByName (version); }
	}

	public MetaItem DatabaseVersion {
		get { return GetByName (db_version); }
	}

	public MetaItem HiddenTagId {
		get { return GetByName (hidden); }
	}

	private MetaItem GetByName (string name)
	{
		foreach (MetaItem i in this.item_cache.Values)
			if (i.Name == name)
				return i;

		// Otherwise make it and return it
		return Create (name, null);
	}

	private void CreateTable ()
	{
		Database.Execute (
			"CREATE TABLE meta (\n" +
			"	id	INTEGER PRIMARY KEY NOT NULL, \n" +
			"	name	TEXT UNIQUE NOT NULL, \n" +
			"	data	TEXT\n" +
			")");
	}

	private void CreateDefaultItems (bool is_new)
	{
		Create (version, Defines.VERSION);
		Create (db_version, (is_new) ? FSpot.Database.Updater.LatestVersion.ToString () : "0");

		// Get the hidden tag id, if it exists
		try {
			string id = Database.Query<string> ("SELECT id FROM tags WHERE name = 'Hidden'");
			Create (hidden, id);
		} catch (Exception) {}
	}

	private void LoadAllItems ()
	{
		IDataReader reader = Database.Query("SELECT id, name, data FROM meta");

		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader ["id"]);

			string name = reader ["name"].ToString ();

			string data = null;
			if (reader ["data"] != null)
				data = reader ["data"].ToString ();

			MetaItem item = new MetaItem (id, name, data);

			AddToCache (item);
		}

		reader.Dispose ();

		if (FSpotVersion.Value != Defines.VERSION) {
			FSpotVersion.Value = Defines.VERSION;
			Commit (FSpotVersion);
		}
	}

	private MetaItem Create (string name, string data)
	{

		uint id = (uint)Database.Execute(new HyenaSqliteCommand("INSERT INTO meta (name, data) VALUES (?, ?)",
				name, (data == null) ? "NULL" : data ));

		//FIXME This smells bad. This line used to be *before* the
		//Command.executeNonQuery. It smells of a bug, but there might
		//have been a reason for this

		MetaItem item = new MetaItem (id, name, data);


		AddToCache (item);
		EmitAdded (item);

		return item;
	}

	public override void Commit (MetaItem item)
	{
		Database.Execute(new HyenaSqliteCommand("UPDATE meta SET data = ? WHERE name = ?", item.Value, item.Name));

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

	// Constructor

	public MetaStore (FSpotDatabaseConnection database, bool is_new)
		: base (database, true)
	{
		if (is_new || !Database.TableExists ("meta")) {
			CreateTable ();
			CreateDefaultItems (is_new);
		} else
			LoadAllItems ();
	}
}
}
