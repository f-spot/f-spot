using Gdk;
using GLib;
using Gtk;
using GtkSharp;
using System;
using Mono.Posix;
using FSpot.Xmp;

public class PhotoView : EventBox {
	FSpot.Delay description_delay; 

	private bool has_selection = false;

	public FSpot.PhotoImageView View {
		get {
			return photo_view;
		}
	}

	new public FSpot.BrowsablePointer Item {
		get {
			return photo_view.Item;
		}
	}

	private PhotoStore photo_store;

	private FSpot.PhotoQuery query;
	public FSpot.PhotoQuery Query {
		get {
			return query;
		}

		set {
			query = value;
		}
	}
	
	public void Reload ()
	{
		photo_view.Reload ();
	}

	private FSpot.PhotoImageView photo_view;
	private TagView tag_view;
	private Button display_next_button, display_previous_button;
	private Label count_label;
	private Entry description_entry;

	private Gtk.Button crop_button;
	private Gtk.Button redeye_button;
	private Gtk.Button color_button;
	
	Gtk.Button desaturate_button;
	Gtk.Button sepia_button;

	FSpot.AsyncPixbufLoader loader = new FSpot.AsyncPixbufLoader ();

	public double Zoom {
		get {
			return photo_view.Zoom;
		}

		set {
			photo_view.Zoom = value;
		}
	}
	
	public double NormalizedZoom {
		get {
			return photo_view.NormalizedZoom;
		}

		set {
			photo_view.NormalizedZoom = value;
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
		
		public SelectionConstraint (string label, double ratio)
		{
			Label = label;
			XyRatio = ratio;
		}
	}

	private OptionMenu constraints_option_menu;
	private int selection_constraint_ratio_idx;

	private static SelectionConstraint [] constraints = {
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("No Constraint"), 0.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("4 x 3 (Book)"), 4.0 / 3.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("4 x 6 (Postcard)"), 6.0 / 4.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("5 x 7 (L, 2L)"), 7.0 / 5.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("8 x 10"), 10.0 / 8.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("4 x 3 Portrait (Book)"), 3.0 / 4.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("4 x 6 Portrait (Postcard)"), 4.0 / 6.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("5 x 7 Portrait (L, 2L)"), 5.0 / 7.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("8 x 10 Portrait"), 8.0 / 10.0),
		new SelectionConstraint (Mono.Posix.Catalog.GetString ("Square"), 1.0)
	};

	private System.Collections.Hashtable constraint_table = new System.Collections.Hashtable ();

	private void HandleSelectionConstraintOptionMenuActivated (object sender, EventArgs args)
	{
		selection_constraint_ratio_idx = (int) constraint_table [sender];
		photo_view.SelectionXyRatio = constraints [selection_constraint_ratio_idx].XyRatio;
	}

	private OptionMenu CreateConstraintsOptionMenu ()
	{
		Menu menu = new Menu ();

		int i = 0;
		foreach (SelectionConstraint c in constraints) {
			MenuItem menu_item = new MenuItem (c.Label);
			menu_item.Show ();
			constraint_table [menu_item] = i;
			menu_item.Activated += new EventHandler (HandleSelectionConstraintOptionMenuActivated);

			menu.Append (menu_item);
			i ++;
		}

		constraints_option_menu = new OptionMenu ();
		constraints_option_menu.Menu = menu;

		return constraints_option_menu;
	}

	private uint restore_scrollbars_idle_id;


	private void UpdateButtonSensitivity ()
	{
		bool valid = photo_view.Item.IsValid;
		bool prev = valid && Item.Index > 0;
		bool next = valid && Item.Index < query.Count - 1;

		display_previous_button.Sensitive = prev;
		display_next_button.Sensitive = next;

		if (valid) {
			if (has_selection) {
				tips.SetTip (crop_button, Catalog.GetString ("Crop photo to selected area"), "");
				tips.SetTip (redeye_button, Catalog.GetString ("Remove redeye from selected area"), "");
			} else {
				tips.SetTip (crop_button, Catalog.GetString ("Select an area to crop"), null);
				tips.SetTip (redeye_button, Catalog.GetString ("Select an area to remove redeye"), null);
			}
		}

		crop_button.Sensitive = valid;
		redeye_button.Sensitive = valid;
		color_button.Sensitive = valid;
		desaturate_button.Sensitive = valid;
		sepia_button.Sensitive = valid;
	}

