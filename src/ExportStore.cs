using Gdk;
using Gnome;
using Gtk;
using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System;

public class ExportItem : DbItem {

    private uint image_id;
    public uint ImageId {
        get { return image_id; }
        set { image_id = value; }
    }

    private uint image_version_id;
    public uint ImageVersionId {
        get { return image_version_id; }
        set { image_version_id = value; }
    }

    private string export_type;
    public string ExportType {
        get { return export_type; }
        set { export_type = value; }
    }

    private string export_token;
    public string ExportToken {
        get { return export_token; }
        set { export_token = value; }
    }

    public ExportItem (uint id, uint image_id, uint image_version_id, string export_type, string export_token) : base (id)
    {
        this.image_id = image_id;
        this.image_version_id = image_version_id;
        this.export_type = export_type;
        this.export_token = export_token;
    }
}

public class ExportStore : DbStore {
	
	public const string FlickrExportType = "fspot:Flickr";
	public const string OldFolderExportType = "fspot:Folder"; //This is obsolete and meant to be remove once db reach rev4
	public const string FolderExportType = "fspot:FolderUri";
	public const string PicasaExportType = "fspot:Picasa";
	public const string SmugMugExportType = "fspot:SmugMug";
	public const string Gallery2ExportType = "fspot:Gallery2";

	private void CreateTable ()
	{
		ExecuteSqlCommand (
			@"CREATE TABLE exports (
				id		 INTEGER PRIMARY KEY NOT NULL,
                                image_id         INTEGER NOT NULL,
                                image_version_id INTEGER NOT NULL,
                                export_type      TEXT NOT NULL,
                                export_token     TEXT NOT NULL
			)");
	}

	private ExportItem LoadItem (SqliteDataReader reader)
	{
		return new ExportItem (Convert.ToUInt32 (reader[0]), 
				       Convert.ToUInt32 (reader[1]),
				       Convert.ToUInt32 (reader[2]), 
				       reader[3].ToString (), 
				       reader[4].ToString ());
	}
	
	private void LoadAllItems ()
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = "SELECT id, image_id, image_version_id, export_type, export_token FROM exports";
		SqliteDataReader reader = command.ExecuteReader ();

		while (reader.Read ()) {
                    AddToCache (LoadItem (reader));
		}

		reader.Close ();
		command.Dispose ();
	}

	public ExportItem Create (uint image_id, uint image_version_id, string export_type, string export_token)
	{
		ExecuteSqlCommand (String.Format ("INSERT INTO exports (image_id, image_version_id, export_type, export_token) VALUES ({0}, {1}, '{2}', '{3}')",
						  image_id, image_version_id, export_type, export_token));
		
                ExportItem item = new ExportItem ((uint) Connection.LastInsertRowId, 
						  image_id, image_version_id,
                                                  export_type, export_token);

		AddToCache (item);
		EmitAdded (item);

		return item;
	}
	
	public override void Commit (DbItem dbitem)
	{
		ExportItem item = dbitem as ExportItem;

		ExecuteSqlCommand (String.Format ("UPDATE exports SET image_id = {1} SET image_version_id = {2} SET export_type = '{3}' SET export_token = '{4}' WHERE id = {0}", item.Id, item.ImageId, item.ImageVersionId, item.ExportType, item.ExportToken));
		
		EmitChanged (item);
	}
	
	public override DbItem Get (uint id)
	{
            // we never use this
            return null;
	}

	public ArrayList GetByImageId (uint image_id, uint image_version_id)
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
        
		command.CommandText = String.Format ("SELECT id, image_id, image_version_id, export_type, export_token FROM exports WHERE image_id = {0} AND image_version_id = {1}", image_id, image_version_id);
		SqliteDataReader reader = command.ExecuteReader ();

		ArrayList list = new ArrayList ();
		while (reader.Read ()) {
			list.Add (LoadItem (reader));
		}
        
		reader.Close ();
		command.Dispose ();

		return list;
	}
	
	public override void Remove (DbItem item)
	{
		RemoveFromCache (item);

		ExecuteSqlCommand (String.Format ("DELETE FROM exports WHERE id = {0}", item.Id));

		EmitRemoved (item);
	}

	// Constructor

	public ExportStore (SqliteConnection connection, bool is_new)
		: base (connection, true)
	{
		// Ensure the table exists
		bool exists = true;
		try {
			SqliteCommand command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = "UPDATE exports SET id = 1 WHERE 1 = 2";
			command.ExecuteScalar ();
			command.Dispose ();
		} catch (Exception) {
			// Table doesn't exist, so create it
			exists = false;
		}
			
		if (is_new || !exists) {
			CreateTable ();
		} else {
			LoadAllItems ();
                }
	}
}
