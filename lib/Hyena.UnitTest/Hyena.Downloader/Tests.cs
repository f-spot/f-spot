//
// Tests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using NUnit.Framework;

namespace Hyena.Downloader.Tests
{
	[TestFixture]
	public class Tests
	{
		[Test]
		public void SimpleDownload ()
		{
			StartServer ();

			new HttpStringDownloader () {
				Uri = new Uri (server.BaseUrl),
				Finished = (d) => {
					Assert.IsTrue (d.State.Success);
					Assert.AreEqual (server.ResourceCount.ToString () + "\n", d.Content);
				}
			}.StartSync ();

			var f = new HttpFileDownloader () { Uri = new Uri (server.BaseUrl + "/1") };
			f.FileFinished += (d) => {
				Assert.IsTrue (d.State.Success);
				var size = new System.IO.FileInfo (d.LocalPath).Length;
				Assert.IsTrue (size <= server.MaxResourceSize);
				Assert.IsTrue (size >= server.MinResourceSize);
				System.IO.File.Delete (d.LocalPath);
			};
			f.StartSync ();
		}

		[Test]
		public void DownloadManager ()
		{
		}

		void StartServer ()
		{
			server.Stop ();
			new System.Threading.Thread (server.Run).Start ();
			while (!server.IsServing) { }
		}

		HttpTestServer server;

		[OneTimeSetUp]
		public void Setup ()
		{
			server = new HttpTestServer ();
			server.Debug = false;
		}

		[OneTimeTearDown]
		public void Teardown ()
		{
			server.Dispose ();
		}
	}
}
