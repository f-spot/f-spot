// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

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
			=> WriteZip (sources, zipPath, null, null, null);

		// longerSide null copies each file. A set edge asks scale for the archived
		// bytes; a null result keeps the original file. stop returns true to leave
		// the rest out, and a stop before the first photo removes the archive.
		public static void WriteZip (IEnumerable<string> sources, string zipPath, int? longerSide, Func<string, int, byte[]> scale, Func<int, int, string, bool> stop)
		{
			if (sources == null || string.IsNullOrWhiteSpace (zipPath))
				return;

			bool scalePhotos = longerSide.HasValue && longerSide.Value >= 1;
			if (scalePhotos && scale == null)
				throw new ArgumentNullException (nameof (scale));

			var files = new List<string> ();
			foreach (string source in sources) {
				if (string.IsNullOrEmpty (source) || !File.Exists (source))
					continue;
				if (string.IsNullOrEmpty (ZipEntryName (source)))
					continue;
				files.Add (source);
			}

			string directory = Path.GetDirectoryName (zipPath);
			if (!string.IsNullOrEmpty (directory))
				Directory.CreateDirectory (directory);

			// Create refuses a file that is already there. A second export replaces it.
			if (File.Exists (zipPath))
				File.Delete (zipPath);

			int written = 0;
			bool cancelled = false;
			using (var archive = ZipFile.Open (zipPath, ZipArchiveMode.Create)) {
				for (int i = 0; i < files.Count; i++) {
					string name = ZipEntryName (files[i]);
					if (stop != null && stop (i, files.Count, name)) {
						cancelled = true;
						break;
					}
					byte[] scaled = null;
					if (scalePhotos)
						scaled = scale (files[i], longerSide.Value);
					ZipArchiveEntry entry = archive.CreateEntry (name, CompressionLevel.Optimal);
					using (var output = entry.Open ()) {
						if (scaled != null && scaled.Length > 0)
							output.Write (scaled, 0, scaled.Length);
						else {
							using (var input = File.OpenRead (files[i]))
								input.CopyTo (output);
						}
					}
					written++;
				}
			}

			if (cancelled && written == 0 && File.Exists (zipPath))
				File.Delete (zipPath);
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

		// A Unix file name may contain '\' or ':'. On Windows those are a parent
		// segment and a drive marker, so an unzipper would write outside the folder.
		static string ZipEntryName (string path)
		{
			string name = Path.GetFileName (path);
			if (string.IsNullOrEmpty (name))
				return null;
			var builder = new StringBuilder (name.Length);
			foreach (char c in name) {
				if (c == '/' || c == '\\' || c == ':')
					builder.Append ('_');
				else
					builder.Append (c);
			}
			string entry = builder.ToString ();
			if (entry.Length == 0 || entry == "." || entry == "..")
				return null;
			return entry;
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
