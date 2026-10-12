// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using FSpot.Core;

namespace FSpot
{
	public static class PhotoFileActions
	{
		public static IReadOnlyList<string> SelectedPaths (IEnumerable<IPhoto> photos)
		{
			var paths = new List<string> ();
			if (photos == null)
				return paths;
			foreach (var photo in photos) {
				string path = PathOf (photo);
				if (!string.IsNullOrEmpty (path))
					paths.Add (path);
			}
			return paths;
		}

		public static IReadOnlyList<string> CopyToFolder (IEnumerable<string> sources, string destinationFolder)
		{
			var written = new List<string> ();
			if (sources == null || string.IsNullOrWhiteSpace (destinationFolder))
				return written;

			Directory.CreateDirectory (destinationFolder);
			foreach (string source in sources) {
				if (string.IsNullOrEmpty (source) || !File.Exists (source))
					continue;
				string dest = UniqueDestination (destinationFolder, Path.GetFileName (source));
				File.Copy (source, dest);
				written.Add (dest);
			}
			return written;
		}

		public static void WriteZip (IEnumerable<string> sources, string zipPath)
		{
			if (sources == null || string.IsNullOrWhiteSpace (zipPath))
				return;

			string directory = Path.GetDirectoryName (zipPath);
			if (!string.IsNullOrEmpty (directory))
				Directory.CreateDirectory (directory);

			using (var archive = ZipFile.Open (zipPath, ZipArchiveMode.Create)) {
				foreach (string source in sources) {
					if (string.IsNullOrEmpty (source) || !File.Exists (source))
						continue;
					string name = Path.GetFileName (source);
					if (string.IsNullOrEmpty (name))
						continue;
					ZipArchiveEntry entry = archive.CreateEntry (name, CompressionLevel.Optimal);
					using (var input = File.OpenRead (source))
					using (var output = entry.Open ())
						input.CopyTo (output);
				}
			}
		}

		static string PathOf (IPhoto photo)
		{
			if (photo?.DefaultVersion?.Uri == null)
				return null;
			string local = photo.DefaultVersion.Uri.LocalPath;
			if (!string.IsNullOrEmpty (local))
				return local;
			return photo.DefaultVersion.Uri.AbsolutePath;
		}

		static string UniqueDestination (string folder, string filename)
		{
			string dest = Path.Combine (folder, filename);
			int i = 1;
			while (File.Exists (dest)) {
				dest = Path.Combine (folder, string.Format ("{0}-{1}{2}",
					Path.GetFileNameWithoutExtension (filename),
					i++,
					Path.GetExtension (filename)));
			}
			return dest;
		}
	}
}
