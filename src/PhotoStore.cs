/*
 * PhotoStore.cs
 *
 * Author(s):
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Larry Ewing <lewing@gnome.org>
 *	Stephane Delcroix <stephane@delcroix.org>
 * 
 * This is free software. See COPYING for details.
 */

using Gnome;
using Gnome.Vfs;
using Gdk;
using Gtk;

using Mono.Data.SqliteClient;
using Mono.Unix;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

using FSpot;
using FSpot.Query;
using FSpot.Utils;

using Banshee.Database;


public class PhotoStore : DbStore {
	public int TotalPhotos {
		get {
			SqliteDataReader reader = Database.Query("SELECT COUNT(*) FROM photos");
			reader.Read ();
			int total = Convert.ToInt32 (reader [0]);
			reader.Close ();
			return total;
		}
	}

	public static ThumbnailFactory ThumbnailFactory = new ThumbnailFactory (ThumbnailSize.Large);

	// FIXME this is a hack.  Since we don't have Gnome.ThumbnailFactory.SaveThumbnail() in
	// GTK#, and generate them by ourselves directly with Gdk.Pixbuf, we have to make sure here
	// that the "large" thumbnail directory exists.
	private static void EnsureThumbnailDirectory ()
	{
		string large_thumbnail_file_name_template = Thumbnail.PathForUri ("file:///boo", ThumbnailSize.Large);
		string large_thumbnail_directory_path = System.IO.Path.GetDirectoryName (large_thumbnail_file_name_template);

		if (! System.IO.File.Exists (large_thumbnail_directory_path))
			System.IO.Directory.CreateDirectory (large_thumbnail_directory_path);
	}

	//
	// Generates the thumbnail, returns the Pixbuf, and also stores it as a side effect
	//

	public static Pixbuf GenerateThumbnail (System.Uri uri)
	{
		using (FSpot.ImageFile img = FSpot.ImageFile.Create (uri)) {
			return GenerateThumbnail (uri, img);
		}
	}

	public static Pixbuf GenerateThumbnail (System.Uri uri, ImageFile img)
	{
		Pixbuf thumbnail = null;

		if (img is FSpot.IThumbnailContainer) {
			try {
				thumbnail = ((FSpot.IThumbnailContainer)img).GetEmbeddedThumbnail ();
			} catch (Exception e) {
				Log.DebugFormat ("Exception while loading embedded thumbail {0}", e.ToString ());
			}
		}

		// Save embedded thumbnails in a silightly invalid way so that we know to regnerate them later.
		if (thumbnail != null) {
			PixbufUtils.SaveAtomic (thumbnail, FSpot.ThumbnailGenerator.ThumbnailPath (uri), 
						"png", new string [] { null} , new string [] { null});
			//FIXME with gio, set it to uri time minus a few sec
			System.IO.File.SetLastWriteTime (FSpot.ThumbnailGenerator.ThumbnailPath (uri), new DateTime (1980, 1, 1));
		} else 
			thumbnail = FSpot.ThumbnailGenerator.Create (uri);
		
		return thumbnail;
	}

	public static void DeleteThumbnail (System.Uri uri)
	{
		string path = Thumbnail.PathForUri (uri.ToString (), ThumbnailSize.Large);
		if (System.IO.File.Exists (path))
			System.IO.File.Delete (path);
	}

	public static void MoveThumbnail (string old_path, string new_path)
	{
		System.IO.File.Move (ThumbnailGenerator.ThumbnailPath (UriUtils.PathToFileUri (old_path)),
			   ThumbnailGenerator.ThumbnailPath(UriUtils.PathToFileUri (new_path)));
	}

	// Constructor

	public PhotoStore (QueuedSqliteDatabase database, bool is_new)
		: base (database, false)
	{
		EnsureThumbnailDirectory ();

		if (! is_new)
			return;
		
		Database.ExecuteNonQuery ( 
			//WARNING: if you change this schema, reflect your changes 
			//to Updater.cs, at revision 7.0
			"CREATE TABLE photos (                                     " +
			"	id                 INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,   " +
			"	time               INTEGER NOT NULL,	   	   " +
			"	uri		   STRING NOT NULL,		   " +
			"	description        TEXT NOT NULL,	           " +
			"	roll_id            INTEGER NOT NULL,		   " +
			"	default_version_id INTEGER NOT NULL,		   " +
			"	rating		   INTEGER NULL,		   " +
			"	md5_sum		   TEXT NULL  			   " +
			")");


		Database.ExecuteNonQuery (
			"CREATE TABLE photo_tags (        " +
			"	photo_id      INTEGER,    " +
			"       tag_id        INTEGER,    " +
			"       UNIQUE (photo_id, tag_id) " +
			")");


		Database.ExecuteNonQuery (
			"CREATE TABLE photo_versions (		"+
			"	photo_id	INTEGER,	" +
			"	version_id	INTEGER,	" +
			"	name		STRING,		" +
			"	uri		STRING NOT NULL," +
			"	md5_sum		STRING NOT NULL," +
			"	protected	BOOLEAN,	" +
			"	UNIQUE (photo_id, version_id)	" +
			")");

		Database.ExecuteNonQuery ("CREATE INDEX idx_photo_versions_id ON photo_versions(photo_id)");
		Database.ExecuteNonQuery ("CREATE INDEX idx_photos_roll_id ON photos(roll_id)");
	}

