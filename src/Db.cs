using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using FSpot;
using FSpot.Core;
using Hyena;

// A Store maps to a SQL table.  We have separate stores (i.e. SQL tables) for tags, photos and imports.

public class DbException : ApplicationException {
	public DbException(string msg) : base(msg)
	{
	}
}

public abstract class DbStore<T> where T : DbItem {
	// DbItem cache.

	public event EventHandler<DbItemEventArgs<T>> ItemsAdded;
	public event EventHandler<DbItemEventArgs<T>> ItemsRemoved;
	public event EventHandler<DbItemEventArgs<T>> ItemsChanged;

	protected Dictionary<uint, object> item_cache;
	bool cache_is_immortal;

	protected void AddToCache (T item)
	{
		if (item_cache.ContainsKey (item.Id))
			item_cache.Remove (item.Id);

		if (cache_is_immortal)
			item_cache.Add (item.Id, item);
		else
			item_cache.Add (item.Id, new WeakReference (item));
	}

	protected T LookupInCache (uint id)
	{
		if (!item_cache.ContainsKey(id))
			return null;

		if (cache_is_immortal)
			return item_cache [id] as T;

		WeakReference weakref = item_cache [id] as WeakReference;
		return (T) weakref.Target;
	}

	protected void RemoveFromCache (T item)
	{
		item_cache.Remove (item.Id);
	}

	protected void EmitAdded (T item)
	{
		EmitAdded (new T [] { item });
	}

	protected void EmitAdded (T [] items)
	{
		EmitEvent (ItemsAdded, new DbItemEventArgs<T> (items));
	}

	protected void EmitChanged (T item)
	{
		EmitChanged (new T [] { item });
	}

	protected void EmitChanged (T [] items)
	{
		EmitChanged (items, new DbItemEventArgs<T> (items));
	}

	protected void EmitChanged (T [] items, DbItemEventArgs<T> args)
	{
		EmitEvent (ItemsChanged, args);
	}

	protected void EmitRemoved (T item)
	{
		EmitRemoved (new T [] { item });
	}

	protected void EmitRemoved (T [] items)
	{
		EmitEvent (ItemsRemoved, new DbItemEventArgs<T> (items));
	}

	private void EmitEvent (EventHandler<DbItemEventArgs<T>> evnt, DbItemEventArgs<T> args)
	{
		if (evnt == null) // No subscribers.
			return;

		ThreadAssist.ProxyToMain (() => {
			evnt (this, args);
		});
	}

	public bool CacheEmpty {
		get {
			return item_cache.Count == 0;
		}
	}


	FSpotDatabaseConnection database;
	protected FSpotDatabaseConnection Database {
		get {
			return database;
		}
	}


	// Constructor.

	public DbStore (FSpotDatabaseConnection database,
			bool cache_is_immortal)
	{
		this.database = database;
		this.cache_is_immortal = cache_is_immortal;

		item_cache = new Dictionary<uint, object> ();
	}


	// Abstract methods.

	public abstract T Get (uint id);
	public abstract void Remove (T item);
	// If you have made changes to "obj", you have to invoke Commit() to have the changes
	// saved into the database.
	public abstract void Commit (T item);
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
			Database.Execute(query);
		}
	}

	FSpotDatabaseConnection database;
	public FSpotDatabaseConnection Database {
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

		database = new FSpotDatabaseConnection (path);

		// Load or create the meta table
 		meta_store = new MetaStore (Database, new_db);

		// Update the database schema if necessary
		FSpot.Database.Updater.Run (Database);

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
}


