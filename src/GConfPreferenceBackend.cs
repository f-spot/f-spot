/*
 * FSpot.GConfPreferenceBackend.cs
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */
#if !NOGCONF
using System.Runtime.InteropServices;

namespace FSpot
{
	public class GConfPreferenceBackend : IPreferenceBackend
	{
		private static GConf.Client client;
		private GConf.Client Client {
			get {
				if (client == null)
					client = new GConf.Client ();
				return client;	
			}
		}

		public GConfPreferenceBackend ()
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

		public void Set (string key, object o)
		{
			Client.Set (key, o);
		}

		public void AddNotify (string key, NotifyChangedHandler handler)
		{
			Client.AddNotify (key, delegate (object sender, GConf.NotifyEventArgs args) {handler (sender, new NotifyEventArgs (args.Key, args.Value));});
		}
	}
}
#endif
