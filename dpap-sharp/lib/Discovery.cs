// Discovery.cs created with MonoDevelop
// User: andrzej at 09:58 2008-06-01
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
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
        private string machineId;

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
            get { return machineId; }
        }

        public Service (IPAddress address, ushort port, string name, bool isprotected, string machineId) {
            this.address = address;
            this.port = port;
            this.name = name;
            this.isprotected = isprotected;
            this.machineId = machineId;
        }

        public override string ToString()
        {
            return String.Format("{0}:{1} ({2})", Address, Port, Name);
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
		
		public void Start(){
			browser = new ServiceBrowser();
			browser.ServiceAdded += OnServiceAdded;
			browser.Browse("_dpap._tcp","local");
		}
		
		private void OnServiceAdded(object o, ServiceBrowseEventArgs args){
			Console.WriteLine("Found Service: {0}", args.Service.Name);
			args.Service.Resolved += OnServiceResolved;
            args.Service.Resolve ();
		}
		
		private void OnServiceResolved(object o, ServiceResolvedEventArgs args){
				
		        IResolvableService s = (IResolvableService)args.Service;

			    string name = s.Name;
		        string machineId = null;
		        bool pwRequired = false;
			
		        Console.WriteLine("Resolved Service: {0} - {1}:{2} ({3} TXT record entries)", 
		            s.FullName, s.HostEntry.AddressList[0], s.Port, s.TxtRecord.Count);

				if (name.EndsWith ("_PW")) {
					name = name.Substring (0, name.Length - 3);
					pwRequired = true;
				}				
				
				foreach(TxtRecordItem item in s.TxtRecord) {
	                if(item.Key.ToLower () == "password") {
	                    pwRequired = item.ValueString.ToLower () == "true";
	                } else if (item.Key.ToLower () == "machine name") {
	                    name = item.ValueString;
	                } else if (item.Key.ToLower () == "machine id") {
	                    machineId = item.ValueString;
	                }
				}
				
		            DPAP.Service svc = new DPAP.Service (s.HostEntry.AddressList[0], (ushort)s.Port, 
                                                name, pwRequired, machineId);
            
            services[svc.Name] = svc;
			
			if (Found != null)
                Found (this, new ServiceArgs (svc));
		}		    
	}
}