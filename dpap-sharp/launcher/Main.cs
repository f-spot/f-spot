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
			Console.WriteLine("Starting DPAP server");
			DPAP.Database database = new DPAP.Database("DPAP");
			DPAP.Server server = new Server("DPAP");
			server.AuthenticationMethod = AuthenticationMethod.None;
			int collision_count = 0;
			server.Collision += delegate {
				server.Name = "DPAP" + " [" + ++collision_count + "]";
			};
            
			server.AddDatabase(database);
			//server.GetServerInfoNode();			
			try {
                server.Start();
            } catch (System.Net.Sockets.SocketException) {
                server.Port = 0;
                server.Start();
            }
        
             //DaapPlugin.ServerEnabledSchema.Set(true);
            
          //  if(!initial_db_committed) {
                server.Commit();
          //      initial_db_committed = true;
          //  }
			
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
	return;		
			foreach (Database d in client.Databases){

				Console.WriteLine("Database " + d.Name);
				
				foreach (Album alb in d.Albums)
					Console.WriteLine("\tAlbum: "+alb.Name + ", id=" + alb.getId() + " number of items:" + alb.Photos.Count);
				
				foreach (Photo ph in d.Photos)
				{
					if(ph != null)
					{
						Console.WriteLine("\t\tFile: " + ph.FileName + " format = " + ph.Format + "size=" + ph.Width +"x" +ph.Height + " ID=" + ph.Id);
						d.DownloadPhoto(ph,"./"+ph.FileName);
					}
				}
				
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