	public Photo CheckForDuplicate (System.Uri uri) {
		// Here we can go wild in comparing photos,
		// for now we check on uri and md5
		Photo found = GetByUri (uri);
		
		if (found != null)
		 	return found;

		string md5 = Photo.GenerateMD5 (uri);			
		Gnome.Vfs.FileInfo info = new Gnome.Vfs.FileInfo (uri.ToString (), FileInfoOptions.GetMimeType);

		Photo[] md5_matches = GetByMD5 (md5);

		foreach (Photo match in md5_matches)
		{
		 	Gnome.Vfs.FileInfo match_info = new Gnome.Vfs.FileInfo (match.DefaultVersionUri.ToString (), FileInfoOptions.GetMimeType);

			// same mimetype?
			if (info.MimeType != match_info.MimeType)
			 	continue;

			// other comparisons?

			// TODO? load pixbuf and compare sizes?	

			return match;
		}

		return null;
	}

	[Obsolete ("Use Create (Uri, uint, out Pixbuf) instead")]
	public Photo Create (string path, uint roll_id, out Pixbuf thumbnail)
	{
		return Create (path, path, roll_id, out thumbnail);
	}

	[Obsolete ("Use Create (Uri, Uri, uint, out Pixbuf) instead")]
	public Photo Create (string new_path, string orig_path, uint roll_id, out Pixbuf thumbnail)
	{
		return Create (UriUtils.PathToFileUri (new_path), UriUtils.PathToFileUri (orig_path), roll_id, out thumbnail);
	}

	public Photo Create (System.Uri uri, uint roll_id, out Pixbuf thumbnail)
	{
		return Create (uri, uri, roll_id, out thumbnail);
	}

	public Photo Create (System.Uri new_uri, System.Uri orig_uri, uint roll_id, out Pixbuf thumbnail)
	{
		Photo photo;
		using (FSpot.ImageFile img = FSpot.ImageFile.Create (orig_uri)) {
			long unix_time = DbUtils.UnixTimeFromDateTime (img.Date);
			string description = img.Description != null  ? img.Description.Split ('\0') [0] : String.Empty;
			string md5_sum = Photo.GenerateMD5 (new_uri);

	 		uint id = (uint) Database.Execute (
				new DbCommand (
					"INSERT INTO photos (time, uri, description, roll_id, default_version_id, rating, md5_sum) "	+
	 				"VALUES (:time, :uri, :description, :roll_id, :default_version_id, :rating, :md5_sum)",
	 				"time", unix_time,
					"uri", new_uri.OriginalString,
	 				"description", description,
					"roll_id", roll_id,
	 				"default_version_id", Photo.OriginalVersionId,
					"rating", "0",
					"md5_sum", md5_sum
				)
			);
	
			photo = new Photo (id, unix_time, new_uri, md5_sum);
			AddToCache (photo);
			photo.Loaded = true;
	
			thumbnail = GenerateThumbnail (new_uri, img);		
			EmitAdded (photo);
		}
		return photo;
	}


	private void GetVersions (Photo photo)
	{
		SqliteDataReader reader = Database.Query(
			new DbCommand("SELECT version_id, name, uri, md5_sum, protected " + 
				      "FROM photo_versions " + 
				      "WHERE photo_id = :id", 
				      "id", photo.Id
			)
		);

		while (reader.Read ()) {
			uint version_id = Convert.ToUInt32 (reader [0]);
			string name = reader[1].ToString ();
#if MONO_2_0
			System.Uri uri = new System.Uri (reader[2].ToString ());
#else
			System.Uri uri = new System.Uri (reader[2].ToString (), true);
#endif
			string md5_sum = reader[3].ToString ();
			bool is_protected = Convert.ToBoolean (reader[4]);
			photo.AddVersionUnsafely (version_id, uri, md5_sum, name, is_protected);
		}
		reader.Close();
	}

	private void GetTags (Photo photo)
	{
		SqliteDataReader reader = Database.Query(new DbCommand("SELECT tag_id FROM photo_tags WHERE photo_id = :id", "id", photo.Id));

		while (reader.Read ()) {
			uint tag_id = Convert.ToUInt32 (reader [0]);
			Tag tag = Core.Database.Tags.Get (tag_id) as Tag;
			photo.AddTagUnsafely (tag);
		}
		reader.Close();
	}		
	
