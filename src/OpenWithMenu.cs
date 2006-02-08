using System;
using System.Collections;
using System.Runtime.InteropServices;
using Gtk;
using Gdk;
using Gnome.Vfs;

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
			foreach (Widget child in Children)
				child.Destroy ();
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

		ShowAll ();

		populated = true;
	}

	public static OpenWithMenu AppendMenuTo (Gtk.Menu menu, MimeFetcher mime_fetcher)
	{
		Gtk.MenuItem open_with = new Gtk.MenuItem (Mono.Posix.Catalog.GetString ("Open With"));

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
			MimeApplication [] apps = Gnome.Vfs.Mime.GetAllApplications (mime_type);

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

			if (! first)
				foreach (MimeApplication app in intersection)
					if (System.Array.IndexOf (apps, app) == -1)
						intersection.Remove (app);

			first = false;
		}
	}
	
	private void HandleItemActivated (object sender, EventArgs args)
	{
		if (ApplicationActivated != null)
			ApplicationActivated ((sender as AppMenuItem).App);
	}
	
	private class AppMenuItem : ImageMenuItem {
		public MimeApplication App;

		public AppMenuItem (OpenWithMenu menu, MimeApplication mime_application) : base (mime_application.Name)
		{
			App = mime_application;
			
			if (menu.ShowIcons) {
				//System.Console.WriteLine ("icon = {0}", mime_application.Icon);
				
				// FIXME this is stupid, the mime_application.Icon is sometimes just a file name
				// and sometimes a full path.
				//int w, h;
				//w = h = (int) IconSize.Menu;
				//string icon = mime_application.Icon;
				//Console.WriteLine ("w/h = {0}", w);

				//Pixbuf img = new Pixbuf (icon, w, h);
				//Image = new Gtk.Image (mime_application.Icon);

				/*if (Image == null)
					Image = new Gtk.Image ("/usr/share/pixmaps/" + mime_application.Icon);
				
				if (Image == null)
					Image = new Gtk.Image ("/usr/share/icons/gnome/24x24/apps/" + mime_application.Icon);

				if (Image != null)
					(Image as Gtk.Image).IconSize = Gtk.IconSize.Menu;*/
			}
		}
	}
}
