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
	public class NullPreferenceBackend : IPreferenceBackend
	{
		public object Get (string key)
		{
			throw new NoSuchKeyException (key);
		}

		public void Set (string key, object o)
		{
		}

		public void AddNotify (string key, NotifyChangedHandler handler)
		{
		}
	}
}
