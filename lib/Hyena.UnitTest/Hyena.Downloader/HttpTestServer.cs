//
// HttpTestServer.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Hyena.Downloader.Tests
{
	class HttpTestServer : IDisposable
	{
		class Resource
		{
			public string Path;
			public string Checksum;
			public long Length;
		}

		List<Resource> resources = new List<Resource> ();
		bool stop_requested;
		bool running;
		bool serving;

		HttpListener listener;
		public int ResourceCount { get; set; }
		public int MinResourceSize { get; set; }
		public int MaxResourceSize { get; set; }
		public bool IsServing { get { return serving; } }

		public bool Debug = true;

		public string BaseUrl { get { return "http://localhost:8080/"; } }

		public HttpTestServer ()
		{
			ResourceCount = 5;
			MinResourceSize = 5 * 1024 * 1024;
			MaxResourceSize = 15 * 1024 * 1024;
		}

		public void Run ()
		{
			stop_requested = false;
			running = true;
			GenerateStaticContent ();
			ServeStaticContent ();
			running = false;
			serving = false;
		}

		public void Dispose ()
		{
			Stop ();
		}

		public void Stop ()
		{
			lock (this) {
				if (!running)
					return;

				stop_requested = true;
				listener.Abort ();

				// busy wait, oh well
				if (Debug) Console.WriteLine ("waiting for server to stop");
				while (running) { }
				if (Debug) Console.WriteLine ("  > done waiting for server to stop");
			}
		}

		void ServeStaticContent ()
		{
			if (Debug) Console.WriteLine ();
			if (Debug) Console.WriteLine ("Serving static content...");

			listener = new HttpListener ();
			listener.Prefixes.Add (BaseUrl);
			listener.Start ();

			serving = true;

			while (!stop_requested) {
				var async_result = listener.BeginGetContext (result => {
					var context = listener.EndGetContext (result);
					var response = context.Response;
					var path = context.Request.Url.LocalPath;

					response.StatusCode = 200;
					response.StatusDescription = "OK";
					response.ProtocolVersion = new Version ("1.1");

					try {
						if (Debug) Console.WriteLine ("Serving: {0}", path);
						if (path == "/") {
							ServeString (response, resources.Count.ToString () + "\n");
							return;
						} else if (path == "/shutdown") {
							ServeString (response, "Goodbye\n");
							lock (this) {
								stop_requested = true;
							}
						}

						var resource = resources[int.Parse (path.Substring (1))];
						response.ContentType = "application/octet-stream";
						response.ContentLength64 = resource.Length;
						response.AppendHeader ("X-Content-MD5-Sum", resource.Checksum);

						if (context.Request.HttpMethod == "HEAD") {
							response.Close ();
						}

						using (var resource_stream = File.OpenRead (resource.Path)) {
							var buffer = new byte[32 << 10];
							using (response.OutputStream) {
								while (true) {
									var read = resource_stream.Read (buffer, 0, buffer.Length);
									if (read <= 0) {
										break;
									}
									response.OutputStream.Write (buffer, 0, read);
								}
							}
						}
					} catch {
						response.StatusCode = 404;
						response.StatusDescription = "404 Not Found";
						ServeString (response, "Invalid resource: " + path + "\n");
					}

					response.Close ();
				}, null);
				async_result.AsyncWaitHandle.WaitOne ();
			}
		}

		void ServeString (HttpListenerResponse response, string content)
		{
			var buffer = Encoding.UTF8.GetBytes (content);
			response.ContentType = "text/plain";
			response.ContentLength64 = buffer.Length;
			using (var stream = response.OutputStream) {
				stream.Write (buffer, 0, buffer.Length);
			}
		}

		void GenerateStaticContent ()
		{
			resources.Clear ();
			var random = new Random ();
			var root = "/tmp/hyena-download-test-server";

			try {
				Directory.Delete (root, true);
			} catch {
			}

			Directory.CreateDirectory (root);

			for (int i = 0; i < ResourceCount; i++) {
				var md5 = new MD5CryptoServiceProvider ();
				var resource = new Resource () {
					Path = Path.Combine (root, i.ToString ()),
					Length = random.Next (MinResourceSize, MaxResourceSize + 1)
				};

				if (Debug) Console.WriteLine ();

				using (var stream = File.OpenWrite (resource.Path)) {
					var buffer = new byte[32 << 10];
					long written = 0;
					long remaining;

					while ((remaining = resource.Length - written) > 0) {
						var buffer_length = remaining > buffer.Length
							? (int)buffer.Length
							: (int)remaining;

						random.NextBytes (buffer);
						stream.Write (buffer, 0, buffer_length);
						written += buffer_length;
						md5.TransformBlock (buffer, 0, buffer_length, null, 0);

						if (Debug) Console.Write ("\rCreating resource: {0} ({1:0.00} MB): [{2}/{3}]  {4:0.0}% ",
							resource.Path, resource.Length / 1024.0 / 1024.0,
							i + 1, ResourceCount,
							written / (double)resource.Length * 100.0);
					}

					md5.TransformFinalBlock (buffer, 0, 0);
					resource.Checksum = BitConverter.ToString (md5.Hash).Replace ("-", string.Empty).ToLower ();
				}

				resources.Add (resource);
			}
		}
	}
}
