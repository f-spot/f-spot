namespace FSpot {
	public class Global {
		public static string HomeDirectory {
			get {
				return System.IO.Path.Combine (System.Environment.GetEnvironmentVariable ("HOME"), "");	
			}
		}

		public static string BaseDirectory {
			get {
				return System.IO.Path.Combine (HomeDirectory,  System.IO.Path.Combine (".gnome2", "f-spot"));
			}
		}

		public static string PhotoDirectory {
			get {
				return System.IO.Path.Combine (HomeDirectory, "Photos");
			}
		}
		
		public static void ModifyColors (Gtk.Widget widget)
		{
#if false
			Gdk.Color color = widget.Style.Background (Gtk.StateType.Normal);
			color.Red = (ushort) (color.Red / 2);
			color.Blue = (ushort) (color.Blue / 2);
			color.Green = (ushort) (color.Green / 2);
			widget.ModifyBg (Gtk.StateType.Normal, color);
#else 
			try {
				widget.ModifyFg (Gtk.StateType.Normal, widget.Style.TextColors [(int)Gtk.StateType.Normal]);
				widget.ModifyFg (Gtk.StateType.Active, widget.Style.TextColors [(int)Gtk.StateType.Active]);
				widget.ModifyFg (Gtk.StateType.Selected, widget.Style.TextColors [(int)Gtk.StateType.Selected]);
				widget.ModifyBg (Gtk.StateType.Normal, widget.Style.BaseColors [(int)Gtk.StateType.Normal]);
				widget.ModifyBg (Gtk.StateType.Active, widget.Style.BaseColors [(int)Gtk.StateType.Active]);
				widget.ModifyBg (Gtk.StateType.Selected, widget.Style.BaseColors [(int)Gtk.StateType.Selected]);
				
			} catch {
				widget.ModifyFg (Gtk.StateType.Normal, widget.Style.Black);
				widget.ModifyBg (Gtk.StateType.Normal, widget.Style.White);
			}
#endif 
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
