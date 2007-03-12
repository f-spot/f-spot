namespace FSpot {
	public class Global {
		public static string HomeDirectory {
			get {
				return System.IO.Path.Combine (System.Environment.GetEnvironmentVariable ("HOME"), System.String.Empty);	
			}
		}
		
		private static string base_dir = System.IO.Path.Combine (HomeDirectory,  System.IO.Path.Combine (".gnome2", "f-spot"));

		public static string BaseDirectory {
			get {
				return base_dir;
			}
			set {
				base_dir = value;
			}
		}

		private static string photo_directory = (string) Preferences.Get(Preferences.STORAGE_PATH);

		public static string PhotoDirectory {
			get {
				return photo_directory;
			}
			set {
				photo_directory = value;
			}
		}

		public static bool CustomPhotoDirectory {
			get {
				return photo_directory != (string)Preferences.Get(Preferences.STORAGE_PATH);
			}
		}

		public static void ModifyColors (Gtk.Widget widget)
		{
			try {
				widget.ModifyFg (Gtk.StateType.Normal, widget.Style.TextColors [(int)Gtk.StateType.Normal]);
				widget.ModifyFg (Gtk.StateType.Active, widget.Style.TextColors [(int)Gtk.StateType.Active]);
				widget.ModifyFg (Gtk.StateType.Selected, widget.Style.TextColors [(int)Gtk.StateType.Selected]);
				widget.ModifyBg (Gtk.StateType.Normal, widget.Style.BaseColors [(int)Gtk.StateType.Normal]);
				widget.ModifyBg (Gtk.StateType.Active, widget.Style.BaseColors [(int)Gtk.StateType.Active]);
				widget.ModifyBg (Gtk.StateType.Selected, widget.Style.BaseColors [(int)Gtk.StateType.Selected]);
				
			} catch {
				widget.ModifyFg (Gtk.StateType.Normal, widget.Style.Black);
				widget.ModifyBg (Gtk.StateType.Normal, widget.Style.Black);
			}
		}

		public static string HelpDirectory {
			get { 
				return System.IO.Path.Combine(Defines.PREFIX,
					System.IO.Path.Combine("share",
					System.IO.Path.Combine("gnome",
					System.IO.Path.Combine("help", "f-spot"))));
			}	
		}

		private static Cms.Profile display_profile;
		public static Cms.Profile DisplayProfile {
			set { display_profile = value; }
			get { return display_profile; }
		}

		private static Cms.Profile destination_profile;
		public static Cms.Profile DestinationProfile {
			set { destination_profile = value; }
			get { return destination_profile; }
		}
	}
}
