// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using FSpot.Core;
using FSpot.Utils;

using Hyena;

namespace FSpot
{
	// Pairs a raw file with the JPEG that shares its folder and file stem,
	// then keeps both as versions of the raw photo. The JPEG file stays on disk.
	public static class RawJpegPairing
	{
		public sealed class Pair
		{
			public Pair (Photo raw, Photo jpeg)
			{
				Raw = raw;
				Jpeg = jpeg;
			}

			public Photo Raw { get; }
			public Photo Jpeg { get; }
		}

		public static IReadOnlyList<Pair> Find (IEnumerable<Photo> photos, Func<SafeUri, bool> isRaw, Func<SafeUri, bool> isJpeg)
		{
			var ordered = new List<Photo> ();
			if (photos != null) {
				foreach (Photo photo in photos) {
					if (photo != null)
						ordered.Add (photo);
				}
			}
			ordered.Sort (new IPhotoComparer.CompareDirectory ());

			Photo raw = null;
			Photo jpeg = null;
			var pairs = new List<Pair> ();
			foreach (Photo photo in ordered) {
				SafeUri uri;
				try {
					uri = photo.DefaultVersion?.Uri;
				} catch (Exception) {
					continue;
				}
				if (uri == null)
					continue;
				bool rawFile = isRaw != null && isRaw (uri);
				bool jpegFile = isJpeg != null && isJpeg (uri);
				if (!rawFile && !jpegFile)
					continue;
				if (jpegFile)
					jpeg = photo;
				if (rawFile)
					raw = photo;
				if (raw != null && jpeg != null && !ReferenceEquals (raw, jpeg) && SamePlaceAndName (raw, jpeg))
					pairs.Add (new Pair (raw, jpeg));
			}
			return pairs;
		}

		public static void Merge (Photo raw, Photo jpeg)
		{
			if (raw == null || jpeg == null || ReferenceEquals (raw, jpeg))
				return;

			foreach (uint versionId in jpeg.VersionIds) {
				PhotoVersion version = jpeg.GetVersion (versionId);
				if (version == null)
					continue;
				string name = version.Name;
				try {
					raw.DefaultVersionId = raw.CreateReparentedVersion (version, versionId == Photo.OriginalVersionId);
					if (versionId == Photo.OriginalVersionId)
						raw.RenameVersion (raw.DefaultVersionId, "Jpeg");
					else
						raw.RenameVersion (raw.DefaultVersionId, name);
				} catch (Exception ex) {
					Logger.Log.Error (ex, "Could not attach a JPEG version to the raw photo");
				}
			}

			raw.AddTag (jpeg.Tags);
			uint[] versionIds = jpeg.VersionIds;
			Array.Reverse (versionIds);
			foreach (uint versionId in versionIds) {
				try {
					jpeg.DeleteVersion (versionId, true, true);
				} catch (Exception ex) {
					Logger.Log.Error (ex, "Could not drop a JPEG version after attaching it to the raw photo");
				}
			}
			raw.Changes.DataChanged = true;
		}

		// GetBaseUri builds a new SafeUri, so reference equality never matches.
		// Compare the catalog text. Photo.Name is the decoded original file name.
		static bool SamePlaceAndName (Photo first, Photo second)
		{
			SafeUri firstBase = first.VersionUri (Photo.OriginalVersionId)?.GetBaseUri ();
			SafeUri secondBase = second.VersionUri (Photo.OriginalVersionId)?.GetBaseUri ();
			if (firstBase == null || secondBase == null)
				return false;
			return string.Equals (firstBase.AbsoluteUri, secondBase.AbsoluteUri, StringComparison.Ordinal)
				&& string.Equals (Path.GetFileNameWithoutExtension (first.Name), Path.GetFileNameWithoutExtension (second.Name), StringComparison.Ordinal);
		}
	}
}
