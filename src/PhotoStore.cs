/*
 * PhotoStore.cs
 *
 * Author(s):
	Ettore Perazzoli <ettore@perazzoli.org>
	Larry Ewing <lewing@gnome.org>
	Stephane Delcroix <stephane@delcroix.org>
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

using Banshee.Database;

namespace FSpot{
	public class NotRatedException : System.ApplicationException
	{
		public NotRatedException (string message) : base (message)
		{}
	}
}

public class PhotoVersion : FSpot.IBrowsableItem
{
	Photo photo;
	uint version_id;
	System.Uri uri;
	string name;
	bool is_protected;

	public System.DateTime Time {
		get { return photo.Time; }
	}

	public Tag [] Tags {
		get { return photo.Tags; }
	}

	public System.Uri DefaultVersionUri {
		get { return uri; }
	}

	public string Description {
		get { return photo.Description; }
	}

	public string Name {
		get { return name; }
		set { name = value; }
	}

	public Photo Photo {
		get { return photo; }
	}

	public System.Uri Uri {
		get { return uri; }
		set { 
			if (value == null)
				throw new System.ArgumentNullException ("uri");
			uri = value;
		}
	}

	public uint VersionId {
		get { return version_id; }
	}

	public bool IsProtected {
		get { return is_protected; }
	}

	public uint Rating {
		get { return photo.Rating; }
	}

	public PhotoVersion (Photo photo, uint version_id, System.Uri uri, string name, bool is_protected)
	{
		this.photo = photo;
		this.version_id = version_id;
		this.uri = uri;
		this.name = name;
		this.is_protected = is_protected;
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
		return string.Compare (photo1.DirectoryPath, photo2.DirectoryPath);
	}

	private static int CompareName (Photo photo1, Photo photo2)
	{
		return string.Compare (photo1.Name, photo2.Name);
	}

	public class CompareDateName : IComparer
	{
		public int Compare (object obj1, object obj2)
		{
			Photo p1 = (Photo)obj1;
			Photo p2 = (Photo)obj2;

			int result = Photo.CompareDate (p1, p2);
			
			if (result == 0)
				result = CompareName (p1, p2);

			return result;
		}
	}

	public class CompareDirectory : IComparer
	{
		public int Compare (object obj1, object obj2)
		{
			Photo p1 = (Photo)obj1;
			Photo p2 = (Photo)obj2;

			int result = Photo.CompareCurrentDir (p1, p2);
			
			if (result == 0)
				result = CompareName (p1, p2);

			return result;
		}
	}

	public class RandomSort : IComparer
	{
		Random random = new Random ();
		
		public int Compare (object obj1, object obj2)
		{
			return random.Next (-5, 5);
		}
	}

	// The time is always in UTC.
	private DateTime time;
	public DateTime Time {
		get { return time; }
		set { time = value; }
	}

	public string Name {
		get { return System.IO.Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath); }
	}

	//This property no longer keeps a 'directory' path, but the logical container for the image, like:
	// file:///home/bob/Photos/2007/08/23 or
	// http://www.google.com/logos
	[Obsolete ("MARKED FOR REMOVAL. no longer makes sense with versions in different Directories. Any way to get rid of this ?")]
	public string DirectoryPath {
		get { 
			System.Uri uri = VersionUri (OriginalVersionId);
			return uri.Scheme + "://" + uri.Host + System.IO.Path.GetDirectoryName (uri.AbsolutePath);
		}
	}

	private ArrayList tags;
	public Tag [] Tags {
		get {
			if (tags == null)
				return new Tag [0];

			return (Tag []) tags.ToArray (typeof (Tag));
		}
	}

	private bool loaded = false;
	public bool Loaded {
		get { return loaded; }
		set { loaded = value; }
	}

	private string description;
	public string Description {
		get { return description; }
		set { description = value; }
	}

	private uint roll_id = 0;
	public uint RollId {
		get { return roll_id; }
		set { roll_id = value; }
	}

	private uint rating;
	private bool rated = false;
	public uint Rating {
		get {
			if (!rated)
				throw new NotRatedException ("This photo is not rated yet");
			else
				return rating;
		}
		set {
			if (value >= 0 && value <= 5) {
				rating = value;
				rated = true;
			} else
				rated = false;
		}
	}

	public void RemoveRating ()
	{
		rated = false;
	}

	// Version management
	public const int OriginalVersionId = 1;
	private uint highest_version_id;

	private Dictionary<uint, PhotoVersion> versions;
	private Dictionary<uint, PhotoVersion> Versions {
		get {
			if (versions == null)
				versions = new Dictionary<uint, PhotoVersion> ();
			return versions;
		}
	}

	public uint [] VersionIds {
		get {
			if (versions == null)
				return new uint [0];

			uint [] ids = new uint [versions.Count];
			versions.Keys.CopyTo (ids, 0);
			Array.Sort (ids);
			return ids;
		}
	}

	public IBrowsableItem GetVersion (uint version_id)
	{
		if (versions == null)
			return null;

		return versions [version_id];
	}

	private uint default_version_id = OriginalVersionId;
	public uint DefaultVersionId {
		get { return default_version_id; }
		set { default_version_id = value; }
	}

	// This doesn't check if a version of that name already exists, 
	// it's supposed to be used only within the Photo and PhotoStore classes.
	internal void AddVersionUnsafely (uint version_id, System.Uri uri, string name, bool is_protected)
	{
		Versions [version_id] = new PhotoVersion (this, version_id, uri, name, is_protected);

		highest_version_id = Math.Max (version_id, highest_version_id);
	}

	public uint AddVersion (System.Uri uri, string name)
	{
		return AddVersion (uri, name, false);
	}

	public uint AddVersion (System.Uri uri, string name, bool is_protected)
	{
		if (VersionNameExists (name))
			throw new ApplicationException ("A version with that name already exists");
		highest_version_id ++;
		Versions [highest_version_id] = new PhotoVersion (this, highest_version_id, uri, name, is_protected);
		return highest_version_id;
	}

	//FIXME: store versions next to originals. will crash on ro locations.
	private System.Uri GetUriForVersionName (string version_name, string extension)
	{
		string name_without_extension = System.IO.Path.GetFileNameWithoutExtension (Name);

		return new System.Uri (System.IO.Path.Combine (DirectoryPath,  name_without_extension 
					       + " (" + version_name + ")" + extension));
	}

	public bool VersionNameExists (string version_name)
	{
		foreach (PhotoVersion v in Versions.Values)
			if (v.Name == version_name)
				return true;

		return false;
	}

	[Obsolete ("use GetVersion (uint).Name")]
	public string GetVersionName (uint version_id)
	{
		PhotoVersion v = GetVersion (version_id) as PhotoVersion;
		if (v != null)
			return v.Name;
		return null;
	}

	[Obsolete ("Use VersionUri (uint) instead")]
        public string GetVersionPath (uint version_id)
	{
		return VersionUri (version_id).LocalPath;
	}

	public System.Uri VersionUri (uint version_id)
	{
		if (!Versions.ContainsKey (version_id))
			return null;

		PhotoVersion v = Versions [version_id]; 
		if (v != null)
			return v.Uri;

		return null;
	}
	
	public System.Uri DefaultVersionUri {
		get { return VersionUri (DefaultVersionId); }
	}

	public PhotoVersion DefaultVersion {
		get {
			if (!Versions.ContainsKey (DefaultVersionId))
				return null;
			return Versions [DefaultVersionId]; 
		}
	}
	public void DeleteVersion (uint version_id)
	{
		DeleteVersion (version_id, false, false);
	}

	public void DeleteVersion (uint version_id, bool remove_original)
	{
		DeleteVersion (version_id, remove_original, false);
	}

	public void DeleteVersion (uint version_id, bool remove_original, bool keep_file)
	{
		if (version_id == OriginalVersionId && !remove_original)
			throw new Exception ("Cannot delete original version");

		System.Uri uri =  VersionUri (version_id);

		if (!keep_file) {
			if ((new Gnome.Vfs.Uri (uri.ToString ())).Exists) {
				if ((new Gnome.Vfs.Uri (uri.ToString ()).Unlink()) != Result.Ok)
					throw new System.UnauthorizedAccessException();
			}

			try {
				string thumb_path = ThumbnailGenerator.ThumbnailPath (uri);
				System.IO.File.Delete (thumb_path);
			} catch (System.Exception) {
				//ignore an error here we don't really care.
			}
			PhotoStore.DeleteThumbnail (uri);
		}
		Versions.Remove (version_id);

		do {
			version_id --;
			if (Versions.ContainsKey (version_id)) {
				DefaultVersionId = version_id;
				break;
			}
		} while (version_id > OriginalVersionId);
	}

	public uint CreateProtectedVersion (string name, uint base_version_id, bool create)
	{
		return CreateVersion (name, base_version_id, create, true);
	}

	public uint CreateVersion (string name, uint base_version_id, bool create)
	{
		return CreateVersion (name, base_version_id, create, false);
	}

	private uint CreateVersion (string name, uint base_version_id, bool create, bool is_protected)
	{
		System.Uri new_uri = GetUriForVersionName (name, System.IO.Path.GetExtension (VersionUri (base_version_id).AbsolutePath));
		System.Uri original_uri = VersionUri (base_version_id);

		if (VersionNameExists (name))
			throw new Exception ("This version name already exists");

		if (create) {
			if ((new Gnome.Vfs.Uri (new_uri.ToString ())).Exists)
				throw new Exception (String.Format ("An object at this uri {0} already exists", new_uri.ToString ()));

			Xfer.XferUri (
				new Gnome.Vfs.Uri (original_uri.ToString ()), 
				new Gnome.Vfs.Uri (new_uri.ToString ()),
				XferOptions.Default, XferErrorMode.Abort, 
				XferOverwriteMode.Abort, 
				delegate (Gnome.Vfs.XferProgressInfo info) {return 1;});

//			Mono.Unix.Native.Stat stat;
//			int stat_err = Mono.Unix.Native.Syscall.stat (original_path, out stat);
//			File.Copy (original_path, new_path);
			FSpot.ThumbnailGenerator.Create (new_uri).Dispose ();
//			
//			if (stat_err == 0) 
//				try {
//					Mono.Unix.Native.Syscall.chown(new_path, Mono.Unix.Native.Syscall.getuid (), stat.st_gid);
//				} catch (Exception) {}
		}
		highest_version_id ++;
		Versions [highest_version_id] = new PhotoVersion (this, highest_version_id, new_uri, name, is_protected);

		return highest_version_id;
	}

	public uint CreateReparentedVersion (PhotoVersion version)
	{
		return CreateReparentedVersion (version, false);
	}

	public uint CreateReparentedVersion (PhotoVersion version, bool is_protected)
	{
		int num = 0;
		while (true) {
			num++;
			string name = Catalog.GetPluralString ("Reparented", "Reparented ({0})", num);
			name = String.Format (name, num);
			if (VersionNameExists (name))
				continue;

			highest_version_id ++;
			Versions [highest_version_id] = new PhotoVersion (this, highest_version_id, version.Uri, name, is_protected);

			return highest_version_id;
		}
	}

	public uint CreateDefaultModifiedVersion (uint base_version_id, bool create_file)
	{
		int num = 1;

		while (true) {
			string name = Catalog.GetPluralString ("Modified", 
								 "Modified ({0})", 
								 num);
			name = String.Format (name, num);

			if (! VersionNameExists (name))
				return CreateVersion (name, base_version_id, create_file);

			num ++;
		}
	}

	public uint CreateNamedVersion (string name, uint base_version_id, bool create_file)
	{
		int num = 1;
		
		string final_name;
		while (true) {
			final_name = String.Format (
					Catalog.GetPluralString ("Modified in {1}", "Modified in {1} ({0})", num),
					num, name);

			if (num > 1)
				final_name = name + String.Format(" ({0})", num);

			if (! VersionNameExists (final_name))
				return CreateVersion (final_name, base_version_id, create_file);

			num ++;
		}
	}

	public void RenameVersion (uint version_id, string new_name)
	{
		if (version_id == OriginalVersionId)
			throw new Exception ("Cannot rename original version");

		if (VersionNameExists (new_name))
			throw new Exception ("This name already exists");

		(GetVersion (version_id) as PhotoVersion).Name = new_name;

		//TODO: rename file too ???

//		if (System.IO.File.Exists (new_path))
//			throw new Exception ("File with this name already exists");
//
//		File.Move (old_path, new_path);
//		PhotoStore.MoveThumbnail (old_path, new_path);
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

	private static FSpot.Xmp.XmpFile UpdateXmp (FSpot.IBrowsableItem item, FSpot.Xmp.XmpFile xmp)
	{
		if (xmp == null) 
			xmp = new FSpot.Xmp.XmpFile ();

		Tag [] tags = item.Tags;
		string [] names = new string [tags.Length];
		
		for (int i = 0; i < tags.Length; i++)
			names [i] = tags [i].Name;
		
		xmp.Store.Update ("dc:subject", "rdf:Bag", names);
		try { 
			xmp.Store.Update ("xmp:Rating", (item as Photo).Rating.ToString());
// FIXME - Should we also store/overwrite the Urgency field?
//			uint urgency_value = (item as Photo).Rating + 1; // Urgency valid values 1 - 8
//			xmp.Store.Update ("photoshop:Urgency", urgency_value.ToString());
		} catch (NotRatedException) {
			xmp.Store.Delete ("xmp:Rating");
		}
		xmp.Dump ();

		return xmp;
	}

	//FIXME: Won't work on non-file uris
	public void WriteMetadataToImage ()
	{
		string path = this.DefaultVersionUri.LocalPath;

		using (FSpot.ImageFile img = FSpot.ImageFile.Create (DefaultVersionUri)) {
			if (img is FSpot.JpegFile) {
				FSpot.JpegFile jimg = img as FSpot.JpegFile;
			
				jimg.SetDescription (this.Description);
				jimg.SetDateTimeOriginal (this.Time.ToLocalTime ());
				jimg.SetXmp (UpdateXmp (this, jimg.Header.GetXmp ()));

				jimg.SaveMetaData (path);
			} else if (img is FSpot.Png.PngFile) {
				FSpot.Png.PngFile png = img as FSpot.Png.PngFile;
			
				if (img.Description != this.Description)
					png.SetDescription (this.Description);
			
				png.SetXmp (UpdateXmp (this, png.GetXmp ()));

				png.Save (path);
			}
		}
	}

	//FIXME: won't work on non file uris
	public uint SaveVersion (Gdk.Pixbuf buffer, bool create_version)
	{
		uint version = DefaultVersionId;
		using (ImageFile img = ImageFile.Create (DefaultVersionUri)) {
			// Always create a version if the source is not a jpeg for now.
			create_version = create_version || !(img is FSpot.JpegFile);

			if (buffer == null)
				throw new ApplicationException ("invalid (null) image");

			if (create_version)
				version = CreateDefaultModifiedVersion (DefaultVersionId, false);

			try {
				string version_path = GetVersionPath (version);
			
				using (Stream stream = System.IO.File.OpenWrite (version_path)) {
					img.Save (buffer, stream);
				}
				FSpot.ThumbnailGenerator.Create (version_path).Dispose ();
				DefaultVersionId = version;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				if (create_version)
					DeleteVersion (version);
			
				throw e;
			}
		}
		
		return version;
	}

	// Constructor
	public Photo (uint id, long unix_time, System.Uri uri)
		: base (id)
	{
		if (uri == null)
			throw new System.ArgumentNullException ("uri");

		time = DbUtils.DateTimeFromUnixTime (unix_time);

		description = String.Empty;
		rated = false;

		// Note that the original version is never stored in the photo_versions table in the
		// database.
		AddVersionUnsafely (OriginalVersionId, uri, Catalog.GetString ("Original"), true);
	}

	[Obsolete ("Use Photo (uint, long, Uri) instead")]
	public Photo (uint id, long unix_time, string directory_path, string name)
		: this (id, unix_time, System.IO.Path.Combine (directory_path, name))
	{
	}

	[Obsolete ("Use Photo (uint, long, Uri) instead")]
	public Photo (uint id, long unix_time, string path)
		: this (id, unix_time, UriList.PathToFileUri (path))
	{
	}

}

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

//	[Obsolete ("use DeleteThumbnail (System.Uri) instead")]
//	public static void DeleteThumbnail (string path)
//	{
//		DeleteThumbnail (UriList.PathToFileUri (path));
//	}

	public static void DeleteThumbnail (System.Uri uri)
	{
		string path = Thumbnail.PathForUri (uri.ToString (), ThumbnailSize.Large);
		if (System.IO.File.Exists (path))
			System.IO.File.Delete (path);
	}

	public static void MoveThumbnail (string old_path, string new_path)
	{
		System.IO.File.Move (ThumbnailGenerator.ThumbnailPath (UriList.PathToFileUri (old_path)),
			   ThumbnailGenerator.ThumbnailPath(UriList.PathToFileUri (new_path)));
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
		return Create (UriList.PathToFileUri (new_path), UriList.PathToFileUri (orig_path), roll_id, out thumbnail);
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
				"uri", new_uri.ToString (),
	 			"description", description,
				"roll_id", roll_id,
	 			"default_version_id", Photo.OriginalVersionId,
	 			"rating", null));
	
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
			System.Uri uri = new System.Uri (reader[2].ToString ());
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
				System.Uri uri = new System.Uri (reader[3].ToString ());
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

	private void GetData (Photo photo)
	{
		SqliteDataReader reader = Database.Query(new DbCommand("SELECT tag_id, version_id, name, uri, protected "
                                                             + "FROM photo_tags, photo_versions "
                                                             + "WHERE photo_tags.photo_id = photo_versions.photo_id "
                                                             + "AND photo_tags.photo_id = :id", "id", photo.Id));

		while (reader.Read ()) {
		        if (reader [0] != null) {
				uint tag_id = Convert.ToUInt32 (reader [0]);
				Tag tag = Core.Database.Tags.Get (tag_id) as Tag;
				photo.AddTagUnsafely (tag);
			}
			if (reader [1] != null) {
				uint version_id = Convert.ToUInt32 (reader [1]);
				string name = reader[2].ToString ();
				System.Uri uri = new System.Uri (reader[3].ToString ());
				bool is_protected = Convert.ToBoolean (reader[4]);	
				photo.AddVersionUnsafely (version_id, uri, name, is_protected);
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
				new System.Uri (reader [1].ToString ()));

			photo.Description = reader[2].ToString ();
			photo.RollId = Convert.ToUInt32 (reader[3]);
			photo.DefaultVersionId = Convert.ToUInt32 (reader[4]);
			if (reader [5] != null)
				photo.Rating = Convert.ToUInt32 (reader [5]);
			else
				photo.RemoveRating();
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
		return GetByUri (UriList.PathToFileUri (path));
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
			if (reader [5] != null)
				photo.Rating = Convert.ToUInt32 (reader [5]);
			else
				photo.RemoveRating();
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

		Database.ExecuteNonQuery (String.Format ("DELETE FROM photos WHERE {0}", query_builder.ToString ()));
		Database.ExecuteNonQuery (String.Format ("DELETE FROM photo_tags WHERE {0}", tv_query_builder.ToString ()));
		Database.ExecuteNonQuery (String.Format ("DELETE FROM photo_versions WHERE {0}", tv_query_builder.ToString ()));

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

		uint rate = 0;
		bool rated = false;
		try {
			rate = photo.Rating;
			rated = true;
		} catch {
		}
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
			"uri", photo.VersionUri (Photo.OriginalVersionId).ToString (),
			"rating", (rated ? String.Format ("{0}", rate) : null),
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
	private string AddLastImportFilter (RollSet roll_set, bool added_where)
	{
		if (roll_set == null)
			return null;

		return  String.Format (" {0}{1}", added_where ? " AND " : " WHERE ", roll_set.SqlClause () );
	}
	
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
		query_builder.Append("ORDER BY time");
		return Query (query_builder.ToString ());
	}

	public Photo [] Query (IQueryCondition condition, params IQueryCondition [] conditions)
	{
		IQueryCondition [] conds = new IQueryCondition [conditions.Length + 1];
		conds [0] = condition;
		for (int i=0; i < conditions.Length; i++)
			conds [i + 1] = conditions [i];
		return Query (conds);
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
						   new System.Uri (reader [2].ToString ()));
				
				photo.Description = reader[3].ToString ();
				photo.RollId = Convert.ToUInt32 (reader[4]);
				photo.DefaultVersionId = Convert.ToUInt32 (reader[5]);
				if (reader [6] != null)
					photo.Rating = Convert.ToUInt32 (reader [6]);
				else
					photo.RemoveRating ();
				
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
	public Photo [] QueryUntagged (params IQueryCondition [] conditions)
	{
		return Query (new Untagged (), conditions);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange)
	{
		return Query (OrTerm.FromTags(tags), extra_condition, range, importidrange, null);
	}

	[Obsolete ("drop this, use IQueryCondition correctly instead")]
	public Photo [] Query (Tag [] tags, string extra_condition, DateRange range, RollSet importidrange, RatingRange ratingrange)
	{
		return Query (OrTerm.FromTags(tags), extra_condition, range, importidrange, ratingrange);
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

		if (extra_condition != null) {
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

#if TEST_PHOTO_STORE
	static void Dump (Photo photo)
	{
	//	Console.WriteLine ("\t[{0}] {1}", photo.Id, photo.Path);
		Console.WriteLine ("\t{0}", photo.Time.ToLocalTime ());

		if (photo.Description != String.Empty)
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

		Photo ny_landscape = db.Photos.Create (DateTime.Now.ToUniversalTime (), 1, "/home/ettore/Photos/ny_landscape.jpg",
						       out unused_thumbnail);
		ny_landscape.Description = "Pretty NY skyline";
		ny_landscape.AddTag (landscapes_tag);
		ny_landscape.AddTag (favorites_tag);
		db.Photos.Commit (ny_landscape);

		Photo me_in_sf = db.Photos.Create (DateTime.Now.ToUniversalTime (), 2, "/home/ettore/Photos/me_in_sf.jpg",
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

		Photo macro_shot = db.Photos.Create (DateTime.Now.ToUniversalTime (), 2, "/home/ettore/Photos/macro_shot.jpg",
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
