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

using FSpot.Core;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;


namespace FSpot
{
	public class Photo : DbItem, IComparable, IPhoto, IPhotoVersionable
	{
		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;

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

		public string Name {
			get { return Uri.UnescapeDataString (System.IO.Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath)); }
		}

		List<Tag> tags;
		public Tag [] Tags {
			get {
				return tags.ToArray ();
			}
		}

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
					return Array.Empty<uint> ();

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
			return versions?[versionId];
		}

		// This doesn't check if a version of that name already exists,
		// it's supposed to be used only within the Photo and PhotoStore classes.
		public void AddVersionUnsafely (uint version_id, SafeUri base_uri, string filename, string import_md5, string name, bool is_protected)
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
			string import_md5 = string.Empty; // Modified version

			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, base_uri, filename, import_md5, name, is_protected);

			changes.AddVersion (highest_version_id);
			return highest_version_id;
		}

		//FIXME: store versions next to originals. will crash on ro locations.
		string GetFilenameForVersionName (string version_name, string extension)
		{
			string name_without_extension = Path.GetFileNameWithoutExtension (Name);

			var escapeString = UriUtils.EscapeString (version_name, true, true, true);
			return $"{name_without_extension} ({escapeString}){extension}";
		}

		public bool VersionNameExists (string version_name)
		{
			return Versions.Any (v => v.Name == version_name);
		}

		public SafeUri VersionUri (uint version_id)
		{
			if (!versions.ContainsKey (version_id))
				return null;

			PhotoVersion v = versions [version_id];
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
			PhotoVersion photo_version = version as PhotoVersion;
			if (photo_version == null)
				throw new ArgumentException ("Not a valid version for this photo");

			DefaultVersionId = photo_version.VersionId;
		}


		//FIXME: won't work on non file uris
		public uint SaveVersion (Gdk.Pixbuf buffer, bool create_version)
		{
			uint version = DefaultVersionId;
			using (var img = imageFileFactory.Create (DefaultVersion.Uri)) {
				// Always create a version if the source is not a jpeg for now.
				create_version = create_version || imageFileFactory.IsJpeg (DefaultVersion.Uri);

				if (buffer == null)
					throw new ApplicationException ("invalid (null) image");

				if (create_version)
					version = CreateDefaultModifiedVersion (DefaultVersionId, false);

				try {
					var versionUri = VersionUri (version);

					FSpot.Utils.PixbufUtils.CreateDerivedVersion (DefaultVersion.Uri, versionUri, 95, buffer);
					GetVersion (version).ImportMD5 = HashUtils.GenerateMD5 (VersionUri (version));
					DefaultVersionId = version;
				} catch (Exception e) {
					Logger.Log.Error (e, "");
					if (create_version)
						DeleteVersion (version);

					throw;
				}
			}

			return version;
		}

		public void DeleteVersion (uint versionId, bool removeOriginal)
		{
			DeleteVersion (versionId, removeOriginal, false);
		}

		public void DeleteVersion (uint versionId, bool removeOriginal = false, bool keepFile = false)
		{
			if (versionId == OriginalVersionId && !removeOriginal)
				throw new Exception ("Cannot delete original version");

			SafeUri uri = VersionUri (versionId);
			var fileInfo = new FileInfo (uri.AbsolutePath);
			var dirInfo = new DirectoryInfo (uri.AbsolutePath);

			if (!keepFile) {
				if (fileInfo.Exists)
					fileInfo.Delete ();

				try {
					thumbnailService.DeleteThumbnails (uri);
				} catch {
					// ignore an error here we don't really care.
				}

				DeleteEmptyDirectory (dirInfo);
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

		void DeleteEmptyDirectory (DirectoryInfo directory)
		{
			// if the directory we're dealing with is not in the
			// F-Spot photos directory, don't delete anything,
			// even if it is empty
			string photo_uri = SafeUri.UriToFilename (FSpotConfiguration.PhotoUri.ToString ());
			bool path_matched = directory.FullName.IndexOf (photo_uri) > -1;

			if (directory.Name.Equals (photo_uri) || !path_matched)
				return;

			if (DirectoryIsEmpty (directory)) {
				try {
					Logger.Log.Debug ($"Removing empty directory: {directory.FullName}");
					directory.Delete ();
				} catch (GLib.GException e) {
					// silently log the exception, but don't re-throw it
					// as to not annoy the user
					Logger.Log.Error (e, "");
				}
				// check to see if the parent is empty
				DeleteEmptyDirectory (directory.Parent);
			}
		}

		bool DirectoryIsEmpty (DirectoryInfo directory)
			=> !directory.EnumerateFileSystemInfos ().Any ();

		public uint CreateVersion (string name, uint baseVersionId, bool create)
		{
			return CreateVersion (name, null, baseVersionId, create, false);
		}

		uint CreateVersion (string name, string extension, uint base_version_id, bool create, bool is_protected = false)
		{
			extension ??= VersionUri (base_version_id).GetExtension ();
			SafeUri new_base_uri = DefaultVersion.BaseUri;
			string filename = GetFilenameForVersionName (name, extension);
			SafeUri original_uri = VersionUri (base_version_id);
			SafeUri new_uri = new_base_uri.Append (filename);
			string import_md5 = DefaultVersion.ImportMD5;

			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");

			if (create) {
				if (File.Exists (new_uri.AbsolutePath))
					throw new Exception ($"An object at this uri {new_uri} already exists");

				File.Copy (original_uri.AbsolutePath, new_uri.AbsolutePath);
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
				name = filename.Substring (parent_filename.Length).Replace ("(", "").Replace (")", "").Replace ("_", " "). Trim ();

			if (string.IsNullOrEmpty (name)) {
				// Note for translators: Reparented is a picture becoming a version of another one
				string rep = name = Catalog.GetString ("Reparented");
				for (int num = 1; VersionNameExists (name); num++) {
					name = $"rep +  ({num})";
				}
			}

			highest_version_id ++;
			versions [highest_version_id] = new PhotoVersion (this, highest_version_id, version.BaseUri, version.Filename, version.ImportMD5, name, is_protected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}

		public uint CreateDefaultModifiedVersion (uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				string name = Catalog.GetPluralString ("Modified", "Modified ({0})", num);
				name = string.Format (name, num);
				//SafeUri uri = GetUriForVersionName (name, System.IO.Path.GetExtension (VersionUri(baseVersionId).GetFilename()));
				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions [baseVersionId].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);

				if (! VersionNameExists (name) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (name, baseVersionId, createFile);

				num ++;
			}
		}

		public uint CreateNamedVersion (string name, string extension, uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				var final_name = string.Format (
					(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"), num, name);

				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions [baseVersionId].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);

				if (! VersionNameExists (final_name) && !File.Exists (uri.AbsolutePath))
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
				if (tag is Category cat)
					RemoveCategory (cat.Children);

				RemoveTag (tag);
			}
		}

		// FIXME: This should be removed (I think)
		public bool HasTag (Tag tag)
		{
			return tags.Contains (tag);
		}

		static readonly IDictionary<SafeUri, string> md5_cache = new Dictionary<SafeUri, string> ();

		public static void ResetMD5Cache ()
		{
			md5_cache?.Clear ();
		}
		#endregion

		#region Constructor
		public Photo (IImageFileFactory imageFactory, IThumbnailService thumbnailService, uint id, long unix_time)
			: base (id)
		{
			this.imageFileFactory = imageFactory;
			this.thumbnailService = thumbnailService;

			time = DateTimeUtil.ToDateTime (unix_time);
			tags = new List<Tag> ();

			description = string.Empty;
			rating = 0;
		}
		#endregion

		#region IComparable implementation
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
		#endregion
	}
}
