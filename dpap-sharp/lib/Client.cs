// Client.cs
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
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DPAP
{
	
	
	public class Client
	{
		
		private const int UpdateSleepInterval = 2 * 60 * 1000; // 2 minutes
        
        private IPAddress address;
        private UInt16 port;
        private ContentCodeBag bag;
        private ServerInfo server_info;
        private List<Database> databases = new List<Database> ();
        private ContentFetcher fetcher;
        private int revision;
        private bool update_running;
		private string service_name;
		
        public event EventHandler Updated;

        internal int Revision {
            get { return revision; }
        }

        public string Name {
            get { return service_name; }
        }

        public IPAddress Address {
            get { return address; }
        }

        public ushort Port {
            get { return port; }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return server_info.AuthenticationMethod; }
        }

        public IList<Database> Databases {
            get { return new ReadOnlyCollection<Database> (databases); }
        }

        internal ContentCodeBag Bag {
            get { return bag; }
        }

        internal ContentFetcher Fetcher {
            get { return fetcher; }
        }

        public Client (Service service) : this (service.Address, service.Port, service.Name) {
        }

        public Client (string host, UInt16 port) : this (Dns.GetHostEntry (host).AddressList [0], port, "") {
        }

        public Client (IPAddress address, UInt16 port, String name) {
            this.address = address;
            this.port = port;
			this.service_name = name;
            fetcher = new ContentFetcher (address, port);
			Login (null,null);
			
        }

        ~Client () {
            Dispose ();
        }

        public void Dispose () {
            update_running = false;
            
            if (fetcher != null) {
                fetcher.Dispose ();
                fetcher = null;
            }
        }

        private void ParseSessionId (ContentNode node) {
            fetcher.SessionId = (int) node.GetChild ("dmap.sessionid").Value;
        }

        public void Login () {
            Login (null, null);
        }

        public void Login (string password) {
            Login (null, password);
        }

        public void Login (string username, string password) {
            fetcher.Username = username;
            fetcher.Password = password;

            try {
                bag = ContentCodeBag.ParseCodes (fetcher.Fetch ("/content-codes"));
		
                ContentNode node = ContentParser.Parse (bag, fetcher.Fetch ("/login"));
                ParseSessionId (node);
		
                FetchDatabases ();
                Refresh ();
               // FIXME - the update handling mechanism is currently disabled 
               /* if (server_info.SupportsUpdate) {
                    update_running = true;
                    Thread thread = new Thread (UpdateLoop);
                    thread.IsBackground = true;
                    thread.Start ();
                }*/
            } catch (WebException e) {
                if (e.Response != null && (e.Response as HttpWebResponse).StatusCode == HttpStatusCode.Unauthorized)
                    throw new AuthenticationException ("Username or password incorrect");
                else
                    throw new LoginException ("Failed to login", e);
            } catch (Exception e) {
                throw new LoginException ("Failed to login", e);
            }
        }

        public void Logout () {
            try {
                update_running = false;
                fetcher.KillAll ();
                fetcher.Fetch ("/logout");
            } catch (WebException) {
                // some servers don't implement this, etc.
            }
            
            fetcher.SessionId = 0;
        }

        private void FetchDatabases () {
            ContentNode dbnode = ContentParser.Parse (bag, fetcher.Fetch ("/databases"));
			// DEBUG
			//dbnode.Dump ();
			
            foreach (ContentNode child in (ContentNode []) dbnode.Value) {
                if (child.Name != "dmap.listing")
                    continue;

                foreach (ContentNode item in (ContentNode []) child.Value) {
					// DEBUG
					//item.Dump ();
                    Database db = new Database (this, item);
					Console.WriteLine ("Adding database {0} with id={1} and album count={2}." , db.Name,db.Id,db.Albums.Count);
					//Console.WriteLine ("Photo " + db.Photos [0].FileName);
                    databases.Add (db);
                }
            }
        }

        private int GetCurrentRevision () {
            ContentNode rev_node = ContentParser.Parse (bag, fetcher.Fetch ("/update"), "dmap.serverrevision");
            return (int) rev_node.Value;
        }

        private int WaitForRevision (int currentRevision) {
            ContentNode rev_node = ContentParser.Parse (bag, fetcher.Fetch ("/update",
                                                                           "revision-number=" + currentRevision));

            return (int) rev_node.GetChild ("dmap.serverrevision").Value;
        }

        private void Refresh () {
            int newrev = 0;

           /* if (server_info.SupportsUpdate) {
                if (revision == 0)
                    newrev = GetCurrentRevision ();
                else
                    newrev = WaitForRevision (revision);
            
                if (newrev == revision)
                    return;
            }
             */   
            // Console.WriteLine ("Got new revision: " + newrev);
            foreach (Database db in databases) {
                db.Refresh (newrev);
            }
            
            revision = newrev;
            if (Updated != null)
                Updated (this, new EventArgs ());
        }

        private void UpdateLoop () {
            while (true) {
                try {
                    if (!update_running)
                        break;
                    
                    Refresh ();
                } catch (WebException) {
                    if (!update_running)
                        break;
                    
                    // chill out for a while, maybe the server went down
                    // temporarily or something.
                    Thread.Sleep (UpdateSleepInterval);
                } catch (Exception e) {
                    if (!update_running)
                        break;
                    
                    Console.Error.WriteLine ("Exception in update loop: " + e);
                    Thread.Sleep (UpdateSleepInterval);
                }
            }
        }
    }
}
