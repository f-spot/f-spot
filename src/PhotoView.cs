/*
 * FSpot.PhotoView.cs
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing
 *	Stephane Delcroix
 *
 * This is free software. See COPYING for details.
 */

using Gdk;
using GLib;
using Gtk;
using GtkSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using Mono.Unix;

using FSpot.Xmp;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;

namespace FSpot {
	public class PhotoView : EventBox {
		FSpot.Delay commit_delay; 
	
		private bool has_selection = false;
		private FSpot.PhotoImageView photo_view;
		private ScrolledWindow photo_view_scrolled;
		private EventBox background;
		
		private Filmstrip filmstrip;
	
		private Widgets.TagView tag_view;
		
		private Gtk.ToolButton display_next_button, display_previous_button;
		private Label count_label;
		private Entry description_entry;
		private Widgets.Rating rating;
	
		private Gtk.ToolButton crop_button;
		private Gtk.ToolButton redeye_button;
		private Gtk.ToolButton color_button;	
		private Gtk.ToolButton desaturate_button;
		private Gtk.ToolButton sepia_button;
		
		private TreeStore constraints_store;
		private ComboBox constraints_combo = new ComboBox ();
		private uint restore_scrollbars_idle_id;
	
		// Public events.
	
		public delegate void PhotoChangedHandler (PhotoView me);
		public event PhotoChangedHandler PhotoChanged;
	
		public delegate void UpdateStartedHandler (PhotoView view);
		public event UpdateStartedHandler UpdateStarted;
	
		public delegate void UpdateFinishedHandler (PhotoView view);
		public event UpdateFinishedHandler UpdateFinished;
	
		public enum ConstraintType {
			Normal,
			AddCustom,
			SameAsPhoto
		}

		private List<SelectionRatioDialog.SelectionConstraint> custom_constraints;
	
		private static SelectionRatioDialog.SelectionConstraint [] default_constraints = {
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("4 x 3 (Book)"), 4.0 / 3.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("4 x 6 (Postcard)"), 6.0 / 4.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("5 x 7 (L, 2L)"), 7.0 / 5.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("8 x 10"), 10.0 / 8.0),
			new SelectionRatioDialog.SelectionConstraint (Catalog.GetString ("Square"), 1.0)
		};
	
		public FSpot.PhotoImageView View {
			get { return photo_view; }
		}
	
		new public FSpot.BrowsablePointer Item {
			get { return photo_view.Item; }
		}
	
		private IBrowsableCollection query;
		public IBrowsableCollection Query {
			get { return query; }
			set { query = value; }
		}
	
		public double Zoom {
			get { return photo_view.Zoom; }
			set { photo_view.Zoom = value; }
		}
		
		public double NormalizedZoom {
			get { return photo_view.NormalizedZoom; }
			set { photo_view.NormalizedZoom = value; }
		}
	
		public void Reload ()
		{
			photo_view.Reload ();
		}
	
		private void UpdateButtonSensitivity ()
		{
			bool valid = photo_view.Item.IsValid;
			bool prev = valid && Item.Index > 0;
			bool next = valid && Item.Index < query.Count - 1;
	
			if (valid) {
				Gnome.Vfs.Uri vfs = new Gnome.Vfs.Uri (photo_view.Item.Current.DefaultVersionUri.ToString ());
				valid = vfs.Scheme == "file";
			}
	
			display_previous_button.Sensitive = prev;
			display_next_button.Sensitive = next;
	
			if (valid && has_selection) {
				crop_button.SetTooltip (tips, Catalog.GetString ("Crop photo to selected area"), String.Empty);
				redeye_button.SetTooltip (tips, Catalog.GetString ("Remove redeye from selected area"), String.Empty);
			} else {
				crop_button.SetTooltip (tips, Catalog.GetString ("Select an area to crop"), null);
				redeye_button.SetTooltip (tips, Catalog.GetString ("Select an area to remove redeye"), null);
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
				count_label.Text = String.Empty;
			else {
				if (query.Count == 0)
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
	
				string desc = Item.Current.Description;
				if (description_entry.Text != desc) {
					description_entry.Text = desc == null ? String.Empty : desc;
				}
			} else {
				description_entry.Sensitive = false;
				description_entry.Text = String.Empty;
			}
	
			tips.SetTip (description_entry, description_entry.Text ?? String.Empty, description_entry.Text ?? String.Empty);
	
			description_entry.Changed += HandleDescriptionChanged;
		}    
	
		public void UpdateRating ()
		{
			if (Item.IsValid)
				UpdateRating ((int)Item.Current.Rating);
		}

		public void UpdateRating (int r)
		{
			rating.Changed -= HandleRatingChanged;
			rating.Value = r;
			rating.Changed += HandleRatingChanged;
		}

		public void UpdateSelectionCombo ()
		{
			//constraints_combo.Active = 0;	
			TreeIter iter;
			if (constraints_combo.GetActiveIter (out iter)) {
				if (((ConstraintType)constraints_store.GetValue (iter, 3)) == ConstraintType.SameAsPhoto)
					constraints_combo.Active = 0;
			}
		}
	
		private void Update ()
		{
			if (UpdateStarted != null)
				UpdateStarted (this);
	
			UpdateButtonSensitivity ();
			UpdateCountLabel ();
			UpdateDescriptionEntry ();
			UpdateRating ();
			UpdateSelectionCombo ();

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
				popup.Activate (this.Toplevel, args.Event);
			}
		}
	
