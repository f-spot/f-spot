// Copyright (C) 2020 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.FileSystem;
using FSpot.Imaging;
using FSpot.Services;
using FSpot.Settings;
using FSpot.Utils;

using Hyena;

using Mono.Unix;

namespace FSpot.Models
{
	public partial class Photo : BaseDbSet, IPhoto, IPhotoVersionable, IComparable<Photo>
	{
		[NotMapped]
		public long OldId { get; set; }
		public DateTime UtcTime { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }

		public string Description {
			get;
			set;
		}

		public Guid RollId { get; set; }
		[NotMapped]
		public long OldRollId { get; set; }
		public long DefaultVersionId { get; set; }
		public long Rating { get; set; }
		public Roll Roll { get; set; }
		public List<Tag> Tags { get; }
		public List<IPhotoVersion> Versions { get; }


		[NotMapped]
		public string Name {
			get => Uri.UnescapeDataString (Path.GetFileName (VersionUri (OriginalVersionId).AbsolutePath));
		}

		PhotoChanges changes = new PhotoChanges ();
		[NotMapped]
		public PhotoChanges Changes {
			get { return changes; }
			set {
				if (value != null)
					throw new ArgumentException ("The only valid value is null");

				changes = new PhotoChanges ();
			}
		}

		public int CompareTo (Photo other)
		{
			return Id.CompareTo (other.Id);
		}

		//DateTime time;
		//public DateTime Time {
		//	get { return time; }
		//	private set {
		//		if (time == value)
		//			return;

		//		time = value;
		//		changes.TimeChanged = true;
		//	}
		//}

		//public uint DefaultVersionId {
		//	get { return default_version_id; }
		//	set {
		//		if (default_version_id == value)
		//			return;

		//		default_version_id = value;
		//		changes.DefaultVersionIdChanged = true;
		//	}
		//}

		//string description;
		//public string Description {
		//	get { return description; }
		//	set {
		//		if (description == value)
		//			return;
		//		description = value;
		//		changes.DescriptionChanged = true;
		//	}
		//}

		//uint roll_id = 0;
		//public uint RollId {
		//	get { return roll_id; }
		//	set {
		//		if (roll_id == value)
		//			return;
		//		roll_id = value;
		//		changes.RollIdChanged = true;
		//	}
		//}

		//long rating;
		//public long Rating {
		//	get { return rating; }
		//	set {
		//		if (rating == value || value > 5)
		//			return;
		//		rating = value;
		//		changes.RatingChanged = true;
		//	}
		//}

		public Photo ()
		{
			Versions ??= new List<IPhotoVersion> ();
			CreateImageFileFactory ();
		}

		ImageFileFactory imageFileFactory;
		void CreateImageFileFactory ()
		{
			imageFileFactory ??= new ImageFileFactory(new DotNetFileSystem());
		}

		public const int OriginalVersionId = 1;
		uint highest_version_id;
		readonly Dictionary<long, PhotoVersion> versions = new Dictionary<long, PhotoVersion> ();

		bool all_versions_loaded = false;
		[NotMapped]
		public bool AllVersionsLoaded {
			get { return all_versions_loaded; }
			set {
				if (value)
					if (DefaultVersionId != OriginalVersionId && !versions.ContainsKey (DefaultVersionId))
						DefaultVersionId = OriginalVersionId;
				all_versions_loaded = value;
			}
		}

		[NotMapped]
		public long[] VersionIds {
			get {
				if (versions == null)
					return Array.Empty<long> ();

				long[] ids = new long[versions.Count];
				versions.Keys.CopyTo (ids, 0);
				Array.Sort (ids);
				return ids;
			}
		}

		uint default_version_id = OriginalVersionId;

		public PhotoVersion GetVersion (long versionId)
		{
			return versions?[versionId];
		}

		// This doesn't check if a version of that name already exists,
		// it's supposed to be used only within the Photo and PhotoStore classes.
		public void AddVersionUnsafely (uint versionId, SafeUri baseUri, string filename, string importMd5, string name, bool isProtected)
		{
			versions[versionId] = new PhotoVersion (this, versionId, baseUri, filename, importMd5, name, isProtected);
			Versions.Add (versions[versionId]);

			highest_version_id = Math.Max (versionId, highest_version_id);
			changes.AddVersion (versionId);
		}

		public uint AddVersion (SafeUri baseUri, string filename, string name, bool isProtected = false)
		{
			if (VersionNameExists (name))
				throw new ApplicationException ("A version with that name already exists");

			highest_version_id++;
			string importMd5 = string.Empty; // Modified version

			versions[highest_version_id] = new PhotoVersion (this, highest_version_id, baseUri, filename, importMd5, name, isProtected);

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

		public SafeUri VersionUri (long version_id)
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
			if (!(version is PhotoVersion photoVersion))
				throw new ArgumentException ("Not a valid version for this photo");

			DefaultVersionId = photoVersion.VersionId;
		}

