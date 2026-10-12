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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;


namespace FSpot
{
	public class Photo : DbItem, IComparable<Photo>, IPhoto, IPhotoVersionable
	{
		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;

		PhotoChanges changes = new PhotoChanges ();
		public PhotoChanges Changes {
			get { return changes; }
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
			get { return Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath); }
		}

		readonly List<Tag> tags;
		public Tag[] Tags {
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

		public uint[] VersionIds {
			get {
				if (versions == null)
					return Array.Empty<uint> ();

				var ids = new uint[versions.Count];
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

		public PhotoVersion GetVersion (uint versionId)
		{
			return versions?[versionId];
		}

		// This doesn't check if a version of that name already exists,
		// it's supposed to be used only within the Photo and PhotoStore classes.
		public void AddVersionUnsafely (uint version_id, SafeUri base_uri, string filename, string import_md5, string name, bool is_protected)
		{
			versions[version_id] = new PhotoVersion (this, version_id, base_uri, filename, import_md5, name, is_protected);

			highest_version_id = Math.Max (version_id, highest_version_id);
			changes.AddVersion (version_id);
		}

		public uint AddVersion (SafeUri base_uri, string filename, string name, bool is_protected = false)
		{
			if (VersionNameExists (name))
				throw new ApplicationException ("A version with that name already exists");

			highest_version_id++;
			string import_md5 = string.Empty; // Modified version

			versions[highest_version_id] = new PhotoVersion (this, highest_version_id, base_uri, filename, import_md5, name, is_protected);

			changes.AddVersion (highest_version_id);
			return highest_version_id;
		}

		//FIXME: store versions next to originals. will crash on ro locations.
		string GetFilenameForVersionName (string version_name, string extension)
		{
			string name_without_extension = Path.GetFileNameWithoutExtension (Name);
			return $"{name_without_extension} ({version_name}){extension}";
		}

		public bool VersionNameExists (string version_name)
		{
			return Versions.Any (v => v.Name == version_name);
		}

		public SafeUri VersionUri (uint version_id)
		{
			if (!versions.ContainsKey (version_id))
				return null;

			PhotoVersion v = versions[version_id];
			return v?.Uri;
		}

		public IPhotoVersion DefaultVersion {
			get {
				if (!versions.ContainsKey (DefaultVersionId))
					throw new Exception ("Something is horribly wrong, this should never happen: no default version!");

				return versions[DefaultVersionId];
			}
		}

		public void SetDefaultVersion (IPhotoVersion version)
		{
			var photo_version = version as PhotoVersion;
			if (photo_version == null)
				throw new ArgumentException ("Not a valid version for this photo");

			DefaultVersionId = photo_version.VersionId;
		}


		//FIXME: won't work on non file uris
		public uint SaveVersion (ImageBuffer buffer, bool create_version)
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

					ImagingServices.Required.SaveDerivedVersion (buffer, DefaultVersion.Uri, versionUri, 95);
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

		public void DeleteVersion (uint versionId, bool removeOriginal = false, bool keepFile = false)
		{
			if (versionId == OriginalVersionId && !removeOriginal)
				throw new Exception ("Cannot delete original version");

			SafeUri uri = VersionUri (versionId);
			var fileInfo = new FileInfo (uri.AbsolutePath);
			var dirInfo = fileInfo.Directory;

			if (!keepFile) {
				if (fileInfo.Exists)
					fileInfo.Delete ();

				try {
					thumbnailService?.DeleteThumbnails (uri);
				} catch {
					// ignore an error here we don't really care.
				}

				if (dirInfo != null)
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
			// Versions stored outside the library (and a host that has not
			// chosen a photos directory) must not walk parent folders.
			if (directory == null || FSpotConfiguration.PhotoUri == null)
				return;

			// Stop at the library itself. A name that merely starts with the
			// library path (Photos vs Photos-old) is not inside it.
			string library = SafeUri.UriToFilename (FSpotConfiguration.PhotoUri.ToString ());
			if (!IsStrictLibraryChild (directory.FullName, library))
				return;

			if (DirectoryIsEmpty (directory)) {
				try {
					Logger.Log.Debug ($"Removing empty directory: {directory.FullName}");
					directory.Delete ();
				} catch (Exception e) {
					// silently log the exception, but don't re-throw it
					// as to not annoy the user
					Logger.Log.Error (e, "");
				}
				// check to see if the parent is empty
				DeleteEmptyDirectory (directory.Parent);
			}
		}

		internal static bool IsStrictLibraryChild (string directoryFullName, string libraryRoot)
		{
			string root = CanonicalDirectory (libraryRoot);
			string current = CanonicalDirectory (directoryFullName);
			if (string.IsNullOrEmpty (root) || string.IsNullOrEmpty (current))
				return false;
			return current.StartsWith (root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
		}

		static string CanonicalDirectory (string path)
		{
			if (string.IsNullOrWhiteSpace (path))
				return string.Empty;
			try {
				return new DirectoryInfo (path).FullName.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			} catch (Exception) {
				return string.Empty;
			}
		}

		bool DirectoryIsEmpty (DirectoryInfo directory)
		{
			try {
				return !directory.EnumerateFileSystemInfos ().Any ();
			} catch (DirectoryNotFoundException) {
				// The folder is already gone, so the parent walk can continue.
				return true;
			} catch (UnauthorizedAccessException) {
				// Not empty: an access error must not delete the directory.
				return false;
			} catch (IOException) {
				// A file standing where a folder should be, or any other IO
				// error, is not an empty directory.
				return false;
			}
		}

		public uint CreateVersion (string name, uint baseVersionId, bool create)
		{
			return CreateVersion (name, null, baseVersionId, create, false);
		}

		uint CreateVersion (string name, string extension, uint base_version_id, bool create, bool is_protected = false)
		{
			extension ??= Path.GetExtension (VersionUri (base_version_id).AbsolutePath);
			SafeUri new_base_uri = DefaultVersion.BaseUri;
			string filename = GetFilenameForVersionName (name, extension);
			SafeUri original_uri = VersionUri (base_version_id);
			SafeUri new_uri = new_base_uri.AppendFileName (filename);
			string import_md5 = DefaultVersion.ImportMD5;

			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");

			if (create) {
				if (File.Exists (new_uri.AbsolutePath))
					throw new Exception ($"An object at this uri {new_uri} already exists");

				File.Copy (original_uri.AbsolutePath, new_uri.AbsolutePath);
			}

			highest_version_id++;

			versions[highest_version_id] = new PhotoVersion (this, highest_version_id, new_base_uri, new_uri.GetFilename (), import_md5, name, is_protected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}

		public uint CreateReparentedVersion (PhotoVersion version, bool is_protected = false)
		{
			// Try to derive version name from its filename
			string filename = Path.GetFileNameWithoutExtension (version.Uri.AbsolutePath);
			string parent_filename = Path.GetFileNameWithoutExtension (Name);
			string name = null;
			if (filename.StartsWith (parent_filename))
				name = filename.Substring (parent_filename.Length).Replace ("(", "").Replace (")", "").Replace ("_", " ").Trim ();

			if (string.IsNullOrEmpty (name)) {
				name = Strings.Reparented;
				for (int num = 1; VersionNameExists (name); num++)
					name = $"{Strings.Reparented} ({num})";
			}

			highest_version_id++;
			versions[highest_version_id] = new PhotoVersion (this, highest_version_id, version.BaseUri, version.Filename, version.ImportMD5, name, is_protected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}

		public uint CreateDefaultModifiedVersion (uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				string name = num <= 1 ? Strings.Modified : Strings.ModifiedX;
				name = string.Format (name, num);
				string filename = GetFilenameForVersionName (name, Path.GetExtension (versions[baseVersionId].Uri.AbsolutePath));
				SafeUri uri = DefaultVersion.BaseUri.AppendFileName (filename);

				if (!VersionNameExists (name) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (name, baseVersionId, createFile);

				num++;
			}
		}

		public uint CreateNamedVersion (string name, string extension, uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				var final_name = string.Format ((num == 1) ? Strings.ModifiedInY : Strings.ModifiedInYX, num, name);

				string filename = GetFilenameForVersionName (final_name, Path.GetExtension (versions[baseVersionId].Uri.AbsolutePath));
				SafeUri uri = DefaultVersion.BaseUri.AppendFileName (filename);

				if (!VersionNameExists (final_name) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (final_name, extension, baseVersionId, createFile);

				num++;
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

		// This doesn't check if the tag is already there, use with caution.
		public void AddTagUnsafely (Tag tag)
		{
			if (tag == null)
				return;
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
			foreach (Tag tag in taglist)
				AddTag (tag);
		}

		public void RemoveTag (Tag tag)
		{
			if (!tags.Contains (tag))
				return;

			tags.Remove (tag);
			changes.RemoveTag (tag);
		}

		public void RemoveTag (IList<Tag> taglist)
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

		public Photo (IImageFileFactory imageFactory, IThumbnailService thumbnailService, uint id, long unix_time) : base (id)
		{
			imageFileFactory = imageFactory;
			this.thumbnailService = thumbnailService;

			time = DateTimeUtil.ToDateTime (unix_time);
			tags = new List<Tag> ();

			description = string.Empty;
			rating = 0;
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
