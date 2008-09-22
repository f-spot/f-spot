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

using FSpot.Utils;

namespace FSpot.Filters {

	public class FilterRequest : IDisposable
	{
		Uri source;
		Uri current;

		ArrayList temp_uris;

		public FilterRequest (Uri source)
		{
			this.source = source;
			this.current = source;
			temp_uris = new ArrayList ();
		}

		public FilterRequest (string path) : this (UriUtils.PathToFileUri (path))
		{
		}

		~FilterRequest ()
		{
			Close ();
		}

		public Uri Source {
			get { return source; }
		}

		public Uri Current {
			get { return current; }
			set { 
				if (!value.Equals (source) && !temp_uris.Contains (value))
					temp_uris.Add (value);
				current = value; 
			}
		}

		public virtual void Close ()
		{
			foreach (Uri uri in temp_uris) {
				try {
					System.IO.File.Delete (uri.LocalPath);
				} catch (System.IO.IOException e) {
					System.Console.WriteLine (e);
				}
			}
			temp_uris.Clear ();
		}

		public void Dispose ()
		{
			Close ();
			System.GC.SuppressFinalize (this);
		}
		
		public Uri TempUri ()
		{
			return TempUri (null);
		}
		
		public Uri TempUri (string extension)
		{
			string imgtemp;
			if (extension != null) {
				string temp = System.IO.Path.GetTempFileName ();
				imgtemp = temp + "." + extension;
				System.IO.File.Move (temp, imgtemp);
			} else
				imgtemp = System.IO.Path.GetTempFileName ();

			Uri uri = UriUtils.PathToFileUri (imgtemp);
			if (!temp_uris.Contains (uri))
				temp_uris.Add (uri);
			return uri;
		}

		public void Preserve (Uri uri)
		{
			temp_uris.Remove (uri);
		}
	}
}
