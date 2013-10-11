//
// PreferenceBackend.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.Serialization;

using GLib;

namespace FSpot
{
	public class NotifyEventArgs : System.EventArgs
	{
		public string Key { get; private set; }
		public object Value { get; private set; }

		public NotifyEventArgs (string key, object val)
		{
			Key = key;
			Value = val;
		}
	}
}
// GTK3: GSettings backend
namespace FSpot.Platform
{
	[Serializable]
	public class NoSuchKeyException : Exception
	{
		public NoSuchKeyException ()
		{
		}

		public NoSuchKeyException (string key) : base (key)
		{
		}

		public NoSuchKeyException (string key, Exception e) : base (key, e)
		{
		}

		protected NoSuchKeyException (SerializationInfo info, StreamingContext context) : base (info, context)
		{
		}
	}

	public class PreferenceBackend
	{
		static object sync_handler = new object ();

		// GTK3: GSettings
//		static GConf.Client client;
//		GConf.Client Client {
//			get {
//				lock (sync_handler) {
//					if (client == null)
//						client = new GConf.Client ();
//					return client;
//				}
//			}
//		}

		public object Get (string key)
		{
			try {
				return new object();//return Client.Get (key);
			} catch (Exception /*GConf.NoSuchKeyException*/) {
				throw new NoSuchKeyException (key);
			}
		}

		internal T Get<T> (string key)
		{
			T value = default(T);
			try {
				value = (T) Get (key);
			} catch (NoSuchKeyException) {
			} catch (InvalidCastException) {
			}
			return value;
		}

		public void Set (string key, object o)
		{
			//Client.Set (key, o);
		}

		public void AddNotify (string key, EventHandler<NotifyEventArgs> handler)
		{
			// GConf doesn't like trailing slashes
			key = key.TrimEnd('/');	
			//Client.AddNotify (key, delegate (object sender, GConf.NotifyEventArgs args) {handler (sender, new NotifyEventArgs (args.Key, args.Value));});
		}
	}
}
