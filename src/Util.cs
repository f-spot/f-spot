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
		Gtk.StockManager.Lookup (l, ref item);

		if (item.StockId != null) {
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
}
