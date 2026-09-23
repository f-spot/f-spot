// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Resources.Lang;

namespace FSpot.Import
{
	public static class FileDropImport
	{
		public sealed class Plan
		{
			public Plan (IReadOnlyList<string> accepted, IReadOnlyList<string> rejected)
			{
				Accepted = accepted;
				Rejected = rejected;
			}

			public IReadOnlyList<string> Accepted { get; }
			public IReadOnlyList<string> Rejected { get; }
		}

		public static Plan Classify (IEnumerable<string> paths, Func<string, bool> canLoad)
		{
			if (canLoad == null)
				throw new ArgumentNullException (nameof (canLoad));

			var accepted = new List<string> ();
			var rejected = new List<string> ();
			if (paths == null)
				return new Plan (accepted, rejected);

			foreach (string raw in paths) {
				if (string.IsNullOrWhiteSpace (raw))
					continue;
				string path;
				try {
					path = Path.GetFullPath (raw.Trim ());
				} catch (Exception) {
					rejected.Add (raw.Trim ());
					continue;
				}

				try {
					if (Directory.Exists (path)) {
						accepted.Add (path);
						continue;
					}
					if (File.Exists (path) && canLoad (path))
						accepted.Add (path);
					else
						rejected.Add (Path.GetFileName (path.Length == 0 ? raw : path));
				} catch (Exception) {
					rejected.Add (Path.GetFileName (path));
				}
			}

			return new Plan (accepted, rejected);
		}

		public static string Report (IReadOnlyList<string> rejected)
		{
			if (rejected == null || rejected.Count == 0)
				return null;
			return Strings.UnsupportedFileFormat + Environment.NewLine + string.Join (Environment.NewLine, rejected);
		}
	}
}
