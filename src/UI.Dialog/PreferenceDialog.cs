/*
 * FSpot.UI.Dialog.PreferenceDialog.cs
 *
 * Authors(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Collections.Generic;
using Gtk;

using FSpot.Widgets;

namespace FSpot.UI.Dialog {
	public class ProfileList : TreeStore {
		public ProfileList () : base (typeof (Cms.Profile))
		{
			foreach (Cms.Profile profile in FSpot.ColorManagement.Profiles)
				this.AppendValues (profile);
		}

		private const int NameLenth = 50;
		public static void ProfileNameDataFunc (CellLayout layout, CellRenderer renderer, TreeModel model, TreeIter iter)
		{
			if (model.GetValue (iter, 0) != null) {
				Cms.Profile profile = (Cms.Profile) model.GetValue (iter, 0);
				if (profile.ProductName.Length < NameLenth)
					(renderer as Gtk.CellRendererText).Text = profile.ProductName;
				else
					(renderer as Gtk.CellRendererText).Text = profile.ProductName.Substring(0, NameLenth) + "...";
			}
			else
				(renderer as Gtk.CellRendererText).Text = "";
		}
	}

	public class PreferenceDialog : GladeDialog {
		[Glade.Widget] private CheckButton metadata_check;
		[Glade.Widget] private CheckButton colormanagement_check;
		[Glade.Widget] private CheckButton use_x_profile_check;
		[Glade.Widget] private ComboBox display_combo;
		[Glade.Widget] private ComboBox destination_combo;
		[Glade.Widget] private HBox tagselectionhbox;
		[Glade.Widget] private Button set_saver_button;
		[Glade.Widget] private FileChooserButton photosdir_chooser;
		[Glade.Widget] private RadioButton screensaverall_radio;
		[Glade.Widget] private RadioButton screensavertagged_radio;
		[Glade.Widget] private CheckButton dbus_check;
		[Glade.Widget] private RadioButton themenone_radio;
		[Glade.Widget] private RadioButton themecustom_radio;
		[Glade.Widget] private Label themelist_label;
		[Glade.Widget] private Label restartlabel;
		[Glade.Widget] private Label themefile_label;
		[Glade.Widget] private FileChooserButton theme_filechooser;
		[Glade.Widget] private Table theme_table;
		[Glade.Widget] private Button refreshtheme_button;
		private ComboBox themelist_combo;
		private MenuButton tag_button;


		private static PreferenceDialog prefs = null;
		int screensaver_tag;
		private const string SaverCommand = "screensavers-f-spot-screensaver";
		private const string SaverMode = "single";
		Dictionary<string, string> theme_list;

		public PreferenceDialog () : base ("main_preferences")
		{
			tag_button = new MenuButton ();
			LoadPreference (Preferences.METADATA_EMBED_IN_IMAGE);
			LoadPreference (Preferences.COLOR_MANAGEMENT_ENABLED);
			LoadPreference (Preferences.COLOR_MANAGEMENT_USE_X_PROFILE);
			LoadPreference (Preferences.SCREENSAVER_TAG);
			LoadPreference (Preferences.GNOME_SCREENSAVER_THEME);
			if (Global.PhotoDirectory == Preferences.Get<string> (Preferences.STORAGE_PATH)) {
				photosdir_chooser.CurrentFolderChanged += HandlePhotosdirChanged;
				photosdir_chooser.SetCurrentFolder (Global.PhotoDirectory);
			} else {
				photosdir_chooser.SetCurrentFolder(Global.PhotoDirectory);
				photosdir_chooser.Sensitive = false;
			}

			Gtk.CellRendererText name_cell = new Gtk.CellRendererText ();
			Gtk.CellRendererText desc_cell = new Gtk.CellRendererText ();
			
			use_x_profile_check.Sensitive = colormanagement_check.Active;
			
			display_combo.Sensitive = colormanagement_check.Active;
			display_combo.Model = new ProfileList ();                                                                                    
			display_combo.PackStart (desc_cell, false);
			display_combo.PackStart (name_cell, true);
			display_combo.SetCellDataFunc (name_cell, new CellLayoutDataFunc (ProfileList.ProfileNameDataFunc));
			//FIXME
			int it_ = 0;
			foreach (Cms.Profile profile in FSpot.ColorManagement.Profiles) {
				if (profile.ProductName == Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE))
					display_combo.Active = it_;
				it_++;
			}

			display_combo.Changed += HandleDisplayChanged;

			destination_combo.Sensitive = colormanagement_check.Active;
			destination_combo.Model = new ProfileList ();
			destination_combo.PackStart (desc_cell, false);
			destination_combo.PackStart (name_cell, true);
			destination_combo.SetCellDataFunc (name_cell, new CellLayoutDataFunc (ProfileList.ProfileNameDataFunc));
			destination_combo.Changed += HandleDestinationChanged;
			//FIXME
			it_ = 0;
			foreach (Cms.Profile profile in FSpot.ColorManagement.Profiles) {
				if (profile.ProductName ==  Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_OUTPUT_PROFILE))
					destination_combo.Active = it_;
				it_++;
			}

			TagMenu tagmenu = new TagMenu (null, MainWindow.Toplevel.Database.Tags);
	
			tagmenu.Populate (false);

			tag_button.Menu = tagmenu;
			tag_button.ShowAll ();
			tagselectionhbox.Add (tag_button);

			tagmenu.TagSelected += HandleTagMenuSelected;
			set_saver_button.Clicked += HandleUseFSpot;
			screensaverall_radio.Toggled += ToggleTagRadio;

			themenone_radio.Toggled += ToggleThemeRadio;
			themelist_combo = ComboBox.NewText ();
			theme_list = new Dictionary<string, string> ();
			string gtkrc = Path.Combine ("gtk-2.0", "gtkrc");
			string [] search = {Path.Combine (Global.HomeDirectory, ".themes"), "/usr/share/themes"};
			foreach (string path in search)
				if (Directory.Exists (path)) 
					foreach (string dir in Directory.GetDirectories (path))
						if (File.Exists (Path.Combine (dir, gtkrc)) && !theme_list.ContainsKey (Path.GetFileName (dir)))
							theme_list.Add (Path.GetFileName (dir), Path.Combine (dir, gtkrc));
			
			string active_theme = Preferences.Get<string> (Preferences.GTK_RC);
			int it = 0;
			foreach (string theme in theme_list.Keys) {
				themelist_combo.AppendText (Path.GetFileName (theme));
				if (active_theme.Contains (Path.DirectorySeparatorChar + Path.GetFileName (theme) + Path.DirectorySeparatorChar))
					themelist_combo.Active = it;
				it ++;
			}
			
			theme_table.Attach (themelist_combo, 2, 3, 0, 1);
			themelist_combo.Changed += HandleThemeComboChanged;
			themelist_combo.Show ();
			theme_filechooser.Visible = themefile_label.Visible = FSpot.Utils.Log.Debugging;

			themelist_combo.Sensitive = theme_filechooser.Sensitive = themecustom_radio.Active; 
			if (File.Exists (active_theme))
				theme_filechooser.SetFilename (Preferences.Get<string> (Preferences.GTK_RC));
			theme_filechooser.SelectionChanged += HandleThemeFileActivated;
			themecustom_radio.Active = (active_theme != String.Empty);	

#if GTK_2_12_2
			restartlabel.Visible = false;
#endif

#if DEBUGTHEMES
			refreshtheme_button = true;
#endif

			Preferences.SettingChanged += OnPreferencesChanged;
			this.Dialog.Destroyed += HandleDestroyed;
		}

		private void ColorManagementEnabledToggled (object sender, System.EventArgs args)
		{
			Preferences.Set (Preferences.COLOR_MANAGEMENT_ENABLED, colormanagement_check.Active);

			if (FSpot.ColorManagement.IsEnabled != colormanagement_check.Active) {
				FSpot.ColorManagement.IsEnabled = colormanagement_check.Active;
				FSpot.ColorManagement.ReloadSettings();
			}
			
			use_x_profile_check.Sensitive = colormanagement_check.Active;
			display_combo.Sensitive = colormanagement_check.Active;
			destination_combo.Sensitive = colormanagement_check.Active;
		}
		
		private void UseXProfileToggled (object sender, System.EventArgs args)
		{
			Preferences.Set (Preferences.COLOR_MANAGEMENT_USE_X_PROFILE, use_x_profile_check.Active);
			if (FSpot.ColorManagement.UseXProfile != use_x_profile_check.Active) {
				FSpot.ColorManagement.UseXProfile = use_x_profile_check.Active;
				FSpot.ColorManagement.ReloadSettings();
			}
		}

		private void HandleDisplayChanged (object sender, System.EventArgs args)
		{
			TreeIter iter;
//			Gdk.Screen screen = Gdk.Screen.Default;
			if (display_combo.GetActiveIter (out iter)) {
				FSpot.ColorManagement.DisplayProfile = (Cms.Profile) display_combo.Model.GetValue (iter, 0);
//				FSpot.Widgets.CompositeUtils.SetScreenProfile(screen, FSpot.ColorManagement.DisplayProfile);
				FSpot.ColorManagement.ReloadSettings();
			}
		}
		
		private void HandleDestinationChanged (object sender, System.EventArgs args)
		{
			TreeIter iter;
			if (destination_combo.GetActiveIter (out iter))
				FSpot.ColorManagement.DestinationProfile = (Cms.Profile) destination_combo.Model.GetValue (iter, 0);
		}

		private void HandleTagMenuSelected (Tag t)
		{
			tag_button.Label = t.Name;
			screensaver_tag = (int) t.Id;
			Preferences.Set (Preferences.SCREENSAVER_TAG, (int) t.Id);
		}

		private void HandleUseFSpot (object sender, EventArgs args)
		{
			Preferences.Set (Preferences.GNOME_SCREENSAVER_MODE, SaverMode);
			Preferences.Set (Preferences.GNOME_SCREENSAVER_THEME, new string [] { SaverCommand });
		}

		private void ToggleTagRadio (object o, System.EventArgs e)
		{
			tag_button.Sensitive = (screensavertagged_radio.Active);
			if (screensaverall_radio.Active)
				Preferences.Set (Preferences.SCREENSAVER_TAG, 0);
			else
				HandleTagMenuSelected (((tag_button.Menu as Menu).Active as TagMenu.TagMenuItem).Value);
		}

		void ToggleThemeRadio (object o, EventArgs e)
		{
			themelist_combo.Sensitive = theme_filechooser.Sensitive = themecustom_radio.Active; 
			if (themenone_radio.Active) {
				Preferences.Set (Preferences.GTK_RC, String.Empty);
#if GTK_2_12_2
				if (!File.Exists (Path.Combine (Global.BaseDirectory, "gtkrc")))
					(File.Create (Path.Combine (Global.BaseDirectory, "gtkrc"))).Dispose ();
				else
					File.SetLastWriteTime (Path.Combine (Global.BaseDirectory, "gtkrc"), DateTime.Now);
				Gtk.Rc.DefaultFiles = Global.DefaultRcFiles;
				Gtk.Rc.ReparseAll ();
#endif
			}
		}

		void HandleThemeComboChanged (object o, EventArgs e)
		{
			if (o == null)
				return;
			TreeIter iter;
			if ((o as ComboBox).GetActiveIter (out iter))
				Preferences.Set (Preferences.GTK_RC, theme_list [((o as ComboBox).Model.GetValue (iter, 0)) as string]);
#if GTK_2_12_2
			if (!File.Exists (Path.Combine (Global.BaseDirectory, "gtkrc")))
				(File.Create (Path.Combine (Global.BaseDirectory, "gtkrc"))).Dispose ();
			else
				File.SetLastWriteTime (Path.Combine (Global.BaseDirectory, "gtkrc"), DateTime.Now);
			Gtk.Rc.DefaultFiles = Global.DefaultRcFiles;
			Gtk.Rc.AddDefaultFile (Preferences.Get<string> (Preferences.GTK_RC));
			foreach (string s in Rc.DefaultFiles)
			Console.WriteLine (s);
			Gtk.Rc.ReparseAll ();
#endif
		}

		void HandleThemeFileActivated (object o, EventArgs e)
		{
			if (theme_filechooser.Filename != null && theme_filechooser.Filename != Preferences.Get<string> (Preferences.GTK_RC)) {
				Preferences.Set (Preferences.GTK_RC, theme_filechooser.Filename);	
#if GTK_2_12_2
				if (!File.Exists (Path.Combine (Global.BaseDirectory, "gtkrc")))
					(File.Create (Path.Combine (Global.BaseDirectory, "gtkrc"))).Dispose ();
				else
					File.SetLastWriteTime (Path.Combine (Global.BaseDirectory, "gtkrc"), DateTime.Now);
				Gtk.Rc.DefaultFiles = Global.DefaultRcFiles;
				Gtk.Rc.AddDefaultFile (Preferences.Get<string> (Preferences.GTK_RC));
				foreach (string s in Rc.DefaultFiles)
					Console.WriteLine (s);
				Gtk.Rc.ReparseAll ();
#endif
			}
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void MetadataToggled (object sender, System.EventArgs args)
		{
			Preferences.Set (Preferences.METADATA_EMBED_IN_IMAGE, metadata_check.Active);
		}

		void HandlePhotosdirChanged (object sender, System.EventArgs args)
		{
			Preferences.Set (Preferences.STORAGE_PATH, photosdir_chooser.Filename);
			Global.PhotoDirectory = photosdir_chooser.Filename;
		}


		void HandleRefreshTheme (object o, EventArgs e)
		{
#if GTK_2_12_2
			Gtk.Rc.ReparseAll ();	
#endif
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case Preferences.METADATA_EMBED_IN_IMAGE:
				bool active = Preferences.Get<bool> (key);
				if (metadata_check.Active != active)
					metadata_check.Active = active;
				break;
case Preferences.COLOR_MANAGEMENT_ENABLED:
				active = Preferences.Get<bool> (key);
				if (colormanagement_check.Active != active)
					colormanagement_check.Active = active;
				break;
			case Preferences.COLOR_MANAGEMENT_USE_X_PROFILE:
				active = Preferences.Get<bool> (key);
				if (use_x_profile_check.Active != active)
					use_x_profile_check.Active = active;
				break;
			case Preferences.SCREENSAVER_TAG:
				try {
					screensaver_tag = Preferences.Get<int> (key);
				} catch (System.Exception e) {
					Console.WriteLine (e);
					screensaver_tag = 0;
				}
				if (screensaver_tag == 0) {
					screensaverall_radio.Active = true;
					tag_button.Sensitive = false;
				} else {
					screensavertagged_radio.Active = true;
					Tag t = MainWindow.Toplevel.Database.Tags.GetTagById (screensaver_tag);
					tag_button.Label = t.Name;
				}
				break;
			case Preferences.GNOME_SCREENSAVER_THEME:
			case Preferences.GNOME_SCREENSAVER_MODE:
				string [] theme = Preferences.Get<string []> (Preferences.GNOME_SCREENSAVER_THEME);
				string mode = Preferences.Get<string> (Preferences.GNOME_SCREENSAVER_MODE);
				
				bool sensitive = mode != SaverMode;
				sensitive |= (theme == null || theme.Length != 1 || theme [0] != SaverCommand);

				set_saver_button.Sensitive = sensitive;
				break;
			case Preferences.STORAGE_PATH:
				photosdir_chooser.SetCurrentFolder (Preferences.Get<string> (key));
				break;
			case Preferences.GTK_RC:
				themenone_radio.Active = (Preferences.Get<string> (key) == String.Empty);
				themecustom_radio.Active = (Preferences.Get<string> (key) != String.Empty);
				if (theme_filechooser.Sensitive)
					theme_filechooser.SetFilename (Preferences.Get<string> (key));
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
