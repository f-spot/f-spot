// Main.cs
// dpap-sharp server test program
//
// Authors:
//   Andrzej Wytyczak-Partyka <iapart@gmail.com>
//
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
using System.IO;
using System.Collections.Generic;
using System.Collections;
using Mono.Unix;
using System.Text.RegularExpressions;
//using Gtk;

using DPAP;



namespace DPAP {
	
	class MainClass
	{
		
		public static void Main(string[] args)
		{
			//Regex dbPhotoRegex = new Regex ("/databases/([0-9]*?)/items?session-id=([0-9]*)&meta=.*&query=('dmap.itemid:([0-9]*)')");
			
//			string path = "/databases/1/items?session-id=9527&meta=dpap.thumb,dmap.itemid,dpap.filedata&query=('dmap.itemid:35')$";
			//string path = "'dmap.itemid:35'";
	//		Console.WriteLine("regex:"+rg.IsMatch(path));
			
			Console.WriteLine("Starting DPAP server");
			DPAP.Database database = new DPAP.Database("DPAP");
			DPAP.Server server = new Server("f-spot photos");
			server.Port = 8770;
			server.AuthenticationMethod = AuthenticationMethod.None;
			int collision_count = 0;
			server.Collision += delegate {
				server.Name = "f-spot photos" + " [" + ++collision_count + "]";
			};
            
			Photo p = new Photo();
			p.Thumbnail = "./test3-thumb.jpg";
			p.ThumbSize = 44786;
			p.FileName = "test3.jpg";
			p.Path = "./test3.jpg";
			p.Title = "test1";
			p.Format = "JPEG";
			p.Size = 1088386;
			database.AddPhoto(p);
			
			Photo p1 = new Photo();
			p1.Thumbnail = "./test2-thumb.jpg";
			p1.ThumbSize = 11357;
			p1.FileName = "test2.jpg";
			p1.Path = "./test2.jpg";
			p1.Title = "test2";
			p1.Format = "JPEG";
			p1.Size = 35209;
			database.AddPhoto(p1);
		
			Album a = new Album("test album");
			a.AddPhoto(p);		
			a.AddPhoto(p1);
			database.AddAlbum(a);
			Console.WriteLine("Album count is now " + database.Albums.Count);
			Console.WriteLine("Photo name is " + database.Photos[0].FileName);
			server.AddDatabase(database);
			
			//server.GetServerInfoNode();			
			try {
                server.Start();
            } catch (System.Net.Sockets.SocketException) {
				Console.WriteLine("Server socket exception!");
                server.Port = 0;
                server.Start();
            }
        
             //DaapPlugin.ServerEnabledSchema.Set(true);
            
          //  if(!initial_db_committed) {
                server.Commit();
          //      initial_db_committed = true;
          //  }
			

			Console.ReadLine();
		
			
		}
	}
}
