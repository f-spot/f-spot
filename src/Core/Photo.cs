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
using Gnome.Vfs;

using FSpot.Utils;

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
		public uint Rating {
			get { return rating; }
			set {
				if (value >= 0 && value <= 5)
					rating = value;
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
	
#if MONO_2_0
			return new System.Uri (System.IO.Path.Combine (DirectoryPath,  name_without_extension 
						       + " (" + UriUtils.EscapeString (version_name, false, true, true) + ")" + extension));
#else
			return new System.Uri (System.IO.Path.Combine (DirectoryPath,  name_without_extension 
						       + " (" + UriUtils.EscapeString (version_name, false, true, true) + ")" + extension), true);
#endif
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
					string version_path = VersionUri (version).LocalPath;
				
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
				// Note for translators: Reparented is a picture becoming a version of another one
				string name = (num == 1) ? Catalog.GetString ("Reparented") : String.Format (Catalog.GetString( "Reparented ({0})"), num);
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
						(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"),
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
	

		// Constructor
		public Photo (uint id, long unix_time, System.Uri uri)
			: base (id)
		{
			if (uri == null)
				throw new System.ArgumentNullException ("uri");
	
			time = DbUtils.DateTimeFromUnixTime (unix_time);
	
			description = String.Empty;
			rating = 0;
	
			// Note that the original version is never stored in the photo_versions table in the
			// database.
			AddVersionUnsafely (OriginalVersionId, uri, Catalog.GetString ("Original"), true);
		}
	}
}
