using System;
using Gtk;
using Cms;

namespace FSpot {
	public class ProfileList : TreeStore {
		public ProfileList () : base (typeof (Profile))
		{
			this.AppendValues (Profile.CreateStandardRgb ());
			this.AppendValues (Profile.CreateAlternateRgb ());
		}

		public static void ProfileNameDataFunc (CellLayout layout, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			Profile profile = (Profile) model.GetValue (iter, 0);
			(renderer as Gtk.CellRendererText).Text = profile.ProductName;
		}

		public static void ProfileDescriptionDataFunc (CellLayout layout, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			Profile profile = (Profile) model.GetValue (iter, 0);
			(renderer as Gtk.CellRendererText).Text = profile.ProductDescription;
		}
	}

	public class PreferenceDialog : GladeDialog {
		[Glade.Widget] private CheckButton metadata_check;
		[Glade.Widget] private ComboBox display_combo;
		[Glade.Widget] private ComboBox destination_combo;
		[Glade.Widget] private OptionMenu tag_option;
		[Glade.Widget] private Button set_saver_button;
		private static PreferenceDialog prefs = null;
		int screensaver_tag;
		private const string SaverCommand = "f-spot-screensaver";

		public PreferenceDialog () : base ("main_preferences")
		{
			LoadPreference (Preferences.METADATA_EMBED_IN_IMAGE);
			LoadPreference (Preferences.SCREENSAVER_TAG);
			LoadPreference (Preferences.GNOME_SCREENSAVER_THEME);

			Gtk.CellRendererText name_cell = new Gtk.CellRendererText ();
			Gtk.CellRendererText desc_cell = new Gtk.CellRendererText ();
			
			display_combo.Model = new ProfileList ();
			display_combo.PackStart (desc_cell, false);
			display_combo.PackStart (name_cell, true);
			display_combo.SetCellDataFunc (name_cell, new CellLayoutDataFunc (ProfileList.ProfileNameDataFunc));
			display_combo.SetCellDataFunc (desc_cell, new CellLayoutDataFunc (ProfileList.ProfileDescriptionDataFunc));
			display_combo.Changed += HandleDisplayChanged;

			destination_combo.Model = new ProfileList ();
			destination_combo.PackStart (desc_cell, false);
			destination_combo.PackStart (name_cell, true);
			destination_combo.SetCellDataFunc (name_cell, new CellLayoutDataFunc (ProfileList.ProfileNameDataFunc));
			destination_combo.SetCellDataFunc (desc_cell, new CellLayoutDataFunc (ProfileList.ProfileDescriptionDataFunc));
			destination_combo.Changed += HandleDisplayChanged;

			Tag t = MainWindow.Toplevel.Database.Tags.GetTagById (screensaver_tag);
			TagMenu tagmenu = new TagMenu (null, MainWindow.Toplevel.Database.Tags);
	
			tagmenu.Populate (true);
			tag_option.Menu = tagmenu;

			int history = tagmenu.GetPosition (t);
			if (history >= 0)
				tag_option.SetHistory ((uint)history);

			tagmenu.TagSelected += HandleTagMenuSelected;
			set_saver_button.Clicked += HandleUseFSpot;

			Preferences.SettingChanged += OnPreferencesChanged;
			this.Dialog.Destroyed += HandleDestroyed;
		}

		private void HandleDisplayChanged (object sender, System.EventArgs args)
		{
			TreeIter iter;
			if (display_combo.GetActiveIter (out iter))
				FSpot.Global.DisplayProfile = (Profile) display_combo.Model.GetValue (iter, 0);
		}
		
		private void HandleDestinationChanged (object sender, System.EventArgs args)
		{
			TreeIter iter;
			if (destination_combo.GetActiveIter (out iter))
				FSpot.Global.DestinationProfile = (Profile) destination_combo.Model.GetValue (iter, 0);
		}

		private void HandleTagMenuSelected (Tag t)
		{
			screensaver_tag = (int) t.Id;
			Preferences.Set (Preferences.SCREENSAVER_TAG, (int) t.Id);
		}

		private void HandleUseFSpot (object sender, EventArgs args)
		{
			Preferences.Set (Preferences.GNOME_SCREENSAVER_THEME, new string [] { SaverCommand });
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
			case Preferences.SCREENSAVER_TAG:
				try {
					screensaver_tag = (int) val;
				} catch (System.Exception e) {
					Console.WriteLine (e);
					screensaver_tag = 0;
				}
				break;
			case Preferences.GNOME_SCREENSAVER_THEME:
				if (val == null) {
					set_saver_button.Sensitive = false;
					return;
				}

				string [] names = (string []) val;
				set_saver_button.Sensitive = (names.Length != 1 || names [0] != SaverCommand);
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
