using Gnome;
using Gdk;
using Gtk;
using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System.Text;
using System;


public class Photo : DbItem {
	// The time is always in UTC.
	private DateTime time;
	public DateTime Time {
		get {
			return time;
		}
	}

	private string directory_path;
	private string name;

	public string Path {
		get {
			return directory_path + "/" + name;
		}
	}

	public string Name {
		get {
			return name;
		}
	}

	public string DirectoryPath {
		get {
			return directory_path;
		}
	}

	private ArrayList tags = new ArrayList ();
	public ArrayList Tags {
		get {
			return tags;
		}
	}

	private string description;
	public string Description {
		get {
			return description;
		}
		set {
			description = value;
		}
	}


	// Version management

	public const int OriginalVersionId = 1;

	private uint highest_version_id;

	private Hashtable version_names = new Hashtable ();
	public uint [] VersionIds {
		get {
			uint [] ids = new uint [version_names.Count];

			uint i = 0;
			foreach (uint id in version_names.Keys)
				ids [i ++] = id;

			Array.Sort (ids);
			return ids;
		}
	}

	// This doesn't check if a version of that name already exists, it's supposed to be used only within
	// the Photo and PhotoStore classes.
	public void AddVersionUnsafely (uint version_id, string name)
	{
		version_names [version_id] = name;

		highest_version_id = Math.Max (version_id, highest_version_id);
	}

	private string GetPathForVersionName (string version_name)
	{
		string name_without_extension = System.IO.Path.GetFileNameWithoutExtension (name);
		string extension = System.IO.Path.GetExtension (name);

		return System.IO.Path.Combine (directory_path,  name_without_extension + " (" + version_name + ")" + extension);
	}

	private bool VersionNameExists (string version_name)
	{
		foreach (string n in version_names.Values) {
			if (n == version_name)
				return true;
		}

		return false;
	}

	public string GetVersionName (uint version_id)
	{
		return version_names [version_id] as string;
	}

	public string GetVersionPath (uint version_id)
	{
		return GetPathForVersionName (version_names [version_id] as string);
	}

	public void DeleteVersion (uint version_id)
	{
		if (version_id == OriginalVersionId)
			throw new Exception ("Cannot delete original version");

		// Delete file.

		version_names.Remove (version_id);
	}

	public uint CreateVersion (string name, uint base_version_id)
	{
		string new_path = GetPathForVersionName (name);
		string original_path = GetVersionPath (base_version_id);

		if (VersionNameExists (name))
			throw new Exception ("This name already exists");

		// Copy file.

		highest_version_id ++;
		version_names [highest_version_id] = name;

		return highest_version_id;
	}

	public void RenameVersion (int version_id, string new_name)
	{
		if (version_id == OriginalVersionId)
			throw new Exception ("Cannot rename original version");

		if (VersionNameExists (name))
			throw new Exception ("This name already exists");

		string original_name = version_names [version_id] as string;

		string old_path = GetPathForVersionName (original_name);
		string new_path = GetPathForVersionName (new_name);

		if (File.Exists (new_path))
			throw new Exception ("File with this name already exists");

		// Rename file

		version_names [version_id] = name;
	}


	// Tag management.

	// This doesn't check if the tag is already there, use with caution.
	public void AddTagUnsafely (Tag tag)
	{
		tags.Add (tag);
	}

	// This on the other hand does, but is O(n) with n being the number of existing tags.
	public void AddTag (Tag tag)
	{
		if (! tags.Contains (tag))
			AddTagUnsafely (tag);
	}

	public void RemoveTag (Tag tag)
	{
		tags.Remove (tag);
	}

	public bool HasTag (Tag tag)
	{
		return tags.Contains (tag);
	}


	// Constructor

	public Photo (uint id, uint unix_time, string directory_path, string name)
		: base (id)
	{
		time = DbUtils.DateTimeFromUnixTime (unix_time);

		this.directory_path = directory_path;
		this.name = name;

		description = "";

		// Note that the original version is never stored in the photo_versions table in the
		// database.
		AddVersionUnsafely (OriginalVersionId, "Original");
	}

	public Photo (uint id, uint unix_time, string path)
		: this (id, unix_time,
			System.IO.Path.GetDirectoryName (path),
			System.IO.Path.GetFileName (path))
	{
	}

}

