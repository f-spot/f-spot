using Gdk;
using Gtk;
using GtkSharp;
using Glade;
using Gnome;
using System;
using System.IO;

using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class MainWindow {
	Db db;
	TagSelectionWidget tag_selection_widget;
	[Widget] Gtk.Window window1;
	[Widget] VBox left_vbox;
	[Widget] Toolbar main_toolbar;
	[Widget] ScrolledWindow icon_view_scrolled;
	[Widget] Box photo_box;
	[Widget] Notebook view_notebook;
	[Widget] ScrolledWindow tag_selection_scrolled;

	//
	// Menu items
	//
	[Widget] MenuItem version_menu_item;
	[Widget] MenuItem create_version_menu_item, delete_version_menu_item, rename_version_menu_item;

	[Widget] MenuItem delete_selected_tag;
	[Widget] MenuItem edit_selected_tag;

	[Widget] MenuItem attach_tag_to_selection;
	[Widget] MenuItem remove_tag_from_selection;

	[Widget] MenuItem copy;
	[Widget] MenuItem rotate_left;
	[Widget] MenuItem rotate_right;
	[Widget] MenuItem update_thumbnail;
	[Widget] MenuItem delete_from_catalog;

	[Widget] MenuItem set_as_background;

	PhotoVersionMenu versions_submenu;
	
	InfoBox info_box;
	IconView icon_view;
	PhotoView photo_view;
	PhotoQuery query;
	
	// Index into the PhotoQuery.  If -1, no photo is selected or multiple photos are selected.
	const int PHOTO_IDX_NONE = -1;
	private int current_photo_idx = PHOTO_IDX_NONE;
	private bool current_photos = false;

	private Photo CurrentPhoto {
		get {
			if (current_photo_idx != PHOTO_IDX_NONE)
				return query.Photos [current_photo_idx];
			else
				return null;
		}
	}

	//
	// Constructor
	//
	public MainWindow (Db db)
	{
		this.db = db;
		
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "window1", null);
		gui.Autoconnect (this);

		tag_selection_widget = new TagSelectionWidget (db.Tags);
		tag_selection_scrolled.Add (tag_selection_widget);
		
		tag_selection_widget.Selection.Changed += new EventHandler (HandleTagSelectionChanged);
		tag_selection_widget.SelectionChanged += new TagSelectionWidget.SelectionChangedHandler (OnTagSelectionChanged);

		info_box = new InfoBox ();
		info_box.VersionIdChanged += new InfoBox.VersionIdChangedHandler (HandleInfoBoxVersionIdChange);
		left_vbox.PackStart (info_box, false, true, 0);
		
		query = new PhotoQuery (db.Photos);
		
		icon_view = new IconView (query);
		icon_view_scrolled.Add (icon_view);
		icon_view.SelectionChanged += new IconView.SelectionChangedHandler (HandleSelectionChanged);
		icon_view.DoubleClicked += new IconView.DoubleClickedHandler (HandleDoubleClicked);
		
		photo_view = new PhotoView (query, db.Photos);
		photo_box.Add (photo_view);
		photo_view.PhotoChanged += new PhotoView.PhotoChangedHandler (HandlePhotoViewPhotoChanged);
		photo_view.ButtonPressEvent += new ButtonPressEventHandler (HandlePhotoViewButtonPressEvent);

		UpdateMenus ();
		window1.ShowAll ();
	}

	//
	// Commands
	//
	private Photo [] SelectedPhotos () {
		return SelectedPhotos (icon_view.Selection);
	}

	private Photo [] SelectedPhotos (int [] selected_ids)
	{
		Photo [] photo_list = new Photo [selected_ids.Length];
	
		int i = 0;
		foreach (int num in selected_ids)
			photo_list [i ++] = query.Photos [num];
		
		return photo_list;
	}

	private void RotateSelectedPictures (RotateCommand.Direction direction)
	{
		RotateCommand command = new RotateCommand (window1);

		switch (mode) {
		case ModeType.IconView:
			if (query.Photos.Length != 0) {
				int [] selected_ids = icon_view.Selection;

				if (command.Execute (direction, SelectedPhotos (selected_ids))) {
					foreach (int num in selected_ids)
						icon_view.UpdateThumbnail (num);
				}
			}
			break;

		case ModeType.PhotoView:
			Photo [] photo_list = new Photo [1];
			photo_list [0] = query.Photos [photo_view.CurrentPhoto];

			if (command.Execute (direction, photo_list)) {
				photo_view.Update ();
				icon_view.UpdateThumbnail (photo_view.CurrentPhoto);
			}
			break;
		}
	}

	// IconView events.

	void HandleSelectionChanged (IconView view)
	{
		int [] selection = icon_view.Selection;

		if (selection.Length == 1) {
			current_photo_idx = selection [0];
			info_box.Photo = CurrentPhoto;

		} else { 
			current_photo_idx = PHOTO_IDX_NONE;
			info_box.Photo = null;
		}

		current_photos = selection.Length > 0;
			
		UpdateMenus ();
	}

	void HandleDoubleClicked (IconView icon_view, int clicked_item)
	{
		SwitchToPhotoViewMode (clicked_item);
	}

	// PhotoView events.

	void HandlePhotoViewPhotoChanged (PhotoView sender)
	{
		current_photos = true;
		current_photo_idx = photo_view.CurrentPhoto;
		info_box.Photo = CurrentPhoto;
		UpdateMenus ();
	}

	void HandlePhotoViewButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		if (args.Event.Type == EventType.TwoButtonPress && args.Event.Button == 1)
			SwitchToIconViewMode ();
	}


	//
	// Menu commands.
	//

	void HandleImportCommand (object sender, EventArgs e)
	{
		ImportCommand command = new ImportCommand ();
		command.ImportFromFile (db.Photos);
		UpdateQuery ();
	}

	unsafe void HandlePrintCommand (object sender, EventArgs e)
	{
		PrintJob pj = new PrintJob (PrintConfig.Default ());
		PrintDialog dialog = new PrintDialog (pj, "Print Images", 0);
		int response = dialog.Run ();

		Console.WriteLine ("response: " + response);

		if (response == (int) PrintButtons.Cancel) {
			dialog.Destroy ();
		}

		PrintContext ctx = pj.Context;
		
		Print.Beginpage (ctx, "Test");
		
		Pixbuf image  = new Pixbuf (query.Photos[0].DefaultVersionPath);

		Print.Moveto (ctx, 100, 100);
		Print.Gsave (ctx);
		Print.Translate (ctx, 100, 100);
		Print.Scale (ctx, 100, 100);
		//Print.Pixbuf (ctx, image);
		Print.Grestore (ctx);

		Print.Show (ctx, "testing");
		Print.Showpage (ctx);
		
		pj.Close ();

		switch (response) {
		case (int) PrintButtons.Print:
			pj.Print ();
			break;
		case (int) PrintButtons.Preview:
			new PrintJobPreview (pj, "Testing").Show ();
			break;
		}

		dialog.Destroy ();
	}

	void HandleExportCommand (object sender, EventArgs e)
	{
		ExportCommand.Gallery cmd = new ExportCommand.Gallery ();

		if (cmd.Execute (SelectedPhotos ())) {
			Console.WriteLine ("success");
		}
	}	

	void HandleCloseCommand (object sender, EventArgs args)
	{
		// FIXME
		// Should use Application.Quit(), but for that to work we need to terminate the threads
		// first too.
		Environment.Exit (0);
	}
	
	void HandleCreateVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Create cmd = new PhotoVersionCommands.Create ();

		if (cmd.Execute (db.Photos, CurrentPhoto, window1)) {
			info_box.Update ();
			photo_view.Update ();
			icon_view.UpdateThumbnail (current_photo_idx);
			UpdateMenus ();
		}
	}

	void HandleDeleteVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Delete cmd = new PhotoVersionCommands.Delete ();

		if (cmd.Execute (db.Photos, CurrentPhoto, window1)) {
			info_box.Update ();
			photo_view.Update ();
			icon_view.UpdateThumbnail (current_photo_idx);
			UpdateMenus ();
		}
	}

	void HandleRenameVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Rename cmd = new PhotoVersionCommands.Rename ();

		if (cmd.Execute (db.Photos, CurrentPhoto, window1)) {
			info_box.Update ();
			photo_view.Update ();
			icon_view.UpdateThumbnail (current_photo_idx);
			UpdateMenus ();
		}
	}

	void HandleCreateNewTagCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, window1);
		if (command.Execute (TagCommands.TagType.Tag))
			tag_selection_widget.Update ();
	}

	void HandleCreateNewCategoryCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, window1);
		if (command.Execute (TagCommands.TagType.Category))
			tag_selection_widget.Update ();
	}


	void HandleAttachTagCommand (object obj, EventArgs args)
	{
		TreeModel model;
		TreeIter iter;

		Tag [] tags = this.tag_selection_widget.TagHighlight ();

		switch (mode) {
		case ModeType.IconView:
		if (query.Photos.Length != 0) {
			foreach (int num in icon_view.Selection) {
				Photo photo = query.Photos [num];
				
				photo.AddTag (tags);

				db.Photos.Commit (photo);
				icon_view.InvalidateCell (num);
			}
		}
		break;	
		case ModeType.PhotoView:
			Photo photo = query.Photos [photo_view.CurrentPhoto];
			
			photo.AddTag (tags);	
			db.Photos.Commit (photo);
			break;
		}	
	}

	void HandleRemoveTagCommand (object obj, EventArgs args)
	{
		TreeModel model;
		TreeIter iter;
	
		Tag [] tags = this.tag_selection_widget.TagHighlight ();

		switch (mode) {
		case ModeType.IconView:
		if (query.Photos.Length != 0) {
			foreach (int num in icon_view.Selection) {
				Photo photo = query.Photos [num];

				photo.RemoveTag (tags);
				db.Photos.Commit (photo);
				icon_view.InvalidateCell (num);
			}
		}
		break;	
		case ModeType.PhotoView:
			Photo photo = query.Photos [photo_view.CurrentPhoto];
			
			photo.RemoveTag (tags);
			db.Photos.Commit (photo);
			break;
		}	
	}
	
	void HandleViewSmall (object sender, EventArgs args)
	{
		icon_view.ThumbnailWidth = 64;	
	}

	void HandleViewMedium (object sender, EventArgs args)
	{
		icon_view.ThumbnailWidth = 128;	
	}

	void HandleViewLarge (object sender, EventArgs args)
	{
		icon_view.ThumbnailWidth = 256;	
	}

	void HandleViewSlideShow (object sender, EventArgs args)
	{
#if true
		Gtk.Window win = new Gtk.Window ("this is a test");
		win.SetSizeRequest (640, 480);
		SlideView slideview = new SlideView (SelectedPhotos());
		//win.Fullscreen();
		//win.Unfullscreen();
		win.Add (slideview);
		win.ShowAll ();
		slideview.Play ();
#else
		SlideCommands.Create command = new SlideCommands.Create (query.Photos);
		command.Execute ();
#endif
	}
	
        void HandleDeleteCommand (object sender, EventArgs args)
        {
		foreach (int num in icon_view.Selection) {
			Photo photo = query.Photos [num];
			
			db.Photos.Remove (photo);
		}
		UpdateQuery ();
	}

	void HandleSelectAllCommand (object sender, EventArgs args)
	{
		icon_view.SelectAllCells ();
	}

	void HandleSelectNoneCommand (object sender, EventArgs args)
	{
		icon_view.UnselectAllCells ();
	}
	
	void HandleDeleteSelectedTagCommand (object sender, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight ();
		
		db.Photos.Remove (tags);
		tag_selection_widget.Update ();
		icon_view.QueueDraw ();
	}

	void HandleUpdateThumbnailCommand (object sende, EventArgs args)
	{
		if (mode != ModeType.IconView)
			return;

		ThumbnailCommand command = new ThumbnailCommand (window1);
		int [] selected_ids = icon_view.Selection;

		if (command.Execute (SelectedPhotos (selected_ids))) {
			foreach (int num in selected_ids)
				icon_view.UpdateThumbnail (num);
		}
	}

	void HandleRotate90Command (object sender, EventArgs args)
	{
		RotateSelectedPictures (RotateCommand.Direction.Clockwise);
	}

	void HandleRotate270Command (object sender, EventArgs args)
	{
		RotateSelectedPictures (RotateCommand.Direction.Counterclockwise);

	}

	void HandleSetAsBackgroundCommand (object sender, EventArgs args)
	{
		Photo current = CurrentPhoto;
		GConf.Client client = new GConf.Client ();
		
		if (current == null)
			return;

		client.Set ("/desktop/gnome/background/color_shading_type", "solid");
		client.Set ("/desktop/gnome/background/primary_color", "#000000");
		client.Set ("/desktop/gnome/background/picture_options", "scaled");
		client.Set ("/desktop/gnome/background/picture_opacity", 100);
		client.Set ("/desktop/gnome/background/picture_filename", current.DefaultVersionPath);
		client.Set ("/desktop/gnome/background/draw_background", true);
	}

	// Version Id updates.

	void UpdateForVersionIdChange (uint version_id)
	{
		CurrentPhoto.DefaultVersionId = version_id;
		db.Photos.Commit (CurrentPhoto);

		info_box.Update ();
		photo_view.Update ();
		icon_view.UpdateThumbnail (current_photo_idx);
		UpdateMenus ();
	}

	void HandleVersionIdChanged (PhotoVersionMenu menu)
	{
		UpdateForVersionIdChange (menu.VersionId);
	}

	void HandleInfoBoxVersionIdChange (InfoBox box, uint version_id)
	{
		UpdateForVersionIdChange (version_id);
	}
	// Queries.

	void UpdateQuery ()
	{
		query.Tags = tag_selection_widget.TagSelection;
	}

	void OnTagSelectionChanged (object obj)
	{
		SwitchToIconViewMode ();
		UpdateQuery ();
	}
	
	void HandleTagSelectionChanged (object obj, EventArgs args)
	{
		UpdateMenus ();
	}

	void UpdateMenus ()
	{
		if (CurrentPhoto == null) {
			version_menu_item.Sensitive = false;
			version_menu_item.Submenu = new Menu ();

			create_version_menu_item.Sensitive = false;
			delete_version_menu_item.Sensitive = false;
			rename_version_menu_item.Sensitive = false;

			set_as_background.Sensitive = true;
		} else {
			version_menu_item.Sensitive = true;
			create_version_menu_item.Sensitive = true;
			
			if (CurrentPhoto.DefaultVersionId == Photo.OriginalVersionId) {
				delete_version_menu_item.Sensitive = false;
				rename_version_menu_item.Sensitive = false;
			} else {
				delete_version_menu_item.Sensitive = true;
				rename_version_menu_item.Sensitive = true;
			}

			versions_submenu = new PhotoVersionMenu (CurrentPhoto);
			versions_submenu.VersionIdChanged += new PhotoVersionMenu.VersionIdChangedHandler (HandleVersionIdChanged);
			version_menu_item.Submenu = versions_submenu;

			set_as_background.Sensitive = true;
		}

		bool tag_sensitive = tag_selection_widget.Selection.CountSelectedRows () > 0;
		
		rotate_left.Sensitive = current_photos;
		rotate_right.Sensitive = current_photos;
		update_thumbnail.Sensitive = current_photos;
		delete_from_catalog.Sensitive = current_photos;
		copy.Sensitive = current_photos;

		delete_selected_tag.Sensitive = tag_sensitive;
		attach_tag_to_selection.Sensitive = tag_sensitive && current_photos;
		remove_tag_from_selection.Sensitive = tag_sensitive && current_photos;
	}

	// Switching mode.

	enum ModeType {
		IconView,
		PhotoView
	};
	ModeType mode;

	void SwitchToIconViewMode ()
	{
		mode = ModeType.IconView;
		view_notebook.CurrentPage = 0;
	}

	void SwitchToPhotoViewMode (int photo_num)
	{
		mode = ModeType.PhotoView;
		view_notebook.CurrentPage = 1;
		photo_view.CurrentPhoto = photo_num;
	}
}

