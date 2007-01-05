using Mono.Data.SqliteClient;
using System.Threading;
using System.Collections;
using System.IO;
using System;


// All kinds of database items subclass from this.
public class DbItem {
	uint id;
	public uint Id {
		get {
			return id;
		}
	}

	protected DbItem (uint id) {
		this.id = id;
	}
}

public class DbItemEventArgs {
	private DbItem [] items;
	
	public DbItem [] Items {
		get { return items; }
	}
	
	public DbItemEventArgs (DbItem [] items)
	{
		this.items = items;
	}
	
	public DbItemEventArgs (DbItem item)
	{
		this.items = new DbItem [] { item };
	}
}

// A Store maps to a SQL table.  We have separate stores (i.e. SQL tables) for tags, photos and imports.

public delegate void ItemsAddedHandler (object sender, DbItemEventArgs args);
public delegate void ItemsRemovedHandler (object sender, DbItemEventArgs args);
public delegate void ItemsChangedHandler (object sender, DbItemEventArgs args);

public abstract class DbStore {
	// DbItem cache.

	protected const int MAX_RETRIES = 4;
	protected const int SLEEP_TIME = 1000; // 1 sec

	public event ItemsAddedHandler   ItemsAdded;
	public event ItemsRemovedHandler ItemsRemoved;
	public event ItemsChangedHandler ItemsChanged;

	protected Hashtable item_cache;
	bool cache_is_immortal;

	protected void AddToCache (DbItem item)
	{
		if (item_cache.Contains (item.Id))
			item_cache.Remove (item.Id);

		if (cache_is_immortal)
			item_cache.Add (item.Id, item);
		else
			item_cache.Add (item.Id, new WeakReference (item));
	}

	protected DbItem LookupInCache (uint id)
	{
		if (cache_is_immortal)
			return item_cache [id] as DbItem;

		WeakReference weakref = item_cache [id] as WeakReference;
		if (weakref == null)
			return null;
		else
			return (DbItem) weakref.Target;
	}

	protected void RemoveFromCache (DbItem item)
	{
		item_cache.Remove (item.Id);
	}

	protected void EmitAdded (DbItem item)
	{
		EmitAdded (new DbItem [] { item });
	}

	protected void EmitAdded (DbItem [] items)
	{
		if (ItemsAdded != null)
			ItemsAdded (this, new DbItemEventArgs (items));
	}

	protected void EmitChanged (DbItem item)
	{
		EmitChanged (new DbItem [] { item });
	}

	protected void EmitChanged (DbItem [] items)
	{
		EmitChanged (items, new DbItemEventArgs (items));
	}

	protected void EmitChanged (DbItem [] items, DbItemEventArgs args)
	{
		if (ItemsChanged != null)
			ItemsChanged (this, args);
	}

	protected void EmitRemoved (DbItem item)
	{
		EmitRemoved (new DbItem [] { item });
	}

	protected void EmitRemoved (DbItem [] items)
	{
		if (ItemsRemoved != null)
			ItemsRemoved (this, new DbItemEventArgs (items));
	}

	public bool CacheEmpty {
		get {
			return item_cache.Count == 0;
		}
	}

	// Sqlite stuff.

	SqliteConnection connection;
	protected SqliteConnection Connection {
		get {
			return connection;
		}
	}


	// Constructor.

	public DbStore (SqliteConnection connection,
			bool cache_is_immortal)
	{
		this.connection = connection;
		this.cache_is_immortal = cache_is_immortal;

		item_cache = new Hashtable ();
	}


	// Abstract methods.

	public abstract DbItem Get (uint id);
	public abstract void Remove (DbItem item);
	// If you have made changes to "obj", you have to invoke Commit() to have the changes
	// saved into the database.
	public abstract void Commit (DbItem item);


	// Utility methods.
	
	protected void ExecuteSqlCommand (String command_text) 

	{

		int retries = 0;
		TrySqliteCommand (command_text, ref retries);

	}

	private void TrySqliteCommand (String command_text, ref int retries)
	{


		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = command_text;

		try {
			retries++;
			command.ExecuteNonQuery ();
		} catch (SqliteBusyException) {
			if ( retries > MAX_RETRIES )
			{
				//FIXME: show a dialog explaining things to the user
				throw;
			}
			// DB is locked. Sleep a while and try again
			Thread.Sleep (SLEEP_TIME);
			TrySqliteCommand (command_text, ref retries);
		} finally {
			command.Dispose ();
		}

	}



	// Subclasses use this to generate valid SQL commands.
	protected static string SqlString (string s) {
		return s.Replace ("'", "''");
	}

	public void BeginTransaction () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = "BEGIN TRANSACTION";
		command.ExecuteScalar ();
		command.Dispose ();
	}
	
	public void CommitTransaction () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = "COMMIT TRANSACTION";
		command.ExecuteScalar ();
		command.Dispose ();
	}
	
	public void RollbackTransaction () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = "ROLLBACK";
		command.ExecuteScalar ();
		command.Dispose ();
	}

}