public class PhotoStore : DbStore {

	TagStore tag_store;
	ThumbnailFactory thumbnail_factory;


	// Constructor

	// FIXME this is a hack.  Since we don't have Gnome.ThumbnailFactory.SaveThumbnail() in
	// GTK#, and generate them by ourselves directly with Gdk.Pixbuf, we have to make sure here
	// that the "large" thumbnail directory exists.
	private void EnsureThumbnailDirectory ()
	{
		string large_thumbnail_file_name_template = Thumbnail.PathForUri ("file:///boo", ThumbnailSize.Large);
		string large_thumbnail_directory_path = System.IO.Path.GetDirectoryName (large_thumbnail_file_name_template);

		if (! File.Exists (large_thumbnail_directory_path))
			Directory.CreateDirectory (large_thumbnail_directory_path);
	}

	public PhotoStore (SqliteConnection connection, bool is_new, TagStore tag_store)
		: base (connection)
	{
		this.tag_store = tag_store;
		thumbnail_factory = new ThumbnailFactory (ThumbnailSize.Large);
		EnsureThumbnailDirectory ();

		if (! is_new)
			return;
		
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			"CREATE TABLE photos (                                     " +
			"	id              INTEGER PRIMARY KEY NOT NULL,      " +
			"       time            INTEGER NOT NULL,	   	   " +
			"       directory_path  STRING NOT NULL,		   " +
			"       name            STRING NOT NULL,		   " +
			"       description     TEXT NOT NULL		           " +
			")";

		command.ExecuteNonQuery ();
		command.Dispose ();

		// FIXME: No need to do Dispose here?

		command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			"CREATE TABLE photo_tags (     " +
			"	photo_id      INTEGER, " +
			"       tag_id        INTEGER  " +
			")";

		command.ExecuteNonQuery ();
		command.Dispose ();

		// FIXME: No need to do Dispose here?

		command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			"CREATE TABLE photo_versions (      " +
			"	photo_id      INTEGER,      " +
			"       version_id    INTEGER,      " +
			"       name          STRING        " +
			")";

		command.ExecuteNonQuery ();
		command.Dispose ();
	}


	public Photo Create (DateTime time_in_utc, string path, out Pixbuf thumbnail)
	{
		if (! path.EndsWith (".jpg") && ! path.EndsWith (".JPG"))
			throw new Exception ("Only jpeg files supported");

		uint unix_time = DbUtils.UnixTimeFromDateTime (time_in_utc);

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		uint id = (uint) Connection.LastInsertRowId;
		Photo photo = new Photo (id, unix_time, path);
		AddToCache (photo);

		command.CommandText = String.Format ("INSERT INTO photos (time, directory_path, name, description) " +
						     "       VALUES ({0}, '{1}', '{2}', '')                         ",
						     unix_time, SqlString (photo.DirectoryPath), SqlString (photo.Name));
		command.ExecuteScalar ();
		command.Dispose ();

		string uri = "file://" + path;
		thumbnail = thumbnail_factory.GenerateThumbnail ("file://" + path, "image/jpeg");

		// FIXME if this is null then the file doesn't exist.
		if (thumbnail != null) {
			// FIXME there is no SaveThumbnail() in the C# bindings for ThumbnailFactory.
			// This should really be done through SaveThumbnail, which would make sure we dont' do
			// it unnecessarily.
			thumbnail.Savev (Thumbnail.PathForUri (uri, ThumbnailSize.Large), "png", null, null);
		}

		return photo;
	}

	public override DbItem Get (uint id)
	{
		Photo photo = LookupInCache (id) as Photo;
		if (photo != null)
			return photo;

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT time, directory_path, name, description " +
						     "       FROM photos                             " +
						     "       WHERE id = {0}                          ",
						     id);
		SqliteDataReader reader = command.ExecuteReader ();

		if (reader.Read ()) {
			photo = new Photo (id,
					   Convert.ToUInt32 (reader [0]),
					   reader [1].ToString (),
					   reader [2].ToString ());

			photo.Description = reader[3].ToString ();
			AddToCache (photo);
		}

		command.Dispose ();

		if (photo == null)
			return null;

