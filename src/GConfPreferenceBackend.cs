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
#if !GCONF_SHARP_2_20_2
		[DllImport("libgobject-2.0-0.dll")]
		static extern void g_type_init ();
#endif

		private static GConf.Client client;
		private GConf.Client Client {
			get {
				if (client == null) {
#if !GCONF_SHARP_2_20_2
					//workaround for bgo #481741, fixed upstream
					g_type_init ();
#endif
					client = new GConf.Client ();
				}
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
			} catch (GConf.NoSuchKeyException e) {
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
