//
// BlackoutEditor.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;

using Gdk;
using Gtk;

using Mono.Unix;

namespace FSpot.Addins.Editors
{
	class BlackoutEditor : Editor
	{
		public BlackoutEditor () : base (Catalog.GetString ("Blackout"), null) {
			CanHandleMultiple = false;
			NeedsSelection = true;
		}

		public override Widget ConfigurationWidget () {
			VBox vbox = new VBox ();

			Label info = new Label (Catalog.GetString ("Select the area that you want blacked out."));

			vbox.Add (info);

			return vbox;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
			Pixbuf output = input.Copy ();

			Pixbuf sub = new Pixbuf (output, State.Selection.X, State.Selection.Y,
					State.Selection.Width, State.Selection.Height);
			sub.Fill (0x00000000);
			return output;
		}
	}
}
