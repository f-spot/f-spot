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

using LibGPhoto2;

public class MainWindow {
        public static MainWindow Toplevel = null;

	Db db;

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

	[Glade.Widget] VPaned info_vpaned;

	PhotoVersionMenu versions_submenu;

	Gtk.ToggleButton browse_button;
	Gtk.ToggleButton view_button;
	
	InfoBox info_box;
	FSpot.InfoDisplay info_display;
	QueryView icon_view;
	PhotoView photo_view;
	FSpot.FullScreenView fsview;
	FSpot.PhotoQuery query;
	FSpot.GroupSelector group_selector;
	
	FSpot.Delay slide_delay;
	
	string last_import_path;
	ModeType view_mode;

	// Drag and Drop
	enum TargetType {
		UriList,
		TagList,
		PhotoList,
		RootWindow
	};

	private static TargetEntry [] icon_source_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
		new TargetEntry ("application/x-root-window-drop", 0, (uint) TargetType.RootWindow)
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
		browse_button = GtkUtil.MakeToolbarToggleButton (toolbar, "f-spot-browse", 
								 new System.EventHandler (HandleToggleViewBrowse)) as ToggleButton;
		view_button = GtkUtil.MakeToolbarToggleButton (toolbar, "f-spot-edit-image", 
							       new System.EventHandler (HandleToggleViewPhoto)) as ToggleButton;
		toolbar.AppendSpace ();
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-fullscreen", new System.EventHandler (HandleViewFullscreen));
		GtkUtil.MakeToolbarButton (toolbar, "f-spot-slideshow", new System.EventHandler (HandleViewSlideShow));

		tag_selection_widget = new TagSelectionWidget (db.Tags);
		tag_selection_scrolled.Add (tag_selection_widget);
		
		tag_selection_widget.Selection.Changed += HandleTagSelectionChanged;
		tag_selection_widget.SelectionChanged += OnTagSelectionChanged;
		tag_selection_widget.DragDataGet += HandleTagSelectionDragDataGet;
		tag_selection_widget.DragDrop += HandleTagSelectionDragDrop;
		Gtk.Drag.SourceSet (tag_selection_widget, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    tag_target_table, DragAction.Copy | DragAction.Move);

		tag_selection_widget.DragDataReceived += HandleTagSelectionDragDataReceived;
		tag_selection_widget.DragMotion += HandleTagSelectionDragMotion;
		Gtk.Drag.DestSet (tag_selection_widget, DestDefaults.All, tag_dest_target_table, 
				  DragAction.Copy | DragAction.Move ); 

		tag_selection_widget.ButtonPressEvent += HandleTagSelectionButtonPressEvent;

		info_box = new InfoBox ();
		info_box.VersionIdChanged += HandleInfoBoxVersionIdChange;
		left_vbox.PackStart (info_box, false, true, 0);

		query = new FSpot.PhotoQuery (db.Photos);
		query.ItemChanged += HandleQueryItemChanged;

#if SHOW_CALENDAR
		FSpot.SimpleCalendar cal = new FSpot.SimpleCalendar (query);
		cal.DaySelected += HandleCalendarDaySelected;
		left_vbox.PackStart (cal, false, true, 0);
#endif

		group_selector = new FSpot.GroupSelector ();
		FSpot.GroupAdaptor adaptor = new FSpot.TimeAdaptor (query);

		group_selector.Adaptor  = adaptor;
		group_selector.ShowAll ();

		view_vbox.PackStart (group_selector, false, false, 0);
		view_vbox.ReorderChild (group_selector, 0);

		icon_view = new QueryView (query);
		icon_view_scrolled.Add (icon_view);
		icon_view.SelectionChanged += HandleSelectionChanged;
		icon_view.DoubleClicked += HandleDoubleClicked;
		icon_view.GrabFocus ();

		new FSpot.PreviewPopup (icon_view);

		Gtk.Drag.SourceSet (icon_view, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    icon_source_target_table, DragAction.Copy | DragAction.Move);
		
		icon_view.DragBegin += HandleIconViewDragBegin;
		icon_view.DragDataGet += HandleIconViewDragDataGet;

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
		icon_view.DragMotion += HandleIconViewDragMotion;
		icon_view.DragDrop += HandleIconViewDragDrop;
		icon_view.DragDataReceived += HandleIconViewDragDataReceived;