	private void UpdateCountLabel ()
	{
		if (query == null)
			count_label.Text = "";
		else {
			if (Query.Photos.Length == 0)
				count_label.Text = String.Format ("{0} of {1}", 0, 0);
			else 
				count_label.Text = String.Format ("{0} of {1}", Item.Index + 1, Query.Count);
		}
	}

	private void UpdateDescriptionEntry ()
	{
		description_entry.Changed -= HandleDescriptionChanged;
		if (Item.IsValid) {
			if (description_entry.Sensitive == false)
				description_entry.Sensitive = true;
			
			if (description_entry.Text != Item.Current.Description) {
				description_entry.Text = Item.Current.Description;
			}
		} else {
			description_entry.Sensitive = false;
			description_entry.Text = "";
		}

		if (description_entry.Text != "")
			tips.SetTip (description_entry, description_entry.Text, description_entry.Text);
		else
			tips.SetTip (description_entry, null, null);

		description_entry.Changed += HandleDescriptionChanged;
	}    

	private void Update ()
	{
		if (UpdateStarted != null)
			UpdateStarted (this);

		UpdateButtonSensitivity ();
		UpdateCountLabel ();
		UpdateDescriptionEntry ();

		if (UpdateFinished != null)
			UpdateFinished (this);
	}

	public void ZoomIn ()
	{
		photo_view.ZoomIn ();
	}
	
