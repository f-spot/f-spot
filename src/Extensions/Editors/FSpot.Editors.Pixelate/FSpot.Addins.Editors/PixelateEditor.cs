//
// PixelateEditor.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2009 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;

using Gdk;
using Gtk;

using Mono.Unix;

namespace FSpot.Addins.Editors
{
	class PixelateEditor : Editor
	{
		public PixelateEditor () : base (Catalog.GetString ("Pixelate"), null) {
			CanHandleMultiple = false;
			NeedsSelection = true;
		}

		public override Widget ConfigurationWidget () {
			VBox vbox = new VBox ();

			Label info = new Label (Catalog.GetString ("Select the area that you want pixelated."));

			vbox.Add (info);

			return vbox;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
			Pixbuf output = input.Copy ();

			Pixbuf sub = new Pixbuf (output, State.Selection.X, State.Selection.Y,
					State.Selection.Width, State.Selection.Height);
			/* lazy man's pixelate: scale down and then back up */
			Pixbuf down = sub.ScaleSimple (State.Selection.Width/75, State.Selection.Height/75,
					InterpType.Nearest);
			Pixbuf up = down.ScaleSimple (State.Selection.Width, State.Selection.Height,
					InterpType.Nearest);
			up.CopyArea (0, 0, State.Selection.Width, State.Selection.Height, sub, 0, 0);
			return output;
		}
	}
}
