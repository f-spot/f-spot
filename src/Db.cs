using System.Threading;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;


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


	QueuedSqliteDatabase database;
	protected QueuedSqliteDatabase Database {
		get {
			return database;
		}
	}


	// Constructor.

	public DbStore (QueuedSqliteDatabase database,
			bool cache_is_immortal)
	{
		this.database = database;
		this.cache_is_immortal = cache_is_immortal;

		item_cache = new Hashtable ();
	}


	// Abstract methods.

	public abstract DbItem Get (uint id);
	public abstract void Remove (DbItem item);
	// If you have made changes to "obj", you have to invoke Commit() to have the changes
	// saved into the database.
	public abstract void Commit (DbItem item);
}


// The Database puts the stores together.

public class Db : IDisposable {

	TagStore tag_store;
	PhotoStore photo_store;
 	RollStore roll_store;
	ExportStore export_store;
 	JobStore job_store;
 	MetaStore meta_store;
	bool empty;
	string path;

	public TagStore Tags {
		get { return tag_store; }
	}

	public RollStore Rolls {
		get { return roll_store; }
	}

	public ExportStore Exports {
		get { return export_store; }
	}

	public JobStore Jobs {
		get { return job_store; }
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
			string query = "PRAGMA synchronous = " + (value ? "ON" : "OFF");
			Database.ExecuteNonQuery(query);
		}
	}

	QueuedSqliteDatabase database;
	public QueuedSqliteDatabase Database {
		get { return database; }
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
		this.path = path;

		if (new_db && ! create_if_missing)
			throw new Exception (path + ": File not found");

		database = new QueuedSqliteDatabase(path);	

		// Load or create the meta table
 		meta_store = new MetaStore (Database, new_db);

		// Update the database schema if necessary
		FSpot.Database.Updater.Run (this);

		Database.BeginTransaction ();

		tag_store = new TagStore (Database, new_db);
		roll_store = new RollStore (Database, new_db);
		export_store = new ExportStore (Database, new_db);
		job_store = new JobStore (Database, new_db);
 		photo_store = new PhotoStore (Database, new_db);
		
		Database.CommitTransaction ();

		empty = new_db;
	}

	public bool Empty {
		get {
			return empty;
		}
	}

	public void Dispose ()
	{
		Database.Dispose ();
	}

	public void BeginTransaction()
	{
		Database.BeginTransaction ();
	}

	public void CommitTransaction()
	{
		Database.CommitTransaction ();
	}

	public void RollbackTransaction()
	{
		Database.RollbackTransaction ();
	}

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
