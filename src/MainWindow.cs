using Gdk;
using Gtk;
using GtkSharp;
using Glade;
using Gnome;
using System;
using System.IO;
using System.Text;

using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class MainWindow {
        public static MainWindow Toplevel = null;

	Db db;
	public Db Database {
		get {
			return db;
		}
	}

	TagSelectionWidget tag_selection_widget;
	[Widget] Gtk.Window main_window;
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
	[Widget] MenuItem delete_from_drive;

	[Widget] MenuItem set_as_background;

	[Widget] MenuItem attach_tag;
	[Widget] MenuItem remove_tag;
	[Widget] MenuItem find_tag;

	PhotoVersionMenu versions_submenu;
	
	InfoBox info_box;
	IconView icon_view;
	PhotoView photo_view;
	PhotoQuery query;
	
	// Drag and Drop
	enum TargetType {
		UriList,
		TagList,
		PhotoList
	};

	private static TargetEntry [] icon_source_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
	};

	private static TargetEntry [] icon_dest_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-tags", 0, (uint) TargetType.TagList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
	};

	private static TargetEntry [] tag_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-tags", 0, (uint) TargetType.TagList),
	};

	private static TargetEntry [] tag_dest_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
	};

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
		
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "main_window", null);
		gui.Autoconnect (this);

		tag_selection_widget = new TagSelectionWidget (db.Tags);
		tag_selection_scrolled.Add (tag_selection_widget);
		
		tag_selection_widget.Selection.Changed += new EventHandler (HandleTagSelectionChanged);
		tag_selection_widget.SelectionChanged += new TagSelectionWidget.SelectionChangedHandler (OnTagSelectionChanged);
		tag_selection_widget.DragDataGet += new DragDataGetHandler (HandleTagSelectionDragDataGet);
		Gtk.Drag.SourceSet (tag_selection_widget, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    tag_target_table, DragAction.Copy | DragAction.Move);


		tag_selection_widget.DragDataReceived += new DragDataReceivedHandler (HandleTagSelectionDragDataReceived);
		tag_selection_widget.DragMotion += new DragMotionHandler (HandleTagSelectionDragMotion);
		Gtk.Drag.DestSet (tag_selection_widget, DestDefaults.All, tag_dest_target_table, 
				  DragAction.Copy); 

		info_box = new InfoBox ();
		info_box.VersionIdChanged += new InfoBox.VersionIdChangedHandler (HandleInfoBoxVersionIdChange);
		left_vbox.PackStart (info_box, false, true, 0);
		
		query = new PhotoQuery (db.Photos);
		
		icon_view = new IconView (query);
		icon_view_scrolled.Add (icon_view);
		icon_view.SelectionChanged += new IconView.SelectionChangedHandler (HandleSelectionChanged);
		icon_view.DoubleClicked += new IconView.DoubleClickedHandler (HandleDoubleClicked);
		
		Gtk.Drag.SourceSet (icon_view, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    icon_source_target_table, DragAction.Copy | DragAction.Move);
		
		icon_view.DragBegin += new DragBeginHandler (HandleIconViewDragBegin);
		icon_view.DragDataGet += new DragDataGetHandler (HandleIconViewDragDataGet);

		TagMenu menu = new TagMenu (attach_tag, db.Tags);
		menu.TagSelected += HandleAttachTagMenuSelected;

		menu = new TagMenu (find_tag, db.Tags);
		menu.TagSelected += HandleFindTagMenuSelected;

		PhotoTagMenu pmenu = new PhotoTagMenu ();
		pmenu.TagSelected += HandleRemoveTagMenuSelected;
		remove_tag.Submenu = pmenu;
		
		Gtk.Drag.DestSet (icon_view, DestDefaults.All, icon_dest_target_table, 
				  DragAction.Copy | DragAction.Move); 

		//		icon_view.DragLeave += new DragLeaveHandler (HandleIconViewDragLeave);
		icon_view.DragMotion += new DragMotionHandler (HandleIconViewDragMotion);
		icon_view.DragDrop += new DragDropHandler (HandleIconViewDragDrop);
		icon_view.DragDataReceived += new DragDataReceivedHandler (HandleIconViewDragDataReceived);

		photo_view = new PhotoView (query, db.Photos);
		photo_box.Add (photo_view);
		photo_view.PhotoChanged += new PhotoView.PhotoChangedHandler (HandlePhotoViewPhotoChanged);
		photo_view.ButtonPressEvent += new ButtonPressEventHandler (HandlePhotoViewButtonPressEvent);
		photo_view.UpdateStarted += new PhotoView.UpdateStartedHandler (HandlePhotoViewUpdateStarted);
		photo_view.UpdateFinished += new PhotoView.UpdateFinishedHandler (HandlePhotoViewUpdateFinished);

		Gtk.Drag.DestSet (photo_view, DestDefaults.All, tag_target_table, 
				  DragAction.Copy | DragAction.Move); 

		photo_view.DragMotion += new DragMotionHandler (HandlePhotoViewDragMotion);
		photo_view.DragDrop += new DragDropHandler (HandlePhotoViewDragDrop);
		photo_view.DragDataReceived += new DragDataReceivedHandler (HandlePhotoViewDragDataReceived);

		view_notebook.SwitchPage += new SwitchPageHandler (HandleViewNotebookSwitchPage);

		UpdateMenus ();
		main_window.ShowAll ();
		
		if (Toplevel == null)
			Toplevel = this;
	}

	
	private int [] SelectedIds () {
		int [] ids;
		switch (mode) {
		case ModeType.IconView:
			ids = icon_view.SelectedIdxs;
			break;
		default:
		case ModeType.PhotoView:
			ids = new int [1]; 
			ids [0] = photo_view.CurrentPhoto;
			break;
		}

		return ids;
	}
	

	//
	// Selection Interface
	//

	private Photo [] SelectedPhotos (int [] selected_ids)
	{
		Photo [] photo_list = new Photo [selected_ids.Length];
	
		int i = 0;
		foreach (int num in selected_ids)
			photo_list [i ++] = query.Photos [num];
		
		return photo_list;
	}

	private Photo [] SelectedPhotos () 
	{
		return SelectedPhotos (SelectedIds ());
	}


	//
	// Change Notification functions
	//

	private void InvalidateViews ()
	{
		icon_view.QueueDraw ();
		photo_view.Update ();
	}

	private void InvalidateViews (int num)
	{
		icon_view.InvalidateCell (num);
		if (num == photo_view.CurrentPhoto)
			photo_view.Update ();
	}

	private void UpdateViews (int num)
	{
		icon_view.UpdateThumbnail (num);
		if (num == photo_view.CurrentPhoto)
			photo_view.Update ();
	}
		
	//
	// Commands
	//

	private void RotateSelectedPictures (RotateCommand.Direction direction)
	{
		RotateCommand command = new RotateCommand (main_window);

		
		int [] selected_ids = SelectedIds ();
		if (command.Execute (direction, SelectedPhotos (selected_ids))) {
			foreach (int num in selected_ids)
				UpdateViews (num);
		}
	}

	//
	// Tag Selection Drag Handlers
	//

	public void HandleTagSelectionDragMotion (object o, DragMotionArgs args)
	{
		TreePath path;

		if (!tag_selection_widget.GetPathAtPos (args.X, args.Y, out path))
			return;

		tag_selection_widget.SetDragDestRow (path, Gtk.TreeViewDropPosition.IntoOrAfter);
	}

	public void HandleTagSelectionDragDataReceived (object o, DragDataReceivedArgs args)
	{
		Tag [] tags = new Tag [1];

		//FIXME this is a lame api, we need to fix the drop behaviour of these things
		tags [0] = tag_selection_widget.TagAtPosition(args.X, args.Y);

		if (tags [0] == null)
			return;

		switch (args.Info) {
		case (uint)TargetType.PhotoList:
			foreach (int num in SelectedIds ()) {
				Photo photo = query.Photos [num];
				
				photo.AddTag (tags);
				db.Photos.Commit (photo);
				InvalidateViews (num);
			}
			break;
		case (uint)TargetType.TagList:
			UriList list = new UriList (args.SelectionData);
			
			foreach (string path in list.ToLocalPaths ()) {
				Photo photo = db.Photos.GetByPath (path);
				
				// FIXME - at this point we should import the photo, and then continue
				if (photo == null)
					return;
				
				photo.AddTag (tags);
				db.Photos.Commit (photo);
			}
			InvalidateViews ();
			break;
		}
	}

	void HandleTagSelectionDragDataGet (object sender, DragDataGetArgs args)
	{		
		UriList list = new UriList (SelectedPhotos ());
		Byte [] data = Encoding.UTF8.GetBytes (list.ToString ());
		Atom [] targets = args.Context.Targets;
		
		args.SelectionData.Set (targets[0], 8, data, data.Length);
	}

	//
	// IconView Drag Handlers
	//

	void HandleIconViewDragBegin (object sender, DragBeginArgs args)
	{
		Photo [] photos = SelectedPhotos ();
		
		if (photos.Length > 0) {
			string thumbnail_path = Thumbnail.PathForUri ("file://" + photos[0].DefaultVersionPath, ThumbnailSize.Large);
			Pixbuf thumbnail = ThumbnailCache.Default.GetThumbnailForPath (thumbnail_path);
			if (thumbnail != null) {
				Gtk.Drag.SetIconPixbuf (args.Context, thumbnail, 0, 0);
			}
		}
	}

	void HandleIconViewDragDataGet (object sender, DragDataGetArgs args)
	{		
		UriList list = new UriList (SelectedPhotos ());
		Byte [] data = Encoding.UTF8.GetBytes (list.ToString ());
		Atom [] targets = args.Context.Targets;
		
		args.SelectionData.Set (targets[0], 8, data, data.Length);
	}

	void HandleIconViewDragDrop (object sender, DragDropArgs args)
	{
		Widget source = Gtk.Drag.GetSourceWidget (args.Context);
		
		//Console.WriteLine ("Drag Drop {0}", source == null ? "null" : source.TypeName);

		args.RetVal = true;
	}

	void HandleIconViewDragMotion (object sender, DragMotionArgs args)
	{
		Widget source = Gtk.Drag.GetSourceWidget (args.Context);

		//Console.WriteLine ("Drag Motion {0}", source == null ? "null" : source.TypeName);

		Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);
		args.RetVal = true;
	}

	void HandleIconViewDragDataReceived (object sender, DragDataReceivedArgs args)
	{
	 	Widget source = Gtk.Drag.GetSourceWidget (args.Context);     
		
		//Console.WriteLine ("IconView View Drag received {0} type {1}", source == null ? "null" : source.TypeName, (TargetType)args.Info);

		switch (args.Info) {
		case (uint)TargetType.TagList:
			HandleAttachTagCommand (sender, null);
			break;
		case (uint)TargetType.UriList:

			/* 
			 * If the drop is coming from inside f-spot then we don't want to import 
			 */
			if (sender != null)
				return;

			UriList list = new UriList (args.SelectionData);
			ImportCommand command = new ImportCommand ();
			command.ImportFromPaths (db.Photos, list.ToLocalPaths ());
			UpdateQuery ();
			break;
		}

		Gtk.Drag.Finish (args.Context, true, false, args.Time);
	}	


	//
	// IconView event handlers
	// 

	void HandleSelectionChanged (IconView view)
	{
		int [] selection = SelectedIds ();

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
		
		SwitchToPhotoViewMode ();
	}

	//
	// PhotoView event handlers.
	//

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

	void HandlePhotoViewUpdateStarted (PhotoView sender)
	{
		main_window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
		// FIXME: use gdk_display_flush() when available
		main_window.GdkWindow.Display.Sync ();
	}

	void HandlePhotoViewUpdateFinished (PhotoView sender)
	{
		main_window.GdkWindow.Cursor = null;
		// FIXME: use gdk_display_flush() when available
		main_window.GdkWindow.Display.Sync ();
	}

	//
	// PhotoView drag handlers.
	//

	void HandlePhotoViewDragDrop (object sender, DragDropArgs args)
	{
		Widget source = Gtk.Drag.GetSourceWidget (args.Context);
		
		//Console.WriteLine ("Drag Drop {0}", source == null ? "null" : source.TypeName);

		args.RetVal = true;
	}

	void HandlePhotoViewDragMotion (object sender, DragMotionArgs args)
	{
		Widget source = Gtk.Drag.GetSourceWidget (args.Context);

		//Console.WriteLine ("Drag Motion {0}", source == null ? "null" : source.TypeName);

		Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);
		args.RetVal = true;
	}

	void HandlePhotoViewDragDataReceived (object sender, DragDataReceivedArgs args)
	{
	 	Widget source = Gtk.Drag.GetSourceWidget (args.Context);     
		
		//Console.WriteLine ("Drag received {0}", source == null ? "null" : source.TypeName);

		HandleAttachTagCommand (sender, null);
		
		Gtk.Drag.Finish (args.Context, true, false, args.Time);
	}	

	//
	// TagMenu commands.
	//

	public void HandleTagMenuActivate (object sender, EventArgs args)
	{

		MenuItem parent = sender as MenuItem;
		if (parent != null && parent.Submenu is PhotoTagMenu) {
			PhotoTagMenu menu = (PhotoTagMenu) parent.Submenu;
			menu.Populate (SelectedPhotos ()); 
		}
	}

	public void HandleAttachTagMenuSelected (Tag t) 
	{
		foreach (int num in SelectedIds ()) {
			Photo photo = query.Photos [num];
			photo.AddTag (t);
			db.Photos.Commit (photo);
			
			InvalidateViews (num);
		}
	}
	
	void HandleFindTagMenuSelected (Tag t)
	{
		tag_selection_widget.TagSelection = new Tag [] {t};
	}

	public void HandleRemoveTagMenuSelected (Tag t)
	{
		foreach (int num in SelectedIds ()) {
			Photo photo = query.Photos [num];
			photo.RemoveTag (t);
			db.Photos.Commit (photo);
			
			InvalidateViews (num);
		}
	}

	//
	// Main menu commands
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
		Print.Pixbuf (ctx, image);
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

	void HandleSendMailCommand (object sender, EventArgs e)
	{
		StringBuilder url = new StringBuilder ("mailto:?subject=my%20photos");

		foreach (Photo p in SelectedPhotos ()) {
			url.Append ("&attachment=" + p.DefaultVersionPath);
		}

		Console.WriteLine (url.ToString ());

		Gnome.Url.Show (url.ToString ());
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

		if (cmd.Execute (db.Photos, CurrentPhoto, main_window)) {
			info_box.Update ();
			photo_view.Update ();
			icon_view.UpdateThumbnail (current_photo_idx);
			UpdateMenus ();
		}
	}

	void HandleDeleteVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Delete cmd = new PhotoVersionCommands.Delete ();

		if (cmd.Execute (db.Photos, CurrentPhoto, main_window)) {
			info_box.Update ();
			photo_view.Update ();
			icon_view.UpdateThumbnail (current_photo_idx);
			UpdateMenus ();
		}
	}

	void HandlePropertiesCommand (object obje, EventArgs args)
	{
		Photo [] photos = SelectedPhotos ();
		
	        long length = 0;

		foreach (Photo p in photos) {
			FileInfo fi = new FileInfo (p.DefaultVersionPath);

			length += fi.Length;
		}

		Console.WriteLine ("{0} Selected Photos : Total length = {1} - {2}kB - {3}MB", photos.Length, length, length / 1024, length / (1024 * 1024));
	}
		
	void HandleRenameVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Rename cmd = new PhotoVersionCommands.Rename ();

		if (cmd.Execute (db.Photos, CurrentPhoto, main_window)) {
			info_box.Update ();
			photo_view.Update ();
			icon_view.UpdateThumbnail (current_photo_idx);
			UpdateMenus ();
		}
	}

	void HandleCreateNewTagCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, main_window);
		if (command.Execute (TagCommands.TagType.Tag))
			tag_selection_widget.Update ();
	}

	void HandleCreateNewCategoryCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, main_window);
		if (command.Execute (TagCommands.TagType.Category))
			tag_selection_widget.Update ();
	}

	void HandleAttachTagCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight ();
		
		foreach (int num in SelectedIds ()) {
			Photo photo = query.Photos [num];
			
			photo.AddTag (tags);
			db.Photos.Commit (photo);
			InvalidateViews (num);
		}
	}

	void HandleRemoveTagCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight ();

		foreach (int num in SelectedIds ()) {
			Photo photo = query.Photos [num];
			photo.RemoveTag (tags);
			db.Photos.Commit (photo);
			
			InvalidateViews (num);
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

	void HandleViewFullscreen (object sender, EventArgs args)
	{

	}

        public void HandleDeleteCommand (object sender, EventArgs args)
        {
		foreach (Photo photo in SelectedPhotos ()) {
			foreach (uint id in photo.VersionIds) {
				Console.WriteLine (" path == {0}", photo.GetVersionPath (id)); 
				photo.DeleteVersion (id, true);
			}

			db.Photos.Remove (photo);
		}

		UpdateQuery ();
	}

	public void HandleRemoveCommand (object sender, EventArgs args)
	{
		foreach (Photo photo in SelectedPhotos ()) {
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
		ThumbnailCommand command = new ThumbnailCommand (main_window);

		int [] selected_ids = SelectedIds ();
		if (command.Execute (SelectedPhotos (selected_ids))) {
			foreach (int num in selected_ids)
				UpdateViews (num);
		}
	}

	public void HandleRotate90Command (object sender, EventArgs args)
	{
		RotateSelectedPictures (RotateCommand.Direction.Clockwise);
	}

	public void HandleRotate270Command (object sender, EventArgs args)
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
		main_window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
		main_window.GdkWindow.Display.Sync ();
		query.Tags = tag_selection_widget.TagSelection;
		main_window.GdkWindow.Cursor = null;
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

	//
	// Handle Main Menu 

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
		delete_from_drive.Sensitive = current_photos;
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

	void HandleViewNotebookSwitchPage (object sender, SwitchPageArgs args)
	{
		switch (view_notebook.CurrentPage) {
		case 0:
			icon_view.ScrollTo (photo_view.CurrentPhoto);
			mode = ModeType.IconView;
			break;
		case 1:
			mode = ModeType.PhotoView;
			if (current_photo_idx != PHOTO_IDX_NONE)
				photo_view.CurrentPhoto = current_photo_idx;
			else if (current_photos) {
				int [] selection = icon_view.SelectedIdxs;
				
				photo_view.CurrentPhoto = selection[0];
			}

			break;
		}
	}

	void SwitchToIconViewMode ()
	{
		view_notebook.CurrentPage = 0;
	}

	void SwitchToPhotoViewMode ()
	{
		view_notebook.CurrentPage = 1;
	}
}

