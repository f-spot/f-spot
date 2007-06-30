using System.Net;
using System;
using System.Collections.Generic;
using Mono.Unix;

namespace FSpot
{
	public class Preferences
	{
		public const string MAIN_WINDOW_MAXIMIZED = "/apps/f-spot/ui/maximized";

		public const string MAIN_WINDOW_X = "/apps/f-spot/ui/main_window_x";
		public const string MAIN_WINDOW_Y = "/apps/f-spot/ui/main_window_y";
		public const string MAIN_WINDOW_WIDTH = "/apps/f-spot/ui/main_window_width";
		public const string MAIN_WINDOW_HEIGHT = "/apps/f-spot/ui/main_window_height";
		
		public const string VIEWER_WIDTH = "/apps/f-spot/ui/viewer_width";
		public const string VIEWER_HEIGHT = "/apps/f-spot/ui/viewer_height";
		public const string VIEWER_MAXIMIZED = "/apps/f-spot/ui/viewer_maximized";
		public const string VIEWER_SHOW_TOOLBAR = "/apps/f-spot/ui/viewer_show_toolbar";
		public const string VIEWER_SHOW_FILENAMES = "/apps/f-spot/ui/viewer_show_filenames";
		public const string VIEWER_INTERPOLATION = "/apps/f-spot/viewer/interpolation";
		public const string VIEWER_TRANS_COLOR = "/apps/f-spot/viewer/trans_color";
		public const string VIEWER_TRANSPARENCY = "/apps/f-spot/viewer/transparency";
		
		public const string SHOW_TOOLBAR = "/apps/f-spot/ui/show_toolbar";
		public const string SHOW_SIDEBAR = "/apps/f-spot/ui/show_sidebar";
		public const string SHOW_TIMELINE = "/apps/f-spot/ui/show_timeline";
		public const string SHOW_TAGS = "/apps/f-spot/ui/show_tags";
		public const string SHOW_DATES = "/apps/f-spot/ui/show_dates";
		public const string EXPANDED_TAGS = "/apps/f-spot/ui/expanded_tags";
		public const string TAG_ICON_SIZE = "/apps/f-spot/ui/tag_icon_size";
		
		public const string GLASS_POSITION = "/apps/f-spot/ui/glass_position";
		public const string GROUP_ADAPTOR = "/apps/f-spot/ui/group_adaptor";
		public const string GROUP_ADAPTOR_ORDER_ASC = "/apps/f-spot/ui/group_adaptor_sort_asc";
		
		public const string SIDEBAR_POSITION = "/apps/f-spot/ui/sidebar_size";
		public const string ZOOM = "/apps/f-spot/ui/zoom";

		public const string EXPORT_FLICKR_SCALE = "/apps/f-spot/export/flickr/scale";
		public const string EXPORT_FLICKR_SIZE = "/apps/f-spot/export/flickr/size";
		public const string EXPORT_FLICKR_BROWSER = "/apps/f-spot/export/flickr/browser";
		public const string EXPORT_FLICKR_TAGS = "/apps/f-spot/export/flickr/tags";
		public const string EXPORT_FLICKR_STRIP_META = "/apps/f-spot/export/flickr/strip_meta";
		public const string EXPORT_FLICKR_EMAIL = "/apps/f-spot/export/flickr/email";
		public const string EXPORT_FLICKR_PUBLIC = "/apps/f-spot/export/flickr/public";
		public const string EXPORT_FLICKR_FRIENDS = "/apps/f-spot/export/flickr/friends";
		public const string EXPORT_FLICKR_FAMILY = "/apps/f-spot/export/flickr/family";

		public const string EXPORT_TOKEN_FLICKR = "/apps/f-spot/export/tokens/flickr"; 
		public const string EXPORT_TOKEN_23HQ = "/apps/f-spot/export/tokens/23hq"; 
		public const string EXPORT_TOKEN_ZOOOMR = "/apps/f-spot/export/tokens/zooomr"; 

