/*
 * Filters/FilterRequest.cs
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 *
 */

using System;
using System.Collections;

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

		public FilterRequest (string path) : this (UriList.PathToFileUri (path))
		{
		}

		public Uri Source {
			get { return source; }
		}

		public Uri Current {
			get { return current; }
			set { 
				if (value != source && !temp_uris.Contains (value))
					temp_uris.Add (value);
				current = value; 
			}
		}

		public void Dispose ()
		{
			foreach (Uri uri in temp_uris) {
				System.IO.File.Delete (uri.LocalPath);
			}
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

			Uri uri = UriList.PathToFileUri (imgtemp);
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
