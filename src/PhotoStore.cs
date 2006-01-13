using Gnome;
using Gdk;
using Gtk;
using Mono.Data.SqliteClient;
using System.Collections;
using System.IO;
using System.Text;
using System;
using FSpot;

public class PhotoVersion : FSpot.IBrowsableItem {
	Photo photo;
	uint version_id;
	
	public System.DateTime Time {
		get { return photo.Time; }
	}

	public Tag [] Tags {
		get { return photo.Tags; }
	}

	public Uri DefaultVersionUri {
		get { return UriList.PathToFileUri (photo.GetVersionPath (version_id));}
	}

	public string Description {
		get { return photo.Description; }
	}

	public string Name {
		get { return photo.GetVersionName (version_id); }
	}

	public Photo Photo {
		get { return photo; }
	}

	public uint VersionId {
		get { return version_id; }
	}

	public PhotoVersion (Photo photo, uint version_id)
	{
		this.photo = photo;
		this.version_id = version_id;
	}
}

public class Photo : DbItem, IComparable, FSpot.IBrowsableItem {
	// IComparable 
	public int CompareTo (object obj) {
		if (this.GetType () == obj.GetType ()) {
			// FIXME this is way under powered for a real compare in the
			// equal case but for now it should do.

			return Compare (this, (Photo)obj);
		} else if (obj is DateTime) {
			return this.time.CompareTo ((DateTime)obj);
		} else {
			throw new Exception ("Object must be of type Photo");
		}
	}

	public int CompareTo (Photo photo)
	{
		return Compare (this, photo);
	}
	
	public static int Compare (Photo photo1, Photo photo2)
	{
		int result = photo1.Id.CompareTo (photo2.Id);
		
		if (result == 0)
			return 0;
		else 
			result = CompareDate (photo1, photo2);

		if (result == 0)
			result = CompareCurrentDir (photo1, photo2);
		
		if (result == 0)
			result = CompareName (photo1, photo2);
		
		if (result == 0)
			result = photo1.Id.CompareTo (photo2.Id);
		
		return result;
	}

	private static int CompareDate (Photo photo1, Photo photo2)
	{
		return DateTime.Compare (photo1.time, photo2.time);
	}

	private static int CompareCurrentDir (Photo photo1, Photo photo2)
	{
		return string.Compare (photo1.directory_path, photo2.directory_path);
	}

	private static int CompareName (Photo photo1, Photo photo2)
	{
		return string.Compare (photo1.name, photo2.name);
	}

	public class CompareDirectory : IComparer {
		public int Compare (object obj1, object obj2) {
			Photo p1 = (Photo)obj1;
			Photo p2 = (Photo)obj2;

			int result = Photo.CompareCurrentDir (p1, p2);
			
			if (result == 0)
				result = CompareName (p1, p2);

			return result;
		}
	}

	// The time is always in UTC.
	private DateTime time;
	public DateTime Time {
		get {
			return time;
		}
		set {
			time = value;
		}
	}

	private string directory_path;
	private string name;

