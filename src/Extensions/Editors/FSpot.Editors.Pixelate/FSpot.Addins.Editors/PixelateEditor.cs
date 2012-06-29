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
