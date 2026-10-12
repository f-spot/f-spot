// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Hyena;

namespace FSpot
{
	public static class PhotoPathChange
	{
		// GTK compares the directory, so a trailing slash on the old base
		// matches only files in a subdirectory. The slice still uses the
		// path text, which is why "/old" also rewrites "/old-other".
		public static string Rewrite (string absolutePath, string oldBase, string newBase)
		{
			if (string.IsNullOrEmpty (absolutePath) || string.IsNullOrEmpty (oldBase) || newBase == null)
				return null;
			if (absolutePath.Length < oldBase.Length)
				return null;
			string directory = Path.GetDirectoryName (absolutePath);
			if (directory == null || !directory.StartsWith (oldBase, StringComparison.Ordinal))
				return null;
			return newBase + absolutePath.Substring (oldBase.Length);
		}

		// Catalog paths use '/' even on Windows. The year scan looks for /20, /19, or /18.
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

		public static string WithTrailingSlash (string path)
		{
			if (string.IsNullOrEmpty (path))
				return "/";
			path = path.TrimEnd ('/', '\\');
			return path.Length == 0 ? "/" : path + "/";
		}

		static string BaseBeforeYear (string path)
		{
			if (string.IsNullOrEmpty (path))
				return null;
			foreach (string marker in new[] { "/20", "/19", "/18" }) {
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
	}
}
