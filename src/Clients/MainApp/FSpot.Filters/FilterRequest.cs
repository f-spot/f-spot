/*
 * Filters/FilterRequest.cs
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *   Larry Ewing <lewing@novell.com>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using System.Collections;

using Hyena;
using FSpot.Utils;

namespace FSpot.Filters {

	public class FilterRequest : IDisposable
	{
		SafeUri source;
		SafeUri current;

		ArrayList temp_uris;

		public FilterRequest (SafeUri source)
		{
			this.source = source;
			this.current = source;
			temp_uris = new ArrayList ();
		}

		~FilterRequest ()
		{
			Close ();
		}

		public SafeUri Source {
			get { return source; }
		}

		public SafeUri Current {
			get { return current; }
			set {
				if (!value.Equals (source) && !temp_uris.Contains (value))
					temp_uris.Add (value);
				current = value;
			}
		}

		public virtual void Close ()
		{
			foreach (SafeUri uri in temp_uris) {
				try {
					System.IO.File.Delete (uri.LocalPath);
				} catch (System.IO.IOException e) {
					Log.Exception (e);
				}
			}
			temp_uris.Clear ();
		}

		public void Dispose ()
		{
			Close ();
			System.GC.SuppressFinalize (this);
		}

		public SafeUri TempUri ()
		{
			return TempUri (null);
		}

		public SafeUri TempUri (string extension)
		{
			string imgtemp;
			if (extension != null) {
				string temp = System.IO.Path.GetTempFileName ();
				imgtemp = temp + "." + extension;
				System.IO.File.Move (temp, imgtemp);
			} else
				imgtemp = System.IO.Path.GetTempFileName ();

			SafeUri uri = new SafeUri (imgtemp);
			if (!temp_uris.Contains (uri))
				temp_uris.Add (uri);
			return uri;
		}

		public void Preserve (SafeUri uri)
		{
			temp_uris.Remove (uri);
		}
	}
}
