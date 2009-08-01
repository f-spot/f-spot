/*
 * FSpot.Photo.cs
 *
 * Author(s):
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Larry Ewing <lewing@gnome.org>
 *	Stephane Delcroix <stephane@delcroix.org>
 * 
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using FSpot.Utils;
using FSpot.Platform;

namespace FSpot
{
	public class Photo : DbItem, IComparable, IBrowsableItem {
		// IComparable 
		public int CompareTo (object obj) {
			if (this.GetType () == obj.GetType ()) {
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
	
		public class CompareDateName : IComparer<IBrowsableItem>
		{
			public int Compare (IBrowsableItem obj1, IBrowsableItem obj2)
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
	
		PhotoChanges changes = new PhotoChanges ();
		public PhotoChanges Changes {
			get{ return changes; }
			set {
				if (value != null)
					throw new ArgumentException ("The only valid value is null");
				changes = new PhotoChanges ();
			}
		}

		// The time is always in UTC.
		private DateTime time;
		public DateTime Time {
			get { return time; }
			set {
				if (time == value)
					return;
				time = value;
				changes.TimeChanged = true;
			}
		}
	
		public string Name {
			get { return Uri.UnescapeDataString (System.IO.Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath)); }
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
			set { 
				if (value) {
					if (DefaultVersionId != OriginalVersionId && !Versions.ContainsKey (DefaultVersionId)) 
						DefaultVersionId = OriginalVersionId;	
				}
				loaded = value; 
			}
		}
	
		private string description;
		public string Description {
			get { return description; }
			set {
				if (description == value)
					return;
				description = value;
				changes.DescriptionChanged = true;
			}
		}
	
		private uint roll_id = 0;
		public uint RollId {
			get { return roll_id; }
			set {
				if (roll_id == value)
					return;
				roll_id = value;
				changes.RollIdChanged = true;
			}
		}
	
		private uint rating;
		public uint Rating {
			get { return rating; }
			set {
				if (rating == value || value < 0 || value > 5)
					return;
				rating = value;
				changes.RatingChanged = true;
			}
		}

		private string md5_sum;
		public string MD5Sum {
			get { return md5_sum; }
			set { 
				if (md5_sum == value)
				 	return;

				md5_sum = value; 
				changes.MD5SumChanged = true;
			} 
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
			set {
				if (default_version_id == value)
					return;
				default_version_id = value;
				changes.DefaultVersionIdChanged = true;
			}
		}
	
		// This doesn't check if a version of that name already exists, 
		// it's supposed to be used only within the Photo and PhotoStore classes.
		internal void AddVersionUnsafely (uint version_id, System.Uri uri, string md5_sum, string name, bool is_protected)
		{
			Versions [version_id] = new PhotoVersion (this, version_id, uri, md5_sum, name, is_protected);
	
			highest_version_id = Math.Max (version_id, highest_version_id);
			changes.AddVersion (version_id);
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
			string md5_sum = GenerateMD5 (uri);

			Versions [highest_version_id] = new PhotoVersion (this, highest_version_id, uri, md5_sum, name, is_protected);

			changes.AddVersion (highest_version_id);
			return highest_version_id;
		}
	
		//FIXME: store versions next to originals. will crash on ro locations.
		private System.Uri GetUriForVersionName (string version_name, string extension)
		{
			string name_without_extension = System.IO.Path.GetFileNameWithoutExtension (Name);
	
			return new System.Uri (System.IO.Path.Combine (DirectoryPath,  name_without_extension 
						       + " (" + UriUtils.EscapeString (version_name, true, true, true) + ")" + extension));
		}
	
		public bool VersionNameExists (string version_name)
		{
			foreach (PhotoVersion v in Versions.Values)
				if (v.Name == version_name)
					return true;
	
			return false;
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
					Uri versionUri = VersionUri (version);

					using (Stream stream = System.IO.File.OpenWrite (versionUri.LocalPath)) {
						img.Save (buffer, stream);
					}
					(GetVersion (version) as PhotoVersion).MD5Sum = GenerateMD5 (VersionUri (version));
					FSpot.ThumbnailGenerator.Create (versionUri).Dispose ();
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
				GLib.File file = GLib.FileFactory.NewForUri (uri);
				if (file.Exists) 
					try {
						file.Trash (null);
					} catch (GLib.GException) {
						Log.Debug ("Unable to Trash, trying to Delete");
						file.Delete ();
					}	
				try {
					ThumbnailFactory.DeleteThumbnail (uri);
				} catch {
					//ignore an error here we don't really care.
				}
			}
			Versions.Remove (version_id);

			changes.RemoveVersion (version_id);

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
			return CreateVersion (name, null, base_version_id, create, true);
		}
	
		public uint CreateVersion (string name, uint base_version_id, bool create)
		{
			return CreateVersion (name, null, base_version_id, create, false);
		}

		public uint CreateVersion (string name, string extension, uint base_version_id, bool create)
		{
			return CreateVersion (name, extension, base_version_id, create, false);
		}
	
		private uint CreateVersion (string name, string extension, uint base_version_id, bool create, bool is_protected)
		{
			extension = extension ?? System.IO.Path.GetExtension (VersionUri (base_version_id).AbsolutePath);
			System.Uri new_uri = GetUriForVersionName (name, extension);
			System.Uri original_uri = VersionUri (base_version_id);
			string md5_sum = MD5Sum;
	
			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");
	
			if (create) {
				GLib.File destination = GLib.FileFactory.NewForUri (new_uri);
				if (destination.Exists)
					throw new Exception (String.Format ("An object at this uri {0} already exists", new_uri.ToString ()));
	
		//FIXME. or better, fix the copy api !
				GLib.File source = GLib.FileFactory.NewForUri (original_uri);
				source.Copy (destination, GLib.FileCopyFlags.None, null, null);
	
				FSpot.ThumbnailGenerator.Create (new_uri).Dispose ();
			}
			highest_version_id ++;

			Versions [highest_version_id] = new PhotoVersion (this, highest_version_id, new_uri, md5_sum, name, is_protected);

			changes.AddVersion (highest_version_id);
	
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
				// Note for translators: Reparented is a picture becoming a version of another one
				string name = (num == 1) ? Catalog.GetString ("Reparented") : String.Format (Catalog.GetString( "Reparented ({0})"), num);
				name = String.Format (name, num);
				if (VersionNameExists (name))
					continue;
	
				highest_version_id ++;
				Versions [highest_version_id] = new PhotoVersion (this, highest_version_id, version.Uri, version.MD5Sum, name, is_protected);

				changes.AddVersion (highest_version_id);

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
	
		public uint CreateNamedVersion (string name, uint baseVersionId, bool createFile)
		{
			return CreateNamedVersion (name, baseVersionId, createFile);
		}

		public uint CreateNamedVersion (string name, string extension, uint base_version_id, bool create_file)
		{
			int num = 1;
			
			string final_name;
			while (true) {
				final_name = String.Format (
						(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"),
						num, name);
	
				if (! VersionNameExists (final_name))
					return CreateVersion (final_name, extension, base_version_id, create_file);
	
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

			changes.ChangeVersion (version_id);
	
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
			changes.AddTag (tag);
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
			if (!HasTag (tag))
				return;

			tags.Remove (tag);
			changes.RemoveTag (tag);
		}
	
		public void RemoveTag (Tag []taglist)
		{	
			foreach (Tag tag in taglist)
				RemoveTag (tag);
		}	
	
		public void RemoveCategory (IList<Tag> taglist)
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
	
		//
		// MD5 Calculator
		//
		private static System.Security.Cryptography.MD5 md5_generator;

		private static System.Security.Cryptography.MD5 MD5Generator {
			get {
				if (md5_generator == null)
				 	md5_generator = new System.Security.Cryptography.MD5CryptoServiceProvider ();

				return md5_generator;
			} 
		}

		private static IDictionary<System.Uri, string> md5_cache = new Dictionary<System.Uri, string> ();

		public static void ResetMD5Cache () {
			if (md5_cache != null)	
				md5_cache.Clear (); 
		}

		public static string GenerateMD5 (System.Uri uri)
		{
		 	try {
			 	if (md5_cache.ContainsKey (uri))
				 	return md5_cache [uri];

				using (Gdk.Pixbuf pixbuf = ThumbnailGenerator.Create (uri))
				{
					byte[] serialized = GdkUtils.Serialize (pixbuf);
					byte[] md5 = MD5Generator.ComputeHash (serialized);
					string md5_string = Convert.ToBase64String (md5);

					md5_cache.Add (uri, md5_string);
					return md5_string;
				}
			} catch (Exception e) {
			 	Log.DebugException (String.Format ("Failed to create MD5Sum for Uri: {0}\n", uri), e);
			}

			return string.Empty; 
		}


		// Constructor
		public Photo (uint id, long unix_time, System.Uri uri, string md5_sum)
			: base (id)
		{
			if (uri == null)
				throw new System.ArgumentNullException ("uri");
	
			time = DbUtils.DateTimeFromUnixTime (unix_time);
	
			description = String.Empty;
			rating = 0;
			this.md5_sum = md5_sum;
	
			// Note that the original version is never stored in the photo_versions table in the
			// database.
			AddVersionUnsafely (OriginalVersionId, uri, md5_sum, Catalog.GetString ("Original"), true);
		}
	}
}
