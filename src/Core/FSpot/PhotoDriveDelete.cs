// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Hyena;

namespace FSpot
{
	/// <summary>
	/// Deletes every version file of one photo. Derived versions go first.
	/// The original is removed only after every derived version succeeds.
	/// A failure is recorded and does not escape.
	/// </summary>
	public static class PhotoDriveDelete
	{
		public sealed class Failure
		{
			public Failure (Exception exception, string path)
			{
				Exception = exception;
				Path = path ?? string.Empty;
			}

			public Exception Exception { get; }
			public string Path { get; }
		}

		public static IReadOnlyList<Failure> TryDeleteAllVersions (Photo photo)
		{
			var failures = new List<Failure> ();
			if (photo == null)
				return failures;

			uint[] ids = photo.VersionIds;
			Array.Reverse (ids);

			bool derivedFailed = false;
			foreach (uint id in ids) {
				if (id == Photo.OriginalVersionId && derivedFailed)
					continue;

				string path = PathFor (photo, id);
				try {
					photo.DeleteVersion (id, true);
				} catch (Exception ex) {
					if (id != Photo.OriginalVersionId)
						derivedFailed = true;
					failures.Add (new Failure (ex, path));
				}
			}

			return failures;
		}

		public static string DisplayName (Photo photo)
		{
			if (photo == null)
				return string.Empty;

			try {
				return photo.Name ?? string.Empty;
			} catch (Exception) {
				return PathFor (photo, Photo.OriginalVersionId);
			}
		}

		public static string PathFor (Photo photo, uint id)
		{
			if (photo == null)
				return string.Empty;

			try {
				SafeUri uri = photo.VersionUri (id);
				if (uri == null)
					return string.Empty;

				string local = uri.LocalPath;
				return string.IsNullOrEmpty (local) ? uri.ToString () : local;
			} catch (Exception) {
				return string.Empty;
			}
		}
	}
}