		command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT tag_id FROM photo_tags WHERE photo_id = {0}", id);
		reader = command.ExecuteReader ();

		while (reader.Read ()) {
			uint tag_id = Convert.ToUInt32 (reader [0]);
			Tag tag = tag_store.Get (tag_id) as Tag;
			photo.AddTagUnsafely (tag);
		}

		command.Dispose ();

		command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT version_id, name FROM photo_versions WHERE photo_id = {0}", id);
		reader = command.ExecuteReader ();

		while (reader.Read ()) {
			uint version_id = Convert.ToUInt32 (reader [0]);
			string name = reader[1].ToString ();

			photo.AddVersionUnsafely (version_id, name);
		}

		command.Dispose ();

		return photo;
	}

	public override void Remove (DbItem item)
	{
		RemoveFromCache (item);

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM photos WHERE id = {0}", item.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();

		command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM photo_tags WHERE photo_id = {0}", item.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();

		command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM photo_versions WHERE photo_id = {0}", item.Id);
		command.ExecuteNonQuery ();

		command.Dispose ();
	}

	public override void Commit (DbItem item)
	{
		Photo photo = item as Photo;

		// Update photo.

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("UPDATE photos SET description = '{0}'" +
						     "              WHERE id = {1}",
						     SqlString (photo.Description),
						     photo.Id);
		command.ExecuteNonQuery ();
		command.Dispose ();

		// Update tags.

		command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("DELETE FROM photo_tags WHERE photo_id = {0}", photo.Id);
		command.ExecuteNonQuery ();
		command.Dispose ();

		foreach (Tag tag in photo.Tags) {
			command = new SqliteCommand ();
			command.Connection = Connection;
			command.CommandText = String.Format ("INSERT INTO photo_tags (photo_id, tag_id) " +
							     "       VALUES ({0}, {1})",
							     photo.Id, tag.Id);
			command.ExecuteNonQuery ();
			command.Dispose ();
		}

		// Update versions.

		command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("DELETE FROM photo_versions WHERE photo_id = {0}", photo.Id);
		command.ExecuteNonQuery ();
		command.Dispose ();

		foreach (uint version_id in photo.VersionIds) {
			string version_name = photo.GetVersionName (version_id);

			command = new SqliteCommand ();
			command.Connection = Connection;
			command.CommandText = String.Format ("INSERT INTO photo_versions (photo_id, version_id, name) " +
							     "       VALUES ({0}, {1}, '{2}')",
							     photo.Id, version_id, SqlString (version_name));
			command.ExecuteNonQuery ();
			command.Dispose ();
		}
	}


	// Queries.

	public ArrayList Query (ArrayList tags)
	{
		string query;

		if (tags == null || tags.Count == 0) {
			query = "SELECT id FROM photos";
		} else {
			// The SQL query that we want to construct is:
			//
			// SELECT photos.id FROM photos, photo_tags
			//                  WHERE photos.id = photo_tags.photo_id
			// 		          AND (photo_tags.tag_id = tag1
			//			       OR photo_tags.tag_id = tag2
			//                             OR photo_tags.tag_id = tag3 ...)
			//                  GROUP BY photos.id

			StringBuilder query_builder = new StringBuilder ();
			query_builder.Append ("SELECT photos.id FROM photos, photo_tags               " +
					      "                 WHERE photos.id = photo_tags.photo_id ");

			if (tags != null) {
				bool first = true;
				foreach (Tag t in tags) {
					if (first)
						query_builder.Append (" AND (");
					else
						query_builder.Append (" OR ");

					query_builder.Append (String.Format ("photo_tags.tag_id = {0}", t.Id));

					first = false;
				}

				if (tags.Count > 0)
					query_builder.Append (")");
			}

			query_builder.Append (" GROUP BY photos.id");
			query = query_builder.ToString ();
		}

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = query;
		SqliteDataReader reader = command.ExecuteReader ();

		// FIXME: I am doing it in two passes here since the Get() method can potentially
		// invoke more Sqlite queries, and I don't know if we are supposed to do that while
		// we are reading the results from a past query.
		
		ArrayList id_list = new ArrayList ();
		while (reader.Read ())
			id_list.Add (Convert.ToUInt32 (reader [0]));

		command.Dispose ();

		ArrayList photo_list = new ArrayList ();
		foreach (uint id in id_list)
			photo_list.Add (Get (id));

		return photo_list;
	}


