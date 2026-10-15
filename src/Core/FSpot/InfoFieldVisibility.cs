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

		// GTK InfoBox view contexts; each keeps its own field visibility.
		public const string LibraryContext = "Library";
		public const string EditContext = "Edit";

		public string Context { get; private set; }

		public bool Date { get; set; } = true;
		public bool Size { get; set; } = true;
		public bool Exposure { get; set; } = true;
		// GTK InfoBox starts focal length, camera, and rating hidden.
		public bool FocalLength { get; set; }
		public bool Camera { get; set; }
		public bool Creator { get; set; } = true;
		public bool Rating { get; set; }

		public static InfoFieldVisibility Load ()
			=> new InfoFieldVisibility {
				Date = Read (DateKey, true),
				Size = Read (SizeKey, true),
				Exposure = Read (ExposureKey, true),
				FocalLength = Read (FocalLengthKey, false),
				Camera = Read (CameraKey, false),
				Creator = Read (CreatorKey, true),
				Rating = Read (RatingKey, false)
			};

		// Per-context visibility in the GTK MRUInfoBoxContextSwitchStrategy key format and defaults.
		// A context that was never saved falls back to the context-free keys, then to GTK's defaults.
		public static InfoFieldVisibility Load (string context)
		{
			if (string.IsNullOrEmpty (context))
				return Load ();
			return new InfoFieldVisibility {
				Context = context,
				Date = Read (context, "date", DateKey, true),
				Size = Read (context, "size", SizeKey, true),
				Exposure = Read (context, "exposure", ExposureKey, true),
				FocalLength = Read (context, "focal_length", FocalLengthKey, false),
				Camera = Read (context, "camera", CameraKey, false),
				Creator = Read (context, "creator", CreatorKey, true),
				Rating = Read (context, "rating", RatingKey, false)
			};
		}

		public static string ContextKey (string context, string entryId)
			=> $"{Preferences.UIKey}infobox_visible/{entryId}/{context}";

		public void Save ()
		{
			if (!string.IsNullOrEmpty (Context)) {
				Write (Context, "date", Date);
				Write (Context, "size", Size);
				Write (Context, "exposure", Exposure);
				Write (Context, "focal_length", FocalLength);
				Write (Context, "camera", Camera);
				Write (Context, "creator", Creator);
				Write (Context, "rating", Rating);
				return;
			}
			Preferences.Set (DateKey, Date);
			Preferences.Set (SizeKey, Size);
			Preferences.Set (ExposureKey, Exposure);
			Preferences.Set (FocalLengthKey, FocalLength);
			Preferences.Set (CameraKey, Camera);
			Preferences.Set (CreatorKey, Creator);
			Preferences.Set (RatingKey, Rating);
		}

		static void Write (string context, string entryId, bool visible)
			=> Preferences.Set (ContextKey (context, entryId), visible ? "1" : "0");

		static bool Read (string context, string entryId, string legacyKey, bool defaultValue)
		{
			try {
				string stored = Preferences.JsonBackend.Get<string> (ContextKey (context, entryId));
				if (stored != null)
					return stored == "1";
			} catch (NoSuchKeyException) {
			}
			try {
				return Preferences.JsonBackend.Get<bool> (legacyKey);
			} catch (NoSuchKeyException) {
				return defaultValue;
			}
		}

		static bool Read (string key, bool defaultValue)
		{
			try {
				return Preferences.JsonBackend.Get<bool> (key);
			} catch (NoSuchKeyException) {
				return defaultValue;
			}
		}
	}
}
