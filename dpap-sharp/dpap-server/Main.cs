// Main.cs
// dpap-sharp server test program
//
// Authors:
//   Andrzej Wytyczak-Partyka <iapart@gmail.com>
//   James Willcox <snorp@snorp.net>
//
// Copyright (C) 2008 Andrzej Wytyczak-Partyka
// Copyright (C) 2005  James Willcox <snorp@snorp.net>
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
			DPAP.Server server = new Server("apart photos");
			server.Port = 8770;
			server.AuthenticationMethod = AuthenticationMethod.None;
			int collision_count = 0;
			server.Collision += delegate {
				server.Name = "apart photos" + " [" + ++collision_count + "]";
			};
            
			Photo p = new Photo();
			p.FileName = "/home/andrzej/Obrazy/tesco_droid.jpg";
			p.Title = "test";
			p.Format = "JPEG";
			p.Size = 13946;
			database.AddPhoto(p);
			Album a = new Album("test album");
			a.AddPhoto(p);			
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
