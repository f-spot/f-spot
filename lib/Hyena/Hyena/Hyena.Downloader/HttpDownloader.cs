// 
// HttpDownloader.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
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
using System.Threading;

namespace Hyena.Downloader
{
    public class HttpDownloader
    {
        private object sync_root = new object ();
        protected object SyncRoot {
            get { return sync_root; }
        }

        private HttpWebRequest request;
        private HttpWebResponse response;
        private Stream response_stream;
        private DateTime last_raised_percent_complete;
        private IAsyncResult async_begin_result;
        private ManualResetEvent sync_event;

        public string UserAgent { get; set; }
        public Uri Uri { get; set; }
        public TimeSpan ProgressEventRaiseLimit { get; set; }
        public HttpDownloaderState State { get; private set; }
        public string [] AcceptContentTypes { get; set; }

        private int buffer_size = 8192;
        public int BufferSize {
            get { return buffer_size; }
            set {
                if (value <= 0) {
                    throw new InvalidOperationException ("Invalid buffer size");
                }
                buffer_size = value;
            }
        }

        private string name;
        public string Name {
            get { return name ?? Path.GetFileName (Uri.UnescapeDataString (Uri.LocalPath)); }
            set { name = value; }
        }

        public event Action<HttpDownloader> Started;
        public event Action<HttpDownloader> Finished;
        public event Action<HttpDownloader> Progress;
        public event Action<HttpDownloader> BufferUpdated;

        public HttpDownloader ()
        {
            ProgressEventRaiseLimit = TimeSpan.FromSeconds (0.25);
        }

        public void StartSync ()
        {
            sync_event = new ManualResetEvent (false);
            Start ();
            sync_event.WaitOne ();
            sync_event = null;
        }

        public void Start ()
        {
            lock (SyncRoot) {
                if (request != null || async_begin_result != null) {
                    throw new InvalidOperationException ("HttpDownloader is already active");
                }
                
                State = new HttpDownloaderState () {
                    Buffer = new Buffer () {
                        Data = new byte[BufferSize]
                    }
                };

                request = CreateRequest ();
                async_begin_result = request.BeginGetResponse (OnRequestResponse, this);

                State.StartTime = DateTime.Now;
                State.Working = true;
                OnStarted ();
            }
        }

        public void Abort ()
        {
            lock (SyncRoot) {
                Close ();
                OnFinished ();
            }
        }

        private void Close ()
        {
            lock (SyncRoot) {
                State.FinishTime = DateTime.Now;
                State.Working = false;

                if (response_stream != null) {
                    response_stream.Close ();
                }

                if (response != null) {
                    response.Close ();
                }

                response_stream = null;
                response = null;
                request = null;
            }
        }

        protected virtual HttpWebRequest CreateRequest ()
        {
            var request = (HttpWebRequest)WebRequest.Create (Uri);
            request.Method = "GET";
            request.AllowAutoRedirect = true;
            request.UserAgent = UserAgent;
            request.Timeout = 10000;
            return request;
        }

        private void OnRequestResponse (IAsyncResult asyncResult)
        {
            lock (SyncRoot) {
                async_begin_result = null;

                if (request == null) {
                    return;
                }

                var raise = false;

                try {
                    response = (HttpWebResponse)request.EndGetResponse (asyncResult);
                    if (response.StatusCode != HttpStatusCode.OK) {
                        State.Success = false;
                        raise = true;
                        return;
                    } else if (AcceptContentTypes != null) {
                        var accepted = false;
                        foreach (var type in AcceptContentTypes) {
                            if (type == response.ContentType) {
                                accepted = true;
                                break;
                            }
                        }
                        if (!accepted) {
                            throw new WebException ("Invalid content type: " +
                                response.ContentType + "; expected one of: " +
                                String.Join (", ", AcceptContentTypes));
                        }
                    }

                    State.ContentType = response.ContentType;
                    State.CharacterSet = response.CharacterSet;

                    State.TotalBytesExpected = response.ContentLength;

                    response_stream = response.GetResponseStream ();
                    async_begin_result = response_stream.BeginRead (State.Buffer.Data, 0,
                        State.Buffer.Data.Length, OnResponseRead, this);
                } catch (Exception e) {
                    State.FailureException = e;
                    State.Success = false;
                    raise = true;
                } finally {
                    if (raise) {
                        Close ();
                        OnFinished ();
                    }
                }
            }
        }

        private void OnResponseRead (IAsyncResult asyncResult)
        {
            lock (SyncRoot) {
                async_begin_result = null;

                if (request == null || response == null || response_stream == null) {
                    return;
                }

                try {
                    var now = DateTime.Now;

                    State.Buffer.Length = response_stream.EndRead (asyncResult);
                    State.Buffer.TimeStamp = now;
                    State.TotalBytesRead += State.Buffer.Length;
                    State.TransferRate = State.TotalBytesRead / (now - State.StartTime).TotalSeconds;
                    State.PercentComplete = (double)State.TotalBytesRead / (double)State.TotalBytesExpected;

                    OnBufferUpdated ();

                    if (State.Buffer.Length <= 0) {
                        State.Success = true;
                        Close ();
                        OnFinished ();
                        return;
                    }

                    if (State.PercentComplete >= 1 || last_raised_percent_complete == DateTime.MinValue ||
                        (now - last_raised_percent_complete >= ProgressEventRaiseLimit)) {
                        last_raised_percent_complete = now;
                        OnProgress ();
                    }

                    async_begin_result = response_stream.BeginRead (State.Buffer.Data, 0,
                        State.Buffer.Data.Length, OnResponseRead, this);
                } catch (Exception e) {
                    State.FailureException = e;
                    State.Success = false;
                    Close ();
                    OnFinished ();
                }
            }
        }

        protected virtual void OnStarted ()
        {
            var handler = Started;
            if (handler != null) {
                handler (this);
            }
        }

        protected virtual void OnBufferUpdated ()
        {
            var handler = BufferUpdated;
            if (handler != null) {
                handler (this);
            }
        }

        protected virtual void OnProgress ()
        {
            var handler = Progress;
            if (handler != null) {
                handler (this);
            }
        }

        protected virtual void OnFinished ()
        {
            var handler = Finished;
            if (handler != null) {
                try {
                    handler (this);
                } catch (Exception e) {
                    Log.Exception (String.Format ("HttpDownloader.Finished handler ({0})", Uri), e);
                }
            } 

            if (sync_event != null) {
                sync_event.Set ();
            }
        }

        public override string ToString ()
        {
            return Name;
        }
    }
}
