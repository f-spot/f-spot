//
// Photo.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2008-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using Hyena;

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Mono.Unix;

using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;

namespace FSpot.Core
{
	public class Photo : DbItem, IComparable<Photo>, IComparable, IPhotoVersionable
	{
		#region fields

		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;

		#endregion

		#region Properties
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
		DateTime time;
		public DateTime Time {
			get { return time; }
			set {
				if (time == value)
					return;
				time = value;
				changes.TimeChanged = true;
			}
		}

		public string Name => Uri.UnescapeDataString (Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath));

		readonly List<Tag> tags;
		public Tag [] Tags => tags.ToArray ();

		bool all_versions_loaded = false;
		public bool AllVersionsLoaded {
			get { return all_versions_loaded; }
			set {
				if (value)
					if (DefaultVersionId != OriginalVersionId && !versions.ContainsKey (DefaultVersionId))
						DefaultVersionId = OriginalVersionId;
				all_versions_loaded = value;
			}
		}

		string description;
		public string Description {
			get { return description; }
			set {
				if (description == value)
					return;
				description = value;
				changes.DescriptionChanged = true;
			}
		}

		uint roll_id = 0;
		public uint RollId {
			get { return roll_id; }
			set {
				if (roll_id == value)
					return;
				roll_id = value;
				changes.RollIdChanged = true;
			}
		}

		uint rating;
		public uint Rating {
			get { return rating; }
			set {
				if (rating == value || value > 5)
					return;
				rating = value;
				changes.RatingChanged = true;
			}
		}
		#endregion

		#region Properties Version Management
		public const int OriginalVersionId = 1;
		uint highest_version_id;
		readonly Dictionary<uint, PhotoVersion> versions = new Dictionary<uint, PhotoVersion> ();

