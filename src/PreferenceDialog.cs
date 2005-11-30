using System;
using Gtk;

namespace FSpot {
	public class PreferenceDialog : GladeDialog {
		[Glade.Widget] private CheckButton metadata_check;
		[Glade.Widget] private ComboBox display_combo;
		[Glade.Widget] private ComboBox destination_combo;
		private static PreferenceDialog prefs = null;

		public PreferenceDialog () : base ("main_preferences")
		{
			LoadPreference (Preferences.METADATA_EMBED_IN_IMAGE);

			Preferences.SettingChanged += OnPreferencesChanged;
			this.Dialog.Destroyed += HandleDestroyed;
		}

		void OnPreferencesChanged (object sender, GConf.NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void MetadataToggled (object sender, System.EventArgs args)
		{
			Preferences.Set (Preferences.METADATA_EMBED_IN_IMAGE, metadata_check.Active);
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			switch (key) {
			case Preferences.METADATA_EMBED_IN_IMAGE:
				bool active = (bool) val;
				if (metadata_check.Active != active)
					metadata_check.Active = active;
				break;
			}
		}

		void HandleClose (object sender, EventArgs args)
		{
			this.Dialog.Destroy ();
		}

		private void HandleDestroyed (object sender, EventArgs args)
		{
			prefs = null;
		}

		public static void Show ()
		{
			if (prefs == null)
				prefs = new PreferenceDialog ();
			
			prefs.Dialog.Present ();
		}
	}
}
