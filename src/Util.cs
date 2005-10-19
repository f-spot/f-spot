//
// Utility functions.
//
// Miguel de Icaza (miguel@ximian.com).
//
// (C) 2002 Ximian, Inc.
//
//

using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.IO;
using System.Text;
using System;

class Semaphore {
	int count = 0;
	
	public Semaphore ()
	{ }

	public void Down ()
	{
		lock (this){
			while (count <= 0){
				Monitor.Wait (this, Timeout.Infinite);
			}
			count--;
		}
	}

	public void Up ()
	{
		lock (this){
			count++;
			Monitor.Pulse (this);
		}
	}
}

class Timer : IDisposable {
	System.DateTime start;
	string label;

	public Timer (string label) {
		this.label = label;
		start = System.DateTime.Now;
	}

	public System.TimeSpan ElapsedTime {
		get {
			return System.DateTime.Now - start;
		}
	}

	public void WriteElapsed (string message)
	{
		System.Console.WriteLine ("{0} {1} {2}", label, message, ElapsedTime);
	}

	public void Dispose ()
	{
		WriteElapsed ("timer stopped:");
	}
}

public class UriList : ArrayList {
	public UriList (Photo [] photos) {
		foreach (Photo p in photos) {
			Uri uri;
			try {
				uri = p.DefaultVersionUri;
			} catch {
				continue;
			}
			Add (uri);
		}
	}


	private void LoadFromString (string data) {
		//string [] items = System.Text.RegularExpressions.Regex.Split ("\n", data);
		string [] items = data.Split ('\n');
		
		foreach (String i in items) {
			if (!i.StartsWith ("#")) {
				Uri uri;
				String s = i;

				if (i.EndsWith ("\r")) {
					s = i.Substring (0, i.Length - 1);
					Console.WriteLine ("uri = {0}", s);
				}
				
				try {
					uri = new Uri (s);
				} catch {
					continue;
				}
				Add (uri);
			}
		}
	}

	static char[] CharsToQuote = { ';', '?', ':', '@', '&', '=', '$', ',', '#' };

	public static Uri PathToFileUri (string path)
	{
		path = Path.GetFullPath (path);

		StringBuilder builder = new StringBuilder ();
		builder.Append (Uri.UriSchemeFile);
		builder.Append (Uri.SchemeDelimiter);

		int i;
		while ((i = path.IndexOfAny (CharsToQuote)) != -1) {
			if (i > 0)
				builder.Append (path.Substring (0, i));
			builder.Append (Uri.HexEscape (path [i]));
			path = path.Substring (i+1);
		}
		builder.Append (path);

		return new Uri (builder.ToString (), true);
	}

	public UriList (string [] uris)
	{	
		// FIXME this is so lame do real chacking at some point
		foreach (string str in uris) {
			Uri uri;

			if (File.Exists (str) || Directory.Exists (str))
				uri = PathToFileUri (str);
			else 
				uri = new Uri (str);
			
			Add (uri);
		}
	}

	public UriList (string data) {
		LoadFromString (data);
	}
	
	public UriList (Gtk.SelectionData selection) 
	{
		// FIXME this should check the atom etc.
		LoadFromString (System.Text.Encoding.UTF8.GetString (selection.Data));
	}

	public override string ToString () {
		StringBuilder list = new StringBuilder ();

		foreach (Uri uri in this) {
			if (uri == null)
				break;

			list.Append (uri.ToString () + "\r\n");
		}

		return list.ToString ();
	}

	public string [] ToLocalPaths () {
		int count = 0;
		foreach (Uri uri in this) {
			if (uri.IsFile)
				count++;
		}
		
		String [] paths = new String [count];
		count = 0;
		foreach (Uri uri in this) {
			if (uri.IsFile)
				paths[count++] = uri.LocalPath;
		}
		return paths;
	}
}

class GtkUtil {
	public static void MakeMenuItem (Gtk.Menu menu, string l, EventHandler e)
	{
		MakeMenuItem (menu, l, e, true);
	}
	
