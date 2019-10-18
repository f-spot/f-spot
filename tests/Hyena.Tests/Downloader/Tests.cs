//
// Tests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#if ENABLE_TESTS

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Hyena;

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

        private void StartServer ()
        {
            server.Stop ();
            new System.Threading.Thread (server.Run).Start ();
            while (!server.IsServing) {}
        }

        private HttpTestServer server;

        [TestFixtureSetUp]
        public void Setup ()
        {
            server = new HttpTestServer ();
            server.Debug = false;
        }

        [TestFixtureTearDown]
        public void Teardown ()
        {
            server.Dispose ();
        }
    }
}

#endif
