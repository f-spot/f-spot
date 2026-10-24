// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;
using FSpot.Utils;

namespace FSpot.Core
{
	// Text for the Image Information expander. Null metadata (parse failed) or a
	// missing ImageTag must not throw: InfoBox.Update walks every field in one
	// foreach, and an exception there leaves the expander empty.
	public static class PhotoInfoFormatter
	{
		public static string Name (IPhoto photo)
			=> photo?.Name ?? string.Empty;

		public static string Date (IPhoto photo)
		{
			if (photo == null)
				return string.Empty;

			string date = PhotoDateCaption.Format (photo, wide: false);
			if (string.IsNullOrEmpty (date))
				return string.Empty;
			return $"{date}{Environment.NewLine}{PhotoDateCaption.FormatTime (photo.Time)}";
		}

		public static string DateRange (IPhoto[] photos)
		{
			if (photos == null || photos.Length == 0)
				return string.Empty;
			IPhoto first = photos[photos.Length - 1];
			IPhoto last = photos[0];
			if (first.Time.Date == last.Time.Date)
				return string.Format (Strings.OnXBetweenYAndZ,
					PhotoDateCaption.Format (first, wide: false),
					PhotoDateCaption.FormatTime (first.Time),
					PhotoDateCaption.FormatTime (last.Time));
			return string.Format (Strings.BetweenXAndY,
				PhotoDateCaption.Format (first, wide: false),
				PhotoDateCaption.Format (last, wide: false));
		}

		public static string Size (TagLib.Image.File metadata)
		{
			int width = metadata?.Properties?.PhotoWidth ?? 0;
			int height = metadata?.Properties?.PhotoHeight ?? 0;
			if (width != 0 && height != 0)
				return $"{width}x{height}";

			return Strings.ParenUnknownParen;
		}

		public static string Exposure (TagLib.Image.File metadata)
		{
			var tag = metadata?.ImageTag;
			if (tag == null)
				return Strings.ParenNoneParen;

			string info = string.Empty;

			var fnumber = tag.FNumber;
			if (fnumber.HasValue && fnumber.Value != 0.0)
				info += $"f/{fnumber.Value:.0} ";

			var exposure_time = tag.ExposureTime;
			if (exposure_time.HasValue) {
				if (Math.Abs (exposure_time.Value) >= 1.0)
					info += $"{exposure_time.Value} sec ";
				else
					info += $"1/{(int)(1 / exposure_time.Value)} sec ";
			}

			var iso_speed = tag.ISOSpeedRatings;
			if (iso_speed.HasValue)
				info += $"{Environment.NewLine}ISO {iso_speed.Value}";

			var exif = tag.Exif;
			if (exif != null) {
				var flash = exif.ExifIFD.GetLongValue (0, (ushort)TagLib.IFD.Tags.ExifEntryTag.Flash);
				if (flash.HasValue)
					info += (flash.Value & 0x01) == 0x01
						? $", {Strings.Flashfired}"
						: $", {Strings.FlashDidntFire}";
			}

			return string.IsNullOrEmpty (info) ? Strings.ParenNoneParen : info;
		}

		public static string FocalLength (TagLib.Image.File metadata)
		{
			var focal_length = metadata?.ImageTag?.FocalLength;
			if (focal_length == null)
				return Strings.ParenUnknownParen;

			return $"{focal_length.Value} mm";
		}

		public static string Camera (TagLib.Image.File metadata)
			=> metadata?.ImageTag?.Model ?? Strings.ParenUnknownParen;

		public static string Creator (TagLib.Image.File metadata)
			=> metadata?.ImageTag?.Creator ?? Strings.ParenUnknownParen;
	}
}
