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
