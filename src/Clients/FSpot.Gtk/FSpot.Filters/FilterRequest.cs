//
// FilterRequest.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2006, 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using Hyena;

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

		// FIXME: We probably don't want this?
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