#if TEST_PHOTO_STORE

	static void Dump (Photo photo)
	{
		Console.WriteLine ("\t[{0}] {1}", photo.Id, photo.Path);
		Console.WriteLine ("\t{0}", photo.Time.ToLocalTime ());

		if (photo.Description != "")
			Console.WriteLine ("\t{0}", photo.Description);
		else
			Console.WriteLine ("\t(no description)");

		Console.WriteLine ("\tTags:");

		if (photo.Tags.Count == 0) {
			Console.WriteLine ("\t\t(no tags)");
		} else {
			foreach (Tag t in photo.Tags)
				Console.WriteLine ("\t\t{0}", t.Name);
		}

		Console.WriteLine ("\tVersions:");

		foreach (uint id in photo.VersionIds)
			Console.WriteLine ("\t\t[{0}] {1}", id, photo.GetVersionName (id));
	}

	static void Dump (ArrayList photos)
	{
		foreach (Photo p in photos)
			Dump (p);
	}

	static void DumpAll (Db db)
	{
		Console.WriteLine ("\n*** All pictures");
		Dump (db.Photos.Query (null));
	}

	static void DumpForTags (Db db, ArrayList tags)
	{
		Console.Write ("\n*** Pictures for tags: ");
		foreach (Tag t in tags)
			Console.Write ("{0} ", t.Name);
		Console.WriteLine ();

		Dump (db.Photos.Query (tags));
	}

	static void Main (string [] args)
	{
		Program program = new Program ("PhotoStoreTest", "0.0", Modules.UI, args);

		const string path = "/tmp/PhotoStoreTest.db";

		try {
			File.Delete (path);
		} catch {}

		Db db = new Db (path, true);

		Tag portraits_tag = db.Tags.CreateTag (null, "Portraits");
		Tag landscapes_tag = db.Tags.CreateTag (null, "Landscapes");
		Tag favorites_tag = db.Tags.CreateTag (null, "Street");

		uint portraits_tag_id = portraits_tag.Id;
		uint landscapes_tag_id = landscapes_tag.Id;
		uint favorites_tag_id = favorites_tag.Id;

		Pixbuf unused_thumbnail;

		Photo ny_landscape = db.Photos.Create (DateTime.Now.ToUniversalTime (), "/home/ettore/Photos/ny_landscape.jpg",
						       out unused_thumbnail);
		ny_landscape.Description = "Pretty NY skyline";
		ny_landscape.AddTag (landscapes_tag);
		ny_landscape.AddTag (favorites_tag);
		db.Photos.Commit (ny_landscape);

		Photo me_in_sf = db.Photos.Create (DateTime.Now.ToUniversalTime (), "/home/ettore/Photos/me_in_sf.jpg",
						   out unused_thumbnail);
		me_in_sf.AddTag (landscapes_tag);
		me_in_sf.AddTag (portraits_tag);
		me_in_sf.AddTag (favorites_tag);
		db.Photos.Commit (me_in_sf);

		me_in_sf.RemoveTag (favorites_tag);
		me_in_sf.Description = "Myself and the SF skyline";
		me_in_sf.CreateVersion ("cropped", Photo.OriginalVersionId);
		me_in_sf.CreateVersion ("UM-ed", Photo.OriginalVersionId);
		db.Photos.Commit (me_in_sf);

		Photo macro_shot = db.Photos.Create (DateTime.Now.ToUniversalTime (), "/home/ettore/Photos/macro_shot.jpg",
						     out unused_thumbnail);
		db.Dispose ();

		db = new Db (path, false);

		DumpAll (db);

		portraits_tag = db.Tags.Get (portraits_tag_id) as Tag;
		landscapes_tag = db.Tags.Get (landscapes_tag_id) as Tag;
		favorites_tag = db.Tags.Get (favorites_tag_id) as Tag;

		ArrayList query_tags = new ArrayList ();
		query_tags.Add (portraits_tag);
		query_tags.Add (landscapes_tag);
		DumpForTags (db, query_tags);

		query_tags.Clear ();
		query_tags.Add (favorites_tag);
		DumpForTags (db, query_tags);
	}

#endif
}
