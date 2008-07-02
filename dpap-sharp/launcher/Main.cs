// Main.cs created with MonoDevelop
// User: andrzej at 10:26 2008-06-01
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
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
		
		public static void Main(string[] args)
		{
			
			ServiceDiscovery sd = new ServiceDiscovery();
			sd.Found += OnServiceFound;			
			sd.Start();

			
//			sd.Services[0];
			Console.ReadLine();
		}
		private static void OnServiceFound(object o, ServiceArgs args)
		{
			Service service = args.Service;
			Client client;
//			ThreadAssist.Spawn(delegate {
        //        try {

			System.Console.WriteLine("Connecting to {0} at {1}:{2}", service.Name, service.Address, service.Port);
		    client = new Client( service );
			foreach (Database d in client.Databases){

				Console.WriteLine("Database " + d.Name);
				
				foreach (Playlist pl in d.Playlists)
					Console.WriteLine("\tAlbum: "+pl.Name + ", id=" + pl.getId() + " number of items:" + pl.Tracks.Count);
				
				foreach (Track tr in d.Tracks) 
					if(tr != null)
						Console.WriteLine("\t\tFile: " + tr.Title);
				
			}
			client.Logout();
			Console.WriteLine("Press <enter> to exit...");
			//Console.ReadLine();
	//		});
        /*            //client.Updated += OnClientUpdated;
                    if(client.AuthenticationMethod == AuthenticationMethod.None) {
                        client.Login();

			}                        
			*/
                    //} else {
                    //    ThreadAssist.ProxyToMain(PromptLogin);
                   // }
        /*        } catch(Exception e) {
                    ThreadAssist.ProxyToMain(delegate {
                        DaapErrorView error_view = new DaapErrorView(this, DaapErrorType.BrokenAuthentication);
                        while(box.Children.Length > 0) {
                            box.Remove(box.Children[0]);
                        }
                        box.PackStart(error_view, true, true, 0);
                        error_view.Show();
                    });
                }*/
               
         //       is_activating = false;
         //   });			
			
		}
	}
}
