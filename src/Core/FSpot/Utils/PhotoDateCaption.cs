// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Core;

namespace FSpot.Utils
{
	public static class PhotoDateCaption
	{
		public const string DatePattern = "yyyy-MM-dd";
		public const string TimePattern = "HH:mm:ss";
		public const string DateTimePattern = "yyyy-MM-dd HH:mm:ss";

		public static string Format (IPhoto photo, bool wide)
		{
			if (photo == null)
				return string.Empty;
			if (photo is IInvalidPhotoCheck { IsInvalid: true })
				return string.Empty;
			return Format (photo.Time, wide);
		}

		public static string FormatLocal (IPhoto photo, bool wide)
		{
			if (photo == null)
				return string.Empty;
			if (photo is IInvalidPhotoCheck { IsInvalid: true })
				return string.Empty;
			return Format (photo.Time.ToLocalTime (), wide);
		}

		public static string Format (System.DateTime time, bool wide)
			=> time.ToString (wide ? DateTimePattern : DatePattern, CultureInfo.InvariantCulture);

		public static string FormatTime (System.DateTime time)
			=> time.ToString (TimePattern, CultureInfo.InvariantCulture);
	}
}
