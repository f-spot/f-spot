using Gdk;
using Gtk;
using GtkSharp;
using Glade;
using Gnome;
using System;
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
	[Glade.Widget] Gtk.Window main_window;
	[Glade.Widget] Gtk.VBox left_vbox;
	[Glade.Widget] Gtk.VBox group_vbox;
	[Glade.Widget] Gtk.VBox view_vbox;

	[Glade.Widget] Gtk.VBox toolbar_vbox;
	

	[Glade.Widget] ScrolledWindow icon_view_scrolled;
	[Glade.Widget] Box photo_box;
	[Glade.Widget] Notebook view_notebook;
	[Glade.Widget] ScrolledWindow tag_selection_scrolled;

	//
	// Menu items
	//
	[Glade.Widget] MenuItem version_menu_item;
	[Glade.Widget] MenuItem create_version_menu_item;
	[Glade.Widget] MenuItem delete_version_menu_item;
	[Glade.Widget] MenuItem rename_version_menu_item;

	[Glade.Widget] MenuItem delete_selected_tag;
	[Glade.Widget] MenuItem edit_selected_tag;

	[Glade.Widget] MenuItem attach_tag_to_selection;
	[Glade.Widget] MenuItem remove_tag_from_selection;

	[Glade.Widget] MenuItem copy;
	[Glade.Widget] MenuItem rotate_left;
	[Glade.Widget] MenuItem rotate_right;
	[Glade.Widget] MenuItem update_thumbnail;
	[Glade.Widget] MenuItem delete_from_drive;

	[Glade.Widget] MenuItem display_tags_menu_item;

	[Glade.Widget] MenuItem set_as_background;

	[Glade.Widget] MenuItem attach_tag;
	[Glade.Widget] MenuItem remove_tag;
	[Glade.Widget] MenuItem find_tag;

	PhotoVersionMenu versions_submenu;
	
	InfoBox info_box;
	FSpot.InfoDisplay info_display;
	QueryView icon_view;
	PhotoView photo_view;
	FSpot.FullScreenView fsview;
	FSpot.PhotoQuery query;
	FSpot.GroupSelector group_selector;

	FSpot.Delay slide_delay;
	
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

	const int PHOTO_IDX_NONE = -1;
	
	struct YearCount {
		int Year;
		int Count;
	}

	// Index into the PhotoQuery.  If -1, no photo is selected or multiple photos are selected.
	private int ActiveIndex () {
		if (mode == ModeType.IconView && icon_view.CurrentIdx != -1)
			return icon_view.CurrentIdx;

	        int [] selection = SelectedIds ();
		if (selection.Length == 1) 
			return selection [0];
		else 
			return PHOTO_IDX_NONE;
	}

	public bool PhotoSelectionActive ()
	{
		return SelectedIds().Length > 0;
	}

	private Photo CurrentPhoto {
		get {
			int active = ActiveIndex ();
			if (active >= 0)
				return query.Photos [active];
			else
				return null;
		}
	}

	// Switching mode.
	enum ModeType {
		IconView,
		PhotoView
	};
	ModeType mode;

	//
	// Constructor
	//
	public MainWindow (Db db)
	{
		this.db = db;

		Mono.Posix.Catalog.Init ("f-spot", FSpot.Defines.LOCALE_DIR);
		Glade.XML gui = Glade.XML.FromAssembly ("f-spot.glade", "main_window", null);
		gui.Autoconnect (this);

		slide_delay = new FSpot.Delay (new GLib.IdleHandler (SlideShow));

		Gtk.Toolbar toolbar = new Gtk.Toolbar ();
		toolbar_vbox.PackStart (toolbar);
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-rotate-270", new System.EventHandler (HandleRotate270Command));
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-rotate-90", new System.EventHandler (HandleRotate90Command));
		toolbar.AppendSpace ();
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-browse", new System.EventHandler (HandleViewBrowse));
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-edit-image", new System.EventHandler (HandleViewPhoto));
		toolbar.AppendSpace ();
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-fullscreen", new System.EventHandler (HandleViewFullscreen));
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-slideshow", new System.EventHandler (HandleViewSlideShow));

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
		
		query = new FSpot.PhotoQuery (db.Photos);
		query.ItemChanged += HandleQueryItemChanged;

		group_selector = new FSpot.GroupSelector ();
		FSpot.GroupAdaptor adaptor = new FSpot.TimeAdaptor (query);
		//FSpot.GroupAdaptor adaptor = new FSpot.DirectoryAdaptor (query);		
		group_selector.Mode = FSpot.GroupSelector.RangeType.All;

		group_selector.Adaptor  = adaptor;
		group_selector.ShowAll ();

		view_vbox.PackStart (group_selector, false, false, 0);
		view_vbox.ReorderChild (group_selector, 0);

		icon_view = new QueryView (query);
		icon_view_scrolled.Add (icon_view);
		icon_view.SelectionChanged += new IconView.SelectionChangedHandler (HandleSelectionChanged);
		icon_view.DoubleClicked += new IconView.DoubleClickedHandler (HandleDoubleClicked);
		icon_view.GrabFocus ();

		new FSpot.PreviewPopup (icon_view);

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
		photo_view.PhotoChanged += HandlePhotoViewPhotoChanged;
		photo_view.ButtonPressEvent += HandlePhotoViewButtonPressEvent;
		photo_view.KeyPressEvent += HandlePhotoViewKeyPressEvent;
		photo_view.UpdateStarted += HandlePhotoViewUpdateStarted;
		photo_view.UpdateFinished += HandlePhotoViewUpdateFinished;

		Gtk.Drag.DestSet (photo_view, DestDefaults.All, tag_target_table, 
				  DragAction.Copy | DragAction.Move); 

		photo_view.DragMotion += new DragMotionHandler (HandlePhotoViewDragMotion);
		photo_view.DragDrop += new DragDropHandler (HandlePhotoViewDragDrop);
		photo_view.DragDataReceived += new DragDataReceivedHandler (HandlePhotoViewDragDataReceived);

		view_notebook.SwitchPage += new SwitchPageHandler (HandleViewNotebookSwitchPage);
		adaptor.GlassSet += HandleAdaptorGlassSet;

		UpdateMenus ();
		main_window.ShowAll ();
		main_window.Destroyed += HandleCloseCommand;

		if (Toplevel == null)
			Toplevel = this;
	}

	void HandleViewNotebookSwitchPage (object sender, SwitchPageArgs args)
	{
		switch (view_notebook.CurrentPage) {
		case 0:
			mode = ModeType.IconView;
			Present (photo_view.CurrentPhoto);
			break;
		case 1:
			mode = ModeType.PhotoView;
			Present (icon_view.FocusCell);
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

	public int [] SelectedIds () {
		int [] ids;

		if (fsview != null)
			ids = new int [] { fsview.View.CurrentPhoto };
		else {
			switch (mode) {
			case ModeType.IconView:
				ids = icon_view.SelectedIdxs;
				break;
			default:
			case ModeType.PhotoView:
				ids = new int [] { photo_view.CurrentPhoto };
				break;
			}
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
		photo_view.Reload ();
		if (fsview != null)
			fsview.View.Reload ();
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
				query.MarkChanged (num);
		}
	}

	//
	// Tag Selection Drag Handlers
	//

	public void AddTagExtended (int num, Tag [] tags)
	{
		query.Photos [num].AddTag (tags);
		query.Commit (num);

		foreach (Tag t in tags) {
			Pixbuf icon = null;

			if (t.Icon == null) {
				if (icon == null) {
					// FIXME this needs a lot more work.
					Pixbuf tmp = PixbufUtils.LoadAtMaxSize (query.Photos[num].DefaultVersionPath, 128, 128);
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
				AddTagExtended (num, tags);
			}
			break;
		case (uint)TargetType.TagList:
			UriList list = new UriList (args.SelectionData);
			
			foreach (string path in list.ToLocalPaths ()) {
				Photo photo = db.Photos.GetByPath (path);
				
				// FIXME - at this point we should import the photo, and then continue
				if (photo == null)
					return;
				
				// FIXME this should really follow the AddTagsExtended path too
				photo.AddTag (tags);
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
			string thumbnail_path = Thumbnail.PathForUri (photos[0].DefaultVersionUri.ToString (), ThumbnailSize.Large);
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
		//Widget source = Gtk.Drag.GetSourceWidget (args.Context);
		//Console.WriteLine ("Drag Drop {0}", source == null ? "null" : source.TypeName);
		
		args.RetVal = true;
	}

	void HandleIconViewDragMotion (object sender, DragMotionArgs args)
	{
		//Widget source = Gtk.Drag.GetSourceWidget (args.Context);
		//Console.WriteLine ("Drag Motion {0}", source == null ? "null" : source.TypeName);

		Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);
		args.RetVal = true;
	}

	public void ImportUriList (UriList list) 
	{
		ImportCommand command = new ImportCommand (main_window);
		if (command.ImportFromPaths (db.Photos, list.ToLocalPaths ()) > 0) {
			UpdateQuery ();
		}
	}

	void HandleIconViewDragDataReceived (object sender, DragDataReceivedArgs args)
	{
	 	Widget source = Gtk.Drag.GetSourceWidget (args.Context);     

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
			ImportUriList (list);
			break;
		}

		Gtk.Drag.Finish (args.Context, true, false, args.Time);
	}

	//
	// IconView event handlers
	// 

	void HandleSelectionChanged (IconView view)
	{
		info_box.Photo = CurrentPhoto;
		if (info_display != null)
			info_display.Photo = CurrentPhoto;
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
		info_box.Photo = CurrentPhoto;
		if (info_display != null)
			info_display.Photo = CurrentPhoto;
		UpdateMenus ();
	}
	
	void HandlePhotoViewKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
	{
		switch (args.Event.Key) {
		case Gdk.Key.F:
		case Gdk.Key.f:
			HandleViewFullscreen (sender, args);
			args.RetVal = true;
			break;
		default:
			break;
		}
		return;
	}

	void HandlePhotoViewButtonPressEvent (object sender, Gtk.ButtonPressEventArgs args)
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
		//Widget source = Gtk.Drag.GetSourceWidget (args.Context);
		//Console.WriteLine ("Drag Drop {0}", source == null ? "null" : source.TypeName);

		args.RetVal = true;
	}

	void HandlePhotoViewDragMotion (object sender, DragMotionArgs args)
	{
		//Widget source = Gtk.Drag.GetSourceWidget (args.Context);
		//Console.WriteLine ("Drag Motion {0}", source == null ? "null" : source.TypeName);

		Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);
		args.RetVal = true;
	}

	void HandlePhotoViewDragDataReceived (object sender, DragDataReceivedArgs args)
	{
	 	//Widget source = Gtk.Drag.GetSourceWidget (args.Context);     
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
			AddTagExtended (num, new Tag [] {t});
		}
	}
	
	void HandleFindTagMenuSelected (Tag t)
	{
		tag_selection_widget.TagSelection = new Tag [] {t};
	}

	public void HandleRemoveTagMenuSelected (Tag t)
	{
		foreach (int num in SelectedIds ()) {
			query.Photos [num].RemoveTag (t);
			query.Commit (num);
		}
	}

	//
	// Main menu commands
	//

	void HandleImportCommand (object sender, EventArgs e)
	{
		ImportCommand command = new ImportCommand (main_window);
		if (command.ImportFromFile (db.Photos) > 0) {
			UpdateQuery ();
		}
	}

	unsafe void HandlePrintCommand (object sender, EventArgs e)
	{
		new FSpot.PrintDialog (SelectedPhotos ());
	}

	private Gtk.Dialog info_display_window;
	public void HandleInfoDisplayDestroy (object sender, EventArgs args)
	{
		info_display_window = null;
		info_display = null;
	}
	
	void HandleViewFullExif (object sender, EventArgs args)
	{
		if (info_display_window != null) {
			info_display_window.Present ();
			return;
		}

		info_display = new FSpot.InfoDisplay ();
		info_display_window = new Gtk.Dialog ("EXIF Data", main_window, 
						      Gtk.DialogFlags.NoSeparator | Gtk.DialogFlags.DestroyWithParent);
		info_display_window.SetDefaultSize (400, 400);
		Gtk.ScrolledWindow scroll = new ScrolledWindow ();
		info_display_window.VBox.PackStart (scroll);
		scroll.Add (info_display);

		info_display.Photo = CurrentPhoto;
	       
		info_display_window.ShowAll ();
		info_display_window.Destroyed += HandleInfoDisplayDestroy;
	}


	void HandleExportToGallery (object sender, EventArgs args)
	{
		new FSpot.GalleryExport (new FSpot.PhotoArray (SelectedPhotos ()));
	}

	void HandleExportToVfs (object sender, EventArgs args)
	{
		new FSpot.VfsExport (new FSpot.PhotoArray (SelectedPhotos ()));
	}

	void HandleViewDirectory (object sender, EventArgs args)
	{
		Gtk.Window win = new Gtk.Window ("Directory View");
		IconView view = new IconView (new FSpot.DirectoryCollection (System.IO.Directory.GetCurrentDirectory ()));
		new FSpot.PreviewPopup (view);

		view.DisplayTags = false;

		Gtk.ScrolledWindow scrolled = new ScrolledWindow ();
		win.Add (scrolled);
		scrolled.Add (view);
		win.ShowAll ();
	}

	void HandleExportToFlickr (object sender, EventArgs args)
	{
		new FSpot.FlickrExport (new FSpot.PhotoArray (SelectedPhotos ()));
	}
	
	void HandleExportToFotki (object sender, EventArgs args)
	{
		
	}

	void HandleSendMailCommand (object sender, EventArgs args)
	{
		StringBuilder url = new StringBuilder ("mailto:?subject=my%20photos");

		foreach (Photo p in SelectedPhotos ()) {
			url.Append ("&attach=" + p.DefaultVersionPath);
		}

		Console.WriteLine (url.ToString ());

		Gnome.Url.Show (url.ToString ());
	}

	void HandleAbout (object sender, EventArgs args)
	{
		string [] authors = new string [] {
			"Ettore Perazzoli",
			"Lawrence Ewing",
			"Nat Friedman",
			"Miguel de Icaza",
			"Vladimir Vukicevic",
			"Jon Trowbridge",
			"Joe Shaw",
			"Tambet Ingo",
			"MOREAU Vincent",
			"Lee Willis",
			"Peter Johanson",
			"Grahm Orr",
			"Cheslack-Postava",
			"Patanjali Somayaji",
			"Matt Jones",
			"Martin Willemoes Hansen",
			"Laurence Hygate"

		};

                // Translators should localize the following string
                // * which will give them credit in the About box.
                // * E.g. "Martin Willemoes Hansen"
                string translators = Mono.Posix.Catalog.GetString ("translator-credits");

                new About (FSpot.Defines.PACKAGE, 
			   FSpot.Defines.VERSION, 
			   "Copyright 2003-2005 Novell Inc.",
                           null, authors, null, translators, null).Show();
	}

	void HandleArrangeByTime (object sender, EventArgs args)
	{
		group_selector.Adaptor.GlassSet -= HandleAdaptorGlassSet;
		FSpot.GroupAdaptor adaptor = new FSpot.TimeAdaptor (query);
		group_selector.Adaptor = adaptor;
		group_selector.Mode = FSpot.GroupSelector.RangeType.All;
		adaptor.GlassSet += HandleAdaptorGlassSet;
	}

	void HandleArrangeByDirectory (object sender, EventArgs args)
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
			query.MarkChanged (ActiveIndex ());
		}
	}

	void HandleDeleteVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Delete cmd = new PhotoVersionCommands.Delete ();

		if (cmd.Execute (db.Photos, CurrentPhoto, main_window)) {
			query.MarkChanged (ActiveIndex ());
		}
	}

	void HandlePropertiesCommand (object obje, EventArgs args)
	{
		Photo [] photos = SelectedPhotos ();
		
	        long length = 0;

		foreach (Photo p in photos) {
			System.IO.FileInfo fi = new System.IO.FileInfo (p.DefaultVersionPath);

			length += fi.Length;
		}

		Console.WriteLine ("{0} Selected Photos : Total length = {1} - {2}kB - {3}MB", photos.Length, length, length / 1024, length / (1024 * 1024));
	}
		
	void HandleRenameVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Rename cmd = new PhotoVersionCommands.Rename ();

		if (cmd.Execute (db.Photos, CurrentPhoto, main_window)) {
			query.MarkChanged (ActiveIndex ());
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
		AttachTags (tag_selection_widget.TagHighlight (), SelectedIds ());
	}

	void AttachTags (Tag [] tags, int [] ids) 
	{
		foreach (int num in ids) {
			AddTagExtended (num, tags);
		}
	}

	public void HandleRemoveTagCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight ();

		foreach (int num in SelectedIds ()) {
			query.Photos [num].RemoveTag (tags);
			query.Commit (num);
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

	void HandleAdjustColor (object sender, EventArgs args)
	{
		if (ActiveIndex () > 0) {
			new FSpot.ColorDialog (query, ActiveIndex ());
		}
	}

	void HandleSharpen (object sender, EventArgs args)
	{
		Gtk.Dialog dialog = new Gtk.Dialog (Mono.Posix.Catalog.GetString ("Unsharp Mask"), main_window, Gtk.DialogFlags.Modal);

		dialog.VBox.Spacing = 6;
		dialog.VBox.BorderWidth = 12;
		
		Gtk.Table table = new Gtk.Table (3, 2, false);
		table.ColumnSpacing = 6;
		table.RowSpacing = 6;
		
		table.Attach (new Gtk.Label (Mono.Posix.Catalog.GetString ("Amount:")), 0, 1, 0, 1);
		table.Attach (new Gtk.Label (Mono.Posix.Catalog.GetString ("Radius:")), 0, 1, 1, 2);
		table.Attach (new Gtk.Label (Mono.Posix.Catalog.GetString ("Threshold:")), 0, 1, 2, 3);

		Gtk.SpinButton amount_spin = new Gtk.SpinButton (0.0, 100.0, .01);
		Gtk.SpinButton radius_spin = new Gtk.SpinButton (0.0, 50.0, .01);
		Gtk.SpinButton threshold_spin = new Gtk.SpinButton (0.0, 50.0, .01);

		table.Attach (amount_spin, 1, 2, 0, 1);
		table.Attach (radius_spin, 1, 2, 1, 2);
		table.Attach (threshold_spin, 1, 2, 2, 3);
		
		dialog.VBox.PackStart (table);

		dialog.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
		dialog.AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);

		dialog.ShowAll ();

		Gtk.ResponseType response = (Gtk.ResponseType) dialog.Run ();

		if (response == Gtk.ResponseType.Ok) {
			foreach (int id in SelectedIds ()) {
				Gdk.Pixbuf orig = FSpot.PhotoLoader.Load (query, id);
				Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig, radius_spin.Value, amount_spin.Value, threshold_spin.Value);
			
				Photo photo = query.Photos [id];

				uint version = photo.DefaultVersionId;
				if (version == Photo.OriginalVersionId) {
					version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
				}
				
				try {
					string version_path = photo.GetVersionPath (version);
					
					final.Savev (version_path, "jpeg", null, null);
					PhotoStore.GenerateThumbnail (version_path);
					photo.DefaultVersionId = version;
					query.Commit (id);
				} catch (GLib.GException ex) {
					// FIXME error dialog.
					Console.WriteLine ("error {0}", ex);
				}
			
			}
		}

		dialog.Destroy ();
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

	void HandleDisplayTags (object sender, EventArgs args)
	{
		icon_view.DisplayTags = !icon_view.DisplayTags;
	}
	
	void HandleDisplayDates (object sender, EventArgs args)
	{
		icon_view.DisplayDates = !icon_view.DisplayDates;
	}

	void HandleDisplayGroupSelector (object sender, EventArgs args)
	{
		if (group_selector.Visible)
			group_selector.Hide ();
		else
			group_selector.Show ();
	}

	void HandleViewSlideShow (object sender, EventArgs args)
	{
		main_window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
		slide_delay.Start ();
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
			main_window.GdkWindow.Cursor = null;
			return false;
		}

		SlideView slideview = new SlideView (bg, photos);
		win.ButtonPressEvent += HandleSlideViewButtonPressEvent;
		win.KeyPressEvent += HandleSlideViewKeyPressEvent;
		win.AddEvents ((int) (EventMask.ButtonPressMask | EventMask.KeyPressMask));
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
	
	[GLib.ConnectBefore]
	private void HandleSlideViewKeyPressEvent (object sender, KeyPressEventArgs args)
	{
		Gtk.Window win = sender as Gtk.Window;
		win.Destroy ();
		args.RetVal = true;
	}

	private void HandleSlideViewButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		Gtk.Window win = sender as Gtk.Window;
		win.Destroy ();
		args.RetVal = true;
	}

	void HandleViewBrowse (object sender, EventArgs args)
	{
		SwitchToIconViewMode ();
	}

	void HandleViewPhoto (object sender, EventArgs args)
	{
		SwitchToPhotoViewMode ();
	}

	void HandleViewFullscreen (object sender, EventArgs args)
	{
		int active = Math.Max (ActiveIndex (), 0);
		if (fsview == null) {
			fsview = new FSpot.FullScreenView (query);
			fsview.Destroyed += HandleFullScreenViewDestroy;
		}
		// FIXME this needs to be another mode like PhotoView and IconView mode.
		fsview.View.CurrentPhoto = active;

		fsview.Show ();
	}
	
	void Present (int item)
	{
		switch (mode) {
		case ModeType.IconView:
			icon_view.ScrollTo (item);
			icon_view.Throb (item);
			break;
		case ModeType.PhotoView:
			photo_view.CurrentPhoto = item;
			break;
		}
	}

	void HandleFullScreenViewDestroy (object sender, EventArgs args)
	{
		Present (fsview.View.CurrentPhoto);
		fsview = null;
	}
	
	void HandleZoomOut (object sender, EventArgs args)
	{
		switch (mode) {
		case ModeType.PhotoView:
			double old_zoom = photo_view.Zoom;

			old_zoom /= FSpot.PhotoImageView.ZoomMultipler;
			if (old_zoom < .001) {
				SwitchToIconViewMode ();
			} else {
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
				photo_view.Zoom *= FSpot.PhotoImageView.ZoomMultipler;
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
				query.MarkChanged (num);
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
		int active = ActiveIndex ();

		query.Commit (active);
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

	void HandleQueryItemChanged (FSpot.IBrowsableCollection browsable, int item)
	{
		if (info_box.Photo == (browsable.Items[item] as Photo))
			info_box.Update ();
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
		bool active_selection = PhotoSelectionActive ();

		rotate_left.Sensitive = active_selection;
		rotate_right.Sensitive = active_selection;
		update_thumbnail.Sensitive = active_selection;
		delete_from_drive.Sensitive = active_selection;
		copy.Sensitive = active_selection;

		delete_selected_tag.Sensitive = tag_sensitive;
		attach_tag_to_selection.Sensitive = tag_sensitive && active_selection;
		remove_tag_from_selection.Sensitive = tag_sensitive && active_selection;
	}

}