		protected override bool OnPopupMenu ()
		{
			PhotoPopup popup = new PhotoPopup ();
			popup.Activate (this.Toplevel);
			return true;
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
			string msg = Catalog.GetString ("Error editing photo");
			string desc = String.Format (Catalog.GetString ("Received exception \"{0}\". Unable to save photo {1}"),
						     e.Message, photo.Name);
			
			HigMessageDialog md = new HigMessageDialog ((Gtk.Window)this.Toplevel, DialogFlags.DestroyWithParent, 
								    Gtk.MessageType.Error, ButtonsType.Ok, 
								    msg,
								    desc);
			md.Run ();
			md.Destroy ();
		}
	
		private void HandleSepiaButtonClicked (object sender, EventArgs args)
		{
			PhotoQuery pq = query as PhotoQuery;
	
			if (pq == null)
				return;
	
			try {
				FSpot.SepiaTone sepia = new FSpot.SepiaTone ((Photo)View.Item.Current);
				sepia.Image = View.CompletePixbuf ();
				sepia.Adjust ();
				pq.Commit (Item.Index);
			} catch (System.Exception e) {
				ShowError (e, (Photo)View.Item.Current); 
			}
		}
	
		private void HandleDesaturateButtonClicked (object sender, EventArgs args)
		{
			PhotoQuery pq = query as PhotoQuery;
	
			if (pq == null)
				return;
	
			try {
				FSpot.Desaturate desaturate = new FSpot.Desaturate ((Photo) View.Item.Current);
				desaturate.Image = View.CompletePixbuf ();
				desaturate.Adjust ();
				pq.Commit (Item.Index);
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
				string msg = Catalog.GetString ("No selection available");
				string desc = Catalog.GetString ("This tool requires an active selection. Please select a region of the photo and try the operation again");
				
				HigMessageDialog md = new HigMessageDialog ((Gtk.Window)this.Toplevel, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error, ButtonsType.Ok, 
									    msg,
									    desc);
	
				md.Run ();
				md.Destroy ();
				return;
			}		
	
			Photo photo = (Photo)Item.Current;
			try {
				Pixbuf original_pixbuf = photo_view.CompletePixbuf ();
				if (original_pixbuf == null) {
					return;
				}
				
				Pixbuf edited;
				if (redeye) {
					Gdk.Rectangle area = new Gdk.Rectangle (x, y, width, height);
					edited = PixbufUtils.RemoveRedeye (original_pixbuf, 
									   area,
									   Preferences.Get<int> (Preferences.EDIT_REDEYE_THRESHOLD));
				} else { // Crop (I told you it was ugly)
					edited = new Pixbuf (original_pixbuf.Colorspace, 
							     original_pixbuf.HasAlpha, original_pixbuf.BitsPerSample,
							     width, height);
					
					original_pixbuf.CopyArea (x, y, width, height, edited, 0, 0);
				}
				
				bool create_version = photo.DefaultVersion.IsProtected;
				photo.SaveVersion (edited, create_version);
				((PhotoQuery)query).Commit (Item.Index);
	
				// FIXME the fact that the selection doesn't go away is a bug in ImageView, it should
				// be fixed there.
				photo_view.Pixbuf = edited;
				original_pixbuf.Dispose ();
				photo_view.UnsetSelection ();
	
				photo_view.Fit = true;
				
				if (PhotoChanged != null)
					PhotoChanged (this);
			} catch (System.Exception e) {
				ShowError (e, photo);
			}
		}
	
