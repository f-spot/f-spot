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

using Hyena;

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using FSpot.Utils;
using FSpot.Platform;
using FSpot.Imaging;

namespace FSpot
{
	public class Photo : DbItem, IComparable, IBrowsableItem, IBrowsableItemVersionable {
		
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
					if (DefaultVersionId != OriginalVersionId && !versions.ContainsKey (DefaultVersionId)) 
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

		// Version management
		public const int OriginalVersionId = 1;
		private uint highest_version_id;
	
		private Dictionary<uint, PhotoVersion> versions = new Dictionary<uint, PhotoVersion> ();
		public IEnumerable<IBrowsableItemVersion> Versions {
			get {
				foreach (var version in versions.Values)
					yield return version;
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

		public PhotoVersion GetVersion (uint version_id)
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
		internal void AddVersionUnsafely (uint version_id, SafeUri base_uri, string filename, string import_md5, string name, bool is_protected)
		{
			versions [version_id] = new PhotoVersion (this, version_id, base_uri, filename, import_md5, name, is_protected);
	
			highest_version_id = Math.Max (version_id, highest_version_id);
			changes.AddVersion (version_id);
		}
	
		public uint AddVersion (SafeUri base_uri, string filename, string name)
		{
			return AddVersion (base_uri, filename, name, false);
		}
	
		public uint AddVersion (SafeUri base_uri, string filename, string name, bool is_protected)
		{
			if (VersionNameExists (name))
				throw new ApplicationException ("A version with that name already exists");
			highest_version_id ++;
			string import_md5 = String.Empty; // Modified version

			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, base_uri, filename, import_md5, name, is_protected);

			changes.AddVersion (highest_version_id);
			return highest_version_id;
		}
	
		//FIXME: store versions next to originals. will crash on ro locations.
		private string GetFilenameForVersionName (string version_name, string extension)
		{
			string name_without_extension = System.IO.Path.GetFileNameWithoutExtension (Name);
	
			return name_without_extension + " (" +
				UriUtils.EscapeString (version_name, true, true, true)
				+ ")" + extension;
		}
	
		public bool VersionNameExists (string version_name)
		{
            return Versions.Where ((v) => v.Name == version_name).Any ();
		}

		public SafeUri VersionUri (uint version_id)
		{
			if (!versions.ContainsKey (version_id))
				return null;
	
			PhotoVersion v = versions [version_id]; 
			if (v != null)
				return v.Uri;
	
			return null;
		}
		
		public IBrowsableItemVersion DefaultVersion {
			get {
				if (!versions.ContainsKey (DefaultVersionId))
					throw new Exception ("Something is horribly wrong, this should never happen: no default version!");
				return versions [DefaultVersionId]; 
			}
		}

		public void SetDefaultVersion (IBrowsableItemVersion version)
		{
			PhotoVersion photo_version = version as PhotoVersion;
			if (photo_version == null)
				throw new ArgumentException ("Not a valid version for this photo");

			DefaultVersionId = photo_version.VersionId;
		}


		//FIXME: won't work on non file uris
		public uint SaveVersion (Gdk.Pixbuf buffer, bool create_version)
		{
			uint version = DefaultVersionId;
			using (var img = ImageFile.Create (DefaultVersion.Uri)) {
				// Always create a version if the source is not a jpeg for now.
				create_version = create_version || ImageFile.IsJpeg (DefaultVersion.Uri);
	
				if (buffer == null)
					throw new ApplicationException ("invalid (null) image");
	
				if (create_version)
					version = CreateDefaultModifiedVersion (DefaultVersionId, false);
	
				try {
					var versionUri = VersionUri (version);

					PixbufUtils.CreateDerivedVersion (DefaultVersion.Uri, versionUri, 95, buffer);
					(GetVersion (version) as PhotoVersion).ImportMD5 = HashUtils.GenerateMD5 (VersionUri (version));
					DefaultVersionId = version;
				} catch (System.Exception e) {
					Log.Exception (e);
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
	
			SafeUri uri =  VersionUri (version_id);
	
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
					XdgThumbnailSpec.RemoveThumbnail (uri);
				} catch {
					//ignore an error here we don't really care.
				}
			}
			versions.Remove (version_id);

			changes.RemoveVersion (version_id);

			for (version_id = highest_version_id; version_id >= OriginalVersionId; version_id--) {
				if (versions.ContainsKey (version_id)) {
					DefaultVersionId = version_id;
					break;
				}
			}
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
			SafeUri new_base_uri = DefaultVersion.BaseUri;
			string filename = GetFilenameForVersionName (name, extension);
			SafeUri original_uri = VersionUri (base_version_id);
			SafeUri new_uri = new_base_uri.Append (filename);
			string import_md5 = DefaultVersion.ImportMD5;
	
			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");
	
			if (create) {
				GLib.File destination = GLib.FileFactory.NewForUri (new_uri);
				if (destination.Exists)
					throw new Exception (String.Format ("An object at this uri {0} already exists", new_uri));
	
		//FIXME. or better, fix the copy api !
				GLib.File source = GLib.FileFactory.NewForUri (original_uri);
				source.Copy (destination, GLib.FileCopyFlags.None, null, null);
			}
			highest_version_id ++;

			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, new_base_uri, filename, import_md5, name, is_protected);

			changes.AddVersion (highest_version_id);
	
			return highest_version_id;
		}
	
