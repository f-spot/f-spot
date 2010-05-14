// DPAPService.cs : DPAP Server for f-spot
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
	

	public class DPAPService : IService
	{
		static ServiceDiscovery sd;
		public bool Start ()
		{
			Console.WriteLine ("Starting DPAP!");
			uint timer = Log.InformationTimerStart ("Starting DPAP");
			StartServer ();
			
			return true;
		}
		private void StartServer ()
		{
		Console.WriteLine ("Starting DPAP server");
			
			DPAP.Database database = new DPAP.Database ("DPAP");
			
			DPAP.Server server = new Server (System.Environment.UserName.ToString() + " f-spot photos");
			server.Port = 8770;
			server.AuthenticationMethod = AuthenticationMethod.None;
			int collision_count = 0;
			server.Collision += delegate {
				server.Name = System.Environment.UserName.ToString() + " f-spot photos" + "[" + ++collision_count + "]";
			};
            
			
			//FSpot.Photo photo = (FSpot.Photo) Core.Database.Photos.Get (1);			
			
			try {
				Album a = new Album ("test album");
				Tag t = Core.Database.Tags.GetTagByName ("Shared items");

				Tag []tags = {t};
				FSpot.Photo [] photos = Core.Database.Photos.Query (tags);
				int i=0;

				foreach (FSpot.Photo photo in photos) {
					string thumbnail_path = Gnome.Thumbnail.PathForUri (photo.DefaultVersionUri.ToString(), Gnome.ThumbnailSize.Large);
					FileInfo f = new FileInfo (thumbnail_path);
					DPAP.Photo p = new DPAP.Photo ();

					p.FileName = photo.Name;
					p.Thumbnail = thumbnail_path;
					p.ThumbSize = (int)f.Length;
					p.Path = photo.DefaultVersionUri.ToString ().Substring (7);
					f = new FileInfo (photo.DefaultVersionUri.ToString ().Substring (7));
					if (!f.Exists)
						continue;

					//if (++i > 2) break;
					Console.WriteLine ("Found photo " + p.Path  + ", thumb " + thumbnail_path);
					p.Title = f.Name;
					p.Size = (int)f.Length;
					p.Format = "JPEG";
					database.AddPhoto (p);
					a.AddPhoto (p);
				}		

				database.AddAlbum (a);
				Console.WriteLine ("Album count is now " + database.Albums.Count);
				server.AddDatabase (database);

				//server.GetServerInfoNode ();			
				try {
					server.Start();
				} catch (System.Net.Sockets.SocketException) {
					Console.WriteLine ("Server socket exception!");
					server.Port = 0;
					server.Start();
				}

				//DaapPlugin.ServerEnabledSchema.Set (true);

				//  if(!initial_db_committed) {
				server.Commit();

				//      initial_db_committed = true;
				//  }
			} catch (Exception e) {
				Console.WriteLine ("Failed starting dpap server \n{0}", e);
			}
		}
		
		public bool Stop ()
		{
			uint timer = Log.InformationTimerStart ("Stopping DPAP");
			if (sd != null) {
                sd.Stop ();
                sd.Found -= OnServiceFound;
                //locator.Removed -= OnServiceRemoved;
                sd = null;
            }
			return true;
		}

		private static void OnServiceFound (object o, ServiceArgs args)
		{
			Service service = args.Service;
			Client client;
//			ThreadAssist.Spawn (delegate {
        //        try {

			System.Console.WriteLine ("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
		    client = new Client (service);
	
			
			/*foreach (Database d in client.Databases){

				Console.WriteLine ("Database " + d.Name);
				
				foreach (Album alb in d.Albums)
					Console.WriteLine ("\tAlbum: "+alb.Name + ", id=" + alb.getId () + " number of items:" + alb.Photos.Count);
				
				Console.WriteLine (d.Photos [0].FileName);
				foreach (DPAP.Photo ph in d.Photos)
				{
					if (ph != null)
					{
						Console.WriteLine ("\t\tFile: " + ph.Title + " format = " + ph.Format + "size=" + ph.Width +"x" +ph.Height + " ID=" + ph.Id);
						d.DownloadPhoto (ph,"./"+ph.Title);
					}
				}
				
			}*/
			//client.Logout ();
		//	Console.WriteLine ("Press <enter> to exit...");
		}		
	}
}
