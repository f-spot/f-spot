/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.GZip;

namespace DPAP {

    internal class ContentFetcher : IDisposable {
        private IPAddress address;
        private UInt16 port;
        private int sessionId;
        private int requestId = 10;

        private DAAPCredentials creds = new DAAPCredentials ();
        private List<WebRequest> requests = new List<WebRequest> ();

        public string Username {
            get { return creds.Username; }
            set { creds.Username = value; }
        }

        public string Password {
            get { return creds.Password; }
            set { creds.Password = value; }
        }
        
        public int SessionId {
            get { return sessionId; }
            set { sessionId = value; }
        }

        public ContentFetcher (IPAddress address, UInt16 port) {
            this.address = address;
            this.port = port;
        }

        public void Dispose () {
            KillAll ();
        }

        public void KillAll () {
            lock (requests) {
                foreach (WebRequest request in requests) {
                    request.Abort ();
                }

                requests.Clear ();
            }
        }

        public byte[] Fetch (string path) {
            return Fetch (path, null, null, 0);
        }

        public byte[] Fetch (string path, string query) {
            return Fetch (path, query, null, 0);
        }

        public byte[] Fetch (string path, string query, WebHeaderCollection extraHeaders,
                             int requestId) {

            HttpWebResponse response = FetchResponse (path, -1, query, extraHeaders, requestId, false);

            MemoryStream data = new MemoryStream ();
            BinaryReader reader = new BinaryReader (GetResponseStream (response));
            try {
                if (response.ContentLength < 0)
                    return null;

                byte[] buf;
                while (true) {
                    buf = reader.ReadBytes (8192);
                    if (buf.Length == 0)
                        break;

                    data.Write (buf, 0, buf.Length);
					//Console.Write(buf.);
                }
				
                data.Flush ();
                return data.GetBuffer ();
            } finally {
                data.Close ();
                reader.Close ();
                response.Close ();
            }
        }

        public HttpWebResponse FetchResponse (string path, string query, WebHeaderCollection headers) {
            return FetchResponse (path, -1, query, headers, ++requestId, false);
        }

        public HttpWebResponse FetchFile (string path, long offset) {
            return FetchResponse (path, offset, null, null, ++requestId, true);
        }

        public HttpWebResponse FetchResponse (string path, long offset, string query,
                                              WebHeaderCollection extraHeaders,
                                              int requestId, bool disableKeepalive) {
            UriBuilder builder = new UriBuilder ("http", address.ToString ());
            builder.Port = port;
            builder.Path = path;

            if (sessionId != 0)
                query = String.Format ("session-id={0}&", sessionId) + query;

            if (query != null)
                builder.Query += query;

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (builder.Uri);
            request.PreAuthenticate = true;
            request.Timeout = System.Threading.Timeout.Infinite;
            request.Headers.Add ("Accept-Encoding", "gzip");

            if (offset > 0) {
                request.AddRange ("bytes", (int) offset);
            }

            request.ServicePoint.ConnectionLimit = 3;
            
            if (extraHeaders != null)
                request.Headers = extraHeaders;

            request.Accept = "*/*";

            request.KeepAlive = !disableKeepalive;

            string hash = Hasher.GenerateHash (3, builder.Uri.PathAndQuery, 2, requestId);

            request.UserAgent = "iPhoto/5.0.2 (Macintosh; PPC)";
            request.Headers.Set ("Client-DMAP-Version", "1.0");
			request.Headers.Set ("Client-DPAP-Version", "1.1");
            /*request.Headers.Set ("Client-DPAP-Validation", hash);
            request.Headers.Set ("Client-DPAP-Access-Index", "2");
			*/
// DEBUG data			
			Console.Write(path + "?"+query);
			Console.Write(request.Headers);
			
            if (requestId >= 0)
                request.Headers.Set ("Client-DPAP-Request-ID", requestId.ToString ());
                                 
            request.Credentials = creds;
            request.PreAuthenticate = true;
            
            try {
                lock (requests) {
                    requests.Add (request);
                }
                HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
				//if(!response.StatusCode.Equals("OK"))
				//	Console.Write(response.StatusCode);
                return response;
            } finally {
                lock (requests) {
                    requests.Remove (request);
                }
            }
        }

        public Stream GetResponseStream (HttpWebResponse response) {
            if (response.ContentEncoding == "gzip") {
                return new GZipInputStream (response.GetResponseStream ());
            } else {
                return response.GetResponseStream ();
            }
        }

        private class DAAPCredentials : ICredentials {

            private string username;
            private string password;

            public string Username {
                get { return username; }
                set { username = value; }
            }

            public string Password {
                get { return password; }
                set { password = value; }
            }
            
            public NetworkCredential GetCredential (Uri uri, string type) {
                return new NetworkCredential (username == null ? "none" : username, password);
            }
        }
    }
}
