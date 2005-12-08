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
		private static PreferenceDialog prefs = null;

		public PreferenceDialog () : base ("main_preferences")
		{
			LoadPreference (Preferences.METADATA_EMBED_IN_IMAGE);


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


			Preferences.SettingChanged += OnPreferencesChanged;
			this.Dialog.Destroyed += HandleDestroyed;
		}

		public void HandleDisplayChanged (object sender, System.EventArgs args)
		{
			TreeIter iter;
			if (display_combo.GetActiveIter (out iter))
				FSpot.Global.DisplayProfile = (Profile) display_combo.Model.GetValue (iter, 0);
		}
		
		public void HandleDestinationChanged (object sender, System.EventArgs args)
		{
			TreeIter iter;
			if (destination_combo.GetActiveIter (out iter))
				FSpot.Global.DestinationProfile = (Profile) destination_combo.Model.GetValue (iter, 0);
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
