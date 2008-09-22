/* 
 * ItemAction.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */
using Gtk;
using Mono.Unix;
using FSpot.Filters;
using System;
using FSpot.UI.Dialog;

namespace FSpot {
	public abstract class ItemAction : Gtk.Action {
		protected BrowsablePointer item;

		public ItemAction (BrowsablePointer pointer,
				   string name,
				   string label,
				   string tooltip,
				   string icon_name) : base (name, label)
		{
			Tooltip = tooltip;
#if GTK_2_10
			IconName = icon_name;
#endif
			item = pointer;
			item.Changed += ItemChanged;
		}

	        protected virtual void ItemChanged (BrowsablePointer sender, 
						    BrowsablePointerChangedArgs args)
		{
			Sensitive = item.IsValid;
		}

	}

	public class RotateAction : ItemAction {
		protected RotateDirection direction;
		
		public RotateAction (BrowsablePointer pointer,
				     RotateDirection direction,
				     string name,
				     string label,
				     string tooltip,
				     string stock_id) 
			: base (pointer, name, label, tooltip, stock_id)
		{
			this.direction = direction;
		}

		protected override void OnActivated ()
		{
			try {
				RotateOperation op = new RotateOperation (item.Current, direction);
				
				while (op.Step ());
				
				item.Collection.MarkChanged (item.Index, FullInvalidate.Instance);
			} catch (Exception e) {
				Dialog d = new EditExceptionDialog (null, e, item.Current);
				d.Show ();
				d.Run ();
				d.Destroy ();
			}
			   
		}
	}

	public class RotateLeftAction : RotateAction {
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

	public class RotateRightAction : RotateAction {
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

	public class NextPictureAction : ItemAction {
		public NextPictureAction (BrowsablePointer p)
			: base (p,
				"NextPicture",
				Catalog.GetString ("Next"),
				Catalog.GetString ("Next picture"),
				"gtk-go-forward-ltr")
		{
		}

		protected override void ItemChanged (BrowsablePointer p,
						     BrowsablePointerChangedArgs args)
		{
			Sensitive = item.Index < item.Collection.Count -1;
		}
		
		protected override void OnActivated ()
		{
			item.MoveNext ();
		}
	}

	public class PreviousPictureAction : ItemAction {
		public PreviousPictureAction (BrowsablePointer p)
			: base (p,
				"PreviousPicture",
				Catalog.GetString ("Previous"),
				Catalog.GetString ("Previous picture"),
				"gtk-go-back-ltr")
		{
		}

		protected override void ItemChanged (BrowsablePointer p,
						     BrowsablePointerChangedArgs args)
		{
			Sensitive =  item.Index > 0;
		}
		
		protected override void OnActivated ()
		{
			item.MovePrevious ();
		}
	}
}
