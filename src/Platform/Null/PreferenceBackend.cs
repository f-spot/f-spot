/*
 * FSpot.NullPreferenceBackend.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

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
}

namespace FSpot.Platform
{
	public class PreferenceBackend : IPreferenceBackend
	{
		public object Get (string key)
		{
			throw new NoSuchKeyException (key);
		}

		public void Set (string key, object o)
		{
		}

		public void AddNotify (string key, EventHandler<NotifyEventArgs> handler)
		{
		}
	}
}
