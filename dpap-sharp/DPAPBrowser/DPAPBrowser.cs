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
			TreeViewColumn artistColumn = new Gtk.TreeViewColumn ();
			//artistColumn.Title = "Artist";
 
			Gtk.CellRendererText artistNameCell = new Gtk.CellRendererText ();
			artistNameCell.Visible = true;
			artistColumn.PackStart (artistNameCell,false);
			tree.AppendColumn (artistColumn);
			//tree.AppendColumn ("Icon", new Gtk.CellRendererPixbuf (), "pixbuf", 0);  

			
			list = new TreeStore (typeof (string));
			tree.Model = list;
			
			artistColumn.AddAttribute (artistNameCell, "text", 0);
			//list.AppendValues ("test");
		
			tree.Selection.Changed += OnSelectionChanged;
		//	tree.ShowNow ();
		//	ShowAll ();
			sd = new DPAP.ServiceDiscovery ();
			sd.Found += OnServiceFound;		
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
				
				Directory.CreateDirectory ("/tmp/" + client.Databases [0].Name + "/" + alb.Name);
				foreach (DPAP.Photo ph in alb.Photos)
				{
					if (ph != null)
					{
					//	Console.WriteLine ("\t\tFile: " + ph.Title + " format = " + ph.Format + "size=" + ph.Width +"x" +ph.Height + " ID=" + ph.Id);
						d.DownloadPhoto (ph,"/tmp/" + client.Databases [0].Name + "/" + alb.Name + "/" + ph.FileName);
						//FSpot.JpegFile = new JpegFile ("file:///tmp/" + client.Databases [0].Name + "/" + ph.FileName);
					}
				}
				FSpot.Core.FindInstance ().View ("file:///tmp/" + client.Databases [0].Name + "/" + alb.Name);
				break;
			}
			
			
		}
		
		private void Connect (string svcName)
		{
			Service service = sd.ServiceByName (svcName);
			System.Console.WriteLine ("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
	
			client = new Client (service);
			TreeIter iter;
			//list.GetIterFromString (out iter, svcName);
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
			if (service.Name.Equals ("f-spot photos")) return;
			list.AppendValues (service.Name);
			
/*			System.Console.WriteLine ("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
		    
			//client.Logout ();
			//Console.WriteLine ("Press <enter> to exit...");
*/
			
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
