//  TermMenuItem.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Larry Ewing <lewing@novell.com>
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2006-2007 Novell, Inc.
// Copyright (C) 2006 Larry Ewing
// Copyright (C) 2006-2007 Gabriel Burt
//
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;

using Mono.Unix;

using FSpot.Core;
using FSpot.Utils;

namespace FSpot.Query
{
	public static class TermMenuItem
	{
		public static void Create (Tag [] tags, Gtk.Menu menu)
		{
			var findWithString = Catalog.GetPluralString ("Find _With", "Find _With", tags.Length);
			var item = new Gtk.MenuItem (string.Format (findWithString, tags.Length));

			Gtk.Menu submenu = GetSubmenu (tags);
			if (submenu == null)
				item.Sensitive = false;
			else
				item.Submenu = submenu;

			menu.Append (item);
			item.Show ();
		}

		public static Gtk.Menu GetSubmenu (Tag [] tags)
		{
			Tag single_tag = null;
			if (tags != null && tags.Length == 1)
				single_tag = tags[0];

			if (LogicWidget.Root == null || LogicWidget.Root.SubTerms.Count == 0) {
				return null;
			}

			var m = new Gtk.Menu ();

			Gtk.MenuItem all_item = GtkUtil.MakeMenuItem (m, Catalog.GetString ("All"), new EventHandler (App.Instance.Organizer.HandleRequireTag));
			GtkUtil.MakeMenuSeparator (m);

			int sensitive_items = 0;
			foreach (Term term in LogicWidget.Root.SubTerms) {
				var term_parts = new List<string> ();

				bool contains_tag = AppendTerm (term_parts, term, single_tag);

				string name = "_" + string.Join (", ", term_parts.ToArray ());

				Gtk.MenuItem item = GtkUtil.MakeMenuItem (m, name, new EventHandler (App.Instance.Organizer.HandleAddTagToTerm));
				item.Sensitive = !contains_tag;

				if (!contains_tag)
					sensitive_items++;
			}

			if (sensitive_items == 0)
				all_item.Sensitive = false;

			return m;
		}

		static bool AppendTerm (List<string> parts, Term term, Tag singleTag)
		{
			bool tag_matches = false;
			if (term != null) {
				var literal = term as Literal;
				if (literal != null) {
					if (literal.Tag == singleTag)
						tag_matches = true;

					if (literal.IsNegated)
						parts.Add (string.Format (Catalog.GetString ("Not {0}"), literal.Tag.Name));
					else
						parts.Add (literal.Tag.Name);
				} else {
					foreach (Term subterm in term.SubTerms) {
						tag_matches |= AppendTerm (parts, subterm, singleTag);
					}
				}
			}
			return tag_matches;
		}
	}
}
