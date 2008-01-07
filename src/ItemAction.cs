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
				
				item.Collection.MarkChanged (item.Index);
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
				Catalog.GetString ("Rotate picture left"),
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

	// FIXME this class is a hack to work around the considerable brokeness
	// in the gaps between Photo and IBrowsable* It helps but it shouldn't
	// be so introspective.
	internal class EditTarget {
		BrowsablePointer item;
		bool created_version = false;
		uint version;
		Photo photo;
		
		public EditTarget (BrowsablePointer item)
		{
			this.item = item;
			photo = item.Current as Photo;
			if (photo != null) {
				version = photo.DefaultVersionId;
				bool create = photo.DefaultVersion.IsProtected;
				
				if (create) {
					version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
					created_version = true;
				}
			}
		}
		
		public Uri Uri {
			get {
				if (photo != null)
					return photo.VersionUri (version);
				else 
					return item.Current.DefaultVersionUri;
			}
		}

		public void Commit ()
		{
			PhotoQuery q = item.Collection as PhotoQuery;
			if (photo != null && q != null) {
				photo.DefaultVersionId = version;
				q.Commit (item.Index);
			} else {
				item.Collection.MarkChanged (item.Index);
			}
		}
		
		public void Delete ()
		{
			if (created_version)
				photo.DeleteVersion (version);
		}
		
	}
	
	public class FilterAction : ItemAction {
		public FilterAction (BrowsablePointer pointer,
				     string name,
				     string label,
				     string tooltip,
				     string stock_id) : base (pointer, name, label, tooltip, stock_id)
		{
		}

		protected virtual IFilter BuildFilter ()
		{
			throw new ApplicationException ("No filter specified");
		}
		

		protected override void OnActivated ()
		{
			try {
				if (!item.IsValid)
					throw new ApplicationException ("attempt to filter invalid item");
				
				using (FilterRequest req = new FilterRequest (item.Current.DefaultVersionUri)) {
					IFilter filter = BuildFilter ();
					if (filter.Convert (req)) {
						// The filter did something so lets ve
						EditTarget target = new EditTarget (item);
					
						Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;
						result = Gnome.Vfs.Xfer.XferUri (new Gnome.Vfs.Uri (req.Current.ToString ()),
										 new Gnome.Vfs.Uri (target.Uri.ToString ()),
										 Gnome.Vfs.XferOptions.Default,
										 Gnome.Vfs.XferErrorMode.Abort, 
										 Gnome.Vfs.XferOverwriteMode.Replace, 
										 delegate {
											 System.Console.WriteLine ("progress");
											 return 1;
										 });
						
						if (result == Gnome.Vfs.Result.Ok) {
							System.Console.WriteLine ("Done modifying image");
							target.Commit ();
						} else {
							target.Delete ();
							throw new ApplicationException (String.Format (
												       "{0}: error moving to destination {1}",
												       this, target.ToString ()));
						}
					}
				}
			} catch (Exception e) {
				Dialog d = new EditExceptionDialog (null, e, item.Current);
				d.Show ();
				d.Run ();
				d.Destroy ();
			} 
		}
	}

	public class AutoColor : FilterAction {
		public AutoColor (BrowsablePointer p)
			: base (p, "Color", 
				Catalog.GetString ("Auto Color"),
				Catalog.GetString ("Automatically adjust the colors"),
				"autocolor")
		{
		}

		protected override IFilter BuildFilter ()
		{
			return new AutoStretch ();
		}
	}

	public class TiltAction : FilterAction {
		double angle;
		public TiltAction (BrowsablePointer p, double angle)
			: base (p, "ApplyStraighten", 
				Catalog.GetString ("Apply straightening"),
				Catalog.GetString ("Apply straightening to image"),
				"align-horizon")
		{
			this.angle = angle;
		}

		protected override IFilter BuildFilter ()
		{
			return new TiltFilter (angle);
		}
	}

	public class ViewAction : ItemAction {
		protected PhotoImageView view;

 		public ViewAction (PhotoImageView view,
				   string name,
				   string label,
				   string tooltip,
				   string stock_id) : base (view.Item, name, label, tooltip, stock_id)
		{
			this.view = view;
			view.Destroyed += HandleDestroyed;
		}

		private void HandleDestroyed (object sender, EventArgs args)
		{
			view = null;
			Sensitive = false;
		}
	}

	public class ViewEditorAction : ViewAction {
 		public ViewEditorAction (PhotoImageView view,
					 string name,
					 string label,
					 string tooltip,
					 string stock_id) : base (view, name, label, tooltip, stock_id)
		{
		}

		protected override void ItemChanged (BrowsablePointer p,
						     BrowsablePointerChangedArgs args)
		{
			Sensitive = item.IsValid && view.Editor == null;
		}
	}

	public class TiltEditorAction : ViewEditorAction {
		public TiltEditorAction (PhotoImageView view)
			: base (view, 
				"TiltEdit", 
				Catalog.GetString ("Straighten"),
				Catalog.GetString ("Adjust the angle of the image to straighten the horizon"),
				"align-horizon")
		{
		}

		protected override void OnActivated ()
		{
			view.Editor = new Editors.Tilt (view);
		}
	}

	public class SoftFocusEditorAction : ViewEditorAction {
		public SoftFocusEditorAction (PhotoImageView view)
			: base (view,
				"SoftFocusEdit",
				Catalog.GetString ("Soft Focus"),
				Catalog.GetString ("Create a soft focus visual effect"),
				"filter-soft-focus")
		{
		}

		protected override void OnActivated ()
		{
			view.Editor = new Editors.SoftFocus (view);
		}
	}
}
