using System;
using System.Collections;
using System.Runtime.InteropServices;
using Gtk;
using Gdk;
using Gnome.Vfs;
using Mono.Unix;

public class OpenWithMenu: Gtk.Menu {
	public delegate void OpenWithHandler (MimeApplication app);
	public event OpenWithHandler ApplicationActivated;

	public delegate string [] MimeFetcher ();
	private MimeFetcher mime_fetcher;

	private string [] mime_types;
	private bool populated = false;
	
	private string ignore_app = null;
	public string IgnoreApp {
		get { return ignore_app; }
		set { ignore_app = value; }
	}

	private bool show_icons = false;
	public bool ShowIcons {
		get { return show_icons; }
		set { show_icons = value; }
	}

	private bool hide_invalid = true;
	public bool HideInvalid {
		get { return hide_invalid; }
		set { hide_invalid = value; }
	}

	static OpenWithMenu () {
		Gnome.Vfs.Vfs.Initialize ();
	}

	public OpenWithMenu (MimeFetcher mime_fetcher)
	{
		this.mime_fetcher = mime_fetcher;
	}
	
	public void Populate (object sender, EventArgs args)
	{
		string [] mime_types = mime_fetcher ();

		//foreach (string mime in mime_types)
		//	System.Console.WriteLine ("Populating open with menu for {0}", mime);
		
		if (this.mime_types != mime_types && populated) {
			populated = false;

			Widget [] dead_pool = Children;
			for (int i = 0; i < dead_pool.Length; i++)
				dead_pool [i].Destroy ();
		}

		if (populated)
			return;

		ArrayList union, intersection;
		ApplicationsFor (this, mime_types, out union, out intersection);

		ArrayList list = (HideInvalid) ? intersection : union;

		foreach (MimeApplication app in list) {
			//System.Console.WriteLine ("Adding app {0} to open with menu (binary name = {1}", app.Name, app.BinaryName);
			//System.Console.WriteLine ("Desktop file path: {0}, id : {1}", app.DesktopFilePath);
			AppMenuItem i = new AppMenuItem (this, app);
			i.Activated += HandleItemActivated;
			// Make it not sensitive it we're showing everything
			i.Sensitive = (HideInvalid || intersection.Contains (app));
			Append (i);
		}
		
		if (Children.Length == 0) {
			MenuItem none = new Gtk.MenuItem (Catalog.GetString ("No applications available"));
			none.Sensitive = false;
			Append (none);
		}

		ShowAll ();

		populated = true;
	}

	public static OpenWithMenu AppendMenuTo (Gtk.Menu menu, MimeFetcher mime_fetcher)
    {
        return AppendMenuTo (menu, mime_fetcher, false);
    }

    private static string menu_text = Catalog.GetString ("Open With");
	public static OpenWithMenu AppendMenuTo (Gtk.Menu menu, MimeFetcher mime_fetcher, bool with_icon)
	{
		Gtk.MenuItem open_with;
        
        if (with_icon) {
            Gtk.ImageMenuItem img_item = new Gtk.ImageMenuItem (menu_text);
            img_item.Image = new Gtk.Image ("gtk-open", Gtk.IconSize.Menu);
            open_with = img_item;
        } else {
            open_with = new Gtk.MenuItem (menu_text);
        }

		OpenWithMenu app_menu = new OpenWithMenu (mime_fetcher);
		open_with.Submenu = app_menu;
		open_with.ShowAll ();
		open_with.Activated += app_menu.Populate;
		menu.Append (open_with);

		return app_menu;
	}

	private static void ApplicationsFor (OpenWithMenu menu, string [] mime_types, out ArrayList union, out ArrayList intersection)
	{
		//Console.WriteLine ("Getting applications");
		union = new ArrayList ();
		intersection = new ArrayList ();
		
		if (mime_types == null || mime_types.Length < 1)
			return;

		bool first = true;
		foreach (string mime_type in mime_types) {
			if (mime_type == null)
				continue;

			MimeApplication [] apps = Gnome.Vfs.Mime.GetAllApplications (mime_type);
			for (int i = 0; i < apps.Length; i++) {
				apps [i] = apps [i].Copy ();
			}

			foreach (MimeApplication app in apps) {
				// Skip apps that don't take URIs
				if (! app.SupportsUris ())
					continue;
				
				// Skip apps that we were told to ignore
				if (menu.IgnoreApp != null)
					if (app.BinaryName.IndexOf (menu.IgnoreApp) != -1)
						continue;

				if (! union.Contains (app))
					union.Add (app);
				
				if (first)
					intersection.Add (app);
			}

			if (! first) {
				for (int i = 0; i < intersection.Count; i++) {
					MimeApplication app = intersection [i] as MimeApplication;
					if (System.Array.IndexOf (apps, app) == -1) {
						intersection.Remove (app);
						i--;
					}
				}
			}

			first = false;
		}
	}
	
	private void HandleItemActivated (object sender, EventArgs args)
	{
		AppMenuItem app = (sender as AppMenuItem);

		if (ApplicationActivated != null)
			ApplicationActivated (app.App);
	}
	
	private class AppMenuItem : ImageMenuItem {
		public MimeApplication App;

		public AppMenuItem (OpenWithMenu menu, MimeApplication mime_application) : base (mime_application.Name)
		{
			App = mime_application;
			
			if (menu.ShowIcons) {
				if (mime_application.Icon != null) {
					Gdk.Pixbuf pixbuf = null; 

					try {
						if (mime_application.Icon.StartsWith ("/"))
							pixbuf = new Gdk.Pixbuf (mime_application.Icon, 16, 16);
						else 
							pixbuf = IconTheme.Default.LoadIcon (mime_application.Icon,
											     16, (IconLookupFlags)0);
					} catch (System.Exception) {
						pixbuf = null;
					}

					if (pixbuf != null)
						Image = new Gtk.Image (pixbuf);
				}
			}
		}
	}
}