	public void ZoomOut ()
	{
		photo_view.ZoomOut ();
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

	private void HandleDisplayNextButtonClicked (object sender, EventArgs args)
	{
		View.Item.MoveNext ();
	}

	private void HandleDisplayPreviousButtonClicked (object sender, EventArgs args)
	{
		View.Item.MovePrevious ();
	}


	private void HandleRedEyeButtonClicked (object sender, EventArgs args)
	{
		ProcessImage (true);
	}
	
	private void HandleCropButtonClicked (object sender, EventArgs args)
	{
		ProcessImage (false);
	}
	
	private void ShowError (System.Exception e, Photo photo)
	{
		string msg = Mono.Posix.Catalog.GetString ("Error editing photo");
		string desc = String.Format (Mono.Posix.Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
					     e.Message, photo.Name);
		
		HigMessageDialog md = new HigMessageDialog ((Gtk.Window)this.Toplevel, DialogFlags.DestroyWithParent, 
							    Gtk.MessageType.Error, ButtonsType.Ok, 
							    msg,
							    desc);
		md.Run ();
		md.Destroy ();
		
		md.Run ();
		md.Destroy ();
	}

	private void HandleSepiaButtonClicked (object sender, EventArgs args)
	{
		try {
			FSpot.SepiaTone sepia = new FSpot.SepiaTone ((Photo)View.Item.Current);
			sepia.Pixbuf = View.CompletePixbuf ();
			sepia.Adjust ();
			query.Commit (Item.Index);
		} catch (System.Exception e) {
			ShowError (e, (Photo)View.Item.Current); 
		}
	}

	private void HandleDesaturateButtonClicked (object sender, EventArgs args)
	{
		try {
			FSpot.Desaturate desaturate = new FSpot.Desaturate ((Photo) View.Item.Current);
			desaturate.Pixbuf = View.CompletePixbuf ();
			desaturate.Adjust ();
			query.Commit (Item.Index);
		} catch (System.Exception e) {
			ShowError (e, (Photo)View.Item.Current);
		}
	}
	
	// FIXME this design sucks, I'm just doing it this way while
	// I redesign the editing system.
	private void ProcessImage (bool redeye)
	{
		int x, y, width, height;
		if (! photo_view.GetSelection (out x, out y, out width, out height)) {
			string msg = Mono.Posix.Catalog.GetString ("No selection available");
			string desc = Mono.Posix.Catalog.GetString ("This tool requires an active selection. Please select a region of the photo and try the operation again");
			
			HigMessageDialog md = new HigMessageDialog ((Gtk.Window)this.Toplevel, DialogFlags.DestroyWithParent, 
								    Gtk.MessageType.Error, ButtonsType.Ok, 
								    msg,
								    desc);

			md.Run ();
			md.Destroy ();
			return;
		}		

		Pixbuf original_pixbuf = photo_view.CompletePixbuf ();
		if (original_pixbuf == null) {
			return;
		}

		Photo photo = (Photo)Item.Current;

		Pixbuf edited;
		if (redeye) {
			Gdk.Rectangle area = new Gdk.Rectangle (x, y, width, height);
			edited = PixbufUtils.RemoveRedeye (original_pixbuf, 
							   area);
		} else { // Crop (I told you it was ugly)
			edited = new Pixbuf (original_pixbuf.Colorspace, 
					     original_pixbuf.HasAlpha, original_pixbuf.BitsPerSample,
					     width, height);
			
			original_pixbuf.CopyArea (x, y, width, height, edited, 0, 0);
		}


		// FIXME the fact that the selection doesn't go away is a bug in ImageView, it should
		// be fixed there.
		photo_view.Pixbuf = edited;
		photo_view.UnsetSelection ();

		try {
			bool create_version = photo.DefaultVersionId == Photo.OriginalVersionId;
			photo.SaveVersion (edited, create_version);
			query.Commit (Item.Index);
			query.MarkChanged (Item.Index);
		} catch (System.Exception e) {
			ShowError (e, photo);
		}
		
		photo_view.Fit = true;

		if (PhotoChanged != null)
			PhotoChanged (this);
	}

	private void HandleColorButtonClicked (object sender, EventArgs args) {
		new FSpot.ColorDialog (photo_view);
	}	

	int description_photo;
	private bool CommitPendingChanges ()
	{
		if (description_delay.IsPending) {
			description_delay.Stop ();
			Query.Commit (description_photo);
		}
		return true;
	}

	private void HandleDescriptionChanged (object sender, EventArgs args) {
		if (!Item.IsValid)
			return;

		((Photo)Item.Current).Description = description_entry.Text;

		if (description_delay.IsPending)
			if (description_photo == Item.Index)
				description_delay.Stop ();
			else
				CommitPendingChanges ();

		tips.SetTip (description_entry, description_entry.Text, "This is a tip");
		description_photo = Item.Index;
		description_delay.Start ();
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

	private void HandlePhotoChanged (FSpot.PhotoImageView view)
	{
		CommitPendingChanges ();
		Update ();

		tag_view.Current = (Photo)(Item.Current);

		if (this.PhotoChanged != null)
			PhotoChanged (this);
	}

	private void HandleSelectionChanged ()
	{
		int x, y, width, height;
		bool old = has_selection;
		has_selection = photo_view.GetSelection (out x, out y, out width, out height);
	
		if (has_selection != old)
			UpdateButtonSensitivity ();
	}

	private void HandleDestroy (object sender, System.EventArgs args)
	{
		CommitPendingChanges ();
	}

	Gtk.Tooltips tips = new Gtk.Tooltips ();

	public PhotoView (FSpot.PhotoQuery query, PhotoStore photo_store)
		: base ()
	{
		this.query = query;
		this.photo_store = photo_store;

		description_delay = new FSpot.Delay (1000, new GLib.IdleHandler (CommitPendingChanges));
		this.Destroyed += HandleDestroy;


		Box vbox = new VBox (false, 6);
		Add (vbox);

		EventBox eventbox = new EventBox ();
		Frame frame = new Frame ();
		eventbox.Add (frame);
		frame.ShadowType = ShadowType.In;
		vbox.PackStart (eventbox, true, true, 0);
		
		Box inner_vbox = new VBox (false , 2);

		frame.Add (inner_vbox);
		
		photo_view = new FSpot.PhotoImageView (query);
		photo_view.PhotoChanged += HandlePhotoChanged;
		photo_view.SelectionChanged += HandleSelectionChanged;

		ScrolledWindow photo_view_scrolled = new ScrolledWindow (null, null);


		FSpot.Global.ModifyColors (photo_view);
		FSpot.Global.ModifyColors (eventbox);
		FSpot.Global.ModifyColors (photo_view_scrolled);

		photo_view_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
		photo_view_scrolled.ShadowType = ShadowType.None;
		photo_view_scrolled.Add (photo_view);
		photo_view_scrolled.ButtonPressEvent += HandleButtonPressEvent;
		photo_view.AddEvents ((int) EventMask.KeyPressMask);
		inner_vbox.PackStart (photo_view_scrolled, true, true, 0);
		
		HBox inner_hbox = new HBox (false, 2);
		//inner_hbox.BorderWidth = 6;

		EventBox tag_view_box = new EventBox ();
		tag_view = new TagView (tag_view_box);
		tag_view_box.Add (tag_view);
		inner_hbox.PackStart (tag_view_box, false, true, 0);
		FSpot.Global.ModifyColors (tag_view_box);

		Label comment = new Label (Catalog.GetString ("Comment:"));
		inner_hbox.PackStart (comment, false, false, 0);
		description_entry = new Entry ();
		inner_hbox.PackStart (description_entry, true, true, 0);
		description_entry.Changed += HandleDescriptionChanged;
		
		inner_vbox.PackStart (inner_hbox, false, true, 0);

		Box toolbar_hbox = new HBox (false, 6);
		vbox.PackStart (toolbar_hbox, false, true, 0);

		toolbar_hbox.PackStart (CreateConstraintsOptionMenu (), false, false, 0);

		crop_button = new ToolbarButton ();
		crop_button.Add (new Gtk.Image ("f-spot-crop", IconSize.Button));
		toolbar_hbox.PackStart (crop_button, false, true, 0);
	
		crop_button.Clicked += new EventHandler (HandleCropButtonClicked);

		redeye_button = new ToolbarButton ();
		redeye_button.Add (new Gtk.Image ("f-spot-red-eye", IconSize.Button));
		toolbar_hbox.PackStart (redeye_button, false, true, 0);
	
		redeye_button.Clicked += new EventHandler (HandleRedEyeButtonClicked);

		color_button = new ToolbarButton ();
		color_button.Add (new Gtk.Image ("f-spot-adjust-colors", IconSize.Button));
		toolbar_hbox.PackStart (color_button, false, true, 0);
	
		color_button.Clicked += new EventHandler (HandleColorButtonClicked);

		desaturate_button = new ToolbarButton ();
		desaturate_button.Add (new Gtk.Image ("f-spot-desaturate", IconSize.Button));
		toolbar_hbox.PackStart (desaturate_button, false, true, 0);
		desaturate_button.Clicked += HandleDesaturateButtonClicked;

		sepia_button = new ToolbarButton ();
		sepia_button.Add (new Gtk.Image ("f-spot-sepia", IconSize.Button));
		toolbar_hbox.PackStart (sepia_button, false, true, 0);
		sepia_button.Clicked += HandleSepiaButtonClicked;

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

		tips.Enable ();


		UpdateButtonSensitivity ();

		vbox.ShowAll ();
		tips.SetTip (color_button, Mono.Posix.Catalog.GetString ("Adjust the photo colors"), "");
		tips.SetTip (constraints_option_menu, Mono.Posix.Catalog.GetString ("Constrain the aspect ratio of the selection"), "");
		tips.SetTip (display_next_button, Mono.Posix.Catalog.GetString ("Next photo"), "");
		tips.SetTip (display_previous_button, Mono.Posix.Catalog.GetString ("Previous photo"), "");
		tips.SetTip (desaturate_button, Mono.Posix.Catalog.GetString ("Convert the photo to black and white"), "");
		tips.SetTip (sepia_button, Mono.Posix.Catalog.GetString ("Convert the photo to sepia tones"), "");
	}

}