		public uint CreateReparentedVersion (PhotoVersion version)
		{
			return CreateReparentedVersion (version, false);
		}
	
		public uint CreateReparentedVersion (PhotoVersion version, bool is_protected)
		{
			// Try to derive version name from its filename
			string filename = Uri.UnescapeDataString (Path.GetFileNameWithoutExtension (version.Uri.AbsolutePath));
			string parent_filename = Path.GetFileNameWithoutExtension (Name);
			string name = null;
			if (filename.StartsWith (parent_filename))
				name = filename.Substring (parent_filename.Length).Replace ("(", "").Replace (")", "").Replace ("_", " "). Trim();
			
			if (String.IsNullOrEmpty (name)) {
				// Note for translators: Reparented is a picture becoming a version of another one
				string rep = name = Catalog.GetString ("Reparented");
				for (int num = 1; VersionNameExists (name); num++) 
					name = String.Format (rep + " ({0})", num);
			}
			highest_version_id ++;
			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, version.BaseUri, version.Filename, version.ImportMD5, name, is_protected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}
	
		public uint CreateDefaultModifiedVersion (uint base_version_id, bool create_file)
		{
			int num = 1;
	
			while (true) {
				string name = Catalog.GetPluralString ("Modified", 
									 "Modified ({0})", 
									 num);
				name = String.Format (name, num);
				//SafeUri uri = GetUriForVersionName (name, System.IO.Path.GetExtension (VersionUri(base_version_id).GetFilename()));
				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions[base_version_id].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);
				GLib.File file = GLib.FileFactory.NewForUri (uri);
	
				if (! VersionNameExists (name) && ! file.Exists)
					return CreateVersion (name, base_version_id, create_file);
	
				num ++;
			}
		}
	
		public uint CreateNamedVersion (string name, string extension, uint base_version_id, bool create_file)
		{
			int num = 1;
			
			string final_name;
			while (true) {
				final_name = String.Format (
						(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"),
						num, name);
	
				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions[base_version_id].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);
				GLib.File file = GLib.FileFactory.NewForUri (uri);

				if (! VersionNameExists (final_name) && ! file.Exists)
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
		
		public void CopyAttributesFrom (Photo that) 
		{			
			Time = that.Time;
			Description = that.Description;
			Rating = that.Rating;
			AddTag (that.Tags);
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
	
		public void AddTag (IEnumerable<Tag> taglist)
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
	
		private static IDictionary<SafeUri, string> md5_cache = new Dictionary<SafeUri, string> ();

		public static void ResetMD5Cache () {
			if (md5_cache != null)	
				md5_cache.Clear (); 
		}

		// Constructor
		public Photo (uint id, long unix_time)
			: base (id)
		{
			time = DateTimeUtil.ToDateTime (unix_time);
	
			description = String.Empty;
			rating = 0;
		}

#region IComparable implementation

		// IComparable 
		public int CompareTo (object obj) {
			if (this.GetType () == obj.GetType ()) {
				return this.Compare((Photo)obj);
			} else if (obj is DateTime) {
				return this.time.CompareTo ((DateTime)obj);
			} else {
				throw new Exception ("Object must be of type Photo");
			}
		}

		public int CompareTo (Photo photo)
		{
			int result = Id.CompareTo (photo.Id);
			
			if (result == 0)
				return 0;
			else 
				return (this as IBrowsableItem).Compare (photo);
		}

#endregion
	}
}
