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
using FSpot.Settings;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

using Mono.Unix;

namespace FSpot
{
	public class Photo : DbItem, IComparable<Photo>, IPhoto, IPhotoVersionable
	{
		readonly IImageFileFactory imageFileFactory;
		readonly IThumbnailService thumbnailService;

		PhotoChanges changes = new PhotoChanges ();
		public PhotoChanges Changes {
			get => changes;
			set {
				if (value != null)
					throw new ArgumentException ("The only valid value is null");
				changes = new PhotoChanges ();
			}
		}

		// The time is always in UTC.
		DateTime time;
		public DateTime Time {
			get => time;
			set {
				if (time == value)
					return;
				time = value;
				changes.TimeChanged = true;
			}
		}

		public string Name {
			get { return Uri.UnescapeDataString (Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath)); }
		}

		readonly List<Tag> tags;
		public Tag[] Tags {
			get {
				return tags.ToArray ();
			}
		}

		bool all_versions_loaded;
		public bool AllVersionsLoaded {
			get => all_versions_loaded;
			set {
				if (value)
					if (DefaultVersionId != OriginalVersionId && !versions.ContainsKey (DefaultVersionId))
						DefaultVersionId = OriginalVersionId;
				all_versions_loaded = value;
			}
		}

		string description;
		public string Description {
			get => description;
			set {
				if (description == value)
					return;
				description = value;
				changes.DescriptionChanged = true;
			}
		}

		uint roll_id;
		public uint RollId {
			get => roll_id;
			set {
				if (roll_id == value)
					return;
				roll_id = value;
				changes.RollIdChanged = true;
			}
		}

		uint rating;
		public uint Rating {
			get => rating;
			set {
				if (rating == value || value > 5)
					return;
				rating = value;
				changes.RatingChanged = true;
			}
		}

		#region Properties Version Management
		public const int OriginalVersionId = 1;
		uint highestVersionId;
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

				uint[] ids = new uint[versions.Count];
				versions.Keys.CopyTo (ids, 0);
				Array.Sort (ids);
				return ids;
			}
		}

		uint default_version_id = OriginalVersionId;

		public uint DefaultVersionId {
			get => default_version_id;
			set {
				if (default_version_id == value)
					return;
				default_version_id = value;
				changes.DefaultVersionIdChanged = true;
			}
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

		#region Photo Version Management
		public PhotoVersion GetVersion (uint versionId)
		{
			return versions?[versionId];
		}

		// This doesn't check if a version of that name already exists,
		// it's supposed to be used only within the Photo and PhotoStore classes.
		public void AddVersionUnsafely (uint versionId, SafeUri baseUri, string filename, string importMd5, string name, bool isProtected)
		{
			versions[versionId] = new PhotoVersion (this, versionId, baseUri, filename, importMd5, name, isProtected);

			highestVersionId = Math.Max (versionId, highestVersionId);
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

			highestVersionId++;
			string import_md5 = string.Empty; // Modified version

			versions[highestVersionId] = new PhotoVersion (this, highestVersionId, baseUri, filename, import_md5, name, isProtected);

			changes.AddVersion (highestVersionId);
			return highestVersionId;
		}

		//FIXME: store versions next to originals. will crash on ro locations.
		string GetFilenameForVersionName (string version_name, string extension)
		{
			string name_without_extension = Path.GetFileNameWithoutExtension (Name);

			var escapeString = UriUtils.EscapeString (version_name, true, true, true);
			return $"{name_without_extension} ({escapeString}){extension}";
		}

		public bool VersionNameExists (string versionName)
		{
			return Versions.Any (v => v.Name == versionName);
		}

		public SafeUri VersionUri (uint versionId)
		{
			if (!versions.ContainsKey (versionId))
				return null;

			PhotoVersion v = versions[versionId];
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

					FSpot.Utils.PixbufUtils.CreateDerivedVersion (DefaultVersion.Uri, versionUri, 95, buffer);
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

			for (versionId = highestVersionId; versionId >= OriginalVersionId; versionId--) {
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
			string photoUri = SafeUri.UriToFilename (FSpotConfiguration.PhotoUri.ToString ());
			bool path_matched = directory.FullName.IndexOf (photoUri) > -1;

			if (directory.Name.Equals (photoUri) || !path_matched)
				return;

			if (DirectoryIsEmpty (directory)) {
				try {
					Log.Debug ($"Removing empty directory: {directory.FullName}");
					directory.Delete ();
				} catch (GLib.GException e) {
					// silently log the exception, but don't re-throw it
					// as to not annoy the user
					Log.Exception (e);
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

		uint CreateVersion (string name, string extension, uint baseVersionId, bool create, bool isProtected = false)
		{
			extension ??= VersionUri (baseVersionId).GetExtension ();
			SafeUri newBaseUri = DefaultVersion.BaseUri;
			string filename = GetFilenameForVersionName (name, extension);
			SafeUri original_uri = VersionUri (baseVersionId);
			SafeUri new_uri = newBaseUri.Append (filename);
			string importMd5 = DefaultVersion.ImportMD5;

			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");

			if (create) {
				if (File.Exists (new_uri.AbsolutePath))
					throw new Exception ($"An object at this uri {new_uri} already exists");

				File.Copy (original_uri.AbsolutePath, new_uri.AbsolutePath);
			}

			highestVersionId++;

			versions[highestVersionId] = new PhotoVersion (this, highestVersionId, newBaseUri, filename, importMd5, name, isProtected);

			changes.AddVersion (highestVersionId);

			return highestVersionId;
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
				name = filename.Substring (parentFilename.Length).Replace ("(", "").Replace (")", "").Replace ("_", " ").Trim ();

			if (string.IsNullOrEmpty (name)) {
				// Note for translators: Reparented is a picture becoming a version of another one
				string rep = name = Catalog.GetString ("Reparented");
				for (int num = 1; VersionNameExists (name); num++) {
					name = $"rep +  ({num})";
				}
			}

			highestVersionId++;
			versions[highestVersionId] = new PhotoVersion (this, highestVersionId, version.BaseUri, version.Filename, version.ImportMD5, name, isProtected);

			changes.AddVersion (highestVersionId);

			return highestVersionId;
		}

		public uint CreateDefaultModifiedVersion (uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				string name = Catalog.GetPluralString ("Modified", "Modified ({0})", num);
				name = string.Format (name, num);
				//SafeUri uri = GetUriForVersionName (name, System.IO.Path.GetExtension (VersionUri(baseVersionId).GetFilename()));
				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions[baseVersionId].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);

				if (!VersionNameExists (name) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (name, baseVersionId, createFile);

				num++;
			}
		}

		public uint CreateNamedVersion (string name, string extension, uint baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				var finalName = string.Format (
					(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"), num, name);

				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions[baseVersionId].Filename));
				SafeUri uri = DefaultVersion.BaseUri.Append (filename);

				if (!VersionNameExists (finalName) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (finalName, extension, baseVersionId, createFile);

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

		public void RemoveTag (Tag[] taglist)
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

		static readonly IDictionary<SafeUri, string> md5Cache = new Dictionary<SafeUri, string> ();

		public static void ResetMD5Cache ()
		{
			md5Cache?.Clear ();
		}
		#endregion

		public int CompareTo (Photo photo)
		{
			int result = Id.CompareTo (photo.Id);

			if (result == 0)
				return 0;

			return this.Compare (photo);
		}
	}
}