	private string Path {
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

	private ArrayList tags;
	public Tag [] Tags {
		get {
			if (tags == null)
				return new Tag [0];
			else 
				return (Tag []) tags.ToArray (typeof (Tag));
		}
	}

	private bool loaded = false;
	public bool Loaded {
		get {
			return loaded;
		}
		set {
			loaded = value;
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

	private Hashtable version_names;

	public uint [] VersionIds {
		get {
			if (version_names == null)
				return new uint [0];

			uint [] ids = new uint [version_names.Count];
			uint i = 0;
			foreach (uint id in version_names.Keys)
				ids [i ++] = id;

			Array.Sort (ids);
			return ids;
		}
	}

	public IBrowsableItem GetVersion (uint version_id)
	{
		return new PhotoVersion (this, version_id);
	}

	private uint default_version_id = OriginalVersionId;
	public uint DefaultVersionId {
		get {
			return default_version_id;
		}

		set {
			default_version_id = value;
		}
	}

	// This doesn't check if a version of that name already exists, 
	// it's supposed to be used only within the Photo and PhotoStore classes.
	public void AddVersionUnsafely (uint version_id, string name)
	{
		if (version_names == null)
			version_names = new Hashtable ();

		version_names [version_id] = name;

		highest_version_id = Math.Max (version_id, highest_version_id);
	}

	private string GetPathForVersionName (string version_name)
	{
		string name_without_extension = System.IO.Path.GetFileNameWithoutExtension (name);
		string extension = System.IO.Path.GetExtension (name);

		return System.IO.Path.Combine (directory_path,  name_without_extension 
					       + " (" + version_name + ")" + extension);
	}

	public bool VersionNameExists (string version_name)
	{
		if (version_names != null) {
			foreach (string n in version_names.Values) {
				if (n == version_name)
					return true;
			}
		}

		return false;
	}

	public string GetVersionName (uint version_id)
	{
		if (version_names != null)
			return version_names [version_id] as string;
		else 
			return null;
	}

        public string GetVersionPath (uint version_id)
	{
		if (version_id == OriginalVersionId)
			return Path;
		else
			return GetPathForVersionName (GetVersionName (version_id));
	}

	public string DefaultVersionPath {
		get {
			return GetVersionPath (DefaultVersionId);
		}
	}

	public System.Uri DefaultVersionUri {
		get {
			return UriList.PathToFileUri (DefaultVersionPath);
		}
	}

	public void DeleteVersion (uint version_id)
	{
		DeleteVersion (version_id, false);
	}

	public void DeleteVersion (uint version_id, bool remove_original)
	{
		if (version_id == OriginalVersionId && !remove_original)
			throw new Exception ("Cannot delete original version");

		string path = GetVersionPath (version_id);
		if (File.Exists (path))
			File.Delete (path);
		PhotoStore.DeleteThumbnail (path);

		version_names.Remove (version_id);

		do {
			version_id --;
			if (version_names.Contains (version_id)) {
				DefaultVersionId = version_id;
				break;
			}
		} while (version_id > OriginalVersionId);
	}

	public uint CreateVersion (string name, uint base_version_id, bool create_file)
	{
		string new_path = GetPathForVersionName (name);
		string original_path = GetVersionPath (base_version_id);

		if (VersionNameExists (name))
			throw new Exception ("This version name already exists");

		if (File.Exists (new_path))
			throw new Exception (String.Format ("A file named {0} already exists",
							    System.IO.Path.GetFileName (new_path)));

		if (create_file) {
			File.Copy (original_path, new_path);
			FSpot.ThumbnailGenerator.Create (new_path).Dispose ();
		}

		if (version_names == null)
			version_names = new Hashtable ();

		highest_version_id ++;
		version_names [highest_version_id] = name;

		return highest_version_id;
	}

	public uint CreateDefaultModifiedVersion (uint base_version_id, bool create_file)
	{
		int num = 1;

		while (true) {
			string name = Mono.Posix.Catalog.GetPluralString ("Modified", 
								 "Modified ({0})", 
								 num);
			name = String.Format (name, num);

			if (! VersionNameExists (name))
				return CreateVersion (name, base_version_id, create_file);

			num ++;
		}
	}

	public void RenameVersion (uint version_id, string new_name)
	{
		if (version_id == OriginalVersionId)
			throw new Exception ("Cannot rename original version");

		if (VersionNameExists (new_name))
			throw new Exception ("This name already exists");

		string original_name = version_names [version_id] as string;

		string old_path = GetPathForVersionName (original_name);
		string new_path = GetPathForVersionName (new_name);

		if (File.Exists (new_path))
			throw new Exception ("File with this name already exists");

		File.Move (old_path, new_path);
		PhotoStore.MoveThumbnail (old_path, new_path);

		version_names [version_id] = new_name;
	}


	// Tag management.

	// This doesn't check if the tag is already there, use with caution.
	public void AddTagUnsafely (Tag tag)
	{
		if (tags == null)
			tags = new ArrayList ();

		tags.Add (tag);
	}

	// This on the other hand does, but is O(n) with n being the number of existing tags.
	public void AddTag (Tag tag)
	{
		if (!HasTag (tag))
			AddTagUnsafely (tag);
	}

	public void AddTag (Tag []taglist)
	{
		/*
		 * FIXME need a better naming convention here, perhaps just
		 * plain Add.
		 */
		foreach (Tag tag in taglist)
			AddTag (tag);
	}	

	public void RemoveTag (Tag tag)
	{
		if (HasTag (tag))
			tags.Remove (tag);
	}

	public void RemoveTag (Tag []taglist)
	{	
		foreach (Tag tag in taglist)
			RemoveTag (tag);
	}	

	public void RemoveCategory (Tag []taglist)
	{
		foreach (Tag tag in taglist) {
			Category cat = tag as Category;

			if (cat != null)
				RemoveCategory (cat.Children);

			RemoveTag (tag);
		}
	}

	public bool HasTag (Tag tag)
	{
		if (tags == null)
			return false;

		return tags.Contains (tag);
	}

	private static FSpot.Xmp.XmpFile CreateXmp (FSpot.IBrowsableItem item)
	{
		FSpot.Xmp.XmpFile xmp = new FSpot.Xmp.XmpFile ();

		Tag [] tags = item.Tags;
		string [] names = new string [tags.Length];
		
		for (int i = 0; i < tags.Length; i++)
			names [i] = tags [i].Name;
		
		FSpot.MetadataStore.Add (xmp, "dc:subject", "rdf:Bag", names);
		xmp.Dump ();

		return xmp;
	}

	public void WriteMetadataToImage ()
	{
		string path = this.DefaultVersionPath;

		FSpot.ImageFile img = FSpot.ImageFile.Create (path);
		if (img is FSpot.JpegFile) {
			FSpot.JpegFile jimg = img as FSpot.JpegFile;
			
			jimg.SetDescription (this.Description);
			jimg.SetDateTimeOriginal (this.Time);
			FSpot.Xmp.XmpFile xmp = CreateXmp (this);
			jimg.SetXmp (xmp);

			jimg.SaveMetaData (path);
		}
	}

	public uint SaveVersion (Gdk.Pixbuf buffer, bool create_version)
	{
		uint version = DefaultVersionId;
		ImageFile img = ImageFile.Create (DefaultVersionUri.LocalPath);
		Exif.ExifData exif_data = new Exif.ExifData (DefaultVersionPath);

		// Always create a version if the source is not a jpeg for now.
		create_version = create_version || !(img is FSpot.JpegFile);

		if (create_version)
			version = CreateDefaultModifiedVersion (DefaultVersionId, false);

		try {
			string version_path = GetVersionPath (version);
			
			PixbufUtils.SaveJpeg (buffer, version_path, 95, exif_data);
			FSpot.ThumbnailGenerator.Create (version_path).Dispose ();
			DefaultVersionId = version;
			
		} catch (System.Exception e) {
			if (create_version)
				DeleteVersion (version);
			
			throw e;
		}
		
		return version;
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
		AddVersionUnsafely (OriginalVersionId, Mono.Posix.Catalog.GetString ("Original"));
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
	
	public static ThumbnailFactory ThumbnailFactory = new ThumbnailFactory (ThumbnailSize.Large);

	// FIXME this is a hack.  Since we don't have Gnome.ThumbnailFactory.SaveThumbnail() in
	// GTK#, and generate them by ourselves directly with Gdk.Pixbuf, we have to make sure here
	// that the "large" thumbnail directory exists.
	private static void EnsureThumbnailDirectory ()
	{
		string large_thumbnail_file_name_template = Thumbnail.PathForUri ("file:///boo", ThumbnailSize.Large);
		string large_thumbnail_directory_path = System.IO.Path.GetDirectoryName (large_thumbnail_file_name_template);

		if (! File.Exists (large_thumbnail_directory_path))
			Directory.CreateDirectory (large_thumbnail_directory_path);
	}

	//
	// Generates the thumbnail, returns the Pixbuf, and also stores it as a side effect
	//

	public static Pixbuf GenerateThumbnail (string path)
	{
		string uri = UriList.PathToFileUri (path).ToString ();
		Pixbuf thumbnail = null;
		FSpot.ImageFile img = FSpot.ImageFile.Create (path);

		if (img is FSpot.IThumbnailContainer) {
			try {
				thumbnail = ((FSpot.IThumbnailContainer)img).GetEmbeddedThumbnail ();
			} catch (Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}

		// Save embedded thumbnails in a silightly invalid way so that we know to regnerate them later.
		if (thumbnail != null) 
			PixbufUtils.SaveAtomic (thumbnail, Thumbnail.PathForUri (uri, ThumbnailSize.Large), 
						"png", new string [] { null} , new string [] { null});
		else 
			thumbnail = FSpot.ThumbnailGenerator.Create (path);
		
		return thumbnail;
	}

	public static void DeleteThumbnail (string path)
	{
		string uri = UriList.PathToFileUri (path).ToString ();
		path = Thumbnail.PathForUri (uri, ThumbnailSize.Large);
		if (File.Exists (path))
			File.Delete (path);
	}

	public static void MoveThumbnail (string old_path, string new_path)
	{
		string old_uri = UriList.PathToFileUri (old_path).ToString ();
		string new_uri = UriList.PathToFileUri (new_path).ToString ();

		File.Move (Thumbnail.PathForUri (old_uri, ThumbnailSize.Large),
			   Thumbnail.PathForUri (new_uri, ThumbnailSize.Large));
	}


	// Constructor

	public PhotoStore (SqliteConnection connection, bool is_new, TagStore tag_store)
		: base (connection, false)
	{
		this.tag_store = tag_store;
		EnsureThumbnailDirectory ();

		if (! is_new)
			return;
		
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText =
			"CREATE TABLE photos (                                     " +
			"	id                 INTEGER PRIMARY KEY NOT NULL,   " +
			"       time               INTEGER NOT NULL,	   	   " +
			"       directory_path     STRING NOT NULL,		   " +
			"       name               STRING NOT NULL,		   " +
			"       description        TEXT NOT NULL,	           " +
			"       default_version_id INTEGER NOT NULL		   " +
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
			"CREATE TABLE photo_versions (    " +
			"       photo_id        INTEGER,  " +
			"       version_id      INTEGER,  " +
			"       name            STRING    " +
			")";

		command.ExecuteNonQuery ();
		command.Dispose ();
	}


	public Photo Create (string path, out Pixbuf thumbnail)
	{
		return Create (path, path, out thumbnail);
	}

	public Photo Create (string newPath, string origPath, out Pixbuf thumbnail)
	{
		FSpot.ImageFile img = FSpot.ImageFile.Create (origPath);
		uint unix_time = DbUtils.UnixTimeFromDateTime (img.Date);
		string description = img.Description != null ? img.Description : "";
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("INSERT INTO photos (time, " +
						     "directory_path, name, description, default_version_id) " +
						     "       VALUES ({0}, '{1}', '{2}', '{3}', {4})",
						     unix_time,
						     SqlString (System.IO.Path.GetDirectoryName (newPath)),
						     SqlString (System.IO.Path.GetFileName (newPath)),
						     SqlString (description),
						     Photo.OriginalVersionId);

		command.ExecuteScalar ();
		command.Dispose ();

		uint id = (uint) Connection.LastInsertRowId;
		Photo photo = new Photo (id, unix_time, newPath);
		AddToCache (photo);
		photo.Loaded = true;

		thumbnail = GenerateThumbnail (newPath);		
		EmitAdded (photo);

		return photo;
	}

	private void GetVersions (Photo photo)
	{

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT version_id, name FROM photo_versions WHERE photo_id = {0}", photo.Id);
		SqliteDataReader reader = command.ExecuteReader ();

		while (reader.Read ()) {
			uint version_id = Convert.ToUInt32 (reader [0]);
			string name = reader[1].ToString ();
			photo.AddVersionUnsafely (version_id, name);
		}

		command.Dispose ();
	}

	private void GetTags (Photo photo)
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT tag_id FROM photo_tags WHERE photo_id = {0}", photo.Id);
		SqliteDataReader reader = command.ExecuteReader ();

		while (reader.Read ()) {
			uint tag_id = Convert.ToUInt32 (reader [0]);
			Tag tag = tag_store.Get (tag_id) as Tag;
			photo.AddTagUnsafely (tag);
		}

		command.Dispose ();
	}		
	
	private void GetAllVersions  () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT photo_id, version_id, name " +
						     "FROM photo_versions ");
		
		SqliteDataReader reader = command.ExecuteReader ();
		
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
				
				photo.AddVersionUnsafely (version_id, name);
			}

			/*
			string directory_path = null;
			if (reader [3] != null)
				directory_path = reader [3].ToString ();
			System.Console.WriteLine ("directory_path = {0}", directory_path);
			*/


		}
	}

