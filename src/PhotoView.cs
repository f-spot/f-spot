using Gdk;
using GLib;
using Gtk;
using GtkSharp;
using System;

public class PhotoView : EventBox {

	private int current_photo;
	public int CurrentPhoto {
		get {
			return current_photo;
		}

		set {
			current_photo = value;
			Update ();
		}
	}

	private PhotoStore photo_store;

	private PhotoQuery query;
	public PhotoQuery Query {
		get {
			return query;
		}

		set {
			query = value;

			// FIXME which picture to display?
			current_photo = 0;
			Update ();
		}
	}

	private FSpot.ImageView image_view;
	private Button display_next_button, display_previous_button;
	private Label count_label;

	private const double MAX_ZOOM = 5.0;

	private double zoom;
	public double Zoom {
		get {
			return zoom;
		}

		set {
			if (zoom <= 0.0 || zoom > MAX_ZOOM)
				throw new Exception ("Zoom value out of range");
				
			zoom = value;
			UpdateZoom ();
		}
	}


	// Public events.

	public delegate void PhotoChangedHandler (PhotoView me);
	public event PhotoChangedHandler PhotoChanged;


	// Selection constraints.

	private const string CONSTRAINT_RATIO_IDX_KEY = "FEditModeManager::constraint_idx";

	private struct SelectionConstraint {
		public string Label;
		public double XyRatio;
	}

	private OptionMenu constraints_option_menu;
	private int selection_constraint_ratio_idx;

	// FIXME: Should initialize statically, not sure how you do it.
	// Maybe I should get myself a C# book at some point.
	private static SelectionConstraint [] constraints;

	private void HandleSelectionConstraintOptionMenuActivated (object sender, EventArgs args)
	{
		selection_constraint_ratio_idx = (int) (sender as GLib.Object).Data [CONSTRAINT_RATIO_IDX_KEY];
		image_view.SelectionXyRatio = constraints [selection_constraint_ratio_idx].XyRatio;
	}

	private OptionMenu CreateConstraintsOptionMenu ()
	{
		if (constraints == null) {
			constraints = new SelectionConstraint [10];

			constraints[0].Label = "No Constraint";
			constraints[0].XyRatio = 0.0;

			constraints[1].Label = "4 x 3 (Book)";
			constraints[1].XyRatio = 4.0 / 3.0;

			constraints[2].Label = "4 x 6 (Postcard)";
			constraints[2].XyRatio = 6.0 / 4.0;

			constraints[3].Label = "5 x 7 (L, 2L)";
			constraints[3].XyRatio = 7.0 / 5.0;

			constraints[4].Label = "8 x 10";
			constraints[4].XyRatio = 10.0 / 8.0;

			constraints[5].Label = "4 x 3 Portrait (Book)";
			constraints[5].XyRatio = 3.0 / 4.0;

			constraints[6].Label = "4 x 6 Portrait (Postcard)";
			constraints[6].XyRatio = 4.0 / 6.0;

			constraints[7].Label = "5 x 7 Portrait (L, 2L)";
			constraints[7].XyRatio = 5.0 / 7.0;

			constraints[8].Label = "8 x 10 Portrait";
			constraints[8].XyRatio = 8.0 / 10.0;

			constraints[9].Label = "Square";
			constraints[9].XyRatio = 1.0;
		}

		Menu menu = new Menu ();

		int i = 0;
		foreach (SelectionConstraint c in constraints) {
			MenuItem menu_item = new MenuItem (c.Label);
			menu_item.Show ();
			menu_item.Data.Add (CONSTRAINT_RATIO_IDX_KEY, i);
			menu_item.Activated += new EventHandler (HandleSelectionConstraintOptionMenuActivated);

			menu.Append (menu_item);
			i ++;
		}

		constraints_option_menu = new OptionMenu ();
		constraints_option_menu.Menu = menu;

		return constraints_option_menu;
	}


	// Display.

