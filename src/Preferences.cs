using System.Net;
using System;
using System.Collections.Generic;
using Mono.Unix;

namespace FSpot
{
	public class Preferences
	{
		public const string APP_FSPOT = "/apps/f-spot/";
		public const string APP_FSPOT_EXPORT = APP_FSPOT + "export/";
		public const string APP_FSPOT_EXPORT_TOKENS = APP_FSPOT_EXPORT + "tokens/";

		public const string MAIN_WINDOW_MAXIMIZED = "/apps/f-spot/ui/maximized";

		public const string MAIN_WINDOW_X = "/apps/f-spot/ui/main_window_x";
		public const string MAIN_WINDOW_Y = "/apps/f-spot/ui/main_window_y";
		public const string MAIN_WINDOW_WIDTH = "/apps/f-spot/ui/main_window_width";
		public const string MAIN_WINDOW_HEIGHT = "/apps/f-spot/ui/main_window_height";

 		public const string IMPORT_WINDOW_WIDTH = "/apps/f-spot/ui/import_window_width";
 		public const string IMPORT_WINDOW_HEIGHT = "/apps/f-spot/ui/import_window_height";
 		public const string IMPORT_WINDOW_PANE_POSITION = "/apps/f-spot/ui/import_window_pane_position";
		
		public const string VIEWER_WIDTH = "/apps/f-spot/ui/viewer_width";
		public const string VIEWER_HEIGHT = "/apps/f-spot/ui/viewer_height";
		public const string VIEWER_MAXIMIZED = "/apps/f-spot/ui/viewer_maximized";
		public const string VIEWER_SHOW_TOOLBAR = "/apps/f-spot/ui/viewer_show_toolbar";
		public const string VIEWER_SHOW_FILENAMES = "/apps/f-spot/ui/viewer_show_filenames";
		public const string VIEWER_INTERPOLATION = "/apps/f-spot/viewer/interpolation";
		public const string VIEWER_TRANS_COLOR = "/apps/f-spot/viewer/trans_color";
		public const string VIEWER_TRANSPARENCY = "/apps/f-spot/viewer/transparency";
		public const string CUSTOM_CROP_RATIOS = "/apps/f-spot/viewer/custom_crop_ratios";
		
		public const string SHOW_TOOLBAR = "/apps/f-spot/ui/show_toolbar";
		public const string SHOW_SIDEBAR = "/apps/f-spot/ui/show_sidebar";
		public const string SHOW_TIMELINE = "/apps/f-spot/ui/show_timeline";
		public const string SHOW_FILMSTRIP = "/apps/f-spot/ui/show_filmstrip";
		public const string SHOW_TAGS = "/apps/f-spot/ui/show_tags";
		public const string SHOW_DATES = "/apps/f-spot/ui/show_dates";
		public const string EXPANDED_TAGS = "/apps/f-spot/ui/expanded_tags";
		public const string SHOW_RATINGS = "/apps/f-spot/ui/show_ratings";
		public const string TAG_ICON_SIZE = "/apps/f-spot/ui/tag_icon_size";
		
		public const string GLASS_POSITION = "/apps/f-spot/ui/glass_position";
		public const string GROUP_ADAPTOR = "/apps/f-spot/ui/group_adaptor";
		public const string GROUP_ADAPTOR_ORDER_ASC = "/apps/f-spot/ui/group_adaptor_sort_asc";
		
		public const string SIDEBAR_POSITION = "/apps/f-spot/ui/sidebar_size";
		public const string ZOOM = "/apps/f-spot/ui/zoom";

		public const string EXPORT_EMAIL_SIZE = "/apps/f-spot/export/email/size";
		public const string EXPORT_EMAIL_ROTATE = "/apps/f-spot/export/email/auto_rotate";
		public const string EXPORT_EMAIL_DELETE_TIMEOUT_SEC = "/apps/f-spot/export/email/delete_timeout_seconds";

		public const string IMPORT_GUI_ROLL_HISTORY = "/apps/f-spot/import/gui_roll_history";

		public const string DBUS_READ_ONLY = "/apps/f-spot/dbus/read_only";

		public const string SCREENSAVER_TAG = "/apps/f-spot/screensaver/tag_id";

		public const string STORAGE_PATH = "/apps/f-spot/import/storage_path";

		public const string METADATA_EMBED_IN_IMAGE = "/apps/f-spot/metadata/embed_in_image";

		public const string EDIT_REDEYE_THRESHOLD = "/apps/f-spot/edit/redeye_threshold";

		public const string GNOME_SCREENSAVER_THEME = "/apps/gnome-screensaver/themes";
		public const string GNOME_SCREENSAVER_MODE = "/apps/gnome-screensaver/mode";

		public const string GNOME_MAILTO_COMMAND = "/desktop/gnome/url-handlers/mailto/command";
		public const string GNOME_MAILTO_ENABLED = "/desktop/gnome/url-handlers/mailto/enabled";

		public const string PROXY_USE_PROXY = "/system/http_proxy/use_http_proxy";
		public const string PROXY_HOST = "/system/http_proxy/host";
		public const string PROXY_PORT = "/system/http_proxy/port";
		public const string PROXY_USER = "/system/http_proxy/authentication_user";
		public const string PROXY_PASSWORD = "/system/http_proxy/authentication_password";
		public const string PROXY_BYPASS_LIST = "/system/http_proxy/ignore_hosts";


