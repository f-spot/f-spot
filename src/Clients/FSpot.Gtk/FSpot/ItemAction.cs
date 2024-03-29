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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Resources.Lang;
using FSpot.UI.Dialog;

using Gtk;

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
				var op = new RotateOperation (item.Current, direction);

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
			Strings.RotateLeft,
			Strings.RotatePictureLeft,
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
			Strings.RotateRight,
			Strings.RotatePictureRight,
			"object-rotate-right")
		{
		}
	}

	public class NextPictureAction : ItemAction
	{
		public NextPictureAction (BrowsablePointer p)
			: base (p,
			"NextPicture",
			Strings.Next,
			Strings.NextPicture,
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
			Strings.Previous,
			Strings.PreviousPicture,
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