	private void GetAllTags () {
			SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT photo_id, tag_id " +
						     "FROM photo_tags ");
		
		SqliteDataReader reader = command.ExecuteReader ();

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
				Tag tag = tag_store.Get (tag_id) as Tag;
				photo.AddTagUnsafely (tag);
			}
		}
	}

	private void GetAllData () {
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT photo_tags.photo_id, tag_id, version_id, name " +
						     "FROM photo_tags, photo_versions " +
						     "WHERE photo_tags.photo_id = photo_versions.photo_id");
		
		SqliteDataReader reader = command.ExecuteReader ();

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
				uint tag_id = Convert.ToUInt32 (reader [1]);
				Tag tag = tag_store.Get (tag_id) as Tag;
				photo.AddTagUnsafely (tag);
			}
			if (reader [2] != null) {
				uint version_id = Convert.ToUInt32 (reader [2]);
				string name = reader[3].ToString ();
				
				photo.AddVersionUnsafely (version_id, name);
			}
		}
	}

	private void GetData (Photo photo)
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT tag_id, version_id, name " +
						     "  FROM photo_tags, photo_versions " +
						     " WHERE photo_tags.photo_id = photo_versions.photo_id " +
						     "   AND photo_tags.photo_id = {0}", photo.Id);

		SqliteDataReader reader = command.ExecuteReader ();

		while (reader.Read ()) {
		        if (reader [0] != null) {
				uint tag_id = Convert.ToUInt32 (reader [0]);
				Tag tag = tag_store.Get (tag_id) as Tag;
				photo.AddTagUnsafely (tag);
			}
			if (reader [1] != null) {
				uint version_id = Convert.ToUInt32 (reader [1]);
				string name = reader[2].ToString ();
				
				photo.AddVersionUnsafely (version_id, name);
			}
		}
	}

	public override DbItem Get (uint id)
	{
		Photo photo = LookupInCache (id) as Photo;
		if (photo != null)
			return photo;

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("SELECT time,                                 " +
						     "       directory_path,                       " +
						     "       name,                                 " +
						     "       description,                          " +
						     "       default_version_id                    " +
						     "     FROM photos                             " +
						     "     WHERE id = {0}                          ",
						     id);
		SqliteDataReader reader = command.ExecuteReader ();

		if (reader.Read ()) {
			photo = new Photo (id,
					   Convert.ToUInt32 (reader [0]),
					   reader [1].ToString (),
					   reader [2].ToString ());

			photo.Description = reader[3].ToString ();
			photo.DefaultVersionId = Convert.ToUInt32 (reader[4]);
			AddToCache (photo);
		}

		command.Dispose ();

		if (photo == null)
			return null;
		
		GetTags (photo);
		GetVersions (photo);

		return photo;
	}

	public Photo GetByPath (string path)
	{
		//FIXME - No cacheing here - probably not a problem since
		//        this is only used for DND

		Photo photo = null;
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		string directory_path = System.IO.Path.GetDirectoryName (path);
		string filename = System.IO.Path.GetFileName (path);

		directory_path = directory_path.Trim();
		filename = filename.Trim();
		command.CommandText = String.Format ("SELECT id,                                   " +
				                     "       time,                                 " +
						     "       description,                          " +
						     "       default_version_id                    " +
						     "  FROM photos                                " +
						     " WHERE directory_path = \"{0}\"              " +
						     "   AND name = \"{1}\"                        ",
						     directory_path,
						     filename);

		SqliteDataReader reader = command.ExecuteReader ();

		if (reader.Read ()) {
			photo = new Photo (Convert.ToUInt32 (reader [0]),
					   Convert.ToUInt32 (reader [1]),
					   directory_path,
					   filename);

			photo.Description = reader[2].ToString ();
			photo.DefaultVersionId = Convert.ToUInt32 (reader[3]);
			AddToCache (photo);
		}

		command.Dispose ();

		if (photo == null)
			return null;
		
		GetTags (photo);
		GetVersions (photo);

		return photo;
	}

	public void Remove (Tag []tags)
	{
		Photo [] photos = Query (tags);	

		foreach (Photo photo in photos) {
			photo.RemoveCategory (tags);
			Commit (photo);
		}
		
		foreach (Tag tag in tags)
			tag_store.Remove (tag);
		
	}

	public void Remove (Photo []items)
	{
		EmitRemoved (items);

		StringBuilder query_builder = new StringBuilder ();
		StringBuilder tv_query_builder = new StringBuilder ();
		for (int i = 0; i < items.Length; i++) {
			if (i > 0) {
				query_builder.Append (" OR ");
				tv_query_builder.Append (" OR ");
			}

			query_builder.Append (String.Format ("id = {0}", items[i].Id));
			tv_query_builder.Append (String.Format ("photo_id = {0}", items[i].Id));
			RemoveFromCache (items[i]);
		}

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM photos WHERE {0}", query_builder.ToString ());
		command.ExecuteNonQuery ();

		command.Dispose ();

		command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM photo_tags WHERE {0}", tv_query_builder.ToString ());
		command.ExecuteNonQuery ();

		command.Dispose ();

		command = new SqliteCommand ();
		command.Connection = Connection;

		command.CommandText = String.Format ("DELETE FROM photo_versions WHERE {0}", tv_query_builder.ToString ());
		command.ExecuteNonQuery ();

		command.Dispose ();
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
		BeginTransaction ();
		foreach (DbItem item in items) {
			Update ((Photo)item);
		}
		EmitChanged (items, args);
		CommitTransaction ();
	}
	
        private void Update (Photo photo) {
		// Update photo.

		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = String.Format ("UPDATE photos SET description = '{0}',     " +
						     "                  default_version_id = {1} " +
						     "              WHERE id = {2}",
						     SqlString (photo.Description),
						     photo.DefaultVersionId,
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
			if (version_id == Photo.OriginalVersionId)
				continue;

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
	
	public class DateRange 
	{
		private DateTime start;		
		public DateTime Start {
			get {
				return start;
			}
		}

		private DateTime end;
		public DateTime End {
			get {
				return end;
			}
		}

		public DateRange (DateTime start, DateTime end)
		{
			this.start = start;
			this.end = end;
		}
	}

	// Queries.
	public Photo [] Query (Tag [] tags, DateTime start, DateTime end)
	{
		return Query (tags, new DateRange (start, end));
	}
	
	public Photo [] Query (Tag [] tags) {
		return Query (tags, null);
	}

	public Photo [] Query (string query)
	{
		SqliteCommand command = new SqliteCommand ();
		command.Connection = Connection;
		command.CommandText = query;
		SqliteDataReader reader = command.ExecuteReader ();
		
		ArrayList version_list = new ArrayList ();
		ArrayList id_list = new ArrayList ();
		while (reader.Read ()) {
			uint id = Convert.ToUInt32 (reader [0]);
			Photo photo = LookupInCache (id) as Photo;

			if (photo == null) {
				photo = new Photo (id,
						   Convert.ToUInt32 (reader [1]),
						   reader [2].ToString (),
						   reader [3].ToString ());
				
				photo.Description = reader[4].ToString ();
				photo.DefaultVersionId = Convert.ToUInt32 (reader[5]);		 
				
				version_list.Add (photo);
			}

			id_list.Add (photo);
		}

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

		command.Dispose ();

		return id_list.ToArray (typeof (Photo)) as Photo [];
	}

	public Photo [] Query (System.IO.DirectoryInfo dir)
	{
		string query_string = String.Format ("SELECT photos.id,                          " +
						     "       photos.time,                        " +
						     "       photos.directory_path,              " +
						     "       photos.name,                        " +
						     "       photos.description,                 " +
						     "       photos.default_version_id           " +
						     "     FROM photos                           " +
						     "     WHERE directory_path = \"{0}\"", dir.FullName);

		return Query (query_string);
	}

	public Photo [] QueryUntagged (DateRange range)
	{
		StringBuilder query_builder = new StringBuilder ();

		query_builder.Append ("SELECT * FROM photos WHERE id NOT IN " +
					"(SELECT DISTINCT photo_id FROM photo_tags) ");
		
		if (range != null) {
			query_builder.Append (String.Format ("AND photos.time >= {0} AND photos.time < {1} ",
							     DbUtils.UnixTimeFromDateTime (range.Start), 
							     DbUtils.UnixTimeFromDateTime (range.End)));
		}

		query_builder.Append("ORDER BY time");

		return Query (query_builder.ToString ());
	}

	public Photo [] Query (Tag [] tags, DateRange range)
	{
		string query;

		bool hide = true;
		if (tags != null) {
			foreach (Tag t in tags) {
				if (tag_store.Hidden == null || t.Id == tag_store.Hidden.Id) 
					hide = false;
			}
		} else
			hide = tag_store.Hidden != null;

		// The SQL query that we want to construct is:
		//
		// SELECT photos.id
		//        photos.time
		//        photos.directory_path,
		//        photos.name,
		//        photos.description,
		//        photos.default_version_id
		//                  FROM photos, photo_tags
		//                  WHERE photos.id = photo_tags.photo_id
		// 		          AND (photo_tags.tag_id = tag1
		//			       OR photo_tags.tag_id = tag2
		//                             OR photo_tags.tag_id = tag3 ...)
		//                  GROUP BY photos.id
		
		StringBuilder query_builder = new StringBuilder ();
		query_builder.Append ("SELECT photos.id,                          " +
				      "       photos.time,                        " +
				      "       photos.directory_path,              " +
				      "       photos.name,                        " +
				      "       photos.description,                 " +
				      "       photos.default_version_id           " +
				      "     FROM photos                      ");
		
		if (range != null) {
			query_builder.Append (String.Format ("WHERE photos.time >= {0} AND photos.time < {1} ",
							     DbUtils.UnixTimeFromDateTime (range.Start), 
							     DbUtils.UnixTimeFromDateTime (range.End)));
		}
		
		if (hide) {
			query_builder.Append (String.Format ("{0} photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {1})", 
							     range != null ? " AND " : " WHERE ", tag_store.Hidden.Id));
		}
		
		if (tags != null && tags.Length > 0) {
			bool first = true;
			foreach (Tag t in tags) {
				if (t == null)
					continue;
				
				if (tag_store.Hidden != null && t.Id == tag_store.Hidden.Id)
					continue;
				
				if (first) {
					query_builder.Append (String.Format ("{0} photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (",
									     hide || range != null ? " AND " : " WHERE "));
				}
				
				query_builder.Append (String.Format ("{0}{1} ", first ? "" : ", ", t.Id));
				
					first = false;
			}
			
			if (!first)
				query_builder.Append (")) ");
		}
		
		query_builder.Append ("ORDER BY photos.time");
		query = query_builder.ToString ();
		
		return Query (query);
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