	private void GetAllVersions  () {
		SqliteDataReader reader = Database.Query("SELECT photo_id, version_id, name, uri, md5_sum, protected FROM photo_versions");
		
		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			Photo photo = LookupInCache (id) as Photo;
				
			if (photo == null) {
				//Console.WriteLine ("Photo {0} not found", id);
				continue;
			}
				
			if (photo.Loaded) {
				//Console.WriteLine ("Photo {0} already Loaded", photo);
				continue;
			}

			if (reader [1] != null) {
				uint version_id = Convert.ToUInt32 (reader [1]);
				string name = reader[2].ToString ();
#if MONO_2_0
				System.Uri uri = new System.Uri (reader[3].ToString ());
#else
				System.Uri uri = new System.Uri (reader[3].ToString (), true);
#endif
				string md5_sum = reader[4].ToString ();
				bool is_protected = Convert.ToBoolean (reader[5]);	
				photo.AddVersionUnsafely (version_id, uri, md5_sum, name, is_protected);
			}

			/*
			string directory_path = null;
			if (reader [3] != null)
				directory_path = reader [3].ToString ();
			System.Console.WriteLine ("directory_path = {0}", directory_path);
			*/
		}
		reader.Close();
	}

	private void GetAllTags () {
		SqliteDataReader reader = Database.Query("SELECT photo_id, tag_id FROM photo_tags");

		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			Photo photo = LookupInCache (id) as Photo;
				
			if (photo == null) {
				//Console.WriteLine ("Photo {0} not found", id);
				continue;
			}
				
			if (photo.Loaded) {
				//Console.WriteLine ("Photo {0} already Loaded", photo.Id);
				continue;
			}

		        if (reader [1] != null) {
				uint tag_id = Convert.ToUInt32 (reader [1]);
				Tag tag = Core.Database.Tags.Get (tag_id) as Tag;
				photo.AddTagUnsafely (tag);
			}
		}
		reader.Close();
	}

	public override DbItem Get (uint id)
	{
		Photo photo = LookupInCache (id) as Photo;
		if (photo != null)
			return photo;

		SqliteDataReader reader = Database.Query(
			new DbCommand("SELECT time, uri, description, roll_id, default_version_id, rating, md5_sum " + 
				      "FROM photos " + 
				      "WHERE id = :id", "id", id
				     )
		);

		if (reader.Read ()) {
			photo = new Photo (id,
				Convert.ToInt64 (reader [0]),
#if MONO_2_0
				new System.Uri (reader [1].ToString ()),
#else
				new System.Uri (reader [1].ToString (), true),
#endif
				reader[6] != null ? reader[6].ToString () : null
			);

			photo.Description = reader[2].ToString ();
			photo.RollId = Convert.ToUInt32 (reader[3]);
			photo.DefaultVersionId = Convert.ToUInt32 (reader[4]);
			photo.Rating = Convert.ToUInt32 (reader [5]);
			AddToCache (photo);
		}
		reader.Close();

		if (photo == null)
			return null;
		
		GetTags (photo);
		GetVersions (photo);

		return photo;
	}

	[Obsolete ("Use GetByUri instead")]
	public Photo GetByPath (string path)
	{
		return GetByUri (UriUtils.PathToFileUri (path));
	}

	public Photo GetByUri (System.Uri uri)
	{
		Photo photo = null;

		uint timer = Log.DebugTimerStart ();

		SqliteDataReader reader = Database.Query (new DbCommand ("SELECT id, time, description, roll_id, default_version_id, rating, photos.md5_sum AS md5_sum " +
									 " FROM photos " +
									 " LEFT JOIN photo_versions AS pv ON photos.id = pv.photo_id" +
							                 " WHERE photos.uri = :uri OR pv.uri = :uri", "uri", uri.ToString ()));

		if (reader.Read ()) {
			photo = new Photo (Convert.ToUInt32 (reader [0]),
					   Convert.ToInt64 (reader [1]),
					   uri,
					   reader[6] != null ? reader[6].ToString () : null);

			photo.Description = reader[2].ToString ();
			photo.RollId = Convert.ToUInt32 (reader[3]);
			photo.DefaultVersionId = Convert.ToUInt32 (reader[4]);
			photo.Rating = Convert.ToUInt32 (reader [5]);
		}
	        reader.Close();
		Log.DebugTimerPrint (timer, "GetByUri query took {0}");

		if (photo == null)
			return null;

		Photo cached = LookupInCache (photo.Id) as Photo;

		if (cached != null)
			return cached;

		AddToCache (photo);
	
		GetTags (photo);
		GetVersions (photo);

		return photo;
	}

	public Photo[] GetByMD5 (string md5_sum)
	{
		List<Photo> photos = new List<Photo> ();
		
		SqliteDataReader reader = Database.Query (
			new DbCommand ("SELECT DISTINCT " + 
				       "id, time, photos.uri AS uri, description, roll_id, default_version_id, rating " + 
				       "FROM photos " + 
				       "LEFT JOIN photo_versions " + 
				       "ON   photos.id = photo_versions.photo_id " +
				       "WHERE photos.md5_sum = :md5_sum " +
				       "OR photo_versions.md5_sum = :md5_sum", 
				       "md5_sum", md5_sum
				      )
		);

		while (reader.Read ()) {
			Photo photo = new Photo (Convert.ToUInt32 (reader [0]),
				Convert.ToInt64 (reader [1]),
#if MONO_2_0
				new System.Uri (reader [2].ToString ()),
#else
				new System.Uri (reader [2].ToString (), true),
#endif
				md5_sum
			);

			photo.Description = reader[3].ToString ();
			photo.RollId = Convert.ToUInt32 (reader[4]);
			photo.DefaultVersionId = Convert.ToUInt32 (reader[5]);
			photo.Rating = Convert.ToUInt32 (reader [6]);
			photo.MD5Sum = md5_sum;

			// get cached if possible
			Photo cached = LookupInCache (photo.Id) as Photo;

			if (cached != null)
			{
				photos.Add (cached);
				continue;
			}

			// Add to cache and fully load if not found in cache
			AddToCache (photo);
	
			GetTags (photo);
			GetVersions (photo);

			// add to collection
			photos.Add (photo);
		}

	        reader.Close();

		return photos.ToArray ();
	}

	public void Remove (Tag []tags)
	{
		Photo [] photos = Query (tags, String.Empty, null, null);	

		foreach (Photo photo in photos)
			photo.RemoveCategory (tags);
		Commit (photos);

		foreach (Tag tag in tags)
			Core.Database.Tags.Remove (tag);
		
	}

	public void Remove (Photo []items)
	{
		EmitRemoved (items);

		ArrayList query_builder = new ArrayList (items.Length);
		for (int i = 0; i < items.Length; i++) {
			query_builder.Add (String.Format ("{0}", items[i].Id));
			RemoveFromCache (items[i]);
		}

		String id_list = String.Join ("','", ((string []) query_builder.ToArray (typeof (string))));
		Database.ExecuteNonQuery (String.Format ("DELETE FROM photos WHERE id IN ('{0}')", id_list));
		Database.ExecuteNonQuery (String.Format ("DELETE FROM photo_tags WHERE photo_id IN ('{0}')", id_list));
		Database.ExecuteNonQuery (String.Format ("DELETE FROM photo_versions WHERE photo_id IN ('{0}')", id_list));

	}

	public override void Remove (DbItem item)
	{
		Remove (new Photo [] { (Photo)item });
	}

	public override void Commit (DbItem item)
	{
		Commit (new Photo [] {item as Photo});
	}

	public void Commit (Photo [] items)
	{
		uint timer = Log.DebugTimerStart ();
		// Only use a transaction for multiple saves. Avoids recursive transactions.
		bool use_transactions = !Database.InTransaction && items.Length > 1;

		if (use_transactions)
			Database.BeginTransaction ();

		PhotosChanges changes = new PhotosChanges ();
		foreach (DbItem item in items)
			changes |= Update ((Photo)item);

		if (use_transactions)
			Database.CommitTransaction ();

		EmitChanged (items, new PhotoEventArgs (items, changes));
		Log.DebugTimerPrint (timer, "Commit took {0}");
	}

	private PhotoChanges Update (Photo photo) {
		PhotoChanges changes = photo.Changes;
		// Update photo.
		if (changes.DescriptionChanged || changes.DefaultVersionIdChanged || changes.TimeChanged || changes.UriChanged || changes.RatingChanged || changes.MD5SumChanged )
			Database.ExecuteNonQuery (
				new DbCommand (
					"UPDATE photos " + 
					"SET description = :description, " + 
					"    default_version_id = :default_version_id, " + 
					"    time = :time, " + 
					"    uri = :uri, " +
					"    rating = :rating, " +
					"    md5_sum = :md5_sum	" +
					"WHERE id = :id ",
					"description", photo.Description,
					"default_version_id", photo.DefaultVersionId,
					"time", DbUtils.UnixTimeFromDateTime (photo.Time),
					"uri", photo.VersionUri (Photo.OriginalVersionId).OriginalString,
					"rating", String.Format ("{0}", photo.Rating),
					"md5_sum", photo.MD5Sum,
					"id", photo.Id
				)
			);

		// Update tags.
		if (changes.TagsRemoved != null)
			foreach (Tag tag in changes.TagsRemoved)
				Database.ExecuteNonQuery (new DbCommand (
					"DELETE FROM photo_tags WHERE photo_id = :photo_id AND tag_id = :tag_id",
					"photo_id", photo.Id,
					"tag_id", tag.Id));

		if (changes.TagsAdded != null)
			foreach (Tag tag in changes.TagsAdded)
				Database.ExecuteNonQuery (new DbCommand (
					"INSERT OR IGNORE INTO photo_tags (photo_id, tag_id) " +
					"VALUES (:photo_id, :tag_id)",
					"photo_id", photo.Id,
					"tag_id", tag.Id));

		// Update versions.
		if (changes.VersionsRemoved != null)
			foreach (uint version_id in changes.VersionsRemoved)
				Database.ExecuteNonQuery (new DbCommand (
					"DELETE FROM photo_versions WHERE photo_id = :photo_id AND version_id = :version_id",
					"photo_id", photo.Id,
					"version_id", version_id));

		if (changes.VersionsAdded != null)
			foreach (uint version_id in changes.VersionsAdded) {
				PhotoVersion version = photo.GetVersion (version_id) as PhotoVersion;
				Database.ExecuteNonQuery (new DbCommand (
					"INSERT OR IGNORE INTO photo_versions (photo_id, version_id, name, uri, protected, md5_sum) " +
					"VALUES (:photo_id, :version_id, :name, :uri, :is_protected, :md5_sum)",
					"photo_id", photo.Id,
					"version_id", version_id,
					"name", version.Name,
					"uri", version.Uri.ToString (),
					"is_protected", version.IsProtected,
					"md5_sum", version.MD5Sum));
			}
		if (changes.VersionsModified != null)
			foreach (uint version_id in changes.VersionsModified) {
				PhotoVersion version = photo.GetVersion (version_id) as PhotoVersion;
				Database.ExecuteNonQuery (new DbCommand (
					"UPDATE photo_versions SET name = :name, " +
					"uri = :uri, protected = :protected, md5_sum = :md5_sum " +
					"WHERE photo_id = :photo_id AND version_id = :version_id",
					"name", version.Name,
					"uri", version.Uri.ToString (),
					"protected", version.IsProtected,
					"photo_id", photo.Id,
					"md5_sum", version.MD5Sum,
					"version_id", version_id));
			}
		photo.Changes = null;
		return changes;
	}

	public void UpdateMD5Sum (Photo photo) {
		string md5_sum = Photo.GenerateMD5 (photo.VersionUri (Photo.OriginalVersionId)); 
		photo.MD5Sum = md5_sum;

		Database.ExecuteNonQuery (
			new DbCommand (
				"UPDATE photos " +
				"SET    md5_sum = :md5_sum " +
				"WHERE  ID = :id",
				"md5_sum", md5_sum,
				"id", photo.Id
			)
		);

		foreach (uint version_id in photo.VersionIds) {
			if (version_id == Photo.OriginalVersionId)
			 	continue;

			PhotoVersion version = photo.GetVersion (version_id) as PhotoVersion;

			string version_md5_sum = Photo.GenerateMD5 (version.Uri);

			if (version.MD5Sum == version_md5_sum)
			 	continue;

			version.MD5Sum = version_md5_sum; 
			photo.Changes.ChangeVersion (version_id);
		}

		Commit (photo);
	}
	
	// Dbus
	public event ItemsAddedHandler ItemsAddedOverDBus;
	public event ItemsRemovedHandler ItemsRemovedOverDBus;

	public Photo CreateOverDBus (string new_path, string orig_path, uint roll_id, out Gdk.Pixbuf pixbuf)  {
		Photo photo = Create (new_path, orig_path, roll_id, out pixbuf);
		EmitAddedOverDBus (photo);

		return photo;
	}

	public void RemoveOverDBus (Photo photo) {
	 	Remove (photo);
		EmitRemovedOverDBus (photo);
	}


	protected void EmitAddedOverDBus (Photo photo) {
	 	EmitAddedOverDBus (new Photo [] { photo });
	}

	protected void EmitAddedOverDBus (Photo [] photos) {
	 	if (ItemsAddedOverDBus != null)
		 	ItemsAddedOverDBus (this, new DbItemEventArgs (photos));
	}

	protected void EmitRemovedOverDBus (Photo photo) {
		EmitRemovedOverDBus (new Photo [] { photo });
	}

	protected void EmitRemovedOverDBus (Photo [] photos) {
		if (ItemsRemovedOverDBus != null)
		 	ItemsRemovedOverDBus (this, new DbItemEventArgs (photos)); 
	}

	public int Count (string table_name, params IQueryCondition [] conditions)
	{
		StringBuilder query_builder = new StringBuilder ("SELECT COUNT(*) FROM " + table_name + " ");
		bool where_added = false;
		foreach (IQueryCondition condition in conditions) {
			if (condition == null)
				continue;
			if (condition is IOrderCondition)
				continue;
			query_builder.Append (where_added ? " AND " : " WHERE ");
			query_builder.Append (condition.SqlClause ());
			where_added = true;
		}

		SqliteDataReader reader = Database.Query (query_builder.ToString());
		reader.Read ();
		int count = Convert.ToInt32 (reader [0]);
		reader.Close();
		return count;
	}

	public int [] IndicesOf (string table_name, uint [] items)
	{
		StringBuilder query_builder = new StringBuilder ("SELECT ROWID FROM ");
		query_builder.Append (table_name);
		query_builder.Append (" WHERE id IN (");
		for (int i = 0; i < items.Length; i++) {
			query_builder.Append (items [i]);
			query_builder.Append ((i != items.Length - 1) ? ", " : ")" );
		}
		return IndicesOf (query_builder.ToString ());
	}

	public int IndexOf (string table_name, Photo photo)
	{
		string query = String.Format ("SELECT ROWID FROM {0} WHERE id = {1}", table_name, photo.Id);
		return IndexOf (query);
	}

	public int IndexOf (string table_name, DateTime time, bool asc)
	{
		string query = String.Format ("SELECT ROWID FROM {0} WHERE time {2} {1} ORDER BY time {3} LIMIT 1",
				table_name,
				DbUtils.UnixTimeFromDateTime (time),
				asc ? ">=" : "<=",
				asc ? "ASC" : "DESC");
		return IndexOf (query);
	}

	private int IndexOf (string query)
	{
		uint timer = Log.DebugTimerStart ();
		SqliteDataReader reader = Database.Query (query);
		int index = - 1;
		if (reader.Read ())
			index = Convert.ToInt32 (reader [0]);
		reader.Close();
		Log.DebugTimerPrint (timer, "IndexOf took {0} : " + query);
		return index - 1; //ROWID starts counting at 1
	}

	private int [] IndicesOf (string query)
	{
		uint timer = Log.DebugTimerStart ();
		List<int> list = new List<int> ();
		SqliteDataReader reader = Database.Query (query);
		while (reader.Read ())
			list.Add (Convert.ToInt32 (reader [0]) - 1);
		Log.DebugTimerPrint (timer, "IndicesOf took {0} : " + query);
		return list.ToArray ();
	}

	public Dictionary<int,int[]> PhotosPerMonth (params IQueryCondition [] conditions)
	{
		uint timer = Log.DebugTimerStart ();
		Dictionary<int, int[]> val = new Dictionary<int, int[]> ();

		//Sqlite is way more efficient querying to a temp then grouping than grouping at once
		Database.ExecuteNonQuery ("DROP TABLE IF EXISTS population");
		StringBuilder query_builder = new StringBuilder ("CREATE TEMPORARY TABLE population AS SELECT strftime('%Y%m', datetime(time, 'unixepoch')) AS month FROM photos");
		bool where_added = false;
		foreach (IQueryCondition condition in conditions) {
			if (condition == null)
				continue;
			if (condition is IOrderCondition)
				continue;
			query_builder.Append (where_added ? " AND " : " WHERE ");
			query_builder.Append (condition.SqlClause ());
			where_added = true;
		}
		Database.ExecuteNonQuery (query_builder.ToString ());

		int minyear = Int32.MaxValue;
		int maxyear = Int32.MinValue;

		SqliteDataReader reader = Database.Query ("SELECT COUNT (*), month from population GROUP BY month");
		while (reader.Read ()) {
			string yyyymm = reader [1].ToString ();
			int count = Convert.ToInt32 (reader [0]);
			int year = Convert.ToInt32 (yyyymm.Substring (0,4));
			maxyear = Math.Max (year, maxyear);
			minyear = Math.Min (year, minyear);
			int month = Convert.ToInt32 (yyyymm.Substring (4));
			if (!val.ContainsKey (year))
				val.Add (year, new int[12]);
			val[year][month-1] = count;
		}
		reader.Close ();

		//Fill the blank
		for (int i = minyear; i <= maxyear; i++)
			if (!val.ContainsKey (i))
				val.Add (i, new int[12]);

		Log.DebugTimerPrint (timer, "PhotosPerMonth took {0}");
		return val;
	}

	// Queries.
	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags) {
		return Query (tags, null, null, null, null);
	}

	public Photo [] Query (params IQueryCondition [] conditions)
	{
		StringBuilder query_builder = new StringBuilder ("SELECT * FROM photos ");
		
		bool where_added = false;
		foreach (IQueryCondition condition in conditions) {
			if (condition == null)
				continue;
			if (condition is IOrderCondition)
				continue;
			query_builder.Append (where_added ? " AND " : " WHERE ");
			query_builder.Append (condition.SqlClause ());
			where_added = true;
		}

		bool order_added = false;
		foreach (IQueryCondition condition in conditions) {
			if (condition == null)
				continue;
			if (!(condition is IOrderCondition))
				continue;
			query_builder.Append (order_added ? " , " : "ORDER BY ");
			query_builder.Append (condition.SqlClause ());
			order_added = true;
		}
		return Query (query_builder.ToString ());
	}

	public void QueryToTemp (string temp_table, params IQueryCondition [] conditions)
	{
		StringBuilder query_builder = new StringBuilder ("SELECT * FROM photos ");

		bool where_added = false;
		foreach (IQueryCondition condition in conditions) {
			if (condition == null)
				continue;
			if (condition is IOrderCondition)
				continue;
			query_builder.Append (where_added ? " AND " : " WHERE ");
			query_builder.Append (condition.SqlClause ());
			where_added = true;
		}

		bool order_added = false;
		foreach (IQueryCondition condition in conditions) {
			if (condition == null)
				continue;
			if (!(condition is IOrderCondition))
				continue;
			query_builder.Append (order_added ? " , " : "ORDER BY ");
			query_builder.Append (condition.SqlClause ());
			order_added = true;
		}
		QueryToTemp (temp_table, query_builder.ToString ());
	}

	public void QueryToTemp(string temp_table, string query)
	{
		uint timer = Log.DebugTimerStart ();
		Database.BeginTransaction ();
		Database.ExecuteNonQuery (String.Format ("DROP TABLE IF EXISTS {0}", temp_table));
		//Database.ExecuteNonQuery (String.Format ("CREATE TEMPORARY TABLE {0} AS {1}", temp_table, query));
		Database.Query (String.Format ("CREATE TEMPORARY TABLE {0} AS {1}", temp_table, query)).Close ();
		Database.CommitTransaction ();
		Log.DebugTimerPrint (timer, "QueryToTemp took {0} : " + query);
	}

	public Photo [] QueryFromTemp (string temp_table)
	{
		return QueryFromTemp (temp_table, 0, -1);
	}

	public Photo [] QueryFromTemp (string temp_table, int offset, int limit)
	{
		return Query (String.Format ("SELECT * FROM {0} LIMIT {1} OFFSET {2}", temp_table, limit, offset));
	}

	public Photo [] Query (string query)
	{
		return Query (new DbCommand (query));
	}

	private Photo [] Query (DbCommand query)
	{
		uint timer = Log.DebugTimerStart ();
		SqliteDataReader reader = Database.Query(query);

		List<Photo> new_photos = new List<Photo> ();
		List<Photo> query_result = new List<Photo> ();
		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			Photo photo = LookupInCache (id) as Photo;

			if (photo == null) {
				photo = new Photo (id,
						   Convert.ToInt64 (reader [1]),
#if MONO_2_0
						   new System.Uri (reader [2].ToString ()),
#else
						   new System.Uri (reader [2].ToString (), true),
#endif
						   reader[7] != null ? reader [7].ToString () : null
				);
				photo.Description = reader[3].ToString ();
				photo.RollId = Convert.ToUInt32 (reader[4]);
				photo.DefaultVersionId = Convert.ToUInt32 (reader[5]);
				photo.Rating = Convert.ToUInt32 (reader [6]);
				new_photos.Add (photo);
			}

			query_result.Add (photo);
		}
		reader.Close();

		bool need_load = false;
		foreach (Photo photo in new_photos) {
			AddToCache (photo);
			need_load |= !photo.Loaded;
		}
		
		if (need_load) {
			GetAllTags ();
			GetAllVersions ();
			foreach (Photo photo in new_photos)
				photo.Loaded = true;
		} else {
			//Console.WriteLine ("Skipped Loading Data");
		}
		foreach (Photo photo in new_photos)
			photo.Changes = null;

 		Log.DebugTimerPrint (timer, "Query took {0} : " + query.CommandText);
		return query_result.ToArray ();
	}

