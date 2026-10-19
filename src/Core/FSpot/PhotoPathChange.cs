// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using FSpot.Database;

using Hyena;

namespace FSpot
{
	public static class PhotoPathChange
	{
		// GTK compares the directory, so a trailing slash on the old base
		// matches only files in a subdirectory. The slice still uses the
		// path text, which is why "/old" also rewrites "/old-other".
		// Path.GetDirectoryName rewrites '/' to '\' on Windows, so the
		// directory is the text before the last separator instead.
		public static string Rewrite (string absolutePath, string oldBase, string newBase)
		{
			if (string.IsNullOrEmpty (absolutePath) || string.IsNullOrEmpty (oldBase) || newBase == null)
				return null;
			// A root is every absolute path. Accepting the failed guess rewrites the catalog.
			if (IsFilesystemRoot (oldBase))
				return null;
			int slash = absolutePath.LastIndexOf ('/');
			int backslash = absolutePath.LastIndexOf ('\\');
			int cut = slash > backslash ? slash : backslash;
			if (cut <= 0)
				return null;
			string directory = absolutePath.Substring (0, cut);
			if (!directory.StartsWith (oldBase, StringComparison.Ordinal))
				return null;
			return newBase + absolutePath.Substring (oldBase.Length);
		}

		// Catalog paths use '/' even on Windows, except a path stored with '\'.
		// The year scan looks for /20, /19, /18 and the same markers with '\'.
		public static string GuessOldBase (IEnumerable<string> absolutePaths)
		{
			string found = null;
			if (absolutePaths != null) {
				foreach (string path in absolutePaths) {
					found = BaseBeforeYear (path);
					if (found != null)
						break;
				}
			}
			return WithTrailingSlash (found);
		}

		public static bool SameBase (string left, string right)
			=> WithTrailingSlash (left) == WithTrailingSlash (right);

		public static bool IsFilesystemRoot (string path)
		{
			if (string.IsNullOrEmpty (path))
				return false;
			string slashed = WithTrailingSlash (path);
			return slashed == "/" || slashed == "\\";
		}

		public static string WithTrailingSlash (string path)
		{
			if (string.IsNullOrEmpty (path))
				return "/";
			bool windows = path.IndexOf ('\\') >= 0 && path.IndexOf ('/') < 0;
			char slash = windows ? '\\' : '/';
			path = path.TrimEnd ('/', '\\');
			return path.Length == 0 ? slash.ToString () : path + slash;
		}

		static string BaseBeforeYear (string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			foreach (string marker in new[] { "/20", "/19", "/18", "\\20", "\\19", "\\18" }) {
				int index = path.IndexOf (marker, StringComparison.Ordinal);
				if (index > 0)
					return path.Substring (0, index);
			}
			return null;
		}

		public static int Apply (IEnumerable<Photo> photos, string oldBase, string newBase)
		{
			if (photos == null)
				return 0;
			int changed = 0;
			foreach (Photo photo in photos) {
				if (photo == null)
					continue;
				bool photoChanged = false;
				foreach (uint versionId in photo.VersionIds) {
					PhotoVersion version = photo.GetVersion (versionId);
					if (version?.Uri == null)
						continue;
					string rewritten = Rewrite (version.Uri.AbsolutePath, oldBase, newBase);
					if (rewritten == null)
						continue;
					version.Uri = SafeUri.FromPath (rewritten);
					photo.Changes.UriChanged = true;
					photo.Changes.ChangeVersion (versionId);
					photoChanged = true;
				}
				if (photoChanged)
					changed++;
			}
			return changed;
		}

		// Stores every rewritten version, or stores none of them. A cancel or a
		// failed save puts each version URI back, in memory and in the database.
		// Cancel returns -1.
		public static int Commit (Db database, IEnumerable<Photo> photos, string oldBase, string newBase)
			=> Commit (database, photos, oldBase, newBase, null);

		public static int Commit (Db database, IEnumerable<Photo> photos, string oldBase, string newBase, Func<bool> stop)
			=> Commit (database, photos, oldBase, newBase, null, stop);

		internal static int Commit (Db database, IEnumerable<Photo> photos, string oldBase, string newBase, Action<Photo[]> save, Func<bool> stop)
		{
			if (database == null || photos == null)
				return 0;
			Photo[] list = photos.Where (photo => photo != null).ToArray ();
			List<Shot> shots = Snapshot (list);
			try {
				int changed = Apply (list, oldBase, newBase);
				if (changed == 0)
					return 0;
				if (stop != null && stop ()) {
					Restore (shots);
					return -1;
				}
				Photo[] dirty = list.Where (photo => photo.Changes.UriChanged).ToArray ();
				bool own = !database.Database.InTransaction;
				if (own)
					database.BeginTransaction ();
				try {
					if (save != null)
						save (dirty);
					else
						database.Photos.Commit (dirty);
					if (own)
						database.CommitTransaction ();
				} catch {
					if (own && database.Database.InTransaction)
						database.RollbackTransaction ();
					throw;
				}
				return changed;
			} catch {
				Restore (shots);
				throw;
			}
		}

		sealed class Shot
		{
			public Photo Photo;
			public bool Clean;
			public bool UriChanged;
			public List<KeyValuePair<uint, SafeUri>> Versions;
		}

		static List<Shot> Snapshot (Photo[] photos)
		{
			var shots = new List<Shot> ();
			foreach (Photo photo in photos) {
				var shot = new Shot {
					Photo = photo,
					Clean = !photo.Changes.UriChanged && photo.Changes.VersionsModified == null,
					UriChanged = photo.Changes.UriChanged,
					Versions = new List<KeyValuePair<uint, SafeUri>> ()
				};
				foreach (uint versionId in photo.VersionIds) {
					PhotoVersion version = photo.GetVersion (versionId);
					if (version?.Uri != null)
						shot.Versions.Add (new KeyValuePair<uint, SafeUri> (versionId, version.Uri));
				}
				shots.Add (shot);
			}
			return shots;
		}

		static void Restore (List<Shot> shots)
		{
			foreach (Shot shot in shots) {
				foreach (KeyValuePair<uint, SafeUri> version in shot.Versions) {
					PhotoVersion current = shot.Photo.GetVersion (version.Key);
					if (current != null)
						current.Uri = version.Value;
				}
				if (shot.Clean)
					shot.Photo.Changes = null;
				else
					shot.Photo.Changes.UriChanged = shot.UriChanged;
			}
		}
	}
}
