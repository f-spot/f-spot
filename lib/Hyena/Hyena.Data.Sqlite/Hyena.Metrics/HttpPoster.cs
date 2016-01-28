//
// HttpPoster.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Net;

namespace Hyena.Metrics
{
    public class HttpPoster
    {
        private string url;
        private MetricsCollection metrics;

        public HttpPoster (string url, MetricsCollection metrics)
        {
            this.url = url;
            this.metrics = metrics;

            // Sending the Expect header causes lighttpd to fail with a 417 header.
            ServicePointManager.Expect100Continue = false;
        }

        public bool Post ()
        {
            var request = (HttpWebRequest) WebRequest.Create (url);
            request.Timeout = 30 * 1000;
            request.Method = "POST";
            request.KeepAlive = false;
            request.ContentType = "text/json";
            request.AllowAutoRedirect = true;

            try {
                using (var stream = request.GetRequestStream ()) {
                    using (var writer = new StreamWriter (stream)) {
                        // TODO gzip the data
                        writer.Write (metrics.ToJsonString ());
                    }
                }

                var response = (HttpWebResponse) request.GetResponse ();
                using (var strm = new StreamReader (response.GetResponseStream ())) {
                    Console.WriteLine (strm.ReadToEnd ());
                }
                return response.StatusCode == HttpStatusCode.OK;
            } catch (Exception e) {
                Log.Exception ("Error posting metrics", e);
            }

            return false;
        }
    }
}
