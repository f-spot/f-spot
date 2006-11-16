using Gdk;
using Gnome;
using Gtk;
using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;

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

public class MetaStore : DbStore {
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
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			@"CREATE TABLE meta (
				id		INTEGER PRIMARY KEY NOT NULL,
				name		TEXT UNIQUE NOT NULL,
				data		TEXT
			)";

		command.ExecuteNonQuery ();
		command.Dispose ();
	}

	private void CreateDefaultItems (bool is_new)
	{
		Create (version, FSpot.Defines.VERSION);
		Create (db_version, (is_new) ? FSpot.Database.Updater.LatestVersion.ToString () : "0");
		
		// Get the hidden tag id, if it exists
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = "SELECT id FROM tags WHERE name = 'Hidden'";
		
		try {
			SqliteDataReader reader = command.ExecuteReader ();
		
			if (reader.Read ())
				Create (hidden, reader [0].ToString ());

			reader.Close ();
		} catch (Exception) {}
	
		command.Dispose ();
	}
	
	private void LoadAllItems ()
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = "SELECT id, name, data FROM meta";
		SqliteDataReader reader = command.ExecuteReader ();

		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);

			string name = reader [1].ToString ();

			string data = null;
			if (reader [2] != null)
				data = reader [2].ToString ();

			MetaItem item = new MetaItem (id, name, data);

			AddToCache (item);
		}

		reader.Close ();
		command.Dispose ();

		if (FSpotVersion.Value != FSpot.Defines.VERSION) {
			FSpotVersion.Value = FSpot.Defines.VERSION;
			Commit (FSpotVersion);
		}
	}

	private MetaItem Create (string name, string data)
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("INSERT INTO meta (name, data) VALUES ('{0}', {1})",
				name, (data == null) ? "NULL" : "'" + data + "'");
		
		MetaItem item = new MetaItem ((uint) Connection.LastInsertRowId, name, data);

		command.ExecuteScalar ();
		command.Dispose ();
		
		AddToCache (item);
		EmitAdded (item);

		return item;
	}
	
	public override void Commit (DbItem dbitem)
	{
		MetaItem item = dbitem as MetaItem;

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("UPDATE meta SET data = '{1}' WHERE name = '{0}'", item.Name, item.Value);

		command.ExecuteNonQuery ();
		command.Dispose ();
		
		EmitChanged (item);
	}
	
	public override DbItem Get (uint id)
	{
		return LookupInCache (id);
	}
	
	public override void Remove (DbItem item)
	{
		RemoveFromCache (item);
		
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM meta WHERE id = {0}", item.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();
		EmitRemoved (item);
	}

	// Constructor

	public MetaStore (SqliteConnection connection, bool is_new)
		: base (connection, true)
	{
		// Ensure the table exists
		bool exists = true;
		try {
			SqliteCommand command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = "UPDATE meta SET id = 1 WHERE 1 = 2";
			command.ExecuteScalar ();
			command.Dispose ();
		} catch (Exception) {
			// Table doesn't exist, so create it
			exists = false;
		}
			
		if (is_new || !exists) {
			CreateTable ();
			CreateDefaultItems (is_new);
		} else
			LoadAllItems ();
	}
}
