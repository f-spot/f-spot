//
// IImagingBackend.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Hyena;

using TagLib.Image;

namespace FSpot.Imaging
{
	public interface IImagingBackend
	{
		ImageBuffer Decode (Stream stream, ImageOrientation orientation);
		ImageBuffer ScaleToMaxSize (ImageBuffer source, int maxWidth, int maxHeight, bool upscale = true);
		ImageBuffer Scale (ImageBuffer source, int width, int height);
		void SaveDerivedVersion (ImageBuffer buffer, SafeUri source, SafeUri dest, uint jpegQuality);
		ImageBuffer LoadThemeIcon (string name, int size);
		byte[] SerializeIcon (ImageBuffer icon);
		ImageBuffer DeserializeIcon (byte[] data);
		ThumbnailPng LoadPng (Stream stream);
		// optionKeys and optionValues are parallel arrays of equal length. Neither carries a
		// trailing null: terminating the native arrays is the backend's job, not the caller's.
		void SavePng (ImageBuffer image, string path, string[] optionKeys, string[] optionValues);
	}

	public sealed class ThumbnailPng
	{
		public const string UriOptionKey = "tEXt::Thumb::URI";
		public const string MTimeOptionKey = "tEXt::Thumb::MTime";

		public ImageBuffer Image { get; set; }
		public string UriOption { get; set; }
		public string MTimeOption { get; set; }
	}

	public static class ImagingServices
	{
		public static IImagingBackend Backend { get; set; }

		public static IImagingBackend Required {
			get {
				if (Backend == null)
					throw new System.InvalidOperationException ("Imaging backend is not registered.");
				return Backend;
			}
		}
	}
}
