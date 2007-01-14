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

namespace FSpot {
	public abstract class ItemAction : Action {
		protected BrowsablePointer item;
		
		public ItemAction (BrowsablePointer pointer,
				   string name,
				   string label,
				   string tooltip,
				   string stock_id) : base (name, label, tooltip, stock_id)
		{
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
			RotateOperation op = new RotateOperation (item.Current, direction);

			while (op.Step ());

			item.Collection.MarkChanged (item.Index);
		}
	}

	public class RotateLeftAction : RotateAction {
		public RotateLeftAction (BrowsablePointer p) 
			: base (p,
				RotateDirection.Counterclockwise,
				"RotateItemLeft", 
				Catalog.GetString ("Rotate Left"), 
				Catalog.GetString ("Rotate picture left"),
				"f-spot-rotate-270")
		{
		}
	}

	public class RotateRightAction : RotateAction {
		public RotateRightAction (BrowsablePointer p) 
			: base (p,
				RotateDirection.Clockwise,
				"RotateItemRight", 
				Catalog.GetString ("Rotate Right"), 
				Catalog.GetString ("Rotate picture left"),
				"f-spot-rotate-90")
		{
		}
	}

	public class NextPictureAction : ItemAction {
		public NextPictureAction (BrowsablePointer p)
			: base (p,
				"NextPicture",
				Catalog.GetString ("Next"),
				Catalog.GetString ("Next picture"),
				Stock.GoForward)
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
				Stock.GoBack)
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
