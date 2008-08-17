// DPAPBrowser.cs
//
// Author:
// Andrzej Wytyczak-Partyka <iapart@gmail.com>
// Copyright (C) 2008 Andrzej Wytyczak-Partyka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
//
//

using System;
using FSpot;
using FSpot.Extensions;
using FSpot.Utils;
using FSpot.Widgets;

using System.IO;
using DPAP;
using Gtk;

namespace DPAP {
	
	public class DPAPPageWidget : ScrolledWindow {
		TreeView tree;
		TreeStore list;
		ServiceDiscovery sd;
		Client client;
		
		public DPAPPageWidget ()
		{
			Console.WriteLine ("DPAP Page widget ctor!");
			tree = new TreeView ();
			Add (tree);
			TreeViewColumn albumColumn = new Gtk.TreeViewColumn ();
			//albumColumn.Title = "album";
 
			Gtk.CellRendererText albumNameCell = new Gtk.CellRendererText ();
			albumNameCell.Visible = true;
			albumColumn.PackStart (albumNameCell,false);
			tree.AppendColumn (albumColumn);

			list = new TreeStore (typeof (string));
			tree.Model = list;
			
			albumColumn.AddAttribute (albumNameCell, "text", 0);
		
			tree.Selection.Changed += OnSelectionChanged;
			sd = new DPAP.ServiceDiscovery ();
			sd.Found += OnServiceFound;
			sd.Removed += OnServiceRemoved;
			sd.Start ();	
		}
		
		private void OnSelectionChanged (object o, EventArgs args)
		{
			Gtk.TreeSelection selection =  (Gtk.TreeSelection) o;
			Gtk.TreeModel model;
			Gtk.TreeIter iter;
			string data;
			if (selection.GetSelected (out model, out iter)) {
				GLib.Value val = GLib.Value.Empty;
                model.GetValue (iter, 0, ref val);
                data = (string) val.Val;
				
				if (list.IterDepth (iter) == 0)
					Connect (data);
				else
					ViewAlbum (data);
                val.Dispose ();
			}
			
		}
		
		private void ViewAlbum (string name)
		{
			Console.WriteLine ("View Album !");
			Database d = client.Databases [0];
			
			Directory.CreateDirectory ("/tmp/" + client.Databases [0].Name);
			//Console.WriteLine ("Looking for album '" + name + "'");
			foreach (DPAP.Album alb in d.Albums)
			{
				//Console.WriteLine ("\t -- album '" + alb.Name + "'");
				if (!alb.Name.Equals (name)) 
					continue;
				
				Directory.CreateDirectory (System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal) + "/.cache/DPAP/" + client.Name + "/" + alb.Name);
				foreach (DPAP.Photo ph in alb.Photos)
					if (ph != null)
						d.DownloadPhoto (ph, System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal) + "/.cache/DPAP/" + client.Name + "/" + alb.Name + "/" + ph.FileName);
					
				FSpot.Core.FindInstance ().View ( System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal) + "/.cache/DPAP/" + client.Name + "/" + alb.Name);
				break;
			}
			
			
		}
		
		private void Connect (string svcName)
		{
			Service service = sd.ServiceByName (svcName);
			System.Console.WriteLine ("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
	
			client = new Client (service);
			TreeIter iter;
			list.GetIterFirst (out iter);
			foreach (Database d in client.Databases){
				
			//	list.AppendValues (iter,d.Name);
				Console.WriteLine ("Database " + d.Name);
				
				foreach (Album alb in d.Albums)
						list.AppendValues (iter, alb.Name);
				
			// Console.WriteLine ("\tAlbum: "+alb.Name + ", id=" + alb.getId () + " number of items:" + alb.Photos.Count);
			// Console.WriteLine (d.Photos [0].FileName);
								
			}
		}
		
		private void OnServiceFound (object o, ServiceArgs args)
		{
			Service service = args.Service;
			Console.WriteLine ("ServiceFound " + service.Name);
			if (service.Name.Equals (System.Environment.UserName + " f-spot photos")) return;
			list.AppendValues (service.Name);
			
/*			System.Console.WriteLine ("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
		    
			//client.Logout ();
			//Console.WriteLine ("Press <enter> to exit...");
*/
			
		}
		
		private void OnServiceRemoved (object o, ServiceArgs args)
		{
			Service service = args.Service;
			Console.WriteLine ("Service removed " + service.Name);
			TreeIter root = TreeIter.Zero;
			TreeIter iter = TreeIter.Zero;

			bool valid = tree.Model.GetIterFirst (out root);

			while (valid) {
				if(((String)tree.Model.GetValue(root,0)).Equals(service.Name))
					(tree.Model as TreeStore).Remove(ref root);
				valid = tree.Model.IterNext (ref root);
			}
			if (Directory.Exists (System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal) + "/.cache/DPAP/" + service.Name))
				Directory.Delete (System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal) + "/.cache/DPAP/" + service.Name, true);
			
		}
	}
	
	public class DPAPBrowser : SidebarPage
	{
		//public DPAPPage () { }
		private static DPAPPageWidget widget;
		public DPAPBrowser () : base (new DPAPPageWidget (), "Shared items", "gtk-new") 
		{
			Console.WriteLine ("Starting DPAP client...");		
			widget = (DPAPPageWidget)SidebarWidget;
		}
		
				
	}
	
}
