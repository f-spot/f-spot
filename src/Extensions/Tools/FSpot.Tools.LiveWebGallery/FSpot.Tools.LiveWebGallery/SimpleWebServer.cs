/*
 * LiveWebGalleryExtension.SimpleWebServer.cs
 *
 * Author(s):
 *	Anton Keks  <anton@azib.net>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using FSpot;
using FSpot.Extensions;
using Hyena;

namespace FSpot.Tools.LiveWebGallery
{		
	public class SimpleWebServer : IService
	{
		private Thread server_thread;
		private TcpListener listener;
		private Dictionary<string, RequestHandler> handlers = new Dictionary<string, RequestHandler> ();
		
		private IWebStats stats;
		public IWebStats Stats {
			set { stats = value; }
		}

		public bool Active {
			get { return server_thread != null && server_thread.IsAlive; }
		}
		
		public string HostPort {
			get {
				string host = Dns.GetHostName ();
				// TODO: add support for .local hostnames
				foreach (IPAddress addr in Dns.GetHostAddresses(host)) {
					if (!IPAddress.IsLoopback (addr)) {
						host = addr.ToString ();
					}
				}
				return host + ":" + (listener.LocalEndpoint as IPEndPoint).Port;
			}
		}
		
		public void RegisterHandler (string request_prefix, RequestHandler handler) {
			handlers.Add (request_prefix, handler);
		}

		public bool Start () {
			try {
				listener = new TcpListener (IPAddress.Any, 8080);
				listener.Start ();
			}			
			catch (SocketException) {
				// address already in use? choose a random port then
				listener = new TcpListener (IPAddress.Any, 0);
				listener.Start ();
			}
			server_thread = new Thread (new ThreadStart(ServerLoop));
			server_thread.Start ();
			return true;
		}
		
		public bool Stop () {
			server_thread.Abort ();
			server_thread.Join ();
			listener.Stop ();
			return true;
		}

		public void ServerLoop () 
		{
			Log.Information ("Listening on " + listener.LocalEndpoint);
			
			while (true) {
				TcpClient client = listener.AcceptTcpClient ();
				if (client.Connected) {
					if (stats != null)
						stats.IncomingRequest ((client.Client.RemoteEndPoint as IPEndPoint).Address);

					RequestProcessor parser = new RequestProcessor (client, handlers);
					new Thread (new ThreadStart (parser.Process)).Start ();
				}
			}
		}		
	
		class RequestProcessor
		{
			private TcpClient client;
			private Dictionary<string, RequestHandler> handlers;
			
			public RequestProcessor (TcpClient client, Dictionary<string, RequestHandler> handlers) {
				this.client = client;						
				this.handlers = handlers;
			}
			
			public void Process () {			
				using (client)  {
					NetworkStream stream = client.GetStream ();
					TextReader reader = new StreamReader (stream, Encoding.UTF8);
					
					string line = reader.ReadLine ();
					if (line == null)
						return;
					
					Log.Debug ("Incoming request from " + (client.Client.RemoteEndPoint as IPEndPoint).Address + ": " + line);

					string request_method = null, request_string = null;
					int space_pos = line.IndexOf (' ');
					if (space_pos > 0) {
						request_method = line.Substring (0, space_pos);
						request_string = line.Substring (space_pos + 1, line.LastIndexOf (' ') - space_pos - 1);				
					}
					while (!string.IsNullOrEmpty(line = reader.ReadLine ())) {
						// process other request headers here if needed
					}
					
					using (stream) {						
						if (!"GET".Equals (request_method)) { 
							RequestHandler.SendError (stream, "400 Bad Request");
							return;
						}
						
						if (request_string.StartsWith ("/"))
							request_string = request_string.Substring (1);
						
						string request_prefix = request_string;
						int slash_pos = request_string.IndexOf ("/");
						if (slash_pos >= 0) 
							request_prefix = request_string.Substring (0, slash_pos);						
					
						if (!handlers.ContainsKey (request_prefix)) {
							RequestHandler.SendError (stream, "404 No handler for \"" + request_string + "\"");
							return;
						}

						try {
							handlers[request_prefix].Handle (request_string.Substring (slash_pos+1), stream);
						}
						catch (Exception e) {
							Log.Exception (e);
							try {
								RequestHandler.SendError (stream, "500 " + e.Message);
							}
							catch (IOException) {
								// ignore already closed connections
							}
						}
					}
				}
			}
		}
	}
	
	public abstract class RequestHandler
	{		
		public abstract void Handle (string requested, Stream stream);

		public static void SendLine (Stream stream, string header) {
			byte[] buf = Encoding.UTF8.GetBytes (header + "\r\n");
			stream.Write (buf, 0, buf.Length);				
		}
		
		public static void SendStatus (Stream stream, string status) {
			SendLine (stream, "HTTP/1.0 " + status + "\r\nServer: F-Spot");
		}

		public static void SendError (Stream stream, string error) {
			SendStatus (stream, error);
			StartContent (stream);
			SendLine (stream, error);  
		}

		public static void StartContent (Stream stream) {
			// sends the last empty newline after headers
			SendLine (stream, "");
		}
		
		public static void SendHeadersAndStartContent (Stream stream, params string[] headers) {
			SendStatus (stream, "200 OK");
			foreach (string header in headers) {
				SendLine (stream, header);
			}
			StartContent (stream);
		}
		
		public string MimeTypeForExt (string ext)
		{
			switch (ext.ToLower ()) {
			case ".jpg":
			case ".jpeg":
				return "image/jpeg";
			case ".png":
				return "image/png";
			case ".gif":
				return "image/gif";
			case ".js":
				return "text/javascript";
			case ".css":
				return "text/css";
			default:
				throw new Exception ("Unknown file type: " + ext);
			}
		}

	}
	
	public interface IWebStats
	{
		void IncomingRequest (IPAddress addr);
	}
}
