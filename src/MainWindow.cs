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
	[Widget] VBox group_vbox;
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
	FSpot.GroupSelector group_selector;
	
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
	
	struct YearCount {
		int Year;
		int Count;
	}

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

		tag_selection_widget.ButtonPressEvent += new ButtonPressEventHandler (HandleTagSelectionButtonPressEvent);

		info_box = new InfoBox ();
		info_box.VersionIdChanged += new InfoBox.VersionIdChangedHandler (HandleInfoBoxVersionIdChange);
		left_vbox.PackStart (info_box, false, true, 0);
		
		query = new PhotoQuery (db.Photos);

		group_selector = new FSpot.GroupSelector ();
		FSpot.GroupAdaptor adaptor = new FSpot.TimeAdaptor (query);
		//FSpot.GroupAdaptor adaptor = new FSpot.DirectoryAdaptor (query);		
		//group_selector.Mode = FSpot.GroupSelector.RangeType.Min;

		group_selector.Adaptor  = adaptor;
		group_selector.ShowAll ();

		group_vbox.PackStart (group_selector, false, false, 0);
		group_vbox.ReorderChild (group_selector, 0);
		
		icon_view = new IconView (query);
		icon_view_scrolled.Add (icon_view);
		icon_view.SelectionChanged += new IconView.SelectionChangedHandler (HandleSelectionChanged);
		icon_view.DoubleClicked += new IconView.DoubleClickedHandler (HandleDoubleClicked);
		icon_view.GrabFocus ();

		FSpot.PreviewPopup preview = new FSpot.PreviewPopup (icon_view);

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
		adaptor.GlassSet += HandleAdaptorGlassSet;

		UpdateMenus ();
		main_window.ShowAll ();
		
		if (Toplevel == null)
			Toplevel = this;
	}

	
	public int [] SelectedIds () {
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

	public void AddTagExtended (Photo photo, Tag [] tags)
	{
		photo.AddTag (tags);
		db.Photos.Commit (photo);

		foreach (Tag t in tags) {
			Pixbuf icon = null;

			if (t.Icon == null) {
				if (icon == null) {
					// FIXME this needs a lot more work.
					Pixbuf tmp = PixbufUtils.LoadAtMaxSize (photo.DefaultVersionPath, 128, 128);
					icon = PixbufUtils.TagIconFromPixbuf (tmp);
					tmp.Dispose ();
				}
				
				t.Icon = icon;
				db.Tags.Commit (t);
			}
		}
	}



	[GLib.ConnectBefore]
	void HandleTagSelectionButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		if (args.Event.Button == 3)
		{
			TreePath path;
			tag_selection_widget.Selection.UnselectAll ();
			if (tag_selection_widget.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path)) {
				tag_selection_widget.Selection.SelectPath (path);
			}
			TagPopup popup = new TagPopup ();
			popup.Activate (args.Event, tag_selection_widget.TagAtPosition ((int)args.Event.X, (int)args.Event.Y));
			args.RetVal = true;
		}
	}

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
				
				AddTagExtended (photo, tags);
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
				
				AddTagExtended (photo, tags);
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

	void HandleAdaptorGlassSet (FSpot.GroupAdaptor sender, int index)
	{
		switch (mode) {
		case ModeType.PhotoView:
			photo_view.CurrentPhoto = index;
			break;
		case ModeType.IconView:
			icon_view.ScrollTo (index);
			icon_view.Throb (index);
			break;
		}

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
				thumbnail.Dispose ();
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
			//
			// Translate the event args from viewport space to window space,
			// drag events use the viewport.  Owen sends his regrets.
			//
			int item = icon_view.CellAtPosition (args.X + (int) icon_view.Hadjustment.Value, 
							     args.Y + (int) icon_view.Vadjustment.Value);

			//Console.WriteLine ("Drop cell = {0} ({1},{2})", item, args.X, args.Y);

			if (icon_view.CellIsSelected (item))
				AttachTags (tag_selection_widget.TagHighlight (), SelectedIds());
			else 
				AttachTags (tag_selection_widget.TagHighlight (), new int [] {item});

			break;
		case (uint)TargetType.UriList:

			/* 
			 * If the drop is coming from inside f-spot then we don't want to import 
			 */
			if (source != null)
				return;

			UriList list = new UriList (args.SelectionData);
			ImportCommand command = new ImportCommand (main_window);
			command.ImportFromPaths (db.Photos, list.ToLocalPaths ());
			UpdateQuery ();
			break;
		}

		Gtk.Drag.Finish (args.Context, true, false, args.Time);
	}

