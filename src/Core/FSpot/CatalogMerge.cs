// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FSpot.Core;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Thumbnail;
using FSpot.Utils;

using Hyena;

namespace FSpot
{
	public enum CatalogRollChoice
	{
		New,
		All,
		One
	}

	public sealed class CatalogMergeResult
	{
		public CatalogMergeResult (int imported, IReadOnlyList<string> missing)
		{
			Imported = imported;
			Missing = missing ?? Array.Empty<string> ();
		}

		public int Imported { get; }
		public IReadOnlyList<string> Missing { get; }
	}

	// Copies photos from another F-Spot catalog. The source file is opened from a
	// temp copy so a schema upgrade cannot rewrite the original. Names already stored
	// in the catalog are appended as-is, so a '%' in the filename is not encoded again.
	public static class CatalogMerge
	{
		public static CatalogMergeResult Merge (Db destination, string sourcePath, string destinationPath, bool copyFiles, CatalogRollChoice rolls, uint rollId, SafeUri libraryRoot, IImageFileFactory images, IThumbnailService thumbnails, IUpdaterUI updater)
		{
			if (destination == null || string.IsNullOrEmpty (sourcePath))
				return new CatalogMergeResult (0, Array.Empty<string> ());
			if (SameFile (sourcePath, destinationPath))
				return new CatalogMergeResult (0, Array.Empty<string> ());

			string temp = null;
			Db incoming = null;
			try {
				incoming = OpenCopy (sourcePath, images, thumbnails, updater, out temp);
				return MergeOpen (destination, incoming, copyFiles, rolls, rollId, libraryRoot);
			} finally {
				if (incoming != null)
					incoming.Dispose ();
				DeleteTemp (temp);
			}
		}

		// Rolls in the source catalog, read from a temp copy so the original is not upgraded.
		public static IReadOnlyList<(uint Id, DateTime Time, uint PhotoCount)> SourceRolls (string sourcePath, IImageFileFactory images, IThumbnailService thumbnails)
		{
			if (string.IsNullOrEmpty (sourcePath))
				return Array.Empty<(uint, DateTime, uint)> ();

			string temp = null;
			Db incoming = null;
			try {
				incoming = OpenCopy (sourcePath, images, thumbnails, null, out temp);
				var list = new List<(uint, DateTime, uint)> ();
				foreach (Roll roll in incoming.Rolls.GetRolls ())
					list.Add ((roll.Id, roll.Time, incoming.Rolls.PhotosInRoll (roll)));
				return list;
			} finally {
				if (incoming != null)
					incoming.Dispose ();
				DeleteTemp (temp);
			}
		}

		static Db OpenCopy (string sourcePath, IImageFileFactory images, IThumbnailService thumbnails, IUpdaterUI updater, out string temp)
		{
			temp = Path.GetTempFileName ();
			File.Copy (sourcePath, temp, true);
			File.SetAttributes (temp, FileAttributes.Normal);
			var incoming = new Db (images, thumbnails, updater ?? new SilentUpdater ());
			try {
				incoming.Init (temp, true);
				return incoming;
			} catch {
				incoming.Dispose ();
				throw;
			}
		}

		static CatalogMergeResult MergeOpen (Db destination, Db source, bool copyFiles, CatalogRollChoice rolls, uint rollId, SafeUri libraryRoot)
		{
			var tagMap = new Dictionary<uint, Tag> ();
			MergeTags (source.Tags.RootCategory, source.Tags, destination.Tags, tagMap);

			var rollMap = new Dictionary<uint, uint> ();
			foreach (Roll roll in ChosenRolls (source, destination, rolls, rollId)) {
				if (source.Rolls.PhotosInRoll (roll) == 0)
					continue;
				rollMap[roll.Id] = destination.Rolls.Create (roll.Time).Id;
			}

			int imported = 0;
			var missing = new List<string> ();
			foreach (Photo photo in source.Photos.Query ("SELECT * FROM photos")) {
				if (!rollMap.ContainsKey (photo.RollId))
					continue;
				if (ImportPhoto (destination, photo, copyFiles, rollMap[photo.RollId], libraryRoot, tagMap, missing))
					imported++;
			}
			return new CatalogMergeResult (imported, missing);
		}

		static IEnumerable<Roll> ChosenRolls (Db source, Db destination, CatalogRollChoice rolls, uint rollId)
		{
			if (rolls == CatalogRollChoice.One) {
				Roll one = source.Rolls.Get (rollId);
				if (one != null)
					yield return one;
				yield break;
			}

			List<Roll> chosen = source.Rolls.GetRolls ();
			if (rolls != CatalogRollChoice.New)
				foreach (Roll roll in chosen)
					yield return roll;
			else {
				var existing = new HashSet<DateTime> (destination.Rolls.GetRolls ().Select (roll => roll.Time));
				foreach (Roll roll in chosen)
					if (!existing.Contains (roll.Time))
						yield return roll;
			}
		}

