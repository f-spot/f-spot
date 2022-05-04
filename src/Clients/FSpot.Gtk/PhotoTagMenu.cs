//
// PhotoTagMenu.cs
//
// Author:
//   Larry Ewing <lewing@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2004-2006 Novell, Inc.
// Copyright (C) 2004, 2006 Larry Ewing
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.Widgets;

using Gtk;

namespace FSpot
{
	public class PhotoTagMenu : Menu
	{
		public delegate void TagSelectedHandler (Tag t);
		public event TagSelectedHandler TagSelected;

		public PhotoTagMenu ()
		{
		}

		protected PhotoTagMenu (IntPtr raw) : base (raw) { }

		public void Populate (IPhoto[] photos)
		{
			var dict = new Dictionary<uint, Tag> ();
			if (photos != null) {
				foreach (var p in photos) {
					foreach (var t in p.Tags) {
						if (!dict.ContainsKey (t.Id)) {
							dict.Add (t.Id, t);
						}
					}
				}
			}

			foreach (var w in Children) {
				w.Destroy ();
			}

			if (dict.Count == 0) {
				/* Fixme this should really set parent menu
				   items insensitve */
				var item = new MenuItem (Strings.ParenNoTagsParen);
				Append (item);
				item.Sensitive = false;
				item.ShowAll ();
				return;
			}

			foreach (var t in dict.Values) {
				MenuItem item = new TagMenu.TagMenuItem (t);
				Append (item);
				item.ShowAll ();
				item.Activated += HandleActivate;
			}

		}

		void HandleActivate (object obj, EventArgs args)
		{
			if (TagSelected != null) {
				var t = obj as TagMenu.TagMenuItem;
				if (t != null)
					TagSelected (t.Value);
				else
					Logger.Log.Debug ("Item was not a TagMenuItem");
			}
		}
	}
}