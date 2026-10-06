// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

using Hyena;

using FSpot;

namespace FSpot.Filters
{
	public class FilterRequest : IDisposable
	{
		SafeUri current;
		readonly List<SafeUri> temp_uris;

		public FilterRequest (SafeUri source)
		{
			Source = source;
			current = source;
			temp_uris = new List<SafeUri> ();
		}

		~FilterRequest ()
		{
			Close ();
		}

		public SafeUri Source { get; private set; }

		public SafeUri Current {
			get { return current; }
			set {
				if (!value.Equals (Source) && !temp_uris.Contains (value))
					temp_uris.Add (value);
				current = value;
			}
		}

		public virtual void Close ()
		{
			foreach (SafeUri uri in temp_uris) {
				try {
					File.Delete (uri.LocalPath);
				} catch (IOException e) {
					Logger.Log.Error (e, "");
				}
			}
			temp_uris.Clear ();
		}

		public void Dispose ()
		{
			Close ();
			GC.SuppressFinalize (this);
		}

		public SafeUri TempUri ()
		{
			return TempUri (null);
		}

		public SafeUri TempUri (string extension)
		{
			string imgtemp;
			if (extension != null) {
				string temp = Path.GetTempFileName ();
				imgtemp = temp + "." + extension;
				File.Move (temp, imgtemp);
			} else
				imgtemp = Path.GetTempFileName ();

			var uri = SafeUri.FromPath (imgtemp);
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
