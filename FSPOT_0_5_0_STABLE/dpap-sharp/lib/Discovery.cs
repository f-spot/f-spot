// Discovery.cs
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
using System.Net;
using System.Text;
using System.Collections;
using Mono.Zeroconf;

namespace DPAP {
	
    public delegate void ServiceHandler (object o, ServiceArgs args);	

    public class ServiceArgs : EventArgs {

        private Service service;
        
        public Service Service {
            get { return service; }
        }
        
        public ServiceArgs (Service service) {
            this.service = service;
        }
    }
	
	
	public class Service {
        private IPAddress address;
        private ushort port;
        private string name;
        private bool isprotected;
        private string machine_id;

        public IPAddress Address {
            get { return address; }
        }

        public ushort Port {
            get { return port; }
        }

        public string Name {
            get { return name; }
        }

        public bool IsProtected {
            get { return isprotected; }
        }

        public string MachineId {
            get { return machine_id; }
        }

        public Service (IPAddress address, ushort port, string name, bool isprotected, string machine_id) {
            this.address = address;
            this.port = port;
            this.name = name;
            this.isprotected = isprotected;
            this.machine_id = machine_id;
        }

        public override string ToString ()
        {
            return String.Format ("{0}:{1} ({2})", Address, Port, Name);
        }
    }
	
	public class ServiceDiscovery{
		
		private ServiceBrowser browser;
		private Hashtable services = new Hashtable ();

		public event ServiceHandler Found;
        public event ServiceHandler Removed;		
		
		public IEnumerable Services {
            get { return services; }
        }		
		
		//
		// Configure the code that will be called back when the information
		// becomes available
		//
		
		public void Start () {
			browser = new ServiceBrowser ();

			browser.ServiceAdded += OnServiceAdded;
			browser.ServiceRemoved += OnServiceRemoved;
			
			browser.Browse ("_dpap._tcp","local");
		}
		
	    public void Stop () {
            browser.Dispose ();
            browser = null;
            services.Clear ();
        }
		
		private void OnServiceAdded (object o, ServiceBrowseEventArgs args){
			Console.WriteLine ("Found Service: {0}", args.Service.Name);
			args.Service.Resolved += OnServiceResolved;
            args.Service.Resolve ();
		}
		
		public Service ServiceByName (string svcName)
		{
			return (Service)services [svcName];
		}
		
		private void OnServiceResolved (object o, ServiceResolvedEventArgs args){
				
		        IResolvableService s = (IResolvableService)args.Service;

			    string name = s.Name;
		        string machine_id = null;
		        bool pwRequired = false;
			
		        Console.WriteLine ("Resolved Service: {0} - {1}:{2} ({3} TXT record entries)", 
		            s.FullName, s.HostEntry.AddressList [0], s.Port, s.TxtRecord.Count);

				if (name.EndsWith ("_PW")) {
					name = name.Substring (0, name.Length - 3);
					pwRequired = true;
				}				
				
				foreach (TxtRecordItem item in s.TxtRecord) {
	                if (item.Key.ToLower () == "password") {
	                    pwRequired = item.ValueString.ToLower () == "true";
	                } else if (item.Key.ToLower () == "machine name") {
	                    name = item.ValueString;
	                } else if (item.Key.ToLower () == "machine id") {
	                    machine_id = item.ValueString;
	                }
				}
				
		            DPAP.Service svc = new DPAP.Service (s.HostEntry.AddressList [0], (ushort)s.Port, 
                                                name, pwRequired, machine_id);
            
            services [svc.Name] = svc;
			
			if (Found != null)
                Found (this, new ServiceArgs (svc));
		}
		
		private void OnServiceRemoved (object o, ServiceBrowseEventArgs args){
			
			IResolvableService s = args.Service;
			
			Removed (this, new ServiceArgs (new DPAP.Service (null, 0, s.Name, false, null)));
			
		}
	}
}