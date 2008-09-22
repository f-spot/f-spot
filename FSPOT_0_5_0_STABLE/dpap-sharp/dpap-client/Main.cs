// Main.cs
// dpap-sharp client test program
// Author:
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
//using Gtk;

using DPAP;



namespace DPAP {
	
	class MainClass
	{
		
		public static void Main (string [] args)
		{
			
			ServiceDiscovery sd = new ServiceDiscovery ();
			sd.Found += OnServiceFound;			
			sd.Start ();
			
			
//			sd.Services [0];
			Console.ReadLine ();
			if (sd != null) {
                sd.Stop ();
                sd.Found -= OnServiceFound;
                //locator.Removed -= OnServiceRemoved;
                sd = null;
            }
			
		}
		private static void OnServiceFound (object o, ServiceArgs args)
		{
			Service service = args.Service;
			Client client;
//			ThreadAssist.Spawn (delegate {
        //        try {

			System.Console.WriteLine ("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
		    client = new Client (service);
	
			
			foreach (Database d in client.Databases){

				Console.WriteLine ("Database " + d.Name);
				
				foreach (Album alb in d.Albums)
					Console.WriteLine ("\tAlbum: "+alb.Name + ", id=" + alb.getId () + " number of items:" + alb.Photos.Count);
				Console.WriteLine (d.Photos [0].FileName);
				foreach (Photo ph in d.Photos)
				{
					if (ph != null)
					{
						Console.WriteLine ("\t\tFile: " + ph.Title + " format = " + ph.Format + "size=" + ph.Width +"x" +ph.Height + " ID=" + ph.Id);
						d.DownloadPhoto (ph,"./"+ph.Title);
					}
				}
				
			}
			//client.Logout ();
			//Console.WriteLine ("Press <enter> to exit...");

			
		}
	}
}