	public static void MakeMenuItem (Gtk.Menu menu, string l, EventHandler e, bool enabled)
	{
		Gtk.MenuItem i;
		Gtk.StockItem item = Gtk.StockItem.Zero;

		if (Gtk.StockManager.Lookup (l, ref item)) {
			i = new Gtk.ImageMenuItem (l, new Gtk.AccelGroup ());
		} else {
			i = new Gtk.MenuItem (l);
		}
		i.Activated += e;
                i.Sensitive = enabled;
		
		menu.Append (i);
		i.Show ();
	}
	
	public static void MakeMenuItem (Gtk.Menu menu, string label, string image_name, EventHandler e, bool enabled)
	{
		Gtk.ImageMenuItem i = new Gtk.ImageMenuItem (label);
		i.Activated += e;
                i.Sensitive = enabled;
		i.Image = new Gtk.Image (image_name, Gtk.IconSize.Menu);
		
		menu.Append (i);
		i.Show ();
	}

	public static void MakeMenuSeparator (Gtk.Menu menu)
	{
		Gtk.SeparatorMenuItem i = new Gtk.SeparatorMenuItem ();
		menu.Append (i);
		i.Show ();
	}
	
	private class SignalFuncHelper {
		public SignalFuncHelper (System.EventHandler e)
		{
			this.e = e;
		}
		
		public void Func ()
		{
			this.e (Sender, System.EventArgs.Empty);
		}

		System.EventHandler e;
		public object Sender;
	}

	public static Gtk.Widget MakeToolbarButton (Gtk.Toolbar toolbar, string stock_id, System.EventHandler e)
	{
		Gtk.StockItem item = Gtk.StockItem.Zero;
		if (Gtk.StockManager.Lookup (stock_id, ref item)) {
			SignalFuncHelper helper = new SignalFuncHelper (e);
			Gtk.Widget w =  toolbar.AppendItem (item.Label.Replace ("_", null),
							    null, null, 
							    new Gtk.Image (item.StockId, Gtk.IconSize.LargeToolbar), 
							    new Gtk.SignalFunc (helper.Func));
			helper.Sender = w;
			return w;
		}
		return null;
	}
	
	public static Gtk.Widget MakeToolbarToggleButton (Gtk.Toolbar toolbar, string stock_id, System.EventHandler e)
	{
		Gtk.StockItem item = Gtk.StockItem.Zero;
		if (Gtk.StockManager.Lookup (stock_id, ref item)) {
			SignalFuncHelper helper = new SignalFuncHelper (e);

			// FIXME current gtk-sharp bindings don't have a null_ok flag on the 
			// widget parameter so it is impossible to make a toggle button in toolbar.
			Gtk.Widget w;
			try {
				w =  toolbar.AppendElement (Gtk.ToolbarChildType.Togglebutton, 
							    null,
							    item.Label.Replace ("_", null),
							    null, null, 
							    new Gtk.Image (item.StockId, Gtk.IconSize.LargeToolbar), 
							    new Gtk.SignalFunc (helper.Func));
			} catch {
				w =  toolbar.AppendItem (item.Label.Replace ("_", null),
							 null, null, 
							 new Gtk.Image (item.StockId, Gtk.IconSize.LargeToolbar), 
							 new Gtk.SignalFunc (helper.Func));
			}

			helper.Sender = w;
			return w;
		}
		return null;
	}

	
			
}

class GnomeUtil {
	public static void UrlShow (Gtk.Window owner_window, string url)
	{
		try {
			Gnome.Url.Show (url);
		} catch (Exception ge) {
			System.Console.WriteLine (ge.ToString ());
			HigMessageDialog md = new HigMessageDialog (owner_window, Gtk.DialogFlags.DestroyWithParent, 
				Gtk.MessageType.Error, Gtk.ButtonsType.Ok, 
				Mono.Posix.Catalog.GetString ("There was an error invoking the external handler"),
				String.Format (Mono.Posix.Catalog.GetString ("Received error:\n\"{0}\"\n"), 
				ge.Message));

			md.Run ();
			md.Destroy ();
		}
		
	}
}
