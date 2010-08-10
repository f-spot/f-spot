//
// Utility functions.
//
// Miguel de Icaza (miguel@ximian.com).
//
// (C) 2002 Ximian, Inc.
//
//

using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using Hyena;

using FSpot.Core;

namespace FSpot.Utils
{
	public class UriList : List<SafeUri> {
		public UriList (IBrowsableItem [] photos) {
			foreach (IBrowsableItem p in photos) {
				SafeUri uri;
				try {
					uri = p.DefaultVersion.Uri;
				} catch {
					continue;
				}
				Add (uri);
			}
		}

		public UriList () : base ()
		{
		}

		private void LoadFromStrings (string [] items) {
			foreach (String i in items) {
				if (!i.StartsWith ("#")) {
					SafeUri uri;
					String s = i;

					if (i.EndsWith ("\r")) {
						s = i.Substring (0, i.Length - 1);
						Log.DebugFormat ("uri = {0}", s);
					}

					try {
						uri = new SafeUri (s);
					} catch {
						continue;
					}
					Add (uri);
				}
			}
		}

		public void AddUnknown (string unknown)
		{
			Add (new SafeUri (unknown));
		}

		public UriList (string data)
		{
			LoadFromStrings (data.Split ('\n'));
		}

		public UriList (string [] uris)
		{
			LoadFromStrings (uris);
		}

		public void Add (IBrowsableItem item)
		{
			Add (item.DefaultVersion.Uri);
		}

		public override string ToString () {
			StringBuilder list = new StringBuilder ();

			foreach (SafeUri uri in this) {
				if (uri == null)
					break;

				list.Append (uri.ToString () + Environment.NewLine);
			}

			return list.ToString ();
		}
	}
}
