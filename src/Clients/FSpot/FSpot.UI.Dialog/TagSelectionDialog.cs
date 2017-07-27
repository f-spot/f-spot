//
// TagSelectionDialog.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Eric Faehnrich <misterfright@gmail.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2010 Eric Faehnrich
// Copyright (C) 2010 Ruben Vermeersch
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

using Gtk;

using FSpot.Core;
using FSpot.Database;

namespace FSpot.UI.Dialog
{
	public class TagSelectionDialog : BuilderDialog
	{
#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.ScrolledWindow tag_selection_scrolled;
#pragma warning restore 649

		TagSelectionWidget tag_selection_widget;

		public TagSelectionDialog (TagStore tags) : base ("tag_selection_dialog.ui", "tag_selection_dialog")
		{
			tag_selection_widget = new TagSelectionWidget (tags);
			tag_selection_scrolled.Add (tag_selection_widget);
			tag_selection_widget.Show ();
		}

		public new Tag[] Run ()
		{
			int response = base.Run ();
			if ((ResponseType) response == ResponseType.Ok)
				return tag_selection_widget.TagHighlight;

			return null;
		}

		public new void Hide ()
		{
			base.Hide ();
		}
	}
}