		photo_view = new PhotoView (query, db.Photos);
		photo_box.Add (photo_view);
		photo_view.PhotoChanged += HandlePhotoViewPhotoChanged;
		photo_view.ButtonPressEvent += HandlePhotoViewButtonPressEvent;
		photo_view.KeyPressEvent += HandlePhotoViewKeyPressEvent;
		photo_view.UpdateStarted += HandlePhotoViewUpdateStarted;
		photo_view.UpdateFinished += HandlePhotoViewUpdateFinished;

		Gtk.Drag.DestSet (photo_view, DestDefaults.All, tag_target_table, 
				  DragAction.Copy | DragAction.Move); 

		photo_view.DragMotion += HandlePhotoViewDragMotion;
		photo_view.DragDrop += HandlePhotoViewDragDrop;
		photo_view.DragDataReceived += HandlePhotoViewDragDataReceived;

		view_notebook.SwitchPage += HandleViewNotebookSwitchPage;
		adaptor.GlassSet += HandleAdaptorGlassSet;

		UpdateMenus ();
		main_window.ShowAll ();
		main_window.Destroyed += HandleCloseCommand;

		if (Toplevel == null)
			Toplevel = this;

		UpdateToolbar ();

		if (db.Empty)
			HandleImportCommand (null, null);
	}

	// Index into the PhotoQuery.  If -1, no photo is selected or multiple photos are selected.
	private int ActiveIndex () 
	{
		if (view_mode == ModeType.IconView && icon_view.CurrentIdx != -1)
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

	public Db Database {
		get {
			return db;
		}
	}

	public ModeType ViewMode {
		get {
			return view_mode;
		}
	}

	// Switching mode.
	public enum ModeType {
		IconView,
		PhotoView
	};

	public void SetViewMode (ModeType value)
	{
		view_mode = value;
		switch (view_mode) {
		case ModeType.IconView:
			if (view_notebook.CurrentPage != 0)
				view_notebook.CurrentPage = 0;
				
			Present (photo_view.CurrentPhoto);
			break;
		case ModeType.PhotoView:
			if (view_notebook.CurrentPage != 1)
				view_notebook.CurrentPage = 1;
			
			Present (icon_view.FocusCell);
			break;
		}
		UpdateToolbar ();
	}
	
	void UpdateToolbar ()
	{
		if (browse_button != null) {
			bool state = view_mode == ModeType.IconView;
			
			if (browse_button.Active != state)
				browse_button.Active = state;
		}

		if (view_button != null) {
			bool state = view_mode == ModeType.PhotoView;
			
			if (view_button.Active != state)
				view_button.Active = state;
		}
	}
		

	void HandleViewNotebookSwitchPage (object sender, SwitchPageArgs args)
	{
		switch (view_notebook.CurrentPage) {
		case 0:
			SetViewMode (ModeType.IconView);
			break;
		case 1:
			SetViewMode (ModeType.PhotoView);
			break;
		}
	}


	public int [] SelectedIds () {
		int [] ids = new int [0];

		if (fsview != null)
			ids = new int [] { fsview.View.CurrentPhoto };
		else {
			switch (view_mode) {
			case ModeType.IconView:
				ids = icon_view.SelectedIdxs;
				break;
			default:
			case ModeType.PhotoView:
				if (photo_view.View.CurrentPhotoValid ())
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
					try {
						Pixbuf tmp = PixbufUtils.LoadAtMaxSize (query.Photos[num].DefaultVersionPath, 128, 128);
						icon = PixbufUtils.TagIconFromPixbuf (tmp);
						tmp.Dispose ();
					} catch {
						icon = null;
					}
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

	void HandleTagSelectionDragDataGet (object sender, DragDataGetArgs args)
	{		
		UriList list = new UriList (SelectedPhotos ());

		switch (args.Info) {
		case (uint) TargetType.TagList:
			Byte [] data = Encoding.UTF8.GetBytes (list.ToString ());
			Atom [] targets = args.Context.Targets;
		
			args.SelectionData.Set (targets[0], 8, data, data.Length);
			break;
		} 
	}

	void HandleTagSelectionDragDrop (object sender, DragDropArgs args)
	{
		args.RetVal = true;
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

#if SHOW_CALENDAR
	void HandleCalendarDaySelected (object sender, System.EventArgs args)
	{
		FSpot.SimpleCalendar cal = sender as FSpot.SimpleCalendar;
		JumpTo (cal.Date);
	}
#endif

	private void JumpTo (System.DateTime time)
	{
		//FIXME this should make sure the photos are sorted by
		//time.  This should be handled via a property that
		//does all the needed switching.
		if (!(group_selector.Adaptor is FSpot.TimeAdaptor))
			HandleArrangeByTime (null, null);
		
		FSpot.TimeAdaptor time_adaptor = group_selector.Adaptor as FSpot.TimeAdaptor;
		if (time_adaptor != null)
			JumpTo (time_adaptor.LookupItem (time));
	}

	private void JumpTo (int index)
	{
		switch (view_mode) {
		case ModeType.PhotoView:
			photo_view.CurrentPhoto = index;
			break;
		case ModeType.IconView:
			icon_view.ScrollTo (index);
			icon_view.Throb (index);
			break;
		}
	}

	void HandleAdaptorGlassSet (FSpot.GroupAdaptor sender, int index)
	{
		JumpTo (index);
	}

	//
	// IconView Drag Handlers
	//

	void HandleIconViewDragBegin (object sender, DragBeginArgs args)
	{
		Photo [] photos = SelectedPhotos ();
		
		if (photos.Length > 0) {
			int len = Math.Min (photos.Length, 4);
			int size = 48;
			int border  = 2;
			int csize = size/2 + len * size / 2 + 2 * border ;
			
			Pixbuf container = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, csize, csize);
			container.Fill (0x00000000);

			bool use_icon = false;;
			while (len-- > 0) {
				string thumbnail_path = FSpot.ThumbnailGenerator.ThumbnailPath (photos [len].DefaultVersionUri);
				FSpot.PixbufCache.CacheEntry entry = icon_view.Cache.Lookup (thumbnail_path);

				Pixbuf thumbnail = null;
				if (entry != null)
					thumbnail = entry.ShallowCopyPixbuf ();
				
				if (thumbnail != null) {
					Pixbuf small = PixbufUtils.ScaleToMaxSize (thumbnail, size, size);				

					int x = border + len * (size/2) + (size - small.Width)/2;
					int y = border + len * (size/2) + (size - small.Height)/2;
					Pixbuf box = new Pixbuf (container, x - border, y - border, 
								 small.Width + 2 * border, small.Height + 2 * border);

					box.Fill (0x000000ff);
					small.CopyArea (0, 0, small.Width, small.Height, container, x, y); 
					
					thumbnail.Dispose ();
					small.Dispose ();
					use_icon = true;
				}
			}
			if (use_icon)
				Gtk.Drag.SetIconPixbuf (args.Context, container, 0, 0);
			container.Dispose ();
		}
	}

	void HandleIconViewDragDataGet (object sender, DragDataGetArgs args)
	{		
		switch (args.Info) {
		case (uint) TargetType.UriList:
		case (uint) TargetType.PhotoList:
			UriList list = new UriList (SelectedPhotos ());
			Byte [] data = Encoding.UTF8.GetBytes (list.ToString ());
			Atom [] targets = args.Context.Targets;
			args.SelectionData.Set (targets[0], 8, data, data.Length);
			break;
		case (uint) TargetType.RootWindow:
			HandleSetAsBackgroundCommand (null, null);
                        break;
		}
		       
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
			if (item >= 0) {
				if (icon_view.CellIsSelected (item))
					AttachTags (tag_selection_widget.TagHighlight (), SelectedIds());
				else 
					AttachTags (tag_selection_widget.TagHighlight (), new int [] {item});
			}
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
		SetViewMode (ModeType.PhotoView);
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
			SetViewMode (ModeType.IconView);
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
		db.Sync = false;
		ImportCommand command = new ImportCommand (main_window);
		if (command.ImportFromFile (db.Photos, this.last_import_path) > 0) {
			this.last_import_path = command.ImportPath;
			UpdateQuery ();
		}
		db.Sync = true;		
	}

	void HandleImportFromCameraCommand (object sender, EventArgs e)
	{
		GPhotoCamera cam = new GPhotoCamera();

		try {
			int num_cameras = cam.DetectCameras();
			int selected_cam;

			if (num_cameras < 1) {
				HigMessageDialog md = new HigMessageDialog (main_window, DialogFlags.DestroyWithParent, 
					MessageType.Warning, ButtonsType.Ok, 
					Mono.Posix.Catalog.GetString ("No cameras detected."),
					Mono.Posix.Catalog.GetString ("F-Spot was unable to find any cameras attached to this system." + 
								      "  Double check that the camera is connected and has power")); 

				md.Run ();
				md.Destroy ();
				return;
			} else if (num_cameras == 1) {
				selected_cam = 0;
			} else {
				FSpot.CameraSelectionDialog camselect = new FSpot.CameraSelectionDialog (cam.CameraList);
				selected_cam = camselect.Run ();
			}

			if (selected_cam >= 0) {
				cam.SelectCamera (selected_cam);	
				cam.InitializeCamera ();

				FSpot.CameraFileSelectionDialog selector = new FSpot.CameraFileSelectionDialog (cam, db);
				selector.Run ();

				UpdateQuery ();
			}
		}
		catch (GPhotoException ge) {
			System.Console.WriteLine (ge.ToString ());
			HigMessageDialog md = new HigMessageDialog (main_window, DialogFlags.DestroyWithParent, 
				MessageType.Error, ButtonsType.Ok, 
				Mono.Posix.Catalog.GetString ("Error connecting to camera"),
				String.Format (Mono.Posix.Catalog.GetString ("Received error \"{0}\" while connecting to camera"), 
				ge.Message));

			md.Run ();
			md.Destroy ();
		} finally {
			cam.ReleaseGPhotoResources ();
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

	void HandleExportToOriginal (object sender, EventArgs args)
	{
		new FSpot.FolderExport (new FSpot.PhotoArray (SelectedPhotos ()));
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
	
	void HandleExportToCD (object sender, EventArgs args)
	{
		new FSpot.CDExport (new FSpot.PhotoArray (SelectedPhotos ()));
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
			"Alessandro Gervaso",
			"Peter Johanson",
			"Grahm Orr",
			"Ewen Cheslack-Postava",
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
		group_selector.Mode = FSpot.GroupSelector.RangeType.Min;
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
		command.Execute (TagCommands.TagType.Tag, tag_selection_widget.TagHighlight ());
		
	}

	public void HandleCreateNewCategoryCommand (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, main_window);
		command.Execute (TagCommands.TagType.Category, tag_selection_widget.TagHighlight ());
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
		command.Execute (tags [0]);
	}

	void HandleAdjustColor (object sender, EventArgs args)
	{
		if (ActiveIndex () > 0) {
			SetViewMode (ModeType.PhotoView);
			new FSpot.ColorDialog (photo_view.View);
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
				Exif.ExifData exif_data = new Exif.ExifData (photo.DefaultVersionPath);

				uint version = photo.DefaultVersionId;
				if (version == Photo.OriginalVersionId) {
					version = photo.CreateDefaultModifiedVersion (photo.DefaultVersionId, false);
				}
				
				try {
					string version_path = photo.GetVersionPath (version);
					
					PixbufUtils.SaveJpeg (final, version_path, 95, exif_data);
					FSpot.ThumbnailGenerator.Create (version_path).Dispose ();
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

	void HandleDisplayInfoSidebar (object sender, EventArgs args)
	{
		if (info_vpaned.Visible)
			info_vpaned.Hide ();
		else
			info_vpaned.Show ();
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

			// FIXME this should be an  IBrowsableCollection.
			photos = new Photo [query.Photos.Length];
			Array.Copy (query.Photos, i, photos, 0, query.Photos.Length - i);
			Array.Copy (query.Photos, 0, photos, query.Photos.Length - i, i);
			System.Console.WriteLine (photos.Length);
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

	void HandleToggleViewBrowse (object sender, EventArgs args)
	{
	        ToggleButton toggle = sender as ToggleButton;
		if (toggle != null) {
			if (toggle.Active)
				SetViewMode (ModeType.IconView);
		} else
			SetViewMode (ModeType.IconView);
	}

	void HandleToggleViewPhoto (object sender, EventArgs args)
	{
	        ToggleButton toggle = sender as ToggleButton;
		
		if (toggle != null) {
			if (toggle.Active)
				SetViewMode (ModeType.PhotoView);
		} else
			SetViewMode (ModeType.IconView);
	}

	void HandleViewBrowse (object sender, EventArgs args)
	{
		SetViewMode (ModeType.IconView);
	}

	void HandleViewPhoto (object sender, EventArgs args)
	{
		SetViewMode (ModeType.PhotoView);
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
		switch (view_mode) {
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
		switch (view_mode) {
		case ModeType.PhotoView:
			double old_zoom = photo_view.Zoom;

			old_zoom /= FSpot.PhotoImageView.ZoomMultipler;
			if (old_zoom < .001) {
				SetViewMode (ModeType.IconView);
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
		switch (view_mode) {
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
				SetViewMode (ModeType.PhotoView);
			} else {
				icon_view.ThumbnailWidth = width;
			}			
			break;
		}
	}
	
	public void HandleDeleteCommand (object sender, EventArgs args)
	{
   		Photo[] photos = SelectedPhotos();
   		string header = Mono.Posix.Catalog.GetPluralString ("Delete the selected photo permanently?", 
								 "Delete the {0} selected photos permanently?", 
								 photos.Length);
		header = String.Format (header, photos.Length);
		string msg = Mono.Posix.Catalog.GetPluralString ("This deletes all versions of the selected photo from your drive.", 
								 "This deletes all versions of the selected photos from your drive.", 
								 photos.Length);
		string ok_caption = Mono.Posix.Catalog.GetPluralString ("_Delete photo", "_Delete photos", photos.Length);
		if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(main_window, DialogFlags.DestroyWithParent, MessageType.Warning, header, msg, ok_caption)) {                              
			foreach (Photo photo in photos) {
				foreach (uint id in photo.VersionIds) {
					Console.WriteLine (" path == {0}", photo.GetVersionPath (id)); 
					photo.DeleteVersion (id, true);
				}

				db.Photos.Remove (photo);
			}

			UpdateQuery ();
		}
	}

	public void HandleRemoveCommand (object sender, EventArgs args)
	{
   		Photo[] photos = SelectedPhotos();
   		string header = Mono.Posix.Catalog.GetPluralString ("Remove the selected photo from F-Spot?",
								 "Remove the {0} selected photos from F-Spot?", 
								 photos.Length);

		header = String.Format (header, photos.Length);
		string msg = Mono.Posix.Catalog.GetString("If you remove photos from the F-Spot catalog all tag information will be lost. The photos remain on your computer and can be imported into F-Spot again.");
		string ok_caption = Mono.Posix.Catalog.GetString("_Remove from Catalog");
		if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(main_window, DialogFlags.DestroyWithParent, 
									   MessageType.Warning, header, msg, ok_caption)) {                              
			foreach (Photo photo in photos) {
				db.Photos.Remove (photo);
			}

			UpdateQuery ();
		}
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
 		string header = Mono.Posix.Catalog.GetPluralString ("Delete the selected tag?",
								 "Delete the {0} selected tags?", 
								 tags.Length);

		header = String.Format (header, tags.Length);
		string msg = Mono.Posix.Catalog.GetString("If you delete a tag, all associations with photos are lost.");
		string ok_caption = Mono.Posix.Catalog.GetPluralString ("_Delete tag", "_Delete tags", tags.Length);
		 
 		if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(main_window, DialogFlags.DestroyWithParent, MessageType.Warning, header, msg, ok_caption)) {                              
 			db.Photos.Remove (tags);
			icon_view.QueueDraw ();
 		}
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
		SetViewMode (ModeType.IconView);
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
		edit_selected_tag.Sensitive = tag_sensitive;

		attach_tag_to_selection.Sensitive = tag_sensitive && active_selection;
		remove_tag_from_selection.Sensitive = tag_sensitive && active_selection;
	}

}

