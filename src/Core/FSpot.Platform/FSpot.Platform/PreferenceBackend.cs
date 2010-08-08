/*
 * FSpot.Platform.Gnome.PreferenceBackend.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Runtime.Serialization;


namespace FSpot
{
	public class NotifyEventArgs : System.EventArgs
	{
		string key;
		public string Key {
			get { return key; }
		}

		object val;
		public object Value {
			get { return val; }
		}

		public NotifyEventArgs (string key, object val)
		{
			this.key = key;
			this.val = val;
		}
	}
}

namespace FSpot.Platform
{
	[Serializable]
	public class NoSuchKeyException : Exception
	{
		public NoSuchKeyException () : base ()
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

		private static GConf.Client client;
		private GConf.Client Client {
			get {
				lock (sync_handler) {
					if (client == null)
						client = new GConf.Client ();
					return client;
				}
			}
		}

		public PreferenceBackend ()
		{
		}

		public object Get (string key)
		{
			try {
				return Client.Get (key);
			} catch (GConf.NoSuchKeyException) {
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
			Client.Set (key, o);
		}

		public void AddNotify (string key, EventHandler<NotifyEventArgs> handler)
		{
			// GConf doesn't like trailing slashes
			key = key.TrimEnd('/');	
			Client.AddNotify (key, delegate (object sender, GConf.NotifyEventArgs args) {handler (sender, new NotifyEventArgs (args.Key, args.Value));});
		}
	}
}
