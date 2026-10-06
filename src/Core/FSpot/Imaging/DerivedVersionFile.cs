// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Utils;

using Hyena;

using TagLib.Image;

namespace FSpot.Imaging
{
	public static class DerivedVersionFile
	{
		public static void Publish (SafeUri source, SafeUri destination, Action<string> writePixels)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));
			if (destination == null)
				throw new ArgumentNullException (nameof (destination));
			if (writePixels == null)
				throw new ArgumentNullException (nameof (writePixels));

			string extension = destination.GetExtension ().ToLowerInvariant ();
			if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
				throw new NotImplementedException ("Saving this file format is not supported");

			string destPath = destination.AbsolutePath;
			string directory = Path.GetDirectoryName (destPath);
			string temp = Path.Combine (
				directory,
				Path.GetFileNameWithoutExtension (destPath) + ".fspot-tmp" + extension);
			try {
				writePixels (temp);
				CopyMetadataResettingOrientation (source, SafeUri.FromPath (temp));
				Replace (temp, destPath);
			} catch {
				if (System.IO.File.Exists (temp))
					System.IO.File.Delete (temp);
				throw;
			}
		}

		static void CopyMetadataResettingOrientation (SafeUri source, SafeUri temp)
		{
			using var from = MetadataUtils.Parse (source);
			using var to = MetadataUtils.Parse (temp);
			if (from == null || to == null)
				return;
			to.CopyFrom (from);
			to.ImageTag.Orientation = ImageOrientation.TopLeft;
			to.Save ();
		}

		static void Replace (string temp, string destPath)
		{
#if NETFRAMEWORK
			if (System.IO.File.Exists (destPath))
				System.IO.File.Delete (destPath);
			System.IO.File.Move (temp, destPath);
#else
			System.IO.File.Move (temp, destPath, overwrite: true);
#endif
		}
	}
}
