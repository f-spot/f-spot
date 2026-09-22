// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Platform;
using FSpot.Settings;

namespace FSpot
{
	public sealed class InfoFieldVisibility
	{
		public const string DateKey = Preferences.UIKey + "InfoShowDate";
		public const string SizeKey = Preferences.UIKey + "InfoShowSize";
		public const string ExposureKey = Preferences.UIKey + "InfoShowExposure";
		public const string FocalLengthKey = Preferences.UIKey + "InfoShowFocalLength";
		public const string CameraKey = Preferences.UIKey + "InfoShowCamera";
		public const string CreatorKey = Preferences.UIKey + "InfoShowCreator";
		public const string RatingKey = Preferences.UIKey + "InfoShowRating";

		public bool Date { get; set; } = true;
		public bool Size { get; set; } = true;
		public bool Exposure { get; set; } = true;
		public bool FocalLength { get; set; } = true;
		public bool Camera { get; set; } = true;
		public bool Creator { get; set; } = true;
		public bool Rating { get; set; } = true;

		public static InfoFieldVisibility Load ()
			=> new InfoFieldVisibility {
				Date = Read (DateKey),
				Size = Read (SizeKey),
				Exposure = Read (ExposureKey),
				FocalLength = Read (FocalLengthKey),
				Camera = Read (CameraKey),
				Creator = Read (CreatorKey),
				Rating = Read (RatingKey)
			};

		public void Save ()
		{
			Preferences.Set (DateKey, Date);
			Preferences.Set (SizeKey, Size);
			Preferences.Set (ExposureKey, Exposure);
			Preferences.Set (FocalLengthKey, FocalLength);
			Preferences.Set (CameraKey, Camera);
			Preferences.Set (CreatorKey, Creator);
			Preferences.Set (RatingKey, Rating);
		}

		static bool Read (string key)
		{
			try {
				return Preferences.JsonBackend.Get<bool> (key);
			} catch (NoSuchKeyException) {
				return true;
			}
		}
	}
}