	private void UpdateImageView ()
	{
		if (Query == null || current_photo >= Query.Photos.Length)
			image_view.Pixbuf = null;
		else {
			try {
				image_view.Pixbuf = new Pixbuf (Query.Photos [current_photo].DefaultVersionPath);
			} catch (GException ex) {
				// FIXME
				image_view.Pixbuf = null;
			}
		}	

		image_view.UnsetSelection ();
		UpdateZoom ();

		// This is necessary otherwise it will take many pictures before the GC will kick in and in the
		// meantime memory occupation will grow out of control (since all the old pictures will stay in
		// memory).
		System.GC.Collect ();
	}

	private uint restore_scrollbars_idle_id;

	// FIXME need to remove this on dispose.
	private bool IdleUpdateScrollbars ()
	{
		(image_view.Parent as ScrolledWindow).SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

		restore_scrollbars_idle_id = 0;
		return false;
	}

	private void UpdateZoom ()
	{
		const double EPSILON = 1e-6; // FIXME: Can we use something in corlib instead of this?
		
		Pixbuf pixbuf = image_view.Pixbuf;
		if (pixbuf == null)
			return;

		int available_width = image_view.Allocation.Width;
		int available_height = image_view.Allocation.Height;

		double zoom_to_fit = ZoomUtils.FitToScale ((uint) available_width, (uint) available_height,
							   (uint) pixbuf.Width, (uint) pixbuf.Height, false);

		double zoom = (MAX_ZOOM - zoom_to_fit) * Zoom + zoom_to_fit;

		if (Math.Abs (Zoom) < EPSILON)
			((ScrolledWindow) image_view.Parent).SetPolicy (PolicyType.Never, PolicyType.Never);

		image_view.SetZoom (zoom, zoom);

		if (Math.Abs (Zoom) < EPSILON && restore_scrollbars_idle_id == 0)
			restore_scrollbars_idle_id = Idle.Add (new IdleHandler (IdleUpdateScrollbars));
	}

	private void UpdateButtonSensitivity ()
	{
		if (current_photo == 0)
			display_previous_button.Sensitive = false;
		else
			display_previous_button.Sensitive = true;

		if (Query == null || current_photo == Query.Photos.Length - 1)
			display_next_button.Sensitive = false;
		else
			display_next_button.Sensitive = true;
	}

	private void UpdateCountLabel ()
	{
		if (query == null)
			count_label.Text = "";
		else
			count_label.Text = String.Format ("{0} of {1}", current_photo + 1, Query.Photos.Length);
	}

	public void Update ()
	{
		UpdateImageView ();
		UpdateButtonSensitivity ();
		UpdateCountLabel ();
	}


	// Browsing.

	private void DisplayNext ()
	{
		if (Query.Photos.Length > 1 && current_photo < Query.Photos.Length - 1) {
			current_photo ++;
			Update ();
		}
	}

	private void DisplayPrevious ()
	{
		if (current_photo > 0) {
			current_photo --;
			Update ();
		}
	}


	// Event handlers.

	private void HandleImageViewKeyPressEvent (object sender, KeyPressEventArgs args)
	{
		switch (args.Event.Key) {
		case Gdk.Key.Left:
			HandleDisplayPreviousButtonClicked (sender, null);
			break;
		case Gdk.Key.Right:
			HandleDisplayNextButtonClicked (sender, null);
			break;
		default:
			args.RetVal = false;
			return;
		}

		args.RetVal = true;
		return;
	}

	private void HandleDisplayNextButtonClicked (object sender, EventArgs args)
	{
		DisplayNext ();

		if (PhotoChanged != null)
			PhotoChanged (this);
	}

	private void HandleDisplayPreviousButtonClicked (object sender, EventArgs args)
	{
		DisplayPrevious ();

		if (PhotoChanged != null)
			PhotoChanged (this);
	}

	private void HandleImageViewSizeAllocated (object sender, SizeAllocatedArgs args)
	{
		UpdateZoom ();
	}