		private void HandleColorButtonClicked (object sender, EventArgs args) 
		{
			ColorDialog.CreateForView (photo_view);
		}	
	
		int changed_photo;
		private bool CommitPendingChanges ()
		{
			if (commit_delay.IsPending) {
				commit_delay.Stop ();
				((PhotoQuery)query).Commit (changed_photo);
			}
			return true;
		}
	
		private void HandleDescriptionChanged (object sender, EventArgs args) 
		{
			if (!Item.IsValid)
				return;
			
			((Photo)Item.Current).Description = description_entry.Text;
			
			if (commit_delay.IsPending)
				if (changed_photo == Item.Index)
					commit_delay.Stop ();
				else
					CommitPendingChanges ();
			
			tips.SetTip (description_entry, description_entry.Text, "This is a tip");
			changed_photo = Item.Index;
			commit_delay.Start ();
		}
	
		private void HandleRatingChanged (object o, EventArgs e)
		{
			if (!Item.IsValid)
				return;
	
			((Photo)Item.Current).Rating = (uint)(o as Widgets.Rating).Value;
	
			if (commit_delay.IsPending)
				if (changed_photo == Item.Index)
					commit_delay.Stop();
				else
					CommitPendingChanges ();
			changed_photo = Item.Index;
			commit_delay.Start ();
	 	}
	
