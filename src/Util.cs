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

using FSpot.Utils;

public class UriList : ArrayList {
	public UriList (FSpot.IBrowsableItem [] photos) {
		foreach (FSpot.IBrowsableItem p in photos) {
			Uri uri;
			try {
				uri = p.DefaultVersionUri;
			} catch {
				continue;
			}
			Add (uri);
		}
	}
	
	public UriList () : base ()
	{
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
#if true //Workaround to bgo 362016 in gnome-screenshot. Remove this hack when gnome 2.6.18 is widely distributed.
					if (System.Text.RegularExpressions.Regex.IsMatch (s, "^file:/[^/]")) {
						try {
							s = "file:///" + s.Substring(6);
							uri = new Uri (s);
							Console.WriteLine ("Converted uri from file:/ to >>{0}<<", s);
						} catch {
							continue;
						}
					} else
						continue;
#else					
					continue;
#endif
				}
				Add (uri);
			}
		}
	}

	public UriList (string [] uris)
	{	
		// FIXME this is so lame do real chacking at some point
		foreach (string str in uris) {
			AddUnknown (str);
		}
	}

	public void AddUnknown (string unknown)
	{
		Uri uri;
		
		if (File.Exists (unknown) || Directory.Exists (unknown))
			uri = UriUtils.PathToFileUri (unknown);
		else 
			uri = new Uri (unknown);
		
		Add (uri);
	}

	public UriList (string data) 
	{
		LoadFromString (data);
	}
	
	public UriList (Gtk.SelectionData selection) 
	{
		// FIXME this should check the atom etc.
		LoadFromString (System.Text.Encoding.UTF8.GetString (selection.Data));
	}

	public new Uri [] ToArray ()
	{
		return ToArray (typeof (Uri)) as Uri [];
	}
	
	public void Add (string path)
	{
		AddUnknown (path);
	}

	public void Add (Uri uri)
	{
		Add ((object)uri);
	}

	public void Add (FSpot.IBrowsableItem item)
	{
		Add (item.DefaultVersionUri);
	}

	public override string ToString () {
		StringBuilder list = new StringBuilder ();

		foreach (Uri uri in this) {
			if (uri == null)
				break;

			list.Append (uri.ToString () + Environment.NewLine);
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


public class SizeUtil {
	public static string ToHumanReadable (long size)
	{
		string tmp_str = String.Empty;
		float tmp_size = size;
		int k = 0;
		string[] size_abr = {"bytes", "kB", "MB", "GB", "TB" };
		
		while (tmp_size > 700) { //it's easier to read 0.9MB than 932kB
			tmp_size = tmp_size / 1024;
				k++;
		}
		
		if (tmp_size < 7)
			tmp_str = tmp_size.ToString ("0.##");
		else if (tmp_size < 70)
			tmp_str = tmp_size.ToString ("##.#");
		else
			tmp_str = tmp_size.ToString ("#,###");
		
		if (k < size_abr.Length)
			return tmp_str + " " + size_abr[k];
		else
				return size.ToString();
	}
}


