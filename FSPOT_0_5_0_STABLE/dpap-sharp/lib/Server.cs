// Server.cs
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
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Web;

using Mono.Zeroconf;

namespace DPAP {

    internal delegate bool WebHandler (Socket client, string user, string path, NameValueCollection query, int range);

    internal class WebServer {

        private const int ChunkLength = 8192;

        private UInt16 port;
        private Socket server;
        private WebHandler handler;
        private bool running;
        private List<NetworkCredential> creds = new List<NetworkCredential> ();
        private ArrayList clients = new ArrayList ();
        private string realm;
        private AuthenticationMethod auth_method = AuthenticationMethod.None;
        
        public ushort RequestedPort {
            get { return port; }
            set { port = value; }
        }

        public ushort BoundPort {
            get { return (ushort) (server.LocalEndPoint as IPEndPoint).Port; }
        }

        public IList<NetworkCredential> Credentials {
            get { return new ReadOnlyCollection<NetworkCredential> (creds); }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return auth_method; }
            set { auth_method = value; }
        }

        public string Realm {
            get { return realm; }
            set { realm = value; }
        }
        
        public WebServer (UInt16 port, WebHandler handler) {
            this.port = port;
            this.handler = handler;
        }

        public void Start () {
            server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind (new IPEndPoint (IPAddress.Any, port));
            server.Listen (10);

            running = true;
            Thread thread = new Thread (ServerLoop);
            thread.IsBackground = true;
            thread.Start ();
        }

        public void Stop () {
            running = false;
            
            if (server != null) {
                server.Close ();
                server = null;
            }

            foreach (Socket client in (ArrayList) clients.Clone ()) {
                // do not pass go, do not collect $200...
                client.Close ();
            }
        }

        public void AddCredential (NetworkCredential cred) {
            creds.Add (cred);
        }

        public void RemoveCredential (NetworkCredential cred) {
            creds.Remove (cred);
        }
        
        public void WriteResponse (Socket client, ContentNode node) {
            WriteResponse (client, HttpStatusCode.OK,
                           ContentWriter.Write (ContentCodeBag.Default, node));
        }

        public void WriteResponse (Socket client, HttpStatusCode code, string body) {
            WriteResponse (client, code, Encoding.UTF8.GetBytes (body));
        }
        