		private void HandlePhotoChanged (FSpot.PhotoImageView view)
		{
			if (query is PhotoQuery) {
				CommitPendingChanges ();
			}
			
			tag_view.Current = Item.Current;
			Update ();
	
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
	
		public bool FilmStripVisibility {
			get { return filmstrip.Visible; }
			set { filmstrip.Visible = value; }
		}
	
		Gtk.Tooltips tips = new Gtk.Tooltips ();
	
		public PhotoView (IBrowsableCollection query)
			: base ()
		{
			this.query = query;
	
			commit_delay = new FSpot.Delay (1000, new GLib.IdleHandler (CommitPendingChanges));
			this.Destroyed += HandleDestroy;
	
			Name = "ImageContainer";
			Box vbox = new VBox (false, 6);
			Add (vbox);
	
		        background = new EventBox ();
			Frame frame = new Frame ();
			background.Add (frame);
	
			frame.ShadowType = ShadowType.In;
			vbox.PackStart (background, true, true, 0);
			
			Box inner_vbox = new VBox (false , 2);
	
			frame.Add (inner_vbox);
			
			BrowsablePointer bp = new BrowsablePointer (query, -1);
			photo_view = new FSpot.PhotoImageView (bp);
	
			filmstrip = new Filmstrip (bp);
			Gdk.Pixbuf bg = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, 1, 69);
			bg.Fill (0x00000000);
			filmstrip.BackgroundTile = bg;
			filmstrip.ThumbOffset = 1;
			filmstrip.Spacing = 4;
			inner_vbox.PackStart (filmstrip, false, false, 0);
	
			photo_view.PhotoChanged += HandlePhotoChanged;
			photo_view.SelectionChanged += HandleSelectionChanged;
	
			photo_view_scrolled = new ScrolledWindow (null, null);
	
			photo_view_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
			photo_view_scrolled.ShadowType = ShadowType.None;
			photo_view_scrolled.Add (photo_view);
			photo_view_scrolled.ButtonPressEvent += HandleButtonPressEvent;
			photo_view.AddEvents ((int) EventMask.KeyPressMask);
			inner_vbox.PackStart (photo_view_scrolled, true, true, 0);
			
			HBox inner_hbox = new HBox (false, 2);
			//inner_hbox.BorderWidth = 6;
	
			tag_view = new Widgets.TagView (MainWindow.ToolTips);
			inner_hbox.PackStart (tag_view, false, true, 0);
	
			Label comment = new Label (Catalog.GetString ("Comment:"));
			inner_hbox.PackStart (comment, false, false, 0);
			description_entry = new Entry ();
			inner_hbox.PackStart (description_entry, true, true, 0);
			description_entry.Changed += HandleDescriptionChanged;
	
			rating = new Widgets.Rating();
			inner_hbox.PackStart (rating, false, false, 0);
			rating.Changed += HandleRatingChanged;
	
			CellRendererText constraint_name_cell = new CellRendererText ();
			CellRendererPixbuf constraint_pix_cell = new CellRendererPixbuf ();
			constraints_combo.PackStart (constraint_name_cell, true);
			constraints_combo.PackStart (constraint_pix_cell, false);
			constraints_combo.SetCellDataFunc (constraint_name_cell, new CellLayoutDataFunc (ConstraintNameCellFunc));
			constraints_combo.SetCellDataFunc (constraint_pix_cell, new CellLayoutDataFunc (ConstraintPixCellFunc));
			constraints_combo.Changed += HandleConstraintsComboChanged;

			SetColors ();
			
			inner_vbox.PackStart (inner_hbox, false, true, 0);
	
			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			vbox.PackStart (toolbar, false, true, 0);
	
			ToolItem constraints_menu = new ToolItem ();
			LoadPreference (Preferences.CUSTOM_CROP_RATIOS);
			constraints_menu.Child = constraints_combo;
			constraints_combo.Active = 0;
			toolbar.Insert (constraints_menu, -1);	
			constraints_menu.SetTooltip (tips, Catalog.GetString ("Constrain the aspect ratio of the selection"), String.Empty);
	
			crop_button = GtkUtil.ToolButtonFromTheme ("crop", Catalog.GetString ("Crop"), false);
			toolbar.Insert (crop_button, -1);
			crop_button.Clicked += new EventHandler (HandleCropButtonClicked);
	
			redeye_button = GtkUtil.ToolButtonFromTheme ("red-eye-remove", Catalog.GetString ("Reduce Red-Eye"), false);
			toolbar.Insert (redeye_button, -1);
			redeye_button.Clicked += new EventHandler (HandleRedEyeButtonClicked);
	
			color_button = GtkUtil.ToolButtonFromTheme ("adjust-colors", Catalog.GetString ("Adjust Colors"), false);
			toolbar.Insert (color_button, -1);
			color_button.SetTooltip (tips, Catalog.GetString ("Adjust the photo colors"), String.Empty);
			color_button.Clicked += new EventHandler (HandleColorButtonClicked);
	
			desaturate_button = GtkUtil.ToolButtonFromTheme ("color-desaturate", Catalog.GetString ("Desaturate"), false);
			toolbar.Insert (desaturate_button, -1);
			desaturate_button.SetTooltip (tips, Catalog.GetString ("Convert the photo to black and white"), String.Empty);
			desaturate_button.Clicked += HandleDesaturateButtonClicked;
	
			sepia_button = GtkUtil.ToolButtonFromTheme ("color-sepia", Catalog.GetString ("Sepia Tone"), false);
			toolbar.Insert (sepia_button, -1);
			sepia_button.SetTooltip (tips, Catalog.GetString ("Convert the photo to sepia tones"), String.Empty);
			sepia_button.Clicked += HandleSepiaButtonClicked;
	
			ItemAction straighten = new TiltEditorAction (photo_view);
			ToolButton straighten_btn = straighten.CreateToolItem () as ToolButton;
			straighten_btn.SetTooltip (tips, straighten.Tooltip, String.Empty);
			toolbar.Insert (straighten_btn, -1);
			
			ItemAction softfocus = new SoftFocusEditorAction (photo_view);
			ToolButton softfocus_btn = softfocus.CreateToolItem () as ToolButton;
			softfocus_btn.SetTooltip (tips, softfocus.Tooltip, String.Empty);
			toolbar.Insert (softfocus_btn, -1);
	
			ItemAction autocolor = new AutoColor (photo_view.Item);
			ToolButton autocolor_btn = autocolor.CreateToolItem () as ToolButton;
			autocolor_btn.SetTooltip (tips, autocolor.Tooltip, String.Empty);
			toolbar.Insert (autocolor_btn, -1);
	
			SeparatorToolItem white_space = new SeparatorToolItem ();
			white_space.Draw = false;
			white_space.Expand = true;
			toolbar.Insert (white_space, -1);
	
			ToolItem label_item = new ToolItem ();
			count_label = new Label (String.Empty);
			label_item.Child = count_label;
			toolbar.Insert (label_item, -1);
	
			display_previous_button = new ToolButton (Stock.GoBack);
			toolbar.Insert (display_previous_button, -1);
			display_previous_button.SetTooltip (tips, Catalog.GetString ("Previous photo"), String.Empty);
			display_previous_button.Clicked += new EventHandler (HandleDisplayPreviousButtonClicked);
	
			display_next_button = new ToolButton (Stock.GoForward);
			toolbar.Insert (display_next_button, -1);
			display_next_button.SetTooltip (tips, Catalog.GetString ("Next photo"), String.Empty);
			display_next_button.Clicked += new EventHandler (HandleDisplayNextButtonClicked);
	
			UpdateButtonSensitivity ();
	
			vbox.ShowAll ();
	
			Realized += delegate (object o, EventArgs e) {SetColors ();};
			Preferences.SettingChanged += OnPreferencesChanged;
		}
	