		public const string EXPORT_GALLERY_SCALE = "/apps/f-spot/export/gallery/scale";
		public const string EXPORT_GALLERY_SIZE = "/apps/f-spot/export/gallery/size";
		public const string EXPORT_GALLERY_BROWSER = "/apps/f-spot/export/gallery/browser";
		public const string EXPORT_GALLERY_META = "/apps/f-spot/export/gallery/meta";
		public const string EXPORT_GALLERY_ROTATE = "/apps/f-spot/export/gallery/rotate";

		public const string EXPORT_PICASAWEB_SCALE = "/apps/f-spot/export/picasaweb/scale";
		public const string EXPORT_PICASAWEB_SIZE = "/apps/f-spot/export/picasaweb/size";
		public const string EXPORT_PICASAWEB_ROTATE = "/apps/f-spot/export/picasaweb/rotate";
		public const string EXPORT_PICASAWEB_BROWSER = "/apps/f-spot/export/picasaweb/browser";

		public const string EXPORT_SMUGMUG_SCALE = "/apps/f-spot/export/smugmug/scale";
		public const string EXPORT_SMUGMUG_SIZE = "/apps/f-spot/export/smugmug/size";
		public const string EXPORT_SMUGMUG_ROTATE = "/apps/f-spot/export/smugmug/rotate";
		public const string EXPORT_SMUGMUG_BROWSER = "/apps/f-spot/export/smugmug/browser";

		public const string EXPORT_FOLDER_SCALE = "/apps/f-spot/export/folder/scale";
		public const string EXPORT_FOLDER_SIZE = "/apps/f-spot/export/folder/size";
		public const string EXPORT_FOLDER_OPEN = "/apps/f-spot/export/folder/browser";
		public const string EXPORT_FOLDER_ROTATE = "/apps/f-spot/export/folder/rotate";
		public const string EXPORT_FOLDER_METHOD = "/apps/f-spot/export/folder/method";
		public const string EXPORT_FOLDER_URI = "/apps/f-spot/export/folder/uri";
		public const string EXPORT_FOLDER_SHARPEN = "/apps/f-spot/export/folder/sharpen";
		public const string EXPORT_FOLDER_INCLUDE_TARBALLS = "/apps/f-spot/export/folder/include_tarballs";
		
		public const string EXPORT_EMAIL_SIZE = "/apps/f-spot/export/email/size";
		public const string EXPORT_EMAIL_ROTATE = "/apps/f-spot/export/email/auto_rotate";
		public const string EXPORT_EMAIL_DELETE_TIMEOUT_SEC = "/apps/f-spot/export/email/delete_timeout_seconds";

		public const string IMPORT_GUI_ROLL_HISTORY = "/apps/f-spot/import/gui_roll_history";

		public const string SCREENSAVER_TAG = "/apps/f-spot/screensaver/tag_id";

		public const string STORAGE_PATH = "/apps/f-spot/import/storage_path";

		public const string METADATA_EMBED_IN_IMAGE = "/apps/f-spot/metadata/embed_in_image";

		public const string EDIT_REDEYE_THRESHOLD = "/apps/f-spot/edit/redeye_threshold";

		public const string GNOME_SCREENSAVER_THEME = "/apps/gnome-screensaver/themes";
		public const string GNOME_SCREENSAVER_MODE = "/apps/gnome-screensaver/mode";

		public const string GNOME_MAILTO_COMMAND = "/desktop/gnome/url-handlers/mailto/command";

		public const string PROXY_USE_PROXY = "/system/http_proxy/use_http_proxy";
		public const string PROXY_HOST = "/system/http_proxy/host";
		public const string PROXY_PORT = "/system/http_proxy/port";
		public const string PROXY_USER = "/system/http_proxy/authentication_user";
		public const string PROXY_PASSWORD = "/system/http_proxy/authentication_password";
		public const string PROXY_BYPASS_LIST = "/system/http_proxy/ignore_hosts";