        public void WriteResponse (Socket client, HttpStatusCode code, byte [] body) {
            if (!client.Connected)
                return;
            
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("HTTP/1.1 {0} {1}\r\n", (int) code, code.ToString ())));
                writer.Write (Encoding.UTF8.GetBytes ("DPAP-Server: dpap-sharp\r\n"));
                writer.Write (Encoding.UTF8.GetBytes ("Content-Type: application/x-dmap-tagged\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", body.Length)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));
                writer.Write (body);
            }
        }

        public void WriteResponseFile (Socket client, string file, long offset) {
			// DEBUG data			
			Console.WriteLine ("WriteResponseFile!!");
            FileInfo info = new FileInfo (file);

            FileStream stream = info.Open (FileMode.Open, FileAccess.Read, FileShare.Read);
            WriteResponseStream (client, stream, info.Length, offset);
        }

        public void WriteResponseStream (Socket client, Stream response, long len) {
            WriteResponseStream (client, response, len, -1);
        }

        public void WriteResponseStream (Socket client, Stream response, long len, long offset) {
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {

                if (offset > 0) {
                    writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 206 Partial Content\r\n"));
                    writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Range: bytes {0}-{1}/{2}\r\n",
                                                                         offset, len, len + 1)));
                    writer.Write (Encoding.UTF8.GetBytes ("Accept-Range: bytes\r\n"));
                    len = len - offset;
                } else {
                    writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 200 OK\r\n"));
                }

                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", len)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));

                using (BinaryReader reader = new BinaryReader (response)) {
                    if (offset > 0) {
                        reader.BaseStream.Seek (offset, SeekOrigin.Begin);
                    }

                    long count = 0;
                    while (count < len) {
                        byte [] buf = reader.ReadBytes (Math.Min (ChunkLength, (int) len - (int) count));
                        if (buf.Length == 0) {
                            break;
                        }
                        
                        writer.Write (buf);
                        count += buf.Length;
                    }
                }
            }
        }

        public void WriteAccessDenied (Socket client) {
            string msg = "Authorization Required";
            
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 401 Denied\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("WWW-Authenticate: Basic realm=\"{0}\"",
                                                                     realm)));
                writer.Write (Encoding.UTF8.GetBytes ("Content-Type: text/plain\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", msg.Length)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));
                writer.Write (msg);
            }
        }

        private bool IsValidAuth (string user, string pass) {
            if (auth_method == AuthenticationMethod.None)
                return true;

            foreach (NetworkCredential cred in creds) {

                if ( (auth_method != AuthenticationMethod.UserAndPassword || cred.UserName == user) &&
                    cred.Password == pass)
                    return true;
            }

            return false;
        }

        private bool HandleRequest (Socket client) {

            if (!client.Connected)
                return false;
            
            bool ret = true;
            
            using (StreamReader reader = new StreamReader (new NetworkStream (client, false))) {

                string request = reader.ReadLine ();
                if (request == null)
                    return false;
                
                string line = null;
                string user = null;
                string password = null;
                int range = -1;
                
                // read the rest of the request
                do {
                    line = reader.ReadLine ();

                    if (line == "Connection: close") {
                        ret = false;
                    } else if (line != null && line.StartsWith ("Authorization: Basic")) {
                        string [] splitLine = line.Split (' ');

                        if (splitLine.Length != 3)
                            continue;

                        string userpass = Encoding.UTF8.GetString (Convert.FromBase64String (splitLine [2]));

                        string [] splitUserPass = userpass.Split (new char [] {':'}, 2);
                        user = splitUserPass [0];
                        password = splitUserPass [1];
                    } else if (line != null && line.StartsWith ("Range: ")) {
                        // we currently expect 'Range: bytes=<offset>-'
                        string [] splitLine = line.Split ('=');

                        if (splitLine.Length != 2)
                            continue;

                        string rangestr = splitLine [1];
                        if (!rangestr.EndsWith ("-"))
                            continue;

                        try {
                            range = Int32.Parse (rangestr.Substring (0, rangestr.Length - 1));
                        } catch (FormatException) {
                        }
                    }
                } while (line != String.Empty && line != null);
                
                
                string [] splitRequest = request.Split ();
                if (splitRequest.Length < 3) {
                    WriteResponse (client, HttpStatusCode.BadRequest, "Bad Request");
                } else {
                    try {
                        string path = splitRequest [1];
                        if (!path.StartsWith ("dpap://")) {
							Console.WriteLine ("Path is not correct - " + path);
                            path = String.Format ("dpap://localhost{0}", path);
                        }

                        Uri uri = new Uri (path);
                        NameValueCollection query = new NameValueCollection ();

                        if (uri.Query != null && uri.Query != String.Empty) {
                            string [] splitquery = uri.Query.Substring (1).Split ('&');

                            foreach (string queryItem in splitquery) {
                                if (queryItem == String.Empty)
                                    continue;
                                
                                string [] splitQueryItem = queryItem.Split ('=');
                                query [splitQueryItem [0]] = splitQueryItem [1];
                            }
                        }

                        if (auth_method != AuthenticationMethod.None && uri.AbsolutePath == "/login" &&
                            !IsValidAuth (user, password)) {
                            WriteAccessDenied (client);
                            return true;
                        }

                        return handler (client, user, uri.AbsolutePath, query, range);
                    } catch (IOException) {
                        ret = false;
                    } catch (Exception e) {
                        ret = false;
                        Console.Error.WriteLine ("Trouble handling request {0}: {1}", splitRequest [1], e);
                    }
                }
            }

            return ret;
        }

        private void HandleConnection (object o) {
            Socket client = (Socket) o;

            try {
                while (HandleRequest (client)) { }
            } catch (IOException) {
                // ignore
            } catch (Exception e) {
                Console.Error.WriteLine ("Error handling request: " + e);
            } finally {
                clients.Remove (client);
                client.Close ();
            }
        }

        private void ServerLoop () {
            while (true) {
                try {
                    if (!running)
                        break;
                    
                    Socket client = server.Accept ();
                    clients.Add (client);
                    ThreadPool.QueueUserWorkItem (HandleConnection, client);
                } catch (SocketException) {
                    break;
                }
            }
        }
    }

    internal class RevisionManager {

        private Dictionary<int, List<Database>> revisions = new Dictionary<int, List<Database>> ();
        private int current = 1;
        private int limit = 3;

        public int Current {
            get { return current; }
        }

        public int HistoryLimit {
            get { return limit; }
            set { limit = value; }
        }
        
        public void AddRevision (List<Database> databases) {
            revisions [++current] = databases;

            if (revisions.Keys.Count > limit) {
                // remove the oldest

                int oldest = current;
                foreach (int rev in revisions.Keys) {
                    if (rev < oldest) {
                        oldest = rev;
                    }
                }

                RemoveRevision (oldest);
            }
        }

        public void RemoveRevision (int rev) {
            revisions.Remove (rev);
        }

        public List<Database> GetRevision (int rev) {
            if (rev == 0)
                return revisions [current];
            else
                return revisions [rev];
        }

        public Database GetDatabase (int rev, int id) {
            List<Database> dbs = GetRevision (rev);

            if (dbs == null)
                return null;
            
            foreach (Database db in dbs) {
                if (db.Id == id)
                    return db;
            }

            return null;
        }
    }

    public class PhotoRequestedArgs : EventArgs {

        private string user;
        private IPAddress host;
        private Database db;
        private Photo photo;

        public string UserName {
            get { return user; }
        }

        public IPAddress Host {
            get { return host; }
        }

        public Database Database {
            get { return db; }
        }

        public Photo Photo {
            get { return photo; }
        }
        
        public PhotoRequestedArgs (string user, IPAddress host, Database db, Photo photo) {
            this.user = user;
            this.host = host;
            this.db = db;
            this.photo = photo;
        }
    }

    public delegate void PhotoRequestedHandler (object o, PhotoRequestedArgs args);

    public class Server {

        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes (30);
        
        private static Regex dbItemsRegex = new Regex ("/databases/([0-9]*?)/items$");
        private static Regex dbPhotoRegex0 = new Regex ("/databases/([0-9]*?)/items$");
		private static Regex dbPhotoRegex = new Regex (".*'dmap.itemid:([0-9]*)'.*"); 
        private static Regex dbContainersRegex = new Regex ("/databases/([0-9]*?)/containers$");
        private static Regex dbContainerItemsRegex = new Regex ("/databases/([0-9]*?)/containers/([0-9]*?)/items$");
        
        private WebServer ws;
        private ArrayList databases = new ArrayList ();
        private Dictionary<int, User> sessions = new Dictionary<int, User> ();
        private Random random = new Random ();
        private UInt16 port = 8770;
        private ServerInfo serverInfo = new ServerInfo ();
        private bool publish = true;
        private int maxUsers = 0;
        private bool running;
        private string machineId;

        private RegisterService zc_service;

        private object eglock = new object ();
        private RevisionManager revmgr = new RevisionManager ();

        public event EventHandler Collision;
        public event PhotoRequestedHandler PhotoRequested;
        public event UserHandler UserLogin;
        public event UserHandler UserLogout;

        public IList<User> Users {
            get {
                lock (sessions) {
                    return new ReadOnlyCollection<User> (new List<User> (sessions.Values));
                }
            }
        }

        public string Name {
            get { return serverInfo.Name; }
            set {
                serverInfo.Name = value;
                ws.Realm = value;

                if (publish)
                    RegisterService ();
            }
        }

        public string MachineId {
            get { return machineId; }
            set { machineId = value; }
        }

        public UInt16 Port {
            get { return port; }
            set {
                port = value;
                ws.RequestedPort = value;
            }
        }

        public bool IsPublished {
            get { return publish; }
            set {
                publish = value;

                if (running && publish)
                    RegisterService ();
                else if (running && !publish)
                    UnregisterService ();
            }
        }

        public bool IsRunning {
            get { return running; }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return serverInfo.AuthenticationMethod; }
            set {
                serverInfo.AuthenticationMethod = value;
                ws.AuthenticationMethod = value;
            }
        }

        public IList<NetworkCredential> Credentials {
            get { return ws.Credentials; }
        }

        public int MaxUsers {
            get { return maxUsers; }
            set { maxUsers = value; }
        }

        public Server (string name) {
            ws = new WebServer (port, OnHandleRequest);
            serverInfo.Name = name;
            ws.Realm = name;
        }

        public void Start () {
            running = true;
            ws.Start ();

           // if (publish)
                RegisterService ();
        }

		public void Stop () {
            running = false;

            ws.Stop ();
            UnregisterService ();
                
            // get that thread to wake up and exit
            lock (revmgr) {
                Monitor.PulseAll (revmgr);
            }
        }

        public void AddDatabase (Database db) {
            databases.Add (db);
        }

        public void RemoveDatabase (Database db) {
            databases.Remove (db);
        }

        public void AddCredential (NetworkCredential cred) {
            ws.AddCredential (cred);
        }

        public void RemoveCredential (NetworkCredential cred) {
            ws.RemoveCredential (cred);
        }

        public void Commit () {
            List<Database> clones = new List<Database> ();
            foreach (Database db in databases) {
                clones.Add ( (Database) db.Clone ());
            }

            lock (revmgr) {
                revmgr.AddRevision (clones);
                Monitor.PulseAll (revmgr);
            }
        }

        private void RegisterService () {
            lock (eglock) {
                if (zc_service != null) {
                    UnregisterService ();
                }
                
                string auth = serverInfo.AuthenticationMethod == AuthenticationMethod.None ? "false" : "true";
                
                zc_service = new RegisterService ();
                zc_service.Name = serverInfo.Name;
                zc_service.RegType = "_dpap._tcp";
                zc_service.Port = 8770; // (short)ws.BoundPort;
                zc_service.TxtRecord = new TxtRecord ();
                zc_service.TxtRecord.Add ("Password", auth);
                zc_service.TxtRecord.Add ("Machine Name", serverInfo.Name);

                if (machineId != null) {
                    zc_service.TxtRecord.Add ("Machine ID", machineId);
                }
                
                zc_service.TxtRecord.Add ("txtvers", "1");
				zc_service.TxtRecord.Add ("Version", "65537");
				zc_service.ReplyDomain = "local.";
                zc_service.Response += OnRegisterServiceResponse;
                zc_service.Register ();
            }
        }
        
        private void UnregisterService () {
            lock (eglock) {
                if (zc_service == null) {
                    return;
                }
                
                try {
                    zc_service.Dispose ();
                } catch {
                }
                zc_service = null;
            }
        }
        
        private void OnRegisterServiceResponse (object o, RegisterServiceEventArgs args) {
            if (args.ServiceError == ServiceErrorCode.AlreadyRegistered && Collision != null) {
                Collision (this, new EventArgs ());
            }
        }

        private void ExpireSessions () {
            lock (sessions) {
                foreach (int s in new List<int> (sessions.Keys)) {
                    User user = sessions [s];
                    
                    if (DateTime.Now - user.LastActionTime > DefaultTimeout) {
                        sessions.Remove (s);
                        OnUserLogout (user);
                    }
                }
            }
        }

        private void OnUserLogin (User user) {
            UserHandler handler = UserLogin;
            if (handler != null) {
                try {
                    handler (this, new UserArgs (user));
                } catch (Exception e) {
                    Console.Error.WriteLine ("Exception in UserLogin event handler: " + e);
                }
            }
        }

        private void OnUserLogout (User user) {
            UserHandler handler = UserLogout;
            if (handler != null) {
                try {
                    handler (this, new UserArgs (user));
                } catch (Exception e) {
                    Console.Error.WriteLine ("Exception in UserLogout event handler: " + e);
                }
            }
        }

        internal bool OnHandleRequest (Socket client, string username, string path, NameValueCollection query, int range) {
			string photoQuery;
			if (query ["query"] != null)
				photoQuery = query ["query"];
			else
				photoQuery = "";
			
            int session = 0;
            if (query ["session-id"] != null) {
                session = Int32.Parse (query ["session-id"]);
            }
			
/*            if (!sessions.ContainsKey (session) && path != "/server-info" && path != "/content-codes" &&
                path != "/login") {
                ws.WriteResponse (client, HttpStatusCode.Forbidden, "invalid session id");
                return true;
            }
*/
            if (session != 0) {
                sessions [session].LastActionTime = DateTime.Now;
            }

            int clientRev = 0;
            if (query ["revision-number"] != null) {
                clientRev = Int32.Parse (query ["revision-number"]);
            }

            int delta = 0;
            if (query ["delta"] != null) {
                delta = Int32.Parse (query ["delta"]);
            }
			// DEBUG data
			Console.WriteLine ("Before returning resources for path " + path + ", meta " + query ["meta"] + " query " + photoQuery);
			if (dbItemsRegex.IsMatch (path)) //&& photoQuery.Length==0
				Console.WriteLine ("\tThis is a database/items request!");
			
            if (path == "/server-info") {
                ws.WriteResponse (client, GetServerInfoNode ());
            } else if (path == "/content-codes") {
                ws.WriteResponse (client, ContentCodeBag.Default.ToNode ());
            } else if (path == "/login") {
                ExpireSessions ();
                
                if (maxUsers > 0 && sessions.Count + 1 > maxUsers) {
                    ws.WriteResponse (client, HttpStatusCode.ServiceUnavailable, "too many users");
                    return true;
                }
                
                session = random.Next ();
                User user = new User (DateTime.Now, (client.RemoteEndPoint as IPEndPoint).Address, username);
                
                lock (sessions) {
                    sessions [session] = user;
                }
                
                ws.WriteResponse (client, GetLoginNode (session));
                OnUserLogin (user);
            } else if (path == "/logout") {
				Console.WriteLine("logout!");
                User user = sessions [session];
                
                lock (sessions) {
                    sessions.Remove (session);
                }
                
                ws.WriteResponse (client, HttpStatusCode.OK, new byte [0]);
                OnUserLogout (user);
                
                return false;
            } else if (path == "/databases") {
				Console.WriteLine ("path==/databases");
                ws.WriteResponse (client, GetDatabasesNode ());
            } else if (dbItemsRegex.IsMatch (path) && photoQuery.Length==0 ){ //&& !dbPhotoRegex.IsMatch (query ["query"])) {
				Console.WriteLine ("dbItemsRegex, query=" + query ["query"] + " meta=" + query ["meta"]);
                int dbid = Int32.Parse (dbItemsRegex.Match (path).Groups [1].Value);

                Database curdb = revmgr.GetDatabase (clientRev, dbid);

                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                ArrayList deletedIds = new ArrayList ();

                if (delta > 0) {
                    Database olddb = revmgr.GetDatabase (clientRev - delta, dbid);

                    if (olddb != null) {
                        foreach (Photo photo in olddb.Photos) {
                            if (curdb.LookupPhotoById (photo.Id) == null)
                                deletedIds.Add (photo.Id);
                        }
                    }
                }

                ContentNode node = curdb.ToPhotosNode (query ["meta"].Split (','),
                                                      (int []) deletedIds.ToArray (typeof (int)));
                ws.WriteResponse (client, node);
            } else if (dbPhotoRegex.IsMatch (photoQuery)) {
				Console.WriteLine ("dbPhotoRegex");
				Console.WriteLine ("dbPhotosRegex, query=" + query ["query"] + " meta=" + query ["meta"]);
				string [] photoIds = query ["query"].Split (',');
				Match match = dbPhotoRegex0.Match (path);
				int dbid = Int32.Parse (match.Groups [1].Value);
				int photoid = 0;
				
		        
				//match = dbPhotoRegex.Match (photoQuery);
                
                Database db = revmgr.GetDatabase (clientRev, dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }
				ArrayList photoNodes = new ArrayList ();
				Photo photo = db.LookupPhotoById (1);
				
				foreach (string photoId in photoIds) {
					match = dbPhotoRegex.Match (photoId);
					photoid = Int32.Parse (match.Groups [1].Value);
					photo = db.LookupPhotoById (photoid);
					photoNodes.Add (photo.ToFileData (query ["meta"].Contains ("dpap.thumb")));
					// DEBUG
					//Console.WriteLine ("Requested photo id=" + photoid);
				}
        
				ArrayList children = new ArrayList ();
				children.Add (new ContentNode ("dmap.status", 200));
				children.Add (new ContentNode ("dmap.updatetype", (byte) 0));
				children.Add (new ContentNode ("dmap.specifiedtotalcount",  2));
				children.Add (new ContentNode ("dmap.returnedcount", 2));
				children.Add (new ContentNode ("dmap.listing", photoNodes));
				ContentNode dbsongs = new ContentNode ("dpap.databasesongs", children);
                
                if (photo == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid photo id");
                    return true;
                }

                try {
                    try {
                        if (PhotoRequested != null)
                            PhotoRequested (this, new PhotoRequestedArgs (username,
                                                                        (client.RemoteEndPoint as IPEndPoint).Address,
                                                                        db, photo));
                    } catch {}
                    
                    if (photo.FileName != null) {
						// DEBUG
						//Console.WriteLine ("photo.Filename != null" + query ["meta"].Split (',') [0]);
						//ContentNode node = photo.ToFileData ();
						//node.Dump ();
						
						ws.WriteResponse (client, dbsongs);

                    } else if (db.Client != null) {
						Console.WriteLine ("db.Client != null");
                        long photoLength = 0;
                        Stream photoStream = db.StreamPhoto (photo, out photoLength);
                        
                        try {
                            ws.WriteResponseStream (client, photoStream, photoLength);
                        } catch (IOException) {
							Console.WriteLine("IOException!");
                        }
                    } else {
						Console.WriteLine ("Else - internal error");
                        ws.WriteResponse (client, HttpStatusCode.InternalServerError, "no file");
                    }
                } finally {
					// commented out because it breaks the connection after sending a hires photo
					// client.Close()
                }
            } else if (dbContainersRegex.IsMatch (path)) {
                int dbid = Int32.Parse (dbContainersRegex.Match (path).Groups [1].Value);

                Database db = revmgr.GetDatabase (clientRev, dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                ws.WriteResponse (client, db.ToAlbumsNode ());
            } else if (dbContainerItemsRegex.IsMatch (path)) {
				// DEBUG
				Console.WriteLine ("ContainerItems ! path=" + path);
				
                Match match = dbContainerItemsRegex.Match (path);
                int dbid = Int32.Parse (match.Groups [1].Value);
                int plid = Int32.Parse (match.Groups [2].Value);
				
                Database curdb = revmgr.GetDatabase (clientRev, dbid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                Album curpl = curdb.LookupAlbumById (plid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid playlist id");
                    return true;
                }
				// DEBUG
				Console.WriteLine("db and album ready!");
                ArrayList deletedIds = new ArrayList ();
                if (delta > 0) {
                    Database olddb = revmgr.GetDatabase (clientRev - delta, dbid);

                    if (olddb != null) {
                        Album oldpl = olddb.LookupAlbumById (plid);

                        if (oldpl != null) {
                            IList<Photo> oldplPhotos = oldpl.Photos;
                            for (int i = 0; i < oldplPhotos.Count; i++) {
                                int id = oldpl.GetContainerId (i);
                                if (curpl.LookupIndexByContainerId (id) < 0) {
                                    deletedIds.Add (id);
                                }
                            }
                        }
                    }
                }
				Console.WriteLine("About to send response... meta=" + query["meta"]);
                curpl.ToPhotosNode (query ["meta"].Split (',')).Dump ();
				
                ws.WriteResponse (client, curpl.ToPhotosNode (query ["meta"].Split (',')));
				//, (int []) deletedIds.ToArray (typeof (int))));
            } else if (path == "/update") {
                int retrev;
                
                lock (revmgr) {
                    // if they have the current revision, wait for a change
                    if (clientRev == revmgr.Current) {
                        Monitor.Wait (revmgr);
                    }

                    retrev = revmgr.Current;
                }

                if (!running) {
                    ws.WriteResponse (client, HttpStatusCode.NotFound, "server has been stopped");
                } else {
                    ws.WriteResponse (client, GetUpdateNode (retrev));
                }
            } else {
                ws.WriteResponse (client, HttpStatusCode.Forbidden, "GO AWAY");
            }

            return true;
        }

        private ContentNode GetLoginNode (int id) {
            return new ContentNode ("dmap.loginresponse",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.sessionid", id));
        }

        private ContentNode GetServerInfoNode () {
			
            return serverInfo.ToNode (databases.Count);
        }

        private ContentNode GetDatabasesNode () {
            ArrayList databaseNodes = new ArrayList ();

            List<Database> dbs = revmgr.GetRevision (revmgr.Current);
            if (dbs != null) {
                foreach (Database db in revmgr.GetRevision (revmgr.Current)) {
                    databaseNodes.Add (db.ToDatabaseNode ());
                }
            }

            ContentNode node = new ContentNode ("dpap.serverdatabases",
                                                new ContentNode ("dmap.status", 200),
                                                new ContentNode ("dmap.updatetype", (byte) 0),
                                                new ContentNode ("dmap.specifiedtotalcount", databases.Count),
                                                new ContentNode ("dmap.returnedcount", databases.Count),
                                                new ContentNode ("dmap.listing", databaseNodes));

            return node;
        }

        private ContentNode GetUpdateNode (int revision) {
            return new ContentNode ("dmap.updateresponse",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.serverrevision", revision));
        }
    }
}
