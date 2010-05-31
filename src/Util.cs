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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using Hyena;


namespace FSpot.Utils
{

	public class UriList : List<SafeUri> {
		public UriList (FSpot.IBrowsableItem [] photos) {
			foreach (FSpot.IBrowsableItem p in photos) {
				SafeUri uri;
				try {
					uri = p.DefaultVersion.Uri;
				} catch {
					continue;
				}
				Add (uri);
			}
		}
		
		public UriList () : base ()
		{
		}
		
		private void LoadFromStrings (string [] items) {
			//string [] items = System.Text.RegularExpressions.Regex.Split ("\n", data);
			
			foreach (String i in items) {
				if (!i.StartsWith ("#")) {
					SafeUri uri;
					String s = i;
	
					if (i.EndsWith ("\r")) {
						s = i.Substring (0, i.Length - 1);
						Log.DebugFormat ("uri = {0}", s);
					}
					
					try {
						uri = new SafeUri (s);
					} catch {
#if true //Workaround to bgo 362016 in gnome-screenshot. Remove this hack when gnome 2.6.18 is widely distributed.
						if (System.Text.RegularExpressions.Regex.IsMatch (s, "^file:/[^/]")) {
							try {
								s = "file:///" + s.Substring(6);
								uri = new SafeUri (s);
								Log.DebugFormat ("Converted uri from file:/ to >>{0}<<", s);
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

		public void AddUnknown (string unknown)
		{
			SafeUri uri;
			
			if (File.Exists (unknown) || Directory.Exists (unknown))
				uri = new SafeUri (unknown);
			else 
				uri = new SafeUri (unknown);
			
			Add (uri);
		}
	
		public UriList (string data) 
		{
			LoadFromStrings (data.Split ('\n'));
		}
		
		public UriList (string [] uris)
		{
			LoadFromStrings (uris);
		}
		
		/*public UriList (Gtk.SelectionData selection) 
		{
			// FIXME this should check the atom etc.
			LoadFromString (System.Text.Encoding.UTF8.GetString (selection.Data));
		}*/
	
		/*public void Add (string path)
		{
			AddUnknown (path);
		}*/
	
		public void Add (FSpot.IBrowsableItem item)
		{
			Add (item.DefaultVersion.Uri);
		}
	
		public override string ToString () {
			StringBuilder list = new StringBuilder ();
	
			foreach (SafeUri uri in this) {
				if (uri == null)
					break;
	
				list.Append (uri.ToString () + Environment.NewLine);
			}
	
			return list.ToString ();
		}
	
		public string [] ToLocalPaths () {
			int count = 0;
			foreach (SafeUri uri in this) {
				if (uri.IsFile)
					count++;
			}
			
			String [] paths = new String [count];
			count = 0;
			foreach (SafeUri uri in this) {
				if (uri.IsFile)
					paths[count++] = uri.LocalPath;
			}
			return paths;
		}
	}
}