#if false
	void HandleIconViewMotionNotifyEvent (object sender, MotionNotifyEventArgs args)
	{
		if ((args.Event.State & Gdk.ModifierType.Mod1Mask) == 0) {
			HideQuickPreview ();
			return;
		}

		int x = (int) args.Event.X;
		int y = (int) args.Event.Y;
		int cell_num = icon_view.CellAtPosition (x, y);
		
		int image_center_x, image_center_y;

		Rectangle bounds = icon_view.CellBounds (cell_num);
		image_center_x = bounds.X + (bounds.Width / 2);
		image_center_y = bounds.Y + (bounds.Height / 2);

		image_center_x += (int) args.Event.XRoot - x;
		image_center_y += (int) args.Event.YRoot - y;
		
		ShowQuickPreview (cell_num, image_center_x, image_center_y);
	}
#endif

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
		icon_view.FocusCell = clicked_item;
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
			AddTagExtended (photo, new Tag [] {t});
			
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
		ImportCommand command = new ImportCommand (main_window);
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
		double page_width, page_height;
		pj.GetPageSize (out page_width, out page_height);

		foreach (Photo photo in SelectedPhotos ()) {
			Print.Beginpage (ctx, "F-Spot "+ photo.DefaultVersionPath);
			
			Pixbuf image  = FSpot.PhotoLoader.Load (photo);
			double scale = Math.Min (page_width / image.Width, page_height / image.Height);
			
			//Print.Moveto (ctx, 100, 100);
			Print.Gsave (ctx);
			Print.Translate (ctx, 
					 (page_width - image.Width * scale) / 2.0, 
					 (page_height - image.Height * scale) / 2.0);
			Print.Scale (ctx, image.Width * scale, image.Height * scale);
			Print.Pixbuf (ctx, image);
			Print.Grestore (ctx);
			
			//Print.Show (ctx, photo.Description);
			Print.Showpage (ctx);
			image.Dispose ();
		}

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
			url.Append ("&attach=" + p.DefaultVersionPath);
		}

		Console.WriteLine (url.ToString ());

		Gnome.Url.Show (url.ToString ());
	}

	void HandleArrangeByTime (object sender, EventArgs e)
	{
		group_selector.Adaptor.GlassSet -= HandleAdaptorGlassSet;
		FSpot.GroupAdaptor adaptor = new FSpot.TimeAdaptor (query);
		group_selector.Adaptor = adaptor;
		group_selector.Mode = FSpot.GroupSelector.RangeType.All;
		adaptor.GlassSet += HandleAdaptorGlassSet;
	}

	void HandleArrangeByDirectory (object sender, EventArgs e)
	{
		group_selector.Adaptor.GlassSet -= HandleAdaptorGlassSet;
		FSpot.GroupAdaptor adaptor = new FSpot.DirectoryAdaptor (query);		
		group_selector.Adaptor = adaptor;
		group_selector.Mode = FSpot.GroupSelector.RangeType.Min;
		adaptor.GlassSet += HandleAdaptorGlassSet;
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

	public void HandleCreateNewTagCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, main_window);
		if (command.Execute (TagCommands.TagType.Tag))
			tag_selection_widget.Update ();
	}

	public void HandleCreateNewCategoryCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, main_window);
		if (command.Execute (TagCommands.TagType.Category))
			tag_selection_widget.Update ();
	}

	public void HandleAttachTagCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight ();
		
		AttachTags (tag_selection_widget.TagHighlight (), SelectedIds ());
	}

	void AttachTags (Tag [] tags, int [] ids) 
	{
		foreach (int num in ids) {
			Photo photo = query.Photos [num];
			
			AddTagExtended (photo, tags);
			InvalidateViews (num);			
		}
	}

	public void HandleRemoveTagCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight ();

		foreach (int num in SelectedIds ()) {
			Photo photo = query.Photos [num];
			photo.RemoveTag (tags);
			db.Photos.Commit (photo);
			
			InvalidateViews (num);
		}
	}

	public void HandleEditSelectedTag (object obj, EventArgs args)
	{
		Tag [] tags = tag_selection_widget.TagHighlight ();
		if (tags.Length != 1)
			return;
		
		TagCommands.Edit command = new TagCommands.Edit (db, main_window);
		if (command.Execute (tags [0]))
			tag_selection_widget.Update ();
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
		main_window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
                GLib.Idle.Add (new GLib.IdleHandler (SlideShow));
#else
		SlideCommands.Create command = new SlideCommands.Create (query.Photos);
		command.Execute ();
#endif
	}

	private bool SlideShow ()
	{
		Gtk.Window win = new Gtk.Window ("test");
		Pixbuf bg = PixbufUtils.LoadFromScreen ();

		int [] ids = SelectedIds ();
		Photo [] photos = null;
		if (ids.Length < 2) {
			int i = 0;
			if (ids.Length > 0)
				i = ids [0];

			photos = new Photo [query.Photos.Length];
			Array.Copy (query.Photos, i, photos, 0, query.Photos.Length - i);
			Array.Copy (query.Photos, 0, photos, query.Photos.Length - i, i);
		} else {
			photos = SelectedPhotos ();
		}
		
		if (photos.Length == 0) {
			Console.WriteLine ("No photos available -- no slideshow");
			return false;
		}

		SlideView slideview = new SlideView (bg, photos);
		win.ButtonPressEvent += HandleSlideViewButtonPressEvent;
		win.AddEvents ((int) EventMask.ButtonPressMask);
		win.Add (slideview);
		win.Decorated = false;
		win.Fullscreen();
		win.Realize ();
		win.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
	
		Gdk.GCValues values = new Gdk.GCValues ();
		values.SubwindowMode = SubwindowMode.IncludeInferiors;
		Gdk.GC fillgc = new Gdk.GC (win.GdkWindow, values, Gdk.GCValuesMask.Subwindow);

		slideview.Show ();
		win.GdkWindow.SetBackPixmap (null, false);
		win.Show ();
		main_window.GdkWindow.Cursor = null;	
		bg.RenderToDrawable (win.GdkWindow, fillgc, 
				     0, 0, 0, 0, -1, -1, RgbDither.Normal, 0, 0);

		slideview.Play ();
		return false;
	}
	
	private void HandleSlideViewButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		Gtk.Window win = sender as Gtk.Window;
		win.Destroy ();
		args.RetVal = true;
	}

	void HandleViewFullscreen (object sender, EventArgs args)
	{
	}

	void HandleZoomOut (object sender, EventArgs args)
	{
		switch (mode) {
		case ModeType.PhotoView:
			double old_zoom = photo_view.Zoom;
			try {
				photo_view.Zoom -= .1;
			} catch {
				if (old_zoom - .1 < -0.09) {
					photo_view.Zoom = 0.0;
					SwitchToIconViewMode ();
				}
				photo_view.Zoom = old_zoom;
			}
			
			break;
		case ModeType.IconView:
			int width = icon_view.ThumbnailWidth;
			
			width /= 2;
			width = Math.Max (width, 64);
			width = Math.Min (width, 256);
			icon_view.ThumbnailWidth = width;

			break;
		}
	}

	void HandleZoomIn (object sender, EventArgs args)
	{
		switch (mode) {
		case ModeType.PhotoView:
			double old_zoom = photo_view.Zoom;
			try {
				photo_view.Zoom += .1;
			} catch {
				photo_view.Zoom = old_zoom;
			}
			
			break;
		case ModeType.IconView:
			int width = icon_view.ThumbnailWidth;
			 
			width *= 2;
			width = Math.Max (width, 64);
			if (width >= 512) {
				photo_view.Zoom = 0.0;
				SwitchToPhotoViewMode ();
			} else {
				icon_view.ThumbnailWidth = width;
			}			
			break;
		}
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
	
	public void HandleDeleteSelectedTagCommand (object sender, EventArgs args)
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
		Console.WriteLine ("Rotate Left");
		RotateSelectedPictures (RotateCommand.Direction.Clockwise);
	}

	public void HandleRotate270Command (object sender, EventArgs args)
	{
		Console.WriteLine ("Rotate Left");
		RotateSelectedPictures (RotateCommand.Direction.Counterclockwise);
	}

	public void HandleCopyLocation (object sender, EventArgs args)
	{
		/*
		 * FIXME this should really set uri atoms as well as string atoms
		 */
		Clipboard primary = Clipboard.Get (Atom.Intern ("PRIMARY", false));
		Clipboard clipboard = Clipboard.Get (Atom.Intern ("CLIPBOARD", false));

		StringBuilder paths = new StringBuilder ();
		
		int i = 0;
		foreach (Photo p in SelectedPhotos ()) {
			if (i++ > 0)
				paths.Append (" ");

			paths.Append (System.IO.Path.GetFullPath (p.DefaultVersionPath));
		}
		
		String data = paths.ToString ();
		primary.SetText (data);
		clipboard.SetText (data);
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

	void HandleSetDateRange (object sender, EventArgs args) {
		DateCommands.Set set_command = new DateCommands.Set (query, main_window);
		set_command.Execute ();
	}

	void HandleClearDateRange (object sender, EventArgs args) {
		query.Range = null;
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
			icon_view.Throb (photo_view.CurrentPhoto);

			mode = ModeType.IconView;
			break;
		case 1:
			photo_view.CurrentPhoto = icon_view.FocusCell;

			mode = ModeType.PhotoView;
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

