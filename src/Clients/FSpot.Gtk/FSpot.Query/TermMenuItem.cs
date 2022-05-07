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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Models;
using FSpot.Resources.Lang;
using FSpot.Utils;

namespace FSpot.Query
{
	public static class TermMenuItem
	{
		public static void Create (List<Tag> tags, Gtk.Menu menu)
		{
			var findWithString = Strings.FindWithMnemonic;
			var item = new Gtk.MenuItem (string.Format (findWithString, tags.Count));

			Gtk.Menu submenu = GetSubmenu (tags);
			if (submenu == null)
				item.Sensitive = false;
			else
				item.Submenu = submenu;

			menu.Append (item);
			item.Show ();
		}

		public static Gtk.Menu GetSubmenu (List<Tag> tags)
		{
			Tag single_tag = null;
			if (tags != null && tags.Count == 1)
				single_tag = tags[0];

			if (LogicWidget.Root == null || LogicWidget.Root.SubTerms.Count == 0) {
				return null;
			}

			var m = new Gtk.Menu ();

			Gtk.MenuItem all_item = GtkUtil.MakeMenuItem (m, Strings.All, new EventHandler (App.Instance.Organizer.HandleRequireTag));
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
						parts.Add (string.Format (Strings.NotSpaceX, literal.Tag.Name));
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