		private static IPreferenceBackend backend;
		private static NotifyChangedHandler changed_handler;
		private static IPreferenceBackend Backend {
			get {
				if (backend == null) {
#if !NOGCONF
					try {
						backend = new GConfPreferenceBackend ();
					} catch (Exception ex) {
						Console.WriteLine ("Couldn't load Gconf. Check that gconf-daemon is running.{0}{1}",
							Environment.NewLine, ex);
						backend = new NullPreferenceBackend ();
					}
#else
					backend = new NullPreferenceBackend ();
#endif
					changed_handler = new NotifyChangedHandler (OnSettingChanged);
					backend.AddNotify ("/apps/f-spot", changed_handler);
					backend.AddNotify ("/apps/gnome-screensaver/themes", changed_handler);
					backend.AddNotify ("/apps/gnome-screensaver/mode", changed_handler);
					backend.AddNotify ("/desktop/gnome/url-handlers/mailto", changed_handler);
					backend.AddNotify ("/system/http_proxy", changed_handler);
				}
				return backend;
			}
		}
		private static Dictionary<string, object> cache = new Dictionary<string, object>();

		public static object GetDefault (string key)
		{
			switch (key) {
			case MAIN_WINDOW_X:
			case MAIN_WINDOW_Y:
			case MAIN_WINDOW_HEIGHT:
			case MAIN_WINDOW_WIDTH:
				return null;
				
			case IMPORT_WINDOW_HEIGHT:
			case IMPORT_WINDOW_WIDTH:
			case IMPORT_WINDOW_PANE_POSITION:
				return 0;
					
			case METADATA_EMBED_IN_IMAGE:
			case MAIN_WINDOW_MAXIMIZED:
			case GROUP_ADAPTOR_ORDER_ASC:
				return false;

			case GROUP_ADAPTOR:
			case GLASS_POSITION:
				return null;

			case SHOW_TOOLBAR:
			case SHOW_SIDEBAR:
			case SHOW_TIMELINE:
			case SHOW_FILMSTRIP:
			case SHOW_TAGS:
			case SHOW_DATES:
			case SHOW_RATINGS:
			case VIEWER_SHOW_FILENAMES:
			case DBUS_READ_ONLY:
				return true;
			
			case TAG_ICON_SIZE:
				return (int) Tag.IconSize.Large;
		
			case SIDEBAR_POSITION:
			case ZOOM:
				return null;

			case IMPORT_GUI_ROLL_HISTORY:
				return 10;

			case SCREENSAVER_TAG:
				return 1;
			case STORAGE_PATH:
				return System.IO.Path.Combine (FSpot.Global.HomeDirectory, Catalog.GetString("Photos"));
			case EXPORT_EMAIL_SIZE:
				return 3;	// medium size 640px
			case EXPORT_EMAIL_ROTATE:
			case VIEWER_INTERPOLATION:
				return true;
			case EXPORT_EMAIL_DELETE_TIMEOUT_SEC:
				return 30;	// delete temporary email pictures after 30 seconds
			case VIEWER_TRANSPARENCY:
				return "NONE";
			case VIEWER_TRANS_COLOR:
				return "#000000";
			case EDIT_REDEYE_THRESHOLD:
				return -15;

			case PROXY_USE_PROXY:
				return false;
			case PROXY_PORT:
				return 0;
			case PROXY_USER:
			case PROXY_PASSWORD:
				return String.Empty;
			default:
				return null;
			}
		}
		
		public static object Get (string key)
		{
			lock (cache) {
				object val = null;
				if (cache.TryGetValue (key, out val)) 
					return val;

				try {
					val = Backend.Get (key);
					cache.Add (key, val);
				} catch (NoSuchKeyException) {
					val = GetDefault (key);

					if (val != null)
						Set (key, val);
				}

				return val;
			}
		}

		public static void Set (string key, object value)
		{
			lock (cache) {
				try {
					cache [key] = value;				
					Backend.Set (key, value);
				} catch {
					Console.WriteLine ("Unable to write this gconf key :"+key);
				}
			}
		}

		public static event NotifyChangedHandler SettingChanged;

		static void OnSettingChanged (object sender, NotifyEventArgs args)
		{
			lock (cache) {
				if (cache.ContainsKey (args.Key)) {
					cache [args.Key] = args.Value;				
				}
			}

			if (SettingChanged != null) {
				SettingChanged (sender, args);
			}
		}

		public static WebProxy GetProxy () 
		{
			WebProxy proxy = null;
			
			if ((bool) Preferences.Get (PROXY_USE_PROXY))
				return null;

			try {
				string host;
				int    port;
				
				host = (string) Preferences.Get (PROXY_HOST);
				port = (int) Preferences.Get (PROXY_PORT);
				
				string uri = "http://" + host + ":" + port.ToString ();
				proxy = new WebProxy (uri);

				string [] bypass_list = (string []) Preferences.Get (PROXY_BYPASS_LIST);
				if (bypass_list != null) {
					for (int i = 0; i < bypass_list.Length; i++) {
						bypass_list [i] = "http://" + bypass_list [i];
					}
					proxy.BypassList = bypass_list;
				}

				string username = (string) Preferences.Get (PROXY_USER);
				string password = (string) Preferences.Get (PROXY_PASSWORD);

				proxy.Credentials = new NetworkCredential (username, password);
			} catch (Exception) {
				proxy = null;
			}

			return proxy;
		}
	}
}