		private static GConf.Client client;
		private static GConf.NotifyEventHandler changed_handler;
		private static Dictionary<string, object> cache = new Dictionary<string, object>();

		private static GConf.Client Client 
		{
			get {
				if (client == null) {
					client = new GConf.Client ();

					changed_handler = new GConf.NotifyEventHandler (OnSettingChanged);
					client.AddNotify ("/apps/f-spot", changed_handler);
					client.AddNotify ("/apps/gnome-screensaver/themes", changed_handler);
					client.AddNotify ("/apps/gnome-screensaver/mode", changed_handler);
					client.AddNotify ("/desktop/gnome/url-handlers/mailto/command", changed_handler);
					client.AddNotify ("/system/http_proxy", changed_handler);
				}
				return client;
			}
		}

		public static object GetDefault (string key)
		{
			switch (key) {
			case MAIN_WINDOW_X:
			case MAIN_WINDOW_Y:
			case MAIN_WINDOW_HEIGHT:
			case MAIN_WINDOW_WIDTH:
				return null;
			
			case METADATA_EMBED_IN_IMAGE:
			case MAIN_WINDOW_MAXIMIZED:
				return false;

			case GROUP_ADAPTOR:
			case GLASS_POSITION:
			case GROUP_ADAPTOR_ORDER_ASC:
				return null;

			case SHOW_TOOLBAR:
			case SHOW_SIDEBAR:
			case SHOW_TIMELINE:
			case SHOW_TAGS:
			case SHOW_DATES:
			case VIEWER_SHOW_FILENAMES:
			case EXPORT_PICASAWEB_SCALE:
			case EXPORT_PICASAWEB_ROTATE:
			case EXPORT_PICASAWEB_BROWSER:
			case EXPORT_SMUGMUG_SCALE:
			case EXPORT_SMUGMUG_ROTATE:
			case EXPORT_SMUGMUG_BROWSER:
			case EXPORT_FOLDER_SCALE:
			case EXPORT_FOLDER_SHARPEN:
			case EXPORT_FOLDER_INCLUDE_TARBALLS:
				return true;
			
			case EXPORT_PICASAWEB_SIZE:
			case EXPORT_SMUGMUG_SIZE:
			case EXPORT_FOLDER_SIZE:
				return 800;
				
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
			case EXPORT_FOLDER_OPEN:
			case EXPORT_FOLDER_ROTATE:
			case VIEWER_INTERPOLATION:
				return true;
			case EXPORT_EMAIL_DELETE_TIMEOUT_SEC:
				return 30;	// delete temporary email pictures after 30 seconds
			case EXPORT_FOLDER_METHOD:
				return "static";
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
			object val = null;
			if (cache.TryGetValue (key, out val)) 
				return val;

			try {
				val = Client.Get (key);
			} catch (GConf.NoSuchKeyException) {
				val = GetDefault (key);

				if (val != null)
					Set (key, val);
			}

			cache.Add (key, val);
			return val;
		}

		public static void Set (string key, object value)
		{
			try {
				cache [key] = value;				
				Client.Set (key, value);
			} catch {
				Console.WriteLine ("Unable to write this gconf key :"+key);
			}
		}

		public static void SetAsBackground (string path)
		{
			Client.Set ("/desktop/gnome/background/color_shading_type", "solid");
			Client.Set ("/desktop/gnome/background/primary_color", "#000000");
			Client.Set ("/desktop/gnome/background/picture_options", "stretched");
			Client.Set ("/desktop/gnome/background/picture_opacity", 100);
			Client.Set ("/desktop/gnome/background/picture_filename", path);
			Client.Set ("/desktop/gnome/background/draw_background", true);
		}

		public static event GConf.NotifyEventHandler SettingChanged;

		static void OnSettingChanged (object sender, GConf.NotifyEventArgs args)
		{
			if (cache.ContainsKey (args.Key)) {
				cache [args.Key] = args.Value;				
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
