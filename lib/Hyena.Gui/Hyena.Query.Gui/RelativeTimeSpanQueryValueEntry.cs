//
// RelativeTimeSpanQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Gtk;

namespace Hyena.Query.Gui
{
	public class RelativeTimeSpanQueryValueEntry : TimeSpanQueryValueEntry
	{
		public RelativeTimeSpanQueryValueEntry () : base ()
		{
			Add (new Label (Strings.Ago));
		}

		protected override void HandleValueChanged (object o, EventArgs args)
		{
			query_value.SetRelativeValue (-spin_button.ValueAsInt, factors[combo.Active]);
		}
	}
}
