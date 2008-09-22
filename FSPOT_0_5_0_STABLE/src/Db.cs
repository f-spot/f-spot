using System.Threading;
using System.Collections;
using System.IO;
using System;
using Banshee.Database;
using System.Diagnostics;
using FSpot;
using FSpot.Utils;

// A Store maps to a SQL table.  We have separate stores (i.e. SQL tables) for tags, photos and imports.

public delegate void ItemsAddedHandler (object sender, DbItemEventArgs args);
public delegate void ItemsRemovedHandler (object sender, DbItemEventArgs args);
public delegate void ItemsChangedHandler (object sender, DbItemEventArgs args);

public class DbException : ApplicationException {
	public DbException(string msg) : base(msg)
	{
	}
}

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

	public delegate void ExceptionThrownHandler (Exception e);
	public event ExceptionThrownHandler ExceptionThrown;


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
		uint timer = Log.DebugTimerStart ();
		bool new_db = ! File.Exists (path);
		this.path = path;

		if (new_db && ! create_if_missing)
			throw new Exception (path + ": File not found");

		database = new QueuedSqliteDatabase(path);
		database.ExceptionThrown += HandleDbException;

		if (database.GetFileVersion(path) == 2)
			SqliteUpgrade ();

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
		Log.DebugTimerPrint (timer, "Db Initialization took {0}");
	}

	void HandleDbException (Exception e)
	{
		if (ExceptionThrown != null)
			ExceptionThrown (e);
		else
			throw e;
	}

	public bool Empty {
		get {
			return empty;
		}
	}

	public void Dispose ()
	{
		Dispose (true);
		GC.SuppressFinalize (this);
	}

	bool already_disposed = false;
	protected virtual void Dispose (bool is_disposing)
	{
		if (already_disposed)
			return;
		if (is_disposing) {//Free managed resources
			Database.Dispose ();
		}
		//Free eunmanaged resources

		already_disposed = true;
	}

	~Db ()
	{
		Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());
		Dispose (false);
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

	private void SqliteUpgrade ()
	{
		//Close the db
		database.Dispose();

		string upgrader_path = null;
		string [] possible_paths = {
			Path.Combine (Defines.BINDIR, "f-spot-sqlite-upgrade"),
			"../tools/f-spot-sqlite-upgrade",
			"/usr/local/bin/f-spot-sqlite-upgrade",
			"/usr/bin/f-spot-sqlite-upgrade",
		};

		foreach (string p in possible_paths)
			if (File.Exists (p)) {
				upgrader_path = p;
				break;
			}

		if (upgrader_path == null)
			throw new DbException ("Failed to upgrade the f-spot sqlite2 database to sqlite3!\n" + "Unable to find the f-spot-sqlite-upgrade script on your system");

		Console.WriteLine ("Running {0}...", upgrader_path);
		ProcessStartInfo updaterInfo = new ProcessStartInfo (upgrader_path);
		updaterInfo.UseShellExecute = false;
		updaterInfo.RedirectStandardError = true;
		Process updater = Process.Start (updaterInfo);
		string stdError = updater.StandardError.ReadToEnd ();
		updater.WaitForExit ();
		if (updater.ExitCode != 0)
			throw new DbException("Failed to upgrade the f-spot sqlite2 database to sqlite3!\n" + stdError);

		//Re-open the db
		database = new QueuedSqliteDatabase(path);
	}
}