// The Database puts the stores together.

public class Db : IDisposable {

	TagStore tag_store;
	PhotoStore photo_store;
 	ImportStore import_store;
 	MetaStore meta_store;
	bool empty;
	string path;

	public TagStore Tags {
		get { return tag_store; }
	}

	public ImportStore Imports {
		get { return import_store; }
	}

	public PhotoStore Photos {
		get { return photo_store; }
	}
	
	public MetaStore Meta {
		get { return meta_store; }
	}

	// This affects how often the database writes data back to disk, and
	// therefore how likely corruption is in the event of power loss.
	public bool Sync {
		set {
			SqliteCommand command = new SqliteCommand ();
			command.Connection = sqlite_connection;
			command.CommandText = "PRAGMA synchronous = " + (value ? "ON" : "OFF");
			command.ExecuteScalar ();
			command.Dispose ();
		}
	}

	public void BeginTransaction () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = sqlite_connection;
		command.CommandText = "BEGIN TRANSACTION";
		command.ExecuteScalar ();
		command.Dispose ();
	}
	
	public void CommitTransaction () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = sqlite_connection;
		command.CommandText = "COMMIT TRANSACTION";
		command.ExecuteScalar ();
		command.Dispose ();
	}
	
	public void RollbackTransaction () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = sqlite_connection;
		command.CommandText = "ROLLBACK";
		command.ExecuteScalar ();
		command.Dispose ();
	}

	SqliteConnection sqlite_connection;
	public SqliteConnection Connection {
		get { return sqlite_connection; }
	}

	private static int GetFileVersion (string path)
	{
		using (Stream stream = File.OpenRead (path)) {
			byte [] data = new byte [15];
			stream.Read (data, 0, data.Length);

			string magic = System.Text.Encoding.ASCII.GetString (data, 0, data.Length);

			switch (magic) {
			case "SQLite format 3":
				return 3;
			case "** This file co":
				return 2;
			default:
				return -1;
			}
		}
	}

	public string Repair ()
	{
		string backup_path = path;
		int i = 0;

		while (File.Exists (backup_path)) {
			backup_path = String.Format ("{0}-{1}-{2}{3}",
						     Path.GetFileNameWithoutExtension (path),
						     System.DateTime.Now.ToString ("yyyyMMdd"),
						     i++,
						     Path.GetExtension (path));
		}
		
		File.Move (path, backup_path);
		Init (path, true);

		return backup_path;
	}

	public void Init (string path, bool create_if_missing)
	{
		bool new_db = ! File.Exists (path);
		string version_string = ",version=3";
		this.path = path;

		if (new_db && ! create_if_missing)
			throw new Exception (path + ": File not found");

		if (! new_db) {
			int version = Db.GetFileVersion (path);
			// FIXME: we should probably display and error dialog if the version
			// is anything other than the one we were built with, but for now at least
			// use the right version.

			if (version < 2)
				throw new Exception ("Unsupported database version");
			
			version_string = String.Format (",version={0}", version);

			if (version == 2)
				version_string += ",encoding=UTF-8";
		}
		
		sqlite_connection = new SqliteConnection ();
		sqlite_connection.ConnectionString = "URI=file:" + path + version_string;

		sqlite_connection.Open ();
		

		// Load or create the meta table
 		meta_store = new MetaStore (sqlite_connection, new_db);

		// Update the database schema if necessary
		FSpot.Database.Updater.Run (this);

		BeginTransaction ();

		tag_store = new TagStore (sqlite_connection, new_db);
		import_store = new ImportStore (sqlite_connection, new_db);
 		photo_store = new PhotoStore (sqlite_connection, new_db, tag_store);
		
		CommitTransaction ();

		empty = new_db;
	}

	public bool Empty {
		get {
			return empty;
		}
	}

	public void Dispose () {}
}


public class DbUtils {
#if USE_CORRECT_FUNCTION
	public static DateTime DateTimeFromUnixTime (long unix_time)
	{
		DateTime date_time = new DateTime (1970, 1, 1);
		return date_time.AddSeconds (unix_time).ToLocalTime ();
	}

	public static long UnixTimeFromDateTime (DateTime date_time)
	{
		return (long) (date_time.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds;
	}
#else
	public static DateTime DateTimeFromUnixTime (long unix_time)
	{
		DateTime date_time = new DateTime (1970, 1, 1).ToLocalTime ();
		return date_time.AddSeconds (unix_time);
	}
	
	public static long UnixTimeFromDateTime (DateTime date_time)
	{
		return (long) (date_time - new DateTime (1970, 1, 1).ToLocalTime ()).TotalSeconds;
	}
#endif
}
