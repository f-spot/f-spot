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
				System.Console.WriteLine (e.ToString ());
			}
		}

		// Save embedded thumbnails in a silightly invalid way so that we know to regnerate them later.
		if (thumbnail != null) 
			PixbufUtils.SaveAtomic (thumbnail, FSpot.ThumbnailGenerator.ThumbnailPath (uri), 
						"png", new string [] { null} , new string [] { null});
		else 
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
			"	rating		   INTEGER NULL			   " +
			")");


		Database.ExecuteNonQuery (
			"CREATE TABLE photo_tags (     " +
			"	photo_id      INTEGER, " +
			"       tag_id        INTEGER  " +
			")");


		Database.ExecuteNonQuery (
			//WARNING: if you change this schema, reflect your changes 
			//to Updater.cs, at revision 8.0
			"CREATE TABLE photo_versions (    	" +
			"       photo_id        INTEGER,  	" +
			"       version_id      INTEGER,  	" +
			"       name            STRING,    	" +
			"	uri		STRING NOT NULL," +
			"	protected	BOOLEAN		" +
			")");
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
	
	 		uint id = (uint) Database.Execute (new DbCommand (
				"INSERT INTO photos (time, uri, description, roll_id, default_version_id, rating) "	+
	 			"VALUES (:time, :uri, :description, :roll_id, :default_version_id, :rating)",
	 			"time", unix_time,
				"uri", new_uri.OriginalString,
	 			"description", description,
				"roll_id", roll_id,
	 			"default_version_id", Photo.OriginalVersionId,
				"rating", "0"));
	
			photo = new Photo (id, unix_time, new_uri);
			AddToCache (photo);
			photo.Loaded = true;
	
			thumbnail = GenerateThumbnail (new_uri, img);		
			EmitAdded (photo);
		}
		return photo;
	}


	private void GetVersions (Photo photo)
	{
		SqliteDataReader reader = Database.Query(new DbCommand("SELECT version_id, name, uri, protected FROM photo_versions WHERE photo_id = :id", "id", photo.Id));

		while (reader.Read ()) {
			uint version_id = Convert.ToUInt32 (reader [0]);
			string name = reader[1].ToString ();
#if MONO_2_0
			System.Uri uri = new System.Uri (reader[2].ToString ());
#else
			System.Uri uri = new System.Uri (reader[2].ToString (), true);
#endif
			bool is_protected = Convert.ToBoolean (reader[3]);
			photo.AddVersionUnsafely (version_id, uri, name, is_protected);
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
		SqliteDataReader reader = Database.Query("SELECT photo_id, version_id, name, uri, protected FROM photo_versions");
		
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
				bool is_protected = Convert.ToBoolean (reader[4]);	
				photo.AddVersionUnsafely (version_id, uri, name, is_protected);
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

		SqliteDataReader reader = Database.Query(new DbCommand("SELECT time, uri, description, roll_id, default_version_id, rating "
			+ "FROM photos WHERE id = :id", "id", id));

		if (reader.Read ()) {
			photo = new Photo (id,
				Convert.ToInt64 (reader [0]),
#if MONO_2_0
				new System.Uri (reader [1].ToString ()));
#else
				new System.Uri (reader [1].ToString (), true));
#endif

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

		SqliteDataReader reader = Database.Query (new DbCommand ("SELECT id, time, description, roll_id, default_version_id, rating FROM photos "
                + "WHERE uri = :uri", "uri", uri.ToString ()));

		if (reader.Read ()) {
			photo = new Photo (Convert.ToUInt32 (reader [0]),
					   Convert.ToInt64 (reader [1]),
					   uri);

			photo.Description = reader[2].ToString ();
			photo.RollId = Convert.ToUInt32 (reader[3]);
			photo.DefaultVersionId = Convert.ToUInt32 (reader[4]);
			photo.Rating = Convert.ToUInt32 (reader [5]);
		}
	        reader.Close();

		if (photo == null)
			return null;

		if (LookupInCache (photo.Id) as Photo != null)
			return LookupInCache (photo.Id) as Photo;

		AddToCache (photo);
	
		GetTags (photo);
		GetVersions (photo);

		return photo;
	}

	public void Remove (Tag []tags)
	{
		Photo [] photos = Query (tags, String.Empty, null, null);	

		foreach (Photo photo in photos) {
			photo.RemoveCategory (tags);
			Commit (photo);
		}
		
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
		DbItemEventArgs args = new DbItemEventArgs (item);
		Commit (args.Items, args);
	}

	public void Commit (DbItem [] items, DbItemEventArgs args)
	{
		if (items.Length > 1)
			Database.BeginTransaction ();

		foreach (DbItem item in items)
			Update ((Photo)item);
		
		EmitChanged (items, args);

		if (items.Length > 1)
			Database.CommitTransaction ();
	}
	
	private void Update (Photo photo) {
		// Update photo.

		Database.ExecuteNonQuery (new DbCommand (
			"UPDATE photos SET description = :description, " + 
			"default_version_id = :default_version_id, " + 
			"time = :time, " + 
			"uri = :uri, " +
			"rating = :rating " +
			"WHERE id = :id ",
			"description", photo.Description,
			"default_version_id", photo.DefaultVersionId,
			"time", DbUtils.UnixTimeFromDateTime (photo.Time),
			"uri", photo.VersionUri (Photo.OriginalVersionId).OriginalString,
			"rating", String.Format ("{0}", photo.Rating),
			"id", photo.Id));

		// Update tags.

		Database.ExecuteNonQuery (new DbCommand ("DELETE FROM photo_tags WHERE photo_id = :id", "id", photo.Id));

		foreach (Tag tag in photo.Tags) {
			Database.ExecuteNonQuery (new DbCommand ("INSERT INTO photo_tags (photo_id, tag_id) " +
                                         " VALUES (:photo_id, :tag_id)",
                                         "photo_id", photo.Id, "tag_id", tag.Id));
		}

		// Update versions.

		Database.ExecuteNonQuery (new DbCommand ("DELETE FROM photo_versions WHERE photo_id = :id", "id", photo.Id));

		foreach (uint version_id in photo.VersionIds) {
			if (version_id == Photo.OriginalVersionId)
				continue;
			PhotoVersion version = photo.GetVersion (version_id) as PhotoVersion;

			Database.ExecuteNonQuery(new DbCommand ("INSERT INTO photo_versions (photo_id, version_id, name, uri, protected) " +
							     "       VALUES (:photo_id, :version_id, :name, :uri, :is_protected)",
							     "photo_id", photo.Id, "version_id", version.VersionId,
							     "name", version.Name, "uri", version.Uri.ToString (), "is_protected", version.IsProtected));
		}
	}
	
	// Dbus
	public event ItemsAddedHandler ItemsAddedOverDBus;
	public event ItemsRemovedHandler ItemsRemovedOverDBus;

	internal Photo CreateOverDBus (string new_path, string orig_path, uint roll_id, out Gdk.Pixbuf pixbuf)  {
		Photo photo = Create (new_path, orig_path, roll_id, out pixbuf);
		EmitAddedOverDBus (photo);

		return photo;
	}

	internal void RemoveOverDBus (Photo photo) {
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


	// Queries.
	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags) {
		return Query (tags, null, null, null, null);
	}

	public Photo [] Query (params IQueryCondition [] conditions)
	{
		StringBuilder query_builder = new StringBuilder ("SELECT * FROM photos ");
		
		bool where_added = false;
		foreach (IQueryCondition condition in conditions)
			if (condition != null) {
				query_builder.Append (where_added ? " AND " : " WHERE ");
				query_builder.Append (condition.SqlClause ());
				where_added = true;
			}
		query_builder.Append(" ORDER BY time ");
		return Query (query_builder.ToString ());
	}

	public Photo [] Query (string query)
	{
		SqliteDataReader reader = Database.Query(query);

		ArrayList version_list = new ArrayList ();
		ArrayList id_list = new ArrayList ();
		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			Photo photo = LookupInCache (id) as Photo;

			if (photo == null) {
				photo = new Photo (id,
						   Convert.ToInt64 (reader [1]),
#if MONO_2_0
						   new System.Uri (reader [2].ToString ()));
#else
						   new System.Uri (reader [2].ToString (), true));
#endif
				
				photo.Description = reader[3].ToString ();
				photo.RollId = Convert.ToUInt32 (reader[4]);
				photo.DefaultVersionId = Convert.ToUInt32 (reader[5]);
				photo.Rating = Convert.ToUInt32 (reader [6]);
				
				version_list.Add (photo);
			}

			id_list.Add (photo);
		}
		reader.Close();

		bool need_load = false;
		foreach (Photo photo in version_list) {
			AddToCache (photo);
			need_load |= !photo.Loaded;
		}
		
		if (need_load) {
			GetAllTags ();
			GetAllVersions ();
			foreach (Photo photo in version_list) {
				photo.Loaded = true;
			}
		} else {
			//Console.WriteLine ("Skipped Loading Data");
		}

		return id_list.ToArray (typeof (Photo)) as Photo [];
	}

	[Obsolete ("No longer make any sense with uris...")]
	public Photo [] Query (System.IO.DirectoryInfo dir)
	{
		string query_string = String.Format (
			"SELECT photos.id, "			+
				"photos.time, "			+
				"photos.uri, "			+
				"photos.description, "		+
				"photos.roll_id, "		+
				"photos.default_version_id, "	+
				"photos.rating "		+
			"FROM photos " 				+
			"WHERE uri LIKE \"file://{0}%\" "	+
			"AND uri NOT LIKE \"file://{0}/%/%\"" , dir.FullName);

		return Query (query_string);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange)
	{
		return Query (FSpot.OrTerm.FromTags(tags), extra_condition, range, importidrange, null);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange)
	{
		return Query (FSpot.OrTerm.FromTags(tags), extra_condition, range, importidrange, ratingrange);
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
		query_builder.Append ("SELECT photos.id, " 			+
					     "photos.time, "			+
					     "photos.uri, "			+
					     "photos.description, "		+
				      	     "photos.roll_id, "   		+
					     "photos.default_version_id, "	+
					     "photos.rating "			+
				      "FROM photos ");
		
		if (range != null) {
			where_clauses.Add (String.Format ("photos.time >= {0} AND photos.time <= {1}",
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
			where_clauses.Add (String.Format ("photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})", 
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
		query_builder.Append (" ORDER BY photos.time");
		Console.WriteLine ("Query: {0}", query_builder.ToString());
		return Query (query_builder.ToString ());
	}
}