		//FIXME: won't work on non file uris
		public long SaveVersion (Gdk.Pixbuf buffer, bool create_version)
		{
			long version = DefaultVersionId;
			using (var img = imageFileFactory.Create (DefaultVersion.Uri)) {
				// Always create a version if the source is not a jpeg for now.
				create_version = create_version || imageFileFactory.IsJpeg (DefaultVersion.Uri);

				if (buffer == null)
					throw new ApplicationException ("invalid (null) image");

				if (create_version)
					version = CreateDefaultModifiedVersion (DefaultVersionId, false);

				try {
					var versionUri = VersionUri (version);

					PixbufUtils.CreateDerivedVersion (DefaultVersion.Uri, versionUri, 95, buffer);
					GetVersion (version).ImportMd5 = HashUtils.GenerateMD5 (VersionUri (version));
					DefaultVersionId = version;
				} catch (Exception e) {
					Log.Exception (e);
					if (create_version)
						DeleteVersion (version);

					throw;
				}
			}

			return version;
		}

		public void DeleteVersion (long versionId, bool removeOriginal)
		{
			DeleteVersion (versionId, removeOriginal, false);
		}

		public void DeleteVersion (long versionId, bool removeOriginal = false, bool keepFile = false)
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
					// FIXME, re-enable thumbnail service
					//thumbnailService.DeleteThumbnails (uri);
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
					Log.Debug ($"Removing empty directory: {directory.FullName}");
					directory.Delete ();
				} catch (IOException e) {
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

		public uint CreateVersion (string name, long baseVersionId, bool create)
		{
			return CreateVersion (name, null, baseVersionId, create, false);
		}

		uint CreateVersion (string name, string extension, long base_version_id, bool create, bool is_protected = false)
		{
			extension ??= VersionUri (base_version_id).GetExtension ();
			SafeUri new_base_uri = new SafeUri (DefaultVersion.BaseUri);
			string filename = GetFilenameForVersionName (name, extension);
			SafeUri original_uri = VersionUri (base_version_id);
			SafeUri new_uri = new_base_uri.Append (filename);
			string import_md5 = DefaultVersion.ImportMd5;

			if (VersionNameExists (name))
				throw new Exception ("This version name already exists");

			if (create) {
				if (File.Exists (new_uri.AbsolutePath))
					throw new Exception ($"An object at this uri {new_uri} already exists");

				File.Copy (original_uri.AbsolutePath, new_uri.AbsolutePath);
			}

			highest_version_id++;

			versions[highest_version_id] = new PhotoVersion (this, highest_version_id, new_base_uri, filename, import_md5, name, is_protected);

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
				name = filename.Substring (parent_filename.Length).Replace ("(", "").Replace (")", "").Replace ("_", " ").Trim ();

			if (string.IsNullOrEmpty (name)) {
				// Note for translators: Reparented is a picture becoming a version of another one
				string rep = name = Catalog.GetString ("Reparented");
				for (int num = 1; VersionNameExists (name); num++) {
					name = $"rep +  ({num})";
				}
			}

			highest_version_id++;
			versions[highest_version_id] = new PhotoVersion (this, highest_version_id, new SafeUri (version.BaseUri), version.Filename, version.ImportMd5, name, is_protected);

			changes.AddVersion (highest_version_id);

			return highest_version_id;
		}

		public long CreateDefaultModifiedVersion (long baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				string name = Catalog.GetPluralString ("Modified", "Modified ({0})", num);
				name = string.Format (name, num);
				//SafeUri uri = GetUriForVersionName (name, System.IO.Path.GetExtension (VersionUri(baseVersionId).GetFilename()));
				string filename = GetFilenameForVersionName (name, System.IO.Path.GetExtension (versions[baseVersionId].Filename));
				SafeUri uri = new SafeUri (DefaultVersion.BaseUri).Append (filename);

				if (!VersionNameExists (name) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (name, baseVersionId, createFile);

				num++;
			}
		}

		public uint CreateNamedVersion (string name, string extension, long baseVersionId, bool createFile)
		{
			int num = 1;

			while (true) {
				var final_name = string.Format (
					(num == 1) ? Catalog.GetString ("Modified in {1}") : Catalog.GetString ("Modified in {1} ({0})"), num, name);

				string filename = GetFilenameForVersionName (name, Path.GetExtension (versions[baseVersionId].Filename));
				SafeUri uri = new SafeUri (DefaultVersion.BaseUri).Append (filename);

				if (!VersionNameExists (final_name) && !File.Exists (uri.AbsolutePath))
					return CreateVersion (final_name, extension, baseVersionId, createFile);

				num++;
			}
		}

		public void RenameVersion (long versionId, string newName)
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
			UtcTime = that.UtcTime;
			Description = that.Description;
			Rating = that.Rating;
			TagService.Instance.Add (this, that.Tags);// AddTag (that.Tags);
		}
	}
}