	private void HandleCropButtonClicked (object sender, EventArgs args)
	{
		int x, y, width, height;
		if (! image_view.GetSelection (out x, out y, out width, out height))
			return;

		Photo photo = query.Photos [CurrentPhoto];
		if (photo.DefaultVersionId == Photo.OriginalVersionId) {
			photo.DefaultVersionId = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
			photo_store.Commit (photo);
		}

		Pixbuf original_pixbuf = image_view.Pixbuf;
		Pixbuf cropped_pixbuf = new Pixbuf (original_pixbuf.Colorspace, false, original_pixbuf.BitsPerSample,
						    width, height);

		original_pixbuf.CopyArea (x, y, width, height, cropped_pixbuf, 0, 0);
		
		image_view.Pixbuf = cropped_pixbuf;

		// FIXME the fact that the selection doesn't go away is a bug in ImageView, it should
		// be fixed there.
		image_view.UnsetSelection ();

		try {
			cropped_pixbuf.Savev (photo.DefaultVersionPath, "jpeg", null, null);
			PhotoStore.GenerateThumbnail (photo.DefaultVersionPath);
		} catch (GLib.GException ex) {
			// FIXME error dialog.
			Console.WriteLine ("error {0}", ex);
		}

		UpdateZoom ();

		if (PhotoChanged != null)
			PhotoChanged (this);
	}

	private void HandleUnsharpButtonClicked (object sender, EventArgs args) {
		image_view.Pixbuf = PixbufUtils.UnsharpMask (image_view.Pixbuf, 6, 2, 0);
	}	
	// Constructor.

	private class ToolbarButton : Button {
		public ToolbarButton ()
			: base ()
		{
			CanFocus = false;
			Relief = ReliefStyle.None;
		}
	}

	public PhotoView (PhotoStore photo_store)
		: base ()
	{
		this.photo_store = photo_store;

		BorderWidth = 3;

		Box vbox = new VBox (false, 6);
		Add (vbox);

		image_view = new FSpot.ImageView ();
		ScrolledWindow image_view_scrolled = new ScrolledWindow (null, null);
		image_view_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
		image_view_scrolled.ShadowType = ShadowType.In;
		image_view_scrolled.Add (image_view);
		image_view.SizeAllocated += new SizeAllocatedHandler (HandleImageViewSizeAllocated);
		image_view.AddEvents ((int) EventMask.KeyPressMask);
		image_view.KeyPressEvent += new KeyPressEventHandler (HandleImageViewKeyPressEvent);
		vbox.PackStart (image_view_scrolled, true, true, 0);

		Box toolbar_hbox = new HBox (false, 6);
		vbox.PackStart (toolbar_hbox, false, true, 0);

		toolbar_hbox.PackStart (CreateConstraintsOptionMenu (), false, false, 0);

		Button crop_button = new ToolbarButton ();
		Gtk.Image crop_button_icon = new Gtk.Image ("f-spot-crop", IconSize.Button);
		crop_button.Add (crop_button_icon);
		toolbar_hbox.PackStart (crop_button, false, true, 0);
	
		crop_button.Clicked += new EventHandler (HandleCropButtonClicked);

		Button unsharp_button = new ToolbarButton ();
		Gtk.Image unsharp_button_icon = new Gtk.Image ("f-spot-crop", IconSize.Button);
		unsharp_button.Add (unsharp_button_icon);
		toolbar_hbox.PackStart (unsharp_button, false, true, 0);
	
		unsharp_button.Clicked += new EventHandler (HandleUnsharpButtonClicked);

		toolbar_hbox.PackStart (new EventBox (), true, true, 0);

		count_label = new Label ("");
		toolbar_hbox.PackStart (count_label, false, true, 0);

		display_previous_button = new ToolbarButton ();
		Gtk.Image display_previous_image = new Gtk.Image (Stock.GoBack, IconSize.Button);
		display_previous_button.Add (display_previous_image);
		display_previous_button.Clicked += new EventHandler (HandleDisplayPreviousButtonClicked);
		toolbar_hbox.PackStart (display_previous_button, false, true, 0);

		display_next_button = new ToolbarButton ();
		Gtk.Image display_next_image = new Gtk.Image (Stock.GoForward, IconSize.Button);
		display_next_button.Add (display_next_image);
		display_next_button.Clicked += new EventHandler (HandleDisplayNextButtonClicked);
		toolbar_hbox.PackStart (display_next_button, false, true, 0);

		UpdateButtonSensitivity ();

		vbox.ShowAll ();
	}

	public PhotoView (PhotoQuery query, PhotoStore photo_store)
		: this (photo_store)
	{
		Query = query;
	}
}

