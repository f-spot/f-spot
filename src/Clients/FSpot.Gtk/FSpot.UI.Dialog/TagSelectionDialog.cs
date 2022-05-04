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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Core;
using FSpot.Database;

using Gtk;

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

		public new List<Tag> Run ()
		{
			int response = base.Run ();
			if ((ResponseType)response == ResponseType.Ok)
				return tag_selection_widget.TagHighlight;

			return null;
		}

		public new void Hide ()
		{
			base.Hide ();
		}
	}
}
