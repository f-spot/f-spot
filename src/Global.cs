namespace FSpot {
	public class Global {
		public static string HomeDirectory {
			get {
				return System.Environment.GetEnvironmentVariable ("HOME");	
			}
		}

		public static string BaseDirectory {
			get {
				return System.IO.Path.Combine (HomeDirectory,  System.IO.Path.Combine (".gnome2", "f-spot"));
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
			//widget.ModifyFg (Gtk.StateType.Normal, widget.Style.TextColors [(int)Gtk.StateType.Normal]);
			//widget.ModifyBg (Gtk.StateType.Normal, widget.Style.BaseColors [(int)Gtk.StateType.Normal]);
			widget.ModifyFg (Gtk.StateType.Normal, widget.Style.Black);
			widget.ModifyBg (Gtk.StateType.Normal, widget.Style.White);
#endif 
		}

#if false
		private Cms.Profile display_profile;
		public Cms.Profile DisplayProfile {
			get {
				return Cms.Profile.CreateSRgb ();
			}
		}
		
		private System.Collections.Hashtable profile_cache;
		public Cms.Transform DisplayTranform (Cms.Profile image_profile)
		{
			if (image_profile == null && display_profile == null)
				return null;
		}
#endif

	}
}
