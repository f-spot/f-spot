//
// UriList.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Hyena;

namespace FSpot.Utils
{
	public class UriList : List<SafeUri>
	{
		public UriList ()
		{
		}

		public UriList (string data)
		{
			LoadFromStrings (data.Split ('\n'));
		}

		public UriList (IEnumerable<SafeUri> uris)
		{
			foreach (SafeUri uri in uris) {
				Add (uri);
			}
		}

		void LoadFromStrings (IEnumerable<string> items)
		{
			foreach (string i in items) {
				if (!i.StartsWith ("#")) {
					SafeUri uri;
					string s = i;

					if (i.EndsWith ("\r")) {
						s = i.Substring (0, i.Length - 1);
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

		public override string ToString ()
		{
			var list = new StringBuilder ();

			foreach (SafeUri uri in this) {
				if (uri == null)
					break;

				list.Append (uri + Environment.NewLine);
			}

			return list.ToString ();
		}
	}
}
