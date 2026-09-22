// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Utils
{
	public static class PhotoDateCaption
	{
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
			=> wide ? time.ToString () : time.ToShortDateString ();
	}
}
