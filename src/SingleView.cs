using Gtk;
using System;

namespace FSpot {
	public class SingleView {
		[Glade.Widget] Gtk.HBox toolbar_hbox;
		[Glade.Widget] Gtk.VBox info_vbox;
		[Glade.Widget] Gtk.ScrolledWindow image_scrolled;
		[Glade.Widget] Gtk.ScrolledWindow directory_scrolled;
		[Glade.Widget] Gtk.HPaned info_hpaned;

		[Glade.Widget] Gtk.CheckMenuItem side_pane_item;
		[Glade.Widget] Gtk.CheckMenuItem toolbar_item;

		[Glade.Widget] Gtk.Image near_image;
		[Glade.Widget] Gtk.Image far_image;

		[Glade.Widget] Gtk.Scale zoom_scale;

		protected Glade.XML xml;
		private Gtk.Window window;
		PhotoImageView image_view;
		IconView directory_view;
		string path;

		DirectoryCollection collection;
		
		FSpot.Delay slide_delay;

		FullScreenView fsview;

		public SingleView () : this (FSpot.Global.HomeDirectory) {}

		public SingleView (string path) 
		{
			string glade_name = "single_view";
			this.path = path;
			
			xml = new Glade.XML (null, "f-spot.glade", glade_name, "f-spot");
			xml.Autoconnect (this);
			window = (Gtk.Window) xml.GetWidget (glade_name);

			Gtk.Toolbar toolbar = new Gtk.Toolbar ();
			toolbar_hbox.PackStart (toolbar);
			GtkUtil.MakeToolbarButton (toolbar, "f-spot-rotate-270", new System.EventHandler (HandleRotate270Command));
			GtkUtil.MakeToolbarButton (toolbar, "f-spot-rotate-90", new System.EventHandler (HandleRotate90Command));
			toolbar.AppendSpace ();

			collection = new DirectoryCollection (path);
			
			directory_view = new IconView (collection);
			directory_view.Selection.Changed += HandleSelectionChanged;
			directory_view.DisplayTags = false;
			directory_view.DisplayDates = false;
			directory_scrolled.Add (directory_view);

			image_view = new PhotoImageView (collection);
			FSpot.Global.ModifyColors (image_view);
			FSpot.Global.ModifyColors (image_scrolled);
			image_view.ZoomChanged += HandleZoomChanged;
			image_scrolled.Add (image_view);
			
			Window.ShowAll ();

			zoom_scale.ValueChanged += HandleZoomScaleValueChanged;
			
			ShowToolbar = true;
			ShowSidebar = collection.Count > 1;

			near_image.SetFromStock ("f-spot-stock_near", Gtk.IconSize.SmallToolbar);
			far_image.SetFromStock ("f-spot-stock_far", Gtk.IconSize.SmallToolbar);

			slide_delay = new FSpot.Delay (new GLib.IdleHandler (SlideShow));
			
			if (collection.Count > 0)
				directory_view.Selection.Add (0);
		}

		public bool ShowSidebar {
			get {
				return info_vbox.Visible;
			}
			set {
				info_vbox.Visible = value;
				if (side_pane_item.Active != value)
					side_pane_item.Active = value;
			}
		}
		
		public bool ShowToolbar {
			get {
				return toolbar_hbox.Visible;
			}
			set {
				toolbar_hbox.Visible = value;
				if (toolbar_item.Active != value)
					toolbar_item.Active = value;
			}
		}

		void HandleRotate90Command (object sender, System.EventArgs args) 
		{
			RotateCommand command = new RotateCommand (this.Window);
			if (command.Execute (RotateDirection.Clockwise, new IBrowsableItem [] { image_view.Item.Current })) {
				collection.MarkChanged (image_view.Item.Index);
			}
		}

		void HandleRotate270Command (object sender, System.EventArgs args) 
		{
			RotateCommand command = new RotateCommand (this.Window);
			if (command.Execute (RotateDirection.Counterclockwise, new IBrowsableItem [] { image_view.Item.Current })) {
				collection.MarkChanged (image_view.Item.Index);
			}
		}
		
		private void HandleSelectionChanged (FSpot.IBrowsableCollection selection) 
		{
			if (selection.Count > 0) {
				image_view.Item.Index = ((IconView.SelectionCollection)selection).Ids[0];
				zoom_scale.Value = image_view.Zoom;
			}
		}

		private void HandleViewToolbar (object sender, System.EventArgs args)
		{
			ShowToolbar = toolbar_item.Active;
		}
		
		private void HandleHideSidePane (object sender, System.EventArgs args) 
		{
			ShowSidebar = false;
		}

		private void HandleViewSidePane (object sender, System.EventArgs args)
		{
			ShowSidebar = side_pane_item.Active;
		}

		private void HandleViewSlideshow (object sender, System.EventArgs args)
		{
			this.Window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
			slide_delay.Start ();
		}
		
		private bool SlideShow ()
		{
			IBrowsableItem [] items = new IBrowsableItem [collection.Count];
			for (int i = 0; i < collection.Count; i++) {
				items [i] = collection [i];
			}

			FSpot.FullSlide full = new FSpot.FullSlide (Window, items);
			full.Play ();
			this.Window.GdkWindow.Cursor = null;
			return false;
		}

		private void HandleViewFullscreen (object sender, System.EventArgs args)
		{
			if (fsview != null)
				fsview.Destroy ();

			fsview = new FSpot.FullScreenView (collection);
			fsview.Destroyed += HandleFullScreenViewDestroy;

			fsview.View.Item.Index = image_view.Item.Index;
			fsview.Show ();
		}
		
		private void HandleFullScreenViewDestroy (object sender, System.EventArgs args)
		{
			directory_view.Selection.Clear ();
			if (fsview.View.Item.IsValid) 
				directory_view.Selection.Add (fsview.View.Item.Index);
			fsview = null;
		}

		private void HandleZoomScaleValueChanged (object sender, System.EventArgs args)
		{
			if (zoom_scale.Value != image_view.Zoom)
				image_view.Zoom = Math.Max (0.1, zoom_scale.Value);
		}

		private void HandleZoomChanged (object sender, System.EventArgs args)
		{
			if (zoom_scale.Value != image_view.Zoom)
				zoom_scale.Value = Math.Max (0.1, image_view.Zoom);
		}

		private void HandleFileClose (object sender, System.EventArgs args)
		{
			this.Window.Destroy ();
		}

		private void HandleFileOpen (object sender, System.EventArgs args)
		{
			string open = null;
			
			CompatFileChooserDialog file_selector =
				new CompatFileChooserDialog ("Open", this.Window,
							     CompatFileChooserDialog.Action.Open);
			
			file_selector.SelectMultiple = false;
			
			file_selector.Filename = path;
			
			int response = file_selector.Run ();
			
			if ((Gtk.ResponseType) response == Gtk.ResponseType.Ok) {
				open = file_selector.Filename;
				new FSpot.SingleView (open);
			}
			
			file_selector.Destroy ();
		}

		public Gtk.Window Window {
			get { 
				return window;
			}
		}
	}
}
