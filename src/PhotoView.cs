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
			if (current_photo == value)
				return;

			current_photo = value;
			Update ();
		}
	}

	private bool CurrentPhotoValid () {
		if (query == null || query.Photos.Length == 0 || current_photo >= Query.Photos.Length)
			return false;

		return true;
	}

	private PhotoStore photo_store;

	private PhotoQuery query;
	public PhotoQuery Query {
		get {
			return query;
		}

		set {
			query = value;
			query.Reload += HandleQueryReload;

			// FIXME which picture to display?
			current_photo = 0;
			Update ();
		}
	}
	
	private void HandleQueryReload (PhotoQuery query) {
		Update ();
	}

	private FSpot.ImageView image_view;
	private TagView tag_view;
	private Button display_next_button, display_previous_button;
	private Label count_label;
	private Entry description_entry;

	private const double MAX_ZOOM = 5.0;

	private double zoom;
	public double Zoom {
		get {
			return zoom;
		}

		set {
			if ((value < 0.0) || (value > MAX_ZOOM))
				throw new Exception (String.Format ("Zoom value out of range {0}", value));
				
			zoom = value;
			UpdateZoom ();
		}
	}


	// Public events.

	public delegate void PhotoChangedHandler (PhotoView me);
	public event PhotoChangedHandler PhotoChanged;

	public delegate void UpdateStartedHandler (PhotoView view);
	public event UpdateStartedHandler UpdateStarted;

	public delegate void UpdateFinishedHandler (PhotoView view);
	public event UpdateFinishedHandler UpdateFinished;

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
		if (CurrentPhotoValid ()) {
			try {
				Pixbuf old = image_view.Pixbuf;
				
				image_view.Pixbuf = FSpot.PhotoLoader.Load (Query, current_photo);
				tag_view.Current = Query.Photos [current_photo];
				/*
				** This is a hack seeing if the max size stuff acutally helps loading speed 
				**
				*/
				/*
				 if (image_view.Allocation.Width > 0 && image_view.Allocation.Height > 0)
					image_view.Pixbuf = PixbufUtils.LoadAtMaxSize (Query.Photos [current_photo].DefaultVersionPath, 
										       image_view.Allocation.Width, 
										       image_view.Allocation.Height);
				
				*/

				if (old != null)
					old.Dispose ();
			} catch (GException ex) {
				// FIXME
				image_view.Pixbuf = null;
			}
		} else {
			image_view.Pixbuf = null;
		}

		//System.GC.Collect ();
		image_view.UnsetSelection ();
		UpdateZoom ();
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

		double image_zoom = (MAX_ZOOM - zoom_to_fit) * Zoom + zoom_to_fit;
		Console.WriteLine ("Zoom {2} zoom_to_fit {0} image_zoom {1}", zoom_to_fit, image_zoom, Zoom);

		if (Math.Abs (Zoom) < EPSILON)
			((ScrolledWindow) image_view.Parent).SetPolicy (PolicyType.Never, PolicyType.Never);

		image_view.SetZoom (image_zoom, image_zoom);

		if (Math.Abs (Zoom) < EPSILON && restore_scrollbars_idle_id == 0)
			restore_scrollbars_idle_id = Idle.Add (new IdleHandler (IdleUpdateScrollbars));
	}

	private void UpdateButtonSensitivity ()
	{
		bool prev = CurrentPhotoValid () && current_photo > 0;
		bool next = CurrentPhotoValid () && current_photo < query.Photos.Length -1;

		display_previous_button.Sensitive = prev;
		display_next_button.Sensitive = next;
	}

	private void UpdateCountLabel ()
	{
		if (query == null)
			count_label.Text = "";
		else {
			if (Query.Photos.Length == 0)
				count_label.Text = String.Format ("{0} of {1}", 0, 0);
			else 
				count_label.Text = String.Format ("{0} of {1}", current_photo + 1, Query.Photos.Length);
		}
	}

	private void UpdateDescriptionEntry ()
	{
		if (Query.Photos.Length > 1 && current_photo < Query.Photos.Length) {
			description_entry.Sensitive = true;
			description_entry.Text = Query.Photos[current_photo].Description;
		} else {
			description_entry.Sensitive = false;
			description_entry.Text = "";
		}
	}    

	public void Update ()
	{
		if (UpdateStarted != null)
			UpdateStarted (this);

		UpdateImageView ();
		UpdateButtonSensitivity ();
		UpdateCountLabel ();
		UpdateDescriptionEntry ();

		if (UpdateFinished != null)
			UpdateFinished (this);
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
	private void HandleButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		if (args.Event.Type == EventType.ButtonPress
		    && args.Event.Button == 3) {
			PhotoPopup popup = new PhotoPopup ();
			popup.Activate (args.Event);
		}
	}

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
		//image_view.Pixbuf = PixbufUtils.UnsharpMask (image_view.Pixbuf, 6, 2, 0);
		//image_view.Pixbuf = PixbufUtils.ColorCorrect (image_view.Pixbuf);
		new ColorDialog (image_view.Pixbuf);
	}	

	private void HandleDescriptionChanged (object sender, EventArgs args) {
		if (!CurrentPhotoValid ())
			return;

		Photo photo = Query.Photos[current_photo];

		photo.Description = description_entry.Text;
		photo_store.Commit (photo);
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

		Frame frame = new Frame ();
		frame.ShadowType = ShadowType.In;
		vbox.PackStart (frame, true, true, 0);
		
		Box inner_vbox = new VBox (false , 2);
		frame.Add (inner_vbox);

		image_view = new FSpot.ImageView ();
		ScrolledWindow image_view_scrolled = new ScrolledWindow (null, null);
		image_view_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
		image_view_scrolled.ShadowType = ShadowType.None;
		image_view_scrolled.Add (image_view);
		image_view_scrolled.ButtonPressEvent += HandleButtonPressEvent;
		image_view.SizeAllocated += new SizeAllocatedHandler (HandleImageViewSizeAllocated);
		image_view.AddEvents ((int) EventMask.KeyPressMask);
		image_view.KeyPressEvent += new KeyPressEventHandler (HandleImageViewKeyPressEvent);
		inner_vbox.PackStart (image_view_scrolled, true, true, 0);
		
		HBox inner_hbox = new HBox (false, 2);
		inner_hbox.BorderWidth = 6;

		tag_view = new TagView ();
		inner_hbox.PackStart (tag_view, false, true, 0);

		description_entry = new Entry ();
		inner_hbox.PackStart (description_entry, true, true, 0);
		description_entry.Changed += new EventHandler (HandleDescriptionChanged);
		
		inner_vbox.PackStart (inner_hbox, false, true, 0);

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

		/* Spacer Label */
		toolbar_hbox.PackStart (new Label (""), true, true, 0);

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