		private void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}
	
		private void LoadPreference (String key)
		{
			switch (key) {
			case Preferences.CUSTOM_CROP_RATIOS:
				custom_constraints = new List<SelectionRatioDialog.SelectionConstraint> ();
				if (Preferences.Get<string[]> (key) != null) {
					XmlSerializer serializer = new XmlSerializer (typeof(SelectionRatioDialog.SelectionConstraint));
					foreach (string xml in Preferences.Get<string[]> (key))
						custom_constraints.Add ((SelectionRatioDialog.SelectionConstraint)serializer.Deserialize (new StringReader (xml)));
				}
				PopulateConstraints ();
				break;
			}
		}

		private void PopulateConstraints()
		{
			constraints_store = new TreeStore (typeof (string), typeof (string), typeof (double), typeof (ConstraintType));
			constraints_combo.Model = constraints_store;
			constraints_store.AppendValues (null, Catalog.GetString ("No Constraint"), 0.0, ConstraintType.Normal);
			constraints_store.AppendValues (null, Catalog.GetString ("Same as photo"), 0.0, ConstraintType.SameAsPhoto);
			foreach (SelectionRatioDialog.SelectionConstraint constraint in custom_constraints)
				constraints_store.AppendValues (null, constraint.Label, constraint.XyRatio, ConstraintType.Normal);
			foreach (SelectionRatioDialog.SelectionConstraint constraint in default_constraints)
				constraints_store.AppendValues (null, constraint.Label, constraint.XyRatio, ConstraintType.Normal);
			constraints_store.AppendValues (Stock.Edit, Catalog.GetString ("Custom Ratios..."), 0.0, ConstraintType.AddCustom);
			constraints_combo.Active = 0;
		}

		void ConstraintNameCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string name = (string)tree_model.GetValue (iter, 1);
			(cell as CellRendererText).Text = name;	
		}

		void ConstraintPixCellFunc (CellLayout cell_layout, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			string stockname = (string)tree_model.GetValue (iter, 0);
			if (stockname != null)
				(cell as CellRendererPixbuf).Pixbuf = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, stockname, 16, (Gtk.IconLookupFlags)0);
			else
				(cell as CellRendererPixbuf).Pixbuf = null;
		}

		void HandleConstraintsComboChanged (object o, EventArgs e)
		{
			TreeIter iter;
			if (constraints_combo.GetActiveIter (out iter)) {
				double ratio = ((double)constraints_store.GetValue (iter, 2));
				ConstraintType type = ((ConstraintType)constraints_store.GetValue (iter, 3));
				switch (type) {
				case ConstraintType.Normal:
					photo_view.SelectionXyRatio = ratio;
					break;
				case ConstraintType.AddCustom:
					SelectionRatioDialog dialog = new SelectionRatioDialog ();
					dialog.Dialog.Run ();
					break;
				case ConstraintType.SameAsPhoto:
					try {
						Pixbuf pb = photo_view.CompletePixbuf ();
						photo_view.SelectionXyRatio = (double)pb.Width / (double)pb.Height;
					} catch (System.Exception ex) {
						Console.WriteLine (ex);
						photo_view.SelectionXyRatio = 0;
					}
					break;
				default:
					photo_view.SelectionXyRatio = 0;
					break;
				}	
			}	
		}

		private void SetColors ()
		{
			GtkUtil.ModifyColors (filmstrip);
			GtkUtil.ModifyColors (tag_view);
			GtkUtil.ModifyColors (photo_view);
			GtkUtil.ModifyColors (background);
			GtkUtil.ModifyColors (photo_view_scrolled);
			GtkUtil.ModifyColors (rating);
		}
	
		protected override void OnStyleSet (Style previous)
		{
			SetColors ();
		}
	}
}