		public IEnumerable<IPhotoVersion> Versions {
			get {
				foreach (var version in versions.Values) {
					yield return version;
				}
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

		uint default_version_id = OriginalVersionId;

		public uint DefaultVersionId {
			get { return default_version_id; }
			set {
				if (default_version_id == value)
					return;
				default_version_id = value;
				changes.DefaultVersionIdChanged = true;
			}
		}
		#endregion

		#region Photo Version Management
		public PhotoVersion GetVersion (uint versionId)
		{
			return versions? [versionId];
		}

		// This doesn't check if a version of that name already exists,
		// it's supposed to be used only within the Photo and PhotoStore classes.
		public void AddVersionUnsafely (uint versionId, SafeUri baseUri, string filename, string importMd5, string name, bool isProtected)
		{
			versions [versionId] = new PhotoVersion (this, versionId, baseUri, filename, importMd5, name, isProtected);

			highest_version_id = Math.Max (versionId, highest_version_id);
			changes.AddVersion (versionId);
		}

		public uint AddVersion (SafeUri baseUri, string filename, string name)
		{
			return AddVersion (baseUri, filename, name, false);
		}

		public uint AddVersion (SafeUri baseUri, string filename, string name, bool isProtected)
		{
			if (VersionNameExists (name))
				throw new ApplicationException ("A version with that name already exists");
			highest_version_id ++;
			var importMd5 = string.Empty; // Modified version

			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, baseUri, filename, importMd5, name, isProtected);

			changes.AddVersion (highest_version_id);
			return highest_version_id;
		}

		//FIXME: store versions next to originals. will crash on ro locations.
		string GetFilenameForVersionName (string versionName, string extension)
		{
			var nameWithoutExtension = Path.GetFileNameWithoutExtension (Name);

			var escapeString = UriUtils.EscapeString (versionName, true, true, true);
			return $"{nameWithoutExtension} ({escapeString}){extension}";
		}

		public bool VersionNameExists (string versionName)
		{
			return Versions.Any (v => v.Name == versionName);
		}

		public SafeUri VersionUri (uint versionId)
		{
			if (!versions.ContainsKey (versionId))
				return null;

			PhotoVersion v = versions [versionId];
			return v?.Uri;
		}

		public IPhotoVersion DefaultVersion {
			get {
				if (!versions.ContainsKey (DefaultVersionId))
					throw new Exception ("Something is horribly wrong, this should never happen: no default version!");
				return versions [DefaultVersionId];
			}
		}

		public void SetDefaultVersion (IPhotoVersion version)
		{
			PhotoVersion photoVersion = version as PhotoVersion;
			if (photoVersion == null)
				throw new ArgumentException ("Not a valid version for this photo");

			DefaultVersionId = photoVersion.VersionId;
		}


		//FIXME: won't work on non file uris
		public uint SaveVersion (Gdk.Pixbuf buffer, bool createVersion)
		{
			uint version = DefaultVersionId;
			using (var img = imageFileFactory.Create (DefaultVersion.Uri)) {
				// Always create a version if the source is not a jpeg for now.
				createVersion = createVersion || imageFileFactory.IsJpeg (DefaultVersion.Uri);

				if (buffer == null)
					throw new ApplicationException ("invalid (null) image");

				if (createVersion)
					version = CreateDefaultModifiedVersion (DefaultVersionId, false);

				try {
					var versionUri = VersionUri (version);

					PixbufUtils.CreateDerivedVersion (DefaultVersion.Uri, versionUri, 95, buffer);
					GetVersion (version).ImportMD5 = HashUtils.GenerateMD5 (VersionUri (version));
					DefaultVersionId = version;
				} catch (Exception e) {
					Log.Exception (e);
					if (createVersion)
						DeleteVersion (version);

					throw;
				}
			}

			return version;
		}

		public void DeleteVersion (uint versionId)
		{
			DeleteVersion (versionId, false, false);
		}

		public void DeleteVersion (uint versionId, bool removeOriginal)
		{
			DeleteVersion (versionId, removeOriginal, false);
		}

		public void DeleteVersion (uint versionId, bool removeOriginal, bool keepFile)
		{
			if (versionId == OriginalVersionId && !removeOriginal)
				throw new Exception ("Cannot delete original version");

			SafeUri uri = VersionUri (versionId);

			if (!keepFile) {
				GLib.File file = GLib.FileFactory.NewForUri (uri);
				if (file.Exists) {
					try {
						file.Trash (null);
					} catch (GLib.GException) {
						Log.Debug ("Unable to Trash, trying to Delete");
						file.Delete ();
					}
				}

				try {
					thumbnailService.DeleteThumbnails (uri);
				} catch {
					// ignore an error here we don't really care.
				}

				// do we really need to check if the parent is a directory?
				// i.e. is file.Parent always a directory if the file instance is
				// an actual file?
				GLib.File directory = file.Parent;
				GLib.FileType file_type = directory.QueryFileType (GLib.FileQueryInfoFlags.None, null);

				if (directory.Exists && file_type == GLib.FileType.Directory)
					DeleteEmptyDirectory (directory);
			}

			versions.Remove (versionId);
			changes.RemoveVersion (versionId);

			for (versionId = highest_version_id; versionId >= OriginalVersionId; versionId--) {
				if (versions.ContainsKey (versionId)) {
					DefaultVersionId = versionId;
					break;
				}
			}
		}

		void DeleteEmptyDirectory (GLib.File directory)
		{
			// if the directory we're dealing with is not in the
			// F-Spot photos directory, don't delete anything,
			// even if it is empty
			string photoUri = SafeUri.UriToFilename (Global.PhotoUri.ToString ());
			bool pathMatched = directory.Path.IndexOf (photoUri) > -1;

			if (directory.Path.Equals (photoUri) || !pathMatched)
				return;

			if (!DirectoryIsEmpty (directory))
				return;

			try {
				Log.DebugFormat ("Removing empty directory: {0}", directory.Path);
				directory.Delete ();
			} catch (GLib.GException e) {
				// silently log the exception, but don't re-throw it
				// as to not annoy the user
				Log.Exception (e);
			}
			// check to see if the parent is empty
			DeleteEmptyDirectory (directory.Parent);
		}

		bool DirectoryIsEmpty (GLib.File directory)
		{
			uint count = 0;
			using (GLib.FileEnumerator list = directory.EnumerateChildren ("standard::name", GLib.FileQueryInfoFlags.None, null)) {
				foreach (var item in list) {
					count++;
				}
			}
			return count == 0;
		}

		public uint CreateVersion (string name, uint baseVersionId, bool create)
		{
			return CreateVersion (name, null, baseVersionId, create);
		}

		uint CreateVersion (string name, string extension, uint baseVersionId, bool create, bool isProtected = false)
		{
			extension = extension ?? VersionUri (baseVersionId).GetExtension ();
			SafeUri newBaseUri = DefaultVersion.BaseUri;
			string filename = GetFilenameForVersionName (name, extension);
			SafeUri originalUri = VersionUri (baseVersionId);
			SafeUri newUri = newBaseUri.Append (filename);
			string importMd5 = DefaultVersion.ImportMD5;

			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");

			if (create) {
				GLib.File destination = GLib.FileFactory.NewForUri (newUri);
				if (destination.Exists)
					throw new Exception ($"An object at this uri {newUri} already exists");

				//FIXME. or better, fix the copy api !
				GLib.File source = GLib.FileFactory.NewForUri (originalUri);
				source.Copy (destination, GLib.FileCopyFlags.None, null, null);
			}
			highest_version_id ++;

			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, newBaseUri, filename, importMd5, name, isProtected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}

		public uint CreateReparentedVersion (PhotoVersion version)
		{
			return CreateReparentedVersion (version, false);
		}

		public uint CreateReparentedVersion (PhotoVersion version, bool isProtected)
		{
			// Try to derive version name from its filename
			string filename = Uri.UnescapeDataString (Path.GetFileNameWithoutExtension (version.Uri.AbsolutePath));
			string parentFilename = Path.GetFileNameWithoutExtension (Name);
			string name = null;
			if (filename.StartsWith (parentFilename))
				name = filename.Substring (parentFilename.Length).Replace ("(", "").Replace (")", "").Replace ("_", " "). Trim ();

			if (string.IsNullOrEmpty (name)) {
				// Note for translators: Reparented is a picture becoming a version of another one
				string rep = name = Catalog.GetString ("Reparented");
				for (int num = 1; VersionNameExists (name); num++) {
					name = string.Format (rep + " ({0})", num);
				}
			}
			highest_version_id ++;
			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, version.BaseUri, version.Filename, version.ImportMD5, name, isProtected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}

		public uint CreateDefaultModifiedVersion (uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				string name = Catalog.GetPluralString ("Modified", "Modified ({0})", num);
				name = string.Format (name, num);
				//SafeUri uri = GetUriForVersionName (name, System.IO.Path.GetExtension (VersionUri(base_version_id).GetFilename()));
				string filename = GetFilenameForVersionName (name, Path.GetExtension (versions [baseVersionId].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);
				GLib.File file = GLib.FileFactory.NewForUri (uri);

				if (! VersionNameExists (name) && ! file.Exists)
					return CreateVersion (name, baseVersionId, createFile);

				num ++;
			}
		}

		public uint CreateNamedVersion (string name, string extension, uint baseVersionId, bool createFile)
		{
			int num = 1;

			string final_name;
			while (true) {
				final_name = string.Format (
						(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"),
						num, name);

				string filename = GetFilenameForVersionName (name, Path.GetExtension (versions [baseVersionId].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);
				GLib.File file = GLib.FileFactory.NewForUri (uri);

				if (! VersionNameExists (final_name) && ! file.Exists)
					return CreateVersion (final_name, extension, baseVersionId, createFile);

				num ++;
			}
		}

		public void RenameVersion (uint versionId, string newName)
		{
			if (versionId == OriginalVersionId)
				throw new Exception ("Cannot rename original version");

			if (VersionNameExists (newName))
				throw new Exception ("This name already exists");


			GetVersion (versionId).Name = newName;
			changes.ChangeVersion (versionId);

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
		#endregion

		#region Tag management
		// This doesn't check if the tag is already there, use with caution.
		public void AddTagUnsafely (Tag tag)
		{
			tags.Add (tag);
			changes.AddTag (tag);
		}

		// This on the other hand does, but is O(n) with n being the number of existing tags.
		public void AddTag (Tag tag)
		{
			if (!tags.Contains (tag))
				AddTagUnsafely (tag);
		}

		public void AddTag (IEnumerable<Tag> taglist)
		{
			/*
			 * FIXME need a better naming convention here, perhaps just
			 * plain Add.
			 *
			 * tags.AddRange (taglist);
			 *     but, AddTag calls AddTagUnsafely which
			 *     adds and calls changes.AddTag on each tag?
			 *     Need to investigate that.
			 */
			foreach (Tag tag in taglist) {
				AddTag (tag);
			}
		}

		public void RemoveTag (Tag tag)
		{
			if (!tags.Contains (tag))
				return;

			tags.Remove (tag);
			changes.RemoveTag (tag);
		}

		public void RemoveTag (Tag []taglist)
		{
			foreach (Tag tag in taglist) {
				RemoveTag (tag);
			}
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

		// FIXME: This should be removed (I think)
		public bool HasTag (Tag tag)
		{
			return tags.Contains (tag);
		}

		// This seems senseless?
		static readonly IDictionary<SafeUri, string> Md5Cache = new Dictionary<SafeUri, string> ();
		public static void ResetMD5Cache ()
		{
			Md5Cache?.Clear ();
		}
		#endregion

		public Photo (IImageFileFactory imageFactory, IThumbnailService thumbnailService, uint id, long unixTime) : base (id)
		{
			imageFileFactory = imageFactory;
			this.thumbnailService = thumbnailService;

			time = DateTimeUtil.ToDateTime (unixTime);
			tags = new List<Tag> ();

			description = string.Empty;
			rating = 0;
		}

		public int CompareTo (object obj)
		{
			if (GetType () == obj.GetType ())
				return this.Compare((Photo)obj);

			if (obj is DateTime)
				return time.CompareTo ((DateTime)obj);

			throw new Exception ("Object must be of type Photo");
		}

		public int CompareTo (Photo photo)
		{
			int result = Id.CompareTo (photo.Id);

			if (result == 0)
				return 0;

			return this.Compare (photo);
		}
	}
}
