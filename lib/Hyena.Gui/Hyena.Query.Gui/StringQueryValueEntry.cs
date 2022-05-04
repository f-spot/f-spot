//
// StringQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Query.Gui
{
	public class StringQueryValueEntry : QueryValueEntry
	{
		protected Gtk.Entry entry;
		protected StringQueryValue query_value;

		public StringQueryValueEntry () : base ()
		{
			entry = new Entry ();
			entry.WidthRequest = DefaultWidth;
			entry.Changed += HandleChanged;
			Add (entry);
		}

		public override QueryValue QueryValue {
			get { return query_value; }
			set {
				entry.Changed -= HandleChanged;
				query_value = value as StringQueryValue;
				entry.Text = (query_value.Value as string) ?? string.Empty;
				entry.Changed += HandleChanged;
			}
		}

		protected void HandleChanged (object o, EventArgs args)
		{
			query_value.ParseUserQuery (entry.Text);
		}
	}
}
