//
// ItemAction.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2007-2008 Larry Ewing
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

using Gtk;

using Mono.Unix;

using FSpot.Core;
using FSpot.UI.Dialog;

namespace FSpot
{
	public abstract class ItemAction : Gtk.Action
	{
		protected BrowsablePointer item;

		protected ItemAction (BrowsablePointer pointer, string name, string label, string tooltip, string icon_name) : base (name, label)
		{
			Tooltip = tooltip;
			IconName = icon_name;
			item = pointer;
			item.Changed += ItemChanged;
		}

		protected virtual void ItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			Sensitive = item.IsValid;
		}
	}

	public class RotateAction : ItemAction
	{
		protected RotateDirection direction;

		public RotateAction (BrowsablePointer pointer, RotateDirection direction, string name, string label, string tooltip, string stock_id)
		    : base (pointer, name, label, tooltip, stock_id)
		{
			this.direction = direction;
		}

		protected override void OnActivated ()
		{
			try {
				RotateOperation op = new RotateOperation (item.Current, direction);

				while (op.Step ()) { }

				item.Collection.MarkChanged (item.Index, FullInvalidate.Instance);
			} catch (Exception e) {
				Dialog d = new EditExceptionDialog (null, e, item.Current);
				d.Show ();
				d.Run ();
				d.Destroy ();
			}
		}
	}

	public class RotateLeftAction : RotateAction
	{
		public RotateLeftAction (BrowsablePointer p)
		    : base (p,
			RotateDirection.Counterclockwise,
			"RotateItemLeft",
			Catalog.GetString ("Rotate Left"),
			Catalog.GetString ("Rotate picture left"),
			"object-rotate-left")
		{
		}
	}

	public class RotateRightAction : RotateAction
	{
		public RotateRightAction (BrowsablePointer p)
		    : base (p,
			RotateDirection.Clockwise,
			"RotateItemRight",
			Catalog.GetString ("Rotate Right"),
			Catalog.GetString ("Rotate picture right"),
			"object-rotate-right")
		{
		}
	}

	public class NextPictureAction : ItemAction
	{
		public NextPictureAction (BrowsablePointer p)
		    : base (p,
			"NextPicture",
			Catalog.GetString ("Next"),
			Catalog.GetString ("Next picture"),
			"gtk-go-forward-ltr")
		{
		}

		protected override void ItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			Sensitive = item.Index < item.Collection.Count - 1;
		}

		protected override void OnActivated ()
		{
			item.MoveNext ();
		}
	}

	public class PreviousPictureAction : ItemAction
	{
		public PreviousPictureAction (BrowsablePointer p) : base (p,
			"PreviousPicture",
			Catalog.GetString ("Previous"),
			Catalog.GetString ("Previous picture"),
			"gtk-go-back-ltr")
		{
		}

		protected override void ItemChanged (object sender, BrowsablePointerChangedEventArgs args)
		{
			Sensitive = item.Index > 0;
		}

		protected override void OnActivated ()
		{
			item.MovePrevious ();
		}
	}
}