//	[Obsolete ("No longer make any sense with uris...")]
//	public Photo [] Query (System.IO.DirectoryInfo dir)
//	{
//		return Query (new DbCommand (
//			"SELECT photos.id, "			+
//				"photos.time, "			+
//				"photos.uri, "			+
//				"photos.description, "		+
//				"photos.roll_id, "		+
//				"photos.default_version_id, "	+
//				"photos.rating "		+
//			"FROM photos " 				+
//			"WHERE uri LIKE \"file://:dir%\" "	+
//			"AND uri NOT LIKE \"file://:dir/%/%\"",
//			"dir", dir.FullName ));
//	}

	public Photo [] Query (System.Uri uri)
	{
		Log.DebugFormat ("Query Uri {0}", uri);
		return Query (new DbCommand (
			"SELECT id, "			+
				"time, "			+
				"uri, "			+
				"description, "		+
				"roll_id, "		+
				"default_version_id, "	+
				"rating, "		+
				"md5_sum "		+
			"FROM photos " 				+
			"WHERE uri LIKE :uri "		+
			"AND uri NOT LIKE :uri_",
			"uri", uri.ToString () + "%",
			"uri_", uri.ToString () + "/%/%"));
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange)
	{
		return Query (FSpot.OrTerm.FromTags(tags), extra_condition, range, importidrange, null);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public void QueryToTemp (string temp_table, Tag [] tags, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange, OrderByTime orderbytime)
	{
		QueryToTemp (temp_table, FSpot.OrTerm.FromTags(tags), extra_condition, range, importidrange, ratingrange, orderbytime);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange)
	{
		return Query (FSpot.OrTerm.FromTags(tags), extra_condition, range, importidrange, ratingrange);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public void QueryToTemp (string temp_table, Term searchexpression, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange, OrderByTime orderbytime)
	{
		bool hide = (extra_condition == null);

		// The SQL query that we want to construct is:
		//
		// SELECT photos.id
		//        photos.time
		//        photos.uri,
		//        photos.description,
		//	  photos.roll_id,
		//        photos.default_version_id
		//        photos.rating
		//                  FROM photos, photo_tags
		//		    WHERE photos.time >= time1 AND photos.time <= time2
		//				AND photos.rating >= rat1 AND photos.rating <= rat2
		//				AND photos.id NOT IN (select photo_id FROM photo_tags WHERE tag_id = HIDDEN)
		//				AND photos.id IN (select photo_id FROM photo_tags where tag_id IN (tag1, tag2..)
		//				AND extra_condition_string
		//                  GROUP BY photos.id

		StringBuilder query_builder = new StringBuilder ();
		ArrayList where_clauses = new ArrayList ();
		query_builder.Append ("SELECT id, " 			+
					     "time, "			+
					     "uri, "			+
					     "description, "		+
					     "roll_id, "   		+
					     "default_version_id, "	+
					     "rating, "			+
					     "md5_sum "			+
				      "FROM photos ");

		if (range != null) {
			where_clauses.Add (String.Format ("time >= {0} AND time <= {1}",
							  DbUtils.UnixTimeFromDateTime (range.Start),
							  DbUtils.UnixTimeFromDateTime (range.End)));

		}

		if (ratingrange != null) {
			where_clauses.Add (ratingrange.SqlClause ());
		}

		if (importidrange != null) {
			where_clauses.Add (importidrange.SqlClause ());
		}

		if (hide && Core.Database.Tags.Hidden != null) {
			where_clauses.Add (String.Format ("id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})",
							  FSpot.Core.Database.Tags.Hidden.Id));
		}

		if (searchexpression != null) {
			where_clauses.Add (searchexpression.SqlCondition ());
		}

		if (extra_condition != null && extra_condition.Trim () != String.Empty) {
			where_clauses.Add (extra_condition);
		}

		if (where_clauses.Count > 0) {
			query_builder.Append (" WHERE ");
			query_builder.Append (String.Join (" AND ", ((String []) where_clauses.ToArray (typeof(String)))));
		}
		query_builder.Append (" ORDER BY ");
		query_builder.Append (orderbytime.SqlClause ());
		QueryToTemp (temp_table, query_builder.ToString ());
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Term searchexpression, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange)
	{
		bool hide = (extra_condition == null);

		// The SQL query that we want to construct is:
		//
		// SELECT photos.id
		//        photos.time
		//        photos.uri,
		//        photos.description,
		//	  photos.roll_id,
		//        photos.default_version_id
		//        photos.rating
		//                  FROM photos, photo_tags
		//		    WHERE photos.time >= time1 AND photos.time <= time2
		//				AND photos.rating >= rat1 AND photos.rating <= rat2
		//				AND photos.id NOT IN (select photo_id FROM photo_tags WHERE tag_id = HIDDEN)
		//				AND photos.id IN (select photo_id FROM photo_tags where tag_id IN (tag1, tag2..)
		//				AND extra_condition_string
		//                  GROUP BY photos.id
		
		StringBuilder query_builder = new StringBuilder ();
		ArrayList where_clauses = new ArrayList ();
		query_builder.Append ("SELECT id, " 			+
					     "time, "			+
					     "uri, "			+
					     "description, "		+
				      	     "roll_id, "   		+
					     "default_version_id, "	+
					     "rating, "			+
					     "md5_sum "			+
				      "FROM photos ");
		
		if (range != null) {
			where_clauses.Add (String.Format ("time >= {0} AND time <= {1}",
							  DbUtils.UnixTimeFromDateTime (range.Start), 
							  DbUtils.UnixTimeFromDateTime (range.End)));

		}

		if (ratingrange != null) {
			where_clauses.Add (ratingrange.SqlClause ());
		}

		if (importidrange != null) {
			where_clauses.Add (importidrange.SqlClause ());
		}		
		
		if (hide && Core.Database.Tags.Hidden != null) {
			where_clauses.Add (String.Format ("id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})", 
							  FSpot.Core.Database.Tags.Hidden.Id));
		}
		
		if (searchexpression != null) {
			where_clauses.Add (searchexpression.SqlCondition ());
		}

		if (extra_condition != null && extra_condition.Trim () != String.Empty) {
			where_clauses.Add (extra_condition);
		}
		
		if (where_clauses.Count > 0) {
			query_builder.Append (" WHERE ");
			query_builder.Append (String.Join (" AND ", ((String []) where_clauses.ToArray (typeof(String)))));
		}
		query_builder.Append (" ORDER BY time");
		return Query (query_builder.ToString ());
	}
}