		static void MergeTags (Tag tag, TagStore source, TagStore destination, Dictionary<uint, Tag> map)
		{
			if (tag != source.RootCategory) {
				Tag dest = destination.GetTagByName (tag.Name);
				// A plain tag and a category cannot share a name. Children follow the category we create.
				if (dest != null && tag is Category && !(dest is Category))
					dest = null;
				if (dest == null) {
					Category parent = ParentCategory (tag, source, destination, map);
					if (tag is Category) {
						string name = destination.GetTagByName (tag.Name) == null
							? tag.Name
							: Disambiguated (destination, tag.Name);
						dest = destination.CreateCategory (parent, name, false);
					} else
						dest = destination.CreateTag (parent, tag.Name, false);
				}
				map[tag.Id] = dest;
			}

			var category = tag as Category;
			if (category == null)
				return;
			foreach (Tag child in category.Children)
				MergeTags (child, source, destination, map);
		}

		static Category ParentCategory (Tag tag, TagStore source, TagStore destination, Dictionary<uint, Tag> map)
		{
			if (tag.Category == null || tag.Category == source.RootCategory)
				return destination.RootCategory;
			if (map.TryGetValue (tag.Category.Id, out Tag mapped) && mapped is Category category)
				return category;
			return destination.RootCategory;
		}

		static string Disambiguated (TagStore destination, string name)
		{
			for (int i = 2; i < 10000; i++) {
				string candidate = name + " " + i;
				if (destination.GetTagByName (candidate) == null)
					return candidate;
			}
			return name + " " + Guid.NewGuid ().ToString ("N");
		}

		static bool ImportPhoto (Db destination, Photo photo, bool copyFiles, uint rollId, SafeUri libraryRoot, Dictionary<uint, Tag> tagMap, List<string> missing)
		{
			PhotoVersion original = photo.GetVersion (Photo.OriginalVersionId);
			string sourcePath = original?.Uri?.AbsolutePath;
			if (string.IsNullOrEmpty (sourcePath) || !File.Exists (sourcePath)) {
				missing.Add (sourcePath ?? string.Empty);
				return false;
			}

			SafeUri stored = original.Uri;
			if (copyFiles) {
				try {
					stored = CopyTarget (original.Uri, photo.Time, libraryRoot);
					if (stored == null) {
						missing.Add (sourcePath);
						return false;
					}
					if (!string.Equals (stored.AbsolutePath, sourcePath, StringComparison.Ordinal)) {
						File.Copy (sourcePath, stored.AbsolutePath);
						try {
							File.SetAttributes (stored.AbsolutePath, File.GetAttributes (stored.AbsolutePath) & ~FileAttributes.ReadOnly);
							File.SetCreationTime (stored.AbsolutePath, File.GetCreationTime (sourcePath));
							File.SetLastWriteTime (stored.AbsolutePath, File.GetLastWriteTime (sourcePath));
						} catch (IOException) {
						}
					}
				} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
					missing.Add (sourcePath);
					return false;
				}
			}

			PhotoVersion[] versions = photo.VersionIds.Select (photo.GetVersion).Where (version => version != null).ToArray ();
			photo.DefaultVersionId = Photo.OriginalVersionId;
			if (copyFiles)
				original.Uri = stored;

			Photo created = destination.Photos.CreateFrom (photo, true, rollId);
			if (created == null)
				return false;

			if (photo.Tags != null) {
				foreach (Tag tag in photo.Tags)
					if (tag != null && tagMap.TryGetValue (tag.Id, out Tag destTag))
						created.AddTag (destTag);
			}

			foreach (PhotoVersion version in versions) {
				if (version.VersionId == Photo.OriginalVersionId)
					continue;
				created.AddVersion (version.BaseUri, version.Filename, version.Name, version.IsProtected);
			}

			created.Time = photo.Time;
			created.Description = photo.Description ?? string.Empty;
			created.Rating = photo.Rating;
			destination.Photos.Commit (created);
			return true;
		}

		static SafeUri CopyTarget (SafeUri uri, DateTime time, SafeUri libraryRoot)
		{
			if (uri == null || libraryRoot == null)
				return null;
			string name = uri.GetFilename ();
			SafeUri folder = libraryRoot.Append (time.Year.ToString ()).Append (time.Month.ToString ("D2")).Append (time.Day.ToString ("D2"));
			// CreateDirectory follows a directory link, so a year folder pointing outside the library would receive the copy.
			if (!FileIdentity.IsInside (libraryRoot.AbsolutePath, folder.AbsolutePath))
				return null;
			Directory.CreateDirectory (folder.AbsolutePath);

			SafeUri candidate = folder.Append (name);
			if (string.Equals (candidate.AbsoluteUri, uri.AbsoluteUri, StringComparison.Ordinal))
				return candidate;

			int suffix = 1;
			while (File.Exists (candidate.AbsolutePath)) {
				if (suffix > 10000)
					return null;
				candidate = folder.Append (uri.GetFilenameWithoutExtension () + "-" + suffix + uri.GetExtension ());
				suffix++;
			}
			return candidate;
		}

		static bool SameFile (string left, string right)
			=> FileIdentity.Same (left, right);

		sealed class SilentUpdater : IUpdaterUI
		{
			public void Show ()
			{
			}

			public void Pulse ()
			{
			}

			public void Destroy ()
			{
			}
		}

		static void DeleteTemp (string path)
		{
			if (string.IsNullOrEmpty (path))
				return;
			foreach (string extra in new[] { path, path + "-wal", path + "-shm", path + "-journal" }) {
				try {
					if (File.Exists (extra))
						File.Delete (extra);
				} catch (IOException) {
				}
			}
		}
	}
}
