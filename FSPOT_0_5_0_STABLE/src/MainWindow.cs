using Gdk;
using Gtk;
using GtkSharp;
using Glade;
using Mono.Addins;
using Mono.Unix;
using System;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mail;

using Banshee.Kernel;

using FSpot;
using FSpot.Extensions;
using FSpot.Query;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;

using LibGPhoto2;

public class MainWindow {

    public static MainWindow Toplevel;

	Db db;

	Sidebar sidebar;

	TagSelectionWidget tag_selection_widget;
	[Glade.Widget] Gtk.Window main_window;

	[Glade.Widget] Gtk.HPaned main_hpaned;
	[Glade.Widget] Gtk.VBox left_vbox;
	[Glade.Widget] Gtk.VBox group_vbox;
	[Glade.Widget] Gtk.VBox view_vbox;

	[Glade.Widget] Gtk.VBox toolbar_vbox;

	[Glade.Widget] ScrolledWindow icon_view_scrolled;
	[Glade.Widget] Box photo_box;
	[Glade.Widget] Notebook view_notebook;
	
	ScrolledWindow tag_selection_scrolled;

	[Glade.Widget] Label status_label;

	// File
	[Glade.Widget] MenuItem version_menu_item;
	[Glade.Widget] MenuItem create_version_menu_item;
	[Glade.Widget] MenuItem delete_version_menu_item;
	[Glade.Widget] MenuItem rename_version_menu_item;
	
	[Glade.Widget] MenuItem tools;
	[Glade.Widget] MenuItem export;
	[Glade.Widget] MenuItem pagesetup_menu_item;
	[Glade.Widget] MenuItem print;
	[Glade.Widget] MenuItem send_mail;

	// Edit
	[Glade.Widget] MenuItem copy_location;
	[Glade.Widget] MenuItem select_none;
	[Glade.Widget] MenuItem rotate_left;
	[Glade.Widget] MenuItem rotate_right;

	[Glade.Widget] MenuItem sharpen;
	[Glade.Widget] MenuItem adjust_time;

	[Glade.Widget] MenuItem update_thumbnail;
	[Glade.Widget] MenuItem delete_from_drive;
	[Glade.Widget] MenuItem remove_from_catalog;
	[Glade.Widget] MenuItem set_as_background;

	[Glade.Widget] MenuItem attach_tag;
	[Glade.Widget] MenuItem remove_tag;

	// View
	[Glade.Widget] CheckMenuItem display_toolbar;
	[Glade.Widget] CheckMenuItem display_sidebar;
	[Glade.Widget] CheckMenuItem display_timeline;
	[Glade.Widget] CheckMenuItem display_filmstrip;
	[Glade.Widget] CheckMenuItem display_dates_menu_item;
	[Glade.Widget] CheckMenuItem display_tags_menu_item;
	[Glade.Widget] CheckMenuItem display_ratings_menu_item;

	[Glade.Widget] MenuItem zoom_in;
	[Glade.Widget] MenuItem zoom_out;
	[Glade.Widget] CheckMenuItem loupe_menu_item;

	[Glade.Widget] RadioMenuItem tag_icon_hidden;
	[Glade.Widget] RadioMenuItem tag_icon_small;
	[Glade.Widget] RadioMenuItem tag_icon_medium;
	[Glade.Widget] RadioMenuItem tag_icon_large;

	[Glade.Widget] RadioMenuItem month;
	[Glade.Widget] RadioMenuItem directory;
	[Glade.Widget] CheckMenuItem reverse_order;

	// Find
	[Glade.Widget] MenuItem find_by_tag;
	[Glade.Widget] MenuItem find_add_tag;
	[Glade.Widget] MenuItem find_add_tag_with;
	
	[Glade.Widget] MenuItem clear_date_range;
	[Glade.Widget] MenuItem clear_rating_filter;

	[Glade.Widget] CheckMenuItem find_untagged;
	
	[Glade.Widget] MenuItem clear_roll_filter;	
	
	// Tags
	[Glade.Widget] MenuItem edit_selected_tag;
	[Glade.Widget] MenuItem delete_selected_tag;

	[Glade.Widget] MenuItem attach_tag_to_selection;
	[Glade.Widget] MenuItem remove_tag_from_selection;
	
	// Other Widgets
	[Glade.Widget] Scale zoom_scale;

	[Glade.Widget] VBox info_vbox;

	[Glade.Widget] Gtk.HBox tagbar;
	[Glade.Widget] Gtk.VBox tag_entry_container;
	[Glade.Widget] Gtk.VBox sidebar_vbox;
	TagEntry tag_entry;

	Gtk.Toolbar toolbar;

	FindBar find_bar;

	PhotoVersionMenu versions_submenu;

	Gtk.ToggleToolButton browse_button;
	Gtk.ToggleToolButton edit_button;

	InfoBox info_box;
	QueryView icon_view;
	PhotoView photo_view;
	FSpot.FullScreenView fsview;
	FSpot.PhotoQuery query;
	FSpot.GroupSelector group_selector;
	FSpot.QueryWidget query_widget;
	MainSelection selection;
	
	ToolButton rl_button;
	ToolButton rr_button;

	Label count_label;

	Gtk.ToolButton display_next_button;
	Gtk.ToolButton display_previous_button;
	
	ModeType view_mode;
	bool write_metadata = false;

	// Tag Icon Sizes
	public int TagsIconSize {
		get { return (int) Tag.TagIconSize; }
		set { Tag.TagIconSize = (Tag.IconSize) value; }
	}

	public PhotoView PhotoView {
		get { return photo_view; }
	}

	// Drag and Drop
	public enum TargetType {
		UriList,
		TagList,
		TagQueryItem,
		PhotoList,
		RootWindow
	};

	private static TargetEntry [] icon_source_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList),
		new TargetEntry ("application/x-fspot-tag-query-item", 0, (uint) TargetType.TagQueryItem),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
		new TargetEntry ("application/x-root-window-drop", 0, (uint) TargetType.RootWindow)
	};

	private static TargetEntry [] icon_dest_target_table = new TargetEntry [] {
#if ENABLE_REPARENTING
		new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList),
#endif
		new TargetEntry ("application/x-fspot-tags", 0, (uint) TargetType.TagList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
	};

	private static TargetEntry [] tag_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-tags", 0, (uint) TargetType.TagList),
	};

	private static TargetEntry [] tag_dest_target_table = new TargetEntry [] {
		new TargetEntry ("application/x-fspot-photos", 0, (uint) TargetType.PhotoList),
		new TargetEntry ("text/uri-list", 0, (uint) TargetType.UriList),
		new TargetEntry ("application/x-fspot-tags", 0, (uint) TargetType.TagList),
	};

	const int PHOTO_IDX_NONE = -1;

	private static Gtk.Tooltips toolTips;
	public static Gtk.Tooltips ToolTips {
		get {
			if (toolTips == null)
				toolTips = new Gtk.Tooltips ();
			return toolTips;
		}
	}

	//
	// Public Properties
	//

	public Db Database {
		get { return db; }
	}

	public Gtk.Window Window {
		get { return main_window; }
	}
	
	public ModeType ViewMode {
		get { return view_mode; }
	}

	public MainSelection Selection {
		get { return selection; }
	}

    public MenuItem FindByTag {
        get { return find_by_tag; }
    }

	public InfoBox InfoBox {
		get { return info_box; }
	}

	//
	// Constructor
	//
	public MainWindow (Db db)
	{
		this.db = db;

		if (Toplevel == null)
			Toplevel = this;

		Glade.XML gui = new Glade.XML (null, "f-spot.glade", "main_window", "f-spot");
		gui.Autoconnect (this);

		LoadPreference (Preferences.MAIN_WINDOW_WIDTH);
		LoadPreference (Preferences.MAIN_WINDOW_X);
		LoadPreference (Preferences.MAIN_WINDOW_MAXIMIZED);
		main_window.ShowAll ();

		LoadPreference (Preferences.SIDEBAR_POSITION);
		LoadPreference (Preferences.METADATA_EMBED_IN_IMAGE);

		LoadPreference (Preferences.COLOR_MANAGEMENT_ENABLED);
 		LoadPreference (Preferences.COLOR_MANAGEMENT_USE_X_PROFILE);
 		FSpot.ColorManagement.LoadSettings();
	
#if GTK_2_10
		pagesetup_menu_item.Activated += HandlePageSetupActivated;
#else
		pagesetup_menu_item.Visible = false;
#endif
		toolbar = new Gtk.Toolbar ();
		toolbar_vbox.PackStart (toolbar);

		ToolButton import_button = GtkUtil.ToolButtonFromTheme ("gtk-add", Catalog.GetString ("Import"), false);
		import_button.Clicked += HandleImportCommand;
		import_button.SetTooltip (ToolTips, Catalog.GetString ("Import new images"), null);
		toolbar.Insert (import_button, -1);
	
		toolbar.Insert (new SeparatorToolItem (), -1);

		ToolButton rl_button = GtkUtil.ToolButtonFromTheme ("object-rotate-left", Catalog.GetString ("Rotate Left"), true);
		rl_button.Clicked += HandleRotate270Command;
		toolbar.Insert (rl_button, -1);

		ToolButton rr_button = GtkUtil.ToolButtonFromTheme ("object-rotate-right", Catalog.GetString ("Rotate Right"), true);
		rr_button.Clicked += HandleRotate90Command;
		toolbar.Insert (rr_button, -1);

		toolbar.Insert (new SeparatorToolItem (), -1);

		browse_button = new ToggleToolButton ();
		browse_button.Label = Catalog.GetString ("Browse");
		browse_button.IconName = "mode-browse";
		browse_button.IsImportant = true;
		browse_button.Toggled += HandleToggleViewBrowse;
		browse_button.SetTooltip (ToolTips, Catalog.GetString ("Browse many photos simultaneously"), null);
		toolbar.Insert (browse_button, -1);

		edit_button = new ToggleToolButton ();
		edit_button.Label = Catalog.GetString ("Edit Image");
		edit_button.IconName = "mode-image-edit";
		edit_button.IsImportant = true;
		edit_button.Toggled += HandleToggleViewPhoto;
		edit_button.SetTooltip (ToolTips, Catalog.GetString ("View and edit a photo"), null);
		toolbar.Insert (edit_button, -1);

		toolbar.Insert (new SeparatorToolItem (), -1);

		ToolButton fs_button = GtkUtil.ToolButtonFromTheme ("view-fullscreen", Catalog.GetString ("Fullscreen"), true);
		fs_button.Clicked += HandleViewFullscreen;
		fs_button.SetTooltip (ToolTips, Catalog.GetString ("View photos fullscreen"), null);
		toolbar.Insert (fs_button, -1);

		ToolButton ss_button = GtkUtil.ToolButtonFromTheme ("media-playback-start", Catalog.GetString ("Slideshow"), true);
		ss_button.Clicked += HandleViewSlideShow;
		ss_button.SetTooltip (ToolTips, Catalog.GetString ("View photos in a slideshow"), null);
		toolbar.Insert (ss_button, -1);

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
		display_previous_button.SetTooltip (ToolTips, Catalog.GetString ("Previous photo"), String.Empty);
		display_previous_button.Clicked += new EventHandler (HandleDisplayPreviousButtonClicked);

		display_next_button = new ToolButton (Stock.GoForward);
		toolbar.Insert (display_next_button, -1);
		display_next_button.SetTooltip (ToolTips, Catalog.GetString ("Next photo"), String.Empty);
		display_next_button.Clicked += new EventHandler (HandleDisplayNextButtonClicked);

		sidebar = new Sidebar ();
		ViewModeChanged += sidebar.HandleMainWindowViewModeChanged;
		sidebar_vbox.Add (sidebar);

		tag_selection_scrolled = new ScrolledWindow ();
		tag_selection_scrolled.ShadowType = ShadowType.In;
		
		tag_selection_widget = new TagSelectionWidget (db.Tags);
		tag_selection_scrolled.Add (tag_selection_widget);

		sidebar.AppendPage (tag_selection_scrolled, Catalog.GetString ("Tags"), "tag");

		AddinManager.AddExtensionNodeHandler ("/FSpot/Sidebar", OnSidebarExtensionChanged);

		sidebar.Context = ViewContext.Library;
 		
		sidebar.CloseRequested += HideSidebar;
		sidebar.Show ();

		info_box = new InfoBox ();
		ViewModeChanged += info_box.HandleMainWindowViewModeChanged;
		info_box.VersionIdChanged += delegate (InfoBox box, uint version_id) { UpdateForVersionIdChange (version_id);};
		sidebar_vbox.PackEnd (info_box, false, false, 0);

		info_box.Context = ViewContext.Library;
		
		tag_selection_widget.Selection.Changed += HandleTagSelectionChanged;
		tag_selection_widget.DragDataGet += HandleTagSelectionDragDataGet;
		tag_selection_widget.DragDrop += HandleTagSelectionDragDrop;
		tag_selection_widget.DragBegin += HandleTagSelectionDragBegin;
		tag_selection_widget.KeyPressEvent += HandleTagSelectionKeyPress;
		Gtk.Drag.SourceSet (tag_selection_widget, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    tag_target_table, DragAction.Copy | DragAction.Move);

		tag_selection_widget.DragDataReceived += HandleTagSelectionDragDataReceived;
		tag_selection_widget.DragMotion += HandleTagSelectionDragMotion;
		Gtk.Drag.DestSet (tag_selection_widget, DestDefaults.All, tag_dest_target_table, 
				  DragAction.Copy | DragAction.Move ); 

		tag_selection_widget.ButtonPressEvent += HandleTagSelectionButtonPressEvent;
		tag_selection_widget.PopupMenu += HandleTagSelectionPopupMenu;
		tag_selection_widget.RowActivated += HandleTagSelectionRowActivated;
		
		LoadPreference (Preferences.TAG_ICON_SIZE);
		
		try {
			query = new FSpot.PhotoQuery (db.Photos);
		} catch (System.Exception e) {
			//FIXME assume any exception here is due to a corrupt db and handle that.
			new RepairDbDialog (e, db.Repair (), main_window);
			query = new FSpot.PhotoQuery (db.Photos);
		}

		UpdateStatusLabel ();
		query.Changed += HandleQueryChanged;

		db.Photos.ItemsChanged += HandleDbItemsChanged;
		db.Tags.ItemsChanged += HandleTagsChanged;
		db.Tags.ItemsAdded += HandleTagsChanged;
		db.Tags.ItemsRemoved += HandleTagsChanged;
#if SHOW_CALENDAR
		FSpot.SimpleCalendar cal = new FSpot.SimpleCalendar (query);
		cal.DaySelected += HandleCalendarDaySelected;
		left_vbox.PackStart (cal, false, true, 0);
#endif

		group_selector = new FSpot.GroupSelector ();
		group_selector.Adaptor = new FSpot.TimeAdaptor (query, Preferences.Get<bool> (Preferences.GROUP_ADAPTOR_ORDER_ASC));

		group_selector.ShowAll ();
		
		if (zoom_scale != null) {
			zoom_scale.ValueChanged += HandleZoomScaleValueChanged;
		}

		view_vbox.PackStart (group_selector, false, false, 0);
		view_vbox.ReorderChild (group_selector, 0);

		find_bar = new FindBar (query, tag_selection_widget.Model);
		view_vbox.PackStart (find_bar, false, false, 0);
		view_vbox.ReorderChild (find_bar, 1);
		main_window.KeyPressEvent += HandleKeyPressEvent;
		
		query_widget = new FSpot.QueryWidget (query, db, tag_selection_widget);
		query_widget.Logic.Changed += HandleQueryLogicChanged;
		view_vbox.PackStart (query_widget, false, false, 0);
		view_vbox.ReorderChild (query_widget, 2);

		icon_view = new QueryView (query);
		icon_view.ZoomChanged += HandleZoomChanged;
		LoadPreference (Preferences.ZOOM);
		LoadPreference (Preferences.SHOW_TAGS);
		LoadPreference (Preferences.SHOW_DATES);
		LoadPreference (Preferences.SHOW_RATINGS);
		icon_view_scrolled.Add (icon_view);
		icon_view.DoubleClicked += HandleDoubleClicked;
		icon_view.Vadjustment.ValueChanged += HandleIconViewScroll;
		icon_view.GrabFocus ();

		new FSpot.PreviewPopup (icon_view);

		Gtk.Drag.SourceSet (icon_view, Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
				    icon_source_target_table, DragAction.Copy | DragAction.Move);
		
		icon_view.DragBegin += HandleIconViewDragBegin;
		icon_view.DragDataGet += HandleIconViewDragDataGet;

		TagMenu tag_menu = new TagMenu (null, Database.Tags);
		tag_menu.NewTagHandler += delegate { HandleCreateTagAndAttach (this, null); };
		tag_menu.TagSelected += HandleAttachTagMenuSelected;
		tag_menu.Populate();
		attach_tag.Submenu = tag_menu;
		
		PhotoTagMenu pmenu = new PhotoTagMenu ();
		pmenu.TagSelected += HandleRemoveTagMenuSelected;
		remove_tag.Submenu = pmenu;
		
		Gtk.Drag.DestSet (icon_view, DestDefaults.All, icon_dest_target_table, 
				  DragAction.Copy | DragAction.Move); 

		//		icon_view.DragLeave += new DragLeaveHandler (HandleIconViewDragLeave);
		icon_view.DragMotion += HandleIconViewDragMotion;
		icon_view.DragDrop += HandleIconViewDragDrop;
		icon_view.DragDataReceived += HandleIconViewDragDataReceived;
		icon_view.KeyPressEvent += HandleIconViewKeyPressEvent;

		photo_view = new PhotoView (query);
		photo_box.Add (photo_view);

		photo_view.DoubleClicked += HandleDoubleClicked;
		photo_view.KeyPressEvent += HandlePhotoViewKeyPressEvent;
		photo_view.UpdateStarted += HandlePhotoViewUpdateStarted;
		photo_view.UpdateFinished += HandlePhotoViewUpdateFinished;

		photo_view.View.ZoomChanged += HandleZoomChanged;

		// Tag typing: focus the tag entry if the user starts typing a tag
		icon_view.KeyPressEvent += HandlePossibleTagTyping;
		photo_view.KeyPressEvent += HandlePossibleTagTyping;
		tag_entry = new TagEntry (db.Tags);
		tag_entry.KeyPressEvent += HandleTagEntryKeyPressEvent;
		tag_entry.TagsAttached += HandleTagEntryTagsAttached;
		tag_entry.TagsRemoved += HandleTagEntryRemoveTags;
		tag_entry.Activated += HandleTagEntryActivate;
		tag_entry_container.Add (tag_entry);

		Gtk.Drag.DestSet (photo_view, DestDefaults.All, tag_target_table, 
				  DragAction.Copy | DragAction.Move); 

		photo_view.DragMotion += HandlePhotoViewDragMotion;
		photo_view.DragDrop += HandlePhotoViewDragDrop;
		photo_view.DragDataReceived += HandlePhotoViewDragDataReceived;

		view_notebook.SwitchPage += HandleViewNotebookSwitchPage;
		group_selector.Adaptor.GlassSet += HandleAdaptorGlassSet;
		group_selector.Adaptor.Changed += HandleAdaptorChanged;
		LoadPreference (Preferences.GROUP_ADAPTOR);
		LoadPreference (Preferences.GROUP_ADAPTOR_ORDER_ASC);

		this.selection = new MainSelection (this);
		this.selection.Changed += HandleSelectionChanged;
		this.selection.ItemsChanged += HandleSelectionItemsChanged;
		this.selection.Changed += sidebar.HandleSelectionChanged;
		this.selection.ItemsChanged += sidebar.HandleSelectionItemsChanged;

		Mono.Addins.AddinManager.ExtensionChanged += PopulateExtendableMenus;
		PopulateExtendableMenus (null, null);

		UpdateMenus ();

		main_window.ShowAll ();

		tagbar.Hide ();
		find_bar.Hide ();

		UpdateFindByTagMenu ();

		LoadPreference (Preferences.SHOW_TOOLBAR);
		LoadPreference (Preferences.SHOW_SIDEBAR);
		LoadPreference (Preferences.SHOW_TIMELINE);
		LoadPreference (Preferences.SHOW_FILMSTRIP);

		LoadPreference (Preferences.GNOME_MAILTO_ENABLED);
		
		Preferences.SettingChanged += OnPreferencesChanged;

		main_window.DeleteEvent += HandleDeleteEvent;
		
		query_widget.HandleChanged (query);
		query_widget.Hide ();

		// When the icon_view is loaded, set it's initial scroll position
		icon_view.SizeAllocated += HandleIconViewReady;

		export.Activated += HandleExportActivated;
		UpdateToolbar ();

		Banshee.Kernel.Scheduler.Resume ();
	}

	private void HandleDisplayNextButtonClicked (object sender, EventArgs args)
	{
		PhotoView.View.Item.MoveNext ();
	}

	private void HandleDisplayPreviousButtonClicked (object sender, EventArgs args)
	{
		PhotoView.View.Item.MovePrevious ();
	}

	private void OnSidebarExtensionChanged (object s, ExtensionNodeEventArgs args) {
		// FIXME: No sidebar page removal yet!
		if (args.Change == ExtensionChange.Add)
			sidebar.AppendPage ((args.ExtensionNode as SidebarPageNode).GetSidebarPage ());
	}

	private Photo CurrentPhoto {
		get {
			int active = ActiveIndex ();
			if (active >= 0)
				return query [active] as Photo;
			else
				return null;
		}
	}

	// Index into the PhotoQuery.  If -1, no photo is selected or multiple photos are selected.
	private int ActiveIndex () 
	{
		if (selection.Count == 1)
			return SelectedIds() [0];
		else
			return PHOTO_IDX_NONE;
	}

	// Switching mode.
	public enum ModeType {
		IconView,
		PhotoView
	};

	public event EventHandler ViewModeChanged;

	public void SetViewMode (ModeType value)
	{
		if (view_mode == value)
			return;

		view_mode = value;
		switch (view_mode) {
		case ModeType.IconView:
			if (view_notebook.CurrentPage != 0)
				view_notebook.CurrentPage = 0;

			display_timeline.Sensitive = true;
			display_filmstrip.Sensitive = false;
			group_selector.Visible = display_timeline.Active;

			if (photo_view.View.Loupe != null)
				loupe_menu_item.Active = false;
			JumpTo (photo_view.Item.Index);
			zoom_scale.Value = icon_view.Zoom;
			break;
		case ModeType.PhotoView:
			if (view_notebook.CurrentPage != 1)
				view_notebook.CurrentPage = 1;

			display_timeline.Sensitive = false;
			display_filmstrip.Sensitive = true;
			group_selector.Visible = false;
			
			JumpTo (icon_view.FocusCell);
			zoom_scale.Value = photo_view.NormalizedZoom;
			break;
		}
		Selection.MarkChanged ();
		UpdateToolbar ();
		if (ViewModeChanged != null) 
			ViewModeChanged (this, null);
	}
	
	void UpdateToolbar ()
	{
		if (browse_button != null) {
			bool state = view_mode == ModeType.IconView;
			
			if (browse_button.Active != state)
				browse_button.Active = state;
		}

		if (edit_button != null) {
			bool state = view_mode == ModeType.PhotoView;
			
			if (edit_button.Active != state)
				edit_button.Active = state;
		}

		if (view_mode == ModeType.PhotoView) {
			display_previous_button.Visible = true;
			display_next_button.Visible = true;
			count_label.Visible = true;

			bool valid = photo_view.View.Item.IsValid;
			bool prev = valid && photo_view.View.Item.Index > 0;
			bool next = valid && photo_view.View.Item.Index < query.Count - 1;

			if (valid) {
				Gnome.Vfs.Uri vfs = new Gnome.Vfs.Uri (photo_view.View.Item.Current.DefaultVersionUri.ToString ());
				valid = vfs.Scheme == "file";
			}

			display_previous_button.Sensitive = prev;
			display_next_button.Sensitive = next;

			if (Query == null)
				count_label.Text = String.Empty;
			else
				// Note for translators: This indicates the current photo is photo {0} of {1} out of photos
				count_label.Text = String.Format (Catalog.GetString ("{0} of {1}"), Query.Count == 0 ? 0 : photo_view.View.Item.Index + 1, Query.Count == 0 ? 0 : Query.Count);
		} else {
			display_previous_button.Visible = false;
			display_next_button.Visible = false;
			count_label.Visible = false;
		}

	}

	private void HandleExportActivated (object o, EventArgs e)
	{
		FSpot.Extensions.ExportMenuItemNode.SelectedImages = delegate () {return new FSpot.PhotoArray (SelectedPhotos ()); };
	}

	private void HandleDbItemsChanged (object sender, DbItemEventArgs args)
	{
		foreach (DbItem item in args.Items) {
			Photo p = item as Photo;
			if (p == null)
				continue;
			if (write_metadata)
				FSpot.Jobs.SyncMetadataJob.Create (db.Jobs, p);
		}
		
		if (args is PhotoEventArgs && (args as PhotoEventArgs).Changes.TimeChanged)
			query.RequestReload ();
	}

	private void HandleTagsChanged (object sender, DbItemEventArgs args)
	{
		icon_view.QueueDraw ();
		UpdateTagEntryFromSelection ();	
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

	private int [] SelectedIds () {
		int [] ids = new int [0];

		if (fsview != null && fsview.View.Item.IsValid)
			ids = new int [] { fsview.View.Item.Index };
		else {
			switch (view_mode) {
			case ModeType.IconView:
				ids = icon_view.Selection.Ids;
				break;
			default:
			case ModeType.PhotoView:
				if (photo_view.Item.IsValid)
					ids = new int [] { photo_view.Item.Index };
				break;
			}
		}

		return ids;
	}

	public class MainSelection : IBrowsableCollection {
		MainWindow win;

		public MainSelection (MainWindow win)
		{
			this.win = win;
			win.icon_view.Selection.Changed += HandleSelectionChanged;
			win.icon_view.Selection.ItemsChanged += HandleSelectionItemsChanged;
			win.photo_view.PhotoChanged += HandlePhotoChanged;
			win.query.ItemsChanged += HandleQueryItemsChanged;
		}
		
		public int Count {
			get {
				switch (win.view_mode) {
				case ModeType.PhotoView:
					return win.photo_view.Item.IsValid ? 1 : 0;
				case ModeType.IconView:
					return win.icon_view.Selection.Count;
				}
				return 0;
			}
		}

		public int IndexOf (IBrowsableItem item)
		{
			switch (win.view_mode) {
			case ModeType.PhotoView:
				return item == win.photo_view.Item.Current ? 0 : -1;
			case ModeType.IconView:
				return win.icon_view.Selection.IndexOf (item);
			}
			return -1;
		}
		
		public bool Contains (IBrowsableItem item)
		{
			switch (win.view_mode) {
			case ModeType.PhotoView:
				return item == win.photo_view.Item.Current ? true : false;
			case ModeType.IconView:
				return win.icon_view.Selection.Contains (item);
			}
			return false;
		}
		
		public void MarkChanged ()
		{
			if (Changed != null)
				Changed (this);
		}

		public void MarkChanged (int index, IBrowsableItemChanges changes)
		{
			throw new System.NotImplementedException ("I didn't think you'd find me");
		}
		
		public IBrowsableItem this [int index] {
			get {
				switch (win.view_mode) {
				case ModeType.PhotoView:
					if (index == 0)
						return win.photo_view.Item.Current;
					break;
				case ModeType.IconView:
					return win.icon_view.Selection [index];
				}
				throw new ArgumentOutOfRangeException ();
			}
		}
		 
		public IBrowsableItem [] Items {
			get {
				switch (win.view_mode) {
				case ModeType.PhotoView:
					if (win.photo_view.Item.IsValid)
						return new IBrowsableItem [] {win.photo_view.Item.Current};

					break;
				case ModeType.IconView:
					return win.icon_view.Selection.Items;
				}
				return new IBrowsableItem [0];
			}
		}

		private void HandleQueryItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
		{
			// FIXME for now we only listen to changes directly from the query
			// when we are in PhotoView mode because we presume that we'll get
			// proper notification from the icon view selection in icon view mode
			if (win.view_mode != ModeType.PhotoView || ItemsChanged == null) 
				return;

			foreach (int item in args.Items) {
				if (win.photo_view.Item.Index == item ) {
					ItemsChanged (this, new BrowsableEventArgs (item, args.Changes));
					break;
				}
			}
		}

		private void HandlePhotoChanged (PhotoView sender)
		{
			if (win.view_mode == ModeType.PhotoView && Changed != null)
				Changed (this);
		}

		public void HandleSelectionChanged (IBrowsableCollection collection)
		{
			if (win.view_mode == ModeType.IconView && Changed != null)
				Changed (this);


		}

		private void HandleSelectionItemsChanged (IBrowsableCollection collection,  BrowsableEventArgs args)
		{
			if (win.view_mode == ModeType.IconView && ItemsChanged != null)
				ItemsChanged (this, args);
		}

		public event IBrowsableCollectionChangedHandler Changed;
		public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
	}

	private void HandleSelectionChanged (IBrowsableCollection collection)
	{
		UpdateMenus ();
		UpdateTagEntryFromSelection ();
		UpdateStatusLabel ();
		UpdateToolbar ();

		info_box.Photos = SelectedPhotos ();
	}

	private void HandleSelectionItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
	{
		UpdateMenus ();
		UpdateTagEntryFromSelection ();

		info_box.Photos = SelectedPhotos ();
	}


	//
	// Selection Interface
	//

	private Photo [] SelectedPhotos (int [] selected_ids)
	{
		Photo [] photo_list = new Photo [selected_ids.Length];
	
		int i = 0;
		foreach (int num in selected_ids)
			photo_list [i ++] = query [num] as Photo;
		
		return photo_list;
	}

	public Photo [] SelectedPhotos () 
	{
		return SelectedPhotos (SelectedIds ());
	}

	[Obsolete ("MARKED FOR REMOVAL")]
	public Photo [] ActivePhotos () 
	{
		return query.Photos;
	}

	public PhotoQuery Query {
		get { return query; }
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

	private void RotateSelectedPictures (Gtk.Window parent, RotateDirection direction)
	{
		RotateCommand command = new RotateCommand (parent);
		
		int [] selected_ids = SelectedIds ();
		if (command.Execute (direction, SelectedPhotos (selected_ids)))
#if MONO_1_9_0
			query.MarkChanged (selected_ids, new PhotoChanges () {DataChanged = true});
#else
		{
			PhotoChanges changes = new PhotoChanges ();
			changes.DataChanged = true;
			query.MarkChanged (selected_ids, changes);
		}
#endif
	}

	//
	// Tag Selection Drag Handlers
	//
	[Obsolete ("Use AddTagExtended (int [], Tag []) instead")]
	public void AddTagExtended (int num, Tag [] tags)
	{
		AddTagExtended (new int [] {num}, tags);
	}

	public void AddTagExtended (int [] nums, Tag [] tags)
	{
		foreach (int num in nums)
			(query[num] as Photo).AddTag (tags);
		query.Commit (nums);

		foreach (Tag t in tags) {
			if (t.Icon != null)
				continue;
			// FIXME this needs a lot more work.
			Pixbuf icon = null;
			try {
				Pixbuf tmp = FSpot.PhotoLoader.LoadAtMaxSize (query [nums[0]], 128, 128);
				icon = PixbufUtils.TagIconFromPixbuf (tmp);
				tmp.Dispose ();
			} catch {
				icon = null;
			}
			
			t.Icon = icon;
			db.Tags.Commit (t);
		}
	}

	public void RemoveTags (int [] nums, Tag [] tags)
	{
		foreach (int num in nums)
			(query[num] as Photo).RemoveTag (tags);
		query.Commit (nums);
	}

	void HandleTagSelectionRowActivated (object sender, RowActivatedArgs args)
	{
		ShowQueryWidget ();
		query_widget.Include (new Tag [] {tag_selection_widget.TagByPath (args.Path)});
	}

	void HandleTagSelectionButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		if (args.Event.Button == 3) {
			TagPopup popup = new TagPopup ();
			popup.Activate (args.Event, tag_selection_widget.TagAtPosition (args.Event.X, args.Event.Y),
			tag_selection_widget.TagHighlight);
			args.RetVal = true;
		}
	}

	void HandleTagSelectionPopupMenu (object sender, PopupMenuArgs args)
	{
		TagPopup popup = new TagPopup ();
		popup.Activate (null, null, tag_selection_widget.TagHighlight);
		args.RetVal = true;
	}

	void HandleTagSelectionDragBegin (object sender, DragBeginArgs args)
	{
		Tag [] tags = tag_selection_widget.TagHighlight;
		int len = tags.Length;
		int size = 32;
		int csize = size/2 + len * size / 2 + 2;
		
		Pixbuf container = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, csize, csize);
		container.Fill (0x00000000);
		
		bool use_icon = false;;
		while (len-- > 0) {
			Pixbuf thumbnail = tags[len].Icon;
			
			if (thumbnail != null) {
				Pixbuf small = PixbufUtils.ScaleToMaxSize (thumbnail, size, size);				
				
				int x = len * (size/2) + (size - small.Width)/2;
				int y = len * (size/2) + (size - small.Height)/2;

				small.Composite (container, x, y, small.Width, small.Height, x, y, 1.0, 1.0, Gdk.InterpType.Nearest, 0xff);
				small.Dispose ();

				use_icon = true;
			}
		}
		if (use_icon)
			Gtk.Drag.SetIconPixbuf (args.Context, container, 0, 0);
		container.Dispose ();
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
        TreeViewDropPosition position = TreeViewDropPosition.IntoOrAfter;
		tag_selection_widget.GetPathAtPos (args.X, args.Y, out path);

        if (path == null)
            return;

        // Tags can be dropped before, after, or into another tag
        if (args.Context.Targets[0].Name == "application/x-fspot-tags") {
            Gdk.Rectangle rect = tag_selection_widget.GetCellArea(path, tag_selection_widget.Columns[0]);
            double vpos = Math.Abs(rect.Y - args.Y) / (double)rect.Height;
            if (vpos < 0.2) {
                position = TreeViewDropPosition.Before;
            } else if (vpos > 0.8) {
                position = TreeViewDropPosition.After;
            }
        }

		tag_selection_widget.SetDragDestRow (path, position);

		// Scroll if within 20 pixels of the top or bottom of the tag list
		if (args.Y < 20)
			tag_selection_scrolled.Vadjustment.Value -= 30;
        else if (((o as Gtk.Widget).Allocation.Height - args.Y) < 20)
			tag_selection_scrolled.Vadjustment.Value += 30;
	}

	public void HandleTagSelectionDragDataReceived (object o, DragDataReceivedArgs args)
	{
        TreePath path;
        TreeViewDropPosition position;
        if (!tag_selection_widget.GetDestRowAtPos((int)args.X, (int)args.Y, out path, out position))
            return;

        Tag tag = path == null ? null : tag_selection_widget.TagByPath (path);
		if (tag == null)
			return;

		switch (args.Info) {
		case (uint)TargetType.PhotoList:
			db.BeginTransaction ();
			AddTagExtended (SelectedIds (), new Tag[] {tag});
			db.CommitTransaction ();
			query_widget.PhotoTagsChanged (new Tag[] {tag});
			break;
		case (uint)TargetType.UriList:
			UriList list = new UriList (args.SelectionData);
			
			db.BeginTransaction ();
			List<Photo> photos = new List<Photo> ();
			foreach (Uri photo_uri in list) {
				Photo photo = db.Photos.GetByUri (photo_uri);
				
				// FIXME - at this point we should import the photo, and then continue
				if (photo == null)
					continue;
				
				// FIXME this should really follow the AddTagsExtended path too
				photo.AddTag (new Tag[] {tag});
				photos.Add (photo);
			}
			db.Photos.Commit (photos.ToArray ());
			db.CommitTransaction ();
			InvalidateViews ();
			break;
		case (uint)TargetType.TagList:
			Category parent;
            if (position == TreeViewDropPosition.Before || position == TreeViewDropPosition.After) {
                parent = tag.Category;
            } else {
                parent = tag as Category;
            }

			if (parent == null || tag_selection_widget.TagHighlight.Length < 1) {
                args.RetVal = false;
				return;
            }

            int moved_count = 0;
            Tag [] highlighted_tags = tag_selection_widget.TagHighlight;
			foreach (Tag child in tag_selection_widget.TagHighlight) {
                // FIXME with this reparenting via dnd, you cannot move a tag to root.
                if (child != parent && child.Category != parent && !child.IsAncestorOf(parent)) {
                    child.Category = parent as Category;

                    // Saving changes will automatically cause the TreeView to be updated
                    db.Tags.Commit (child);
                    moved_count++;
                }
            }

            // Reselect the same tags
            tag_selection_widget.TagHighlight = highlighted_tags;

            args.RetVal = moved_count > 0;
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
			JumpTo (query.LookupItem (time));
	}

	private void JumpTo (int index)
	{
		switch (view_mode) {
		case ModeType.PhotoView:
			photo_view.Item.Index = index;
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

	void HandleAdaptorChanged (FSpot.GroupAdaptor sender)
	{
		UpdateGlass ();
	}

	/*
	 * Keep the glass temporal slider in sync with the user's scrolling in the icon_view
	 */
	private void UpdateGlass ()
	{
		// If people cant see the timeline don't update it.
		if (! display_timeline.Active)
			return;

		int cell_num = icon_view.TopLeftVisibleCell();
		if (cell_num == -1 /*|| cell_num == lastTopLeftCell*/)
			return;

		FSpot.IBrowsableItem photo = icon_view.Collection [cell_num];
#if false
		group_selector.Adaptor.GlassSet -= HandleAdaptorGlassSet;
		group_selector.Adaptor.SetGlass (group_selector.Adaptor.IndexFromPhoto (photo));
		group_selector.Adaptor.GlassSet = HandleAdaptorGlassSet;
#else
		/* 
		 * FIXME this is a lame hack to get around a delegate chain.  This should 
		 * actually operate directly on the adaptor not on the selector but I don't have 
		 * time to fix it right now.
		 */
		group_selector.SetPosition (group_selector.Adaptor.IndexFromPhoto (photo));
#endif
	}
	
	void HandleIconViewScroll (object sender, EventArgs args)
	{
		UpdateGlass ();
	}

	void HandleIconViewReady (object sender, EventArgs args)
	{
		LoadPreference (Preferences.GLASS_POSITION);

		// We only want to set the position the first time
		// the icon_view is ready (eg on startup)
		icon_view.SizeAllocated -= HandleIconViewReady;
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
				if (entry != null) {
					if (FSpot.ColorManagement.IsEnabled) {
						//FIXME
						thumbnail = entry.ShallowCopyPixbuf ();
						thumbnail = thumbnail.Copy ();
						FSpot.ColorManagement.ApplyScreenProfile (thumbnail);
					}
					else
						thumbnail = entry.ShallowCopyPixbuf ();
				}
				
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

	public void ImportUriList (UriList list, bool copy) 
	{
		ImportCommand command = new ImportCommand (main_window);
		if (command.ImportFromPaths (db.Photos, list.ToLocalPaths (), copy) > 0) {
			query.RollSet = new RollSet (db.Rolls.GetRolls (1)[0]);
			UpdateQuery ();
		}
	}

	public void ImportFile (string path)
	{
		ImportCommand command = new ImportCommand (main_window);
		if (command.ImportFromFile (db.Photos, path) > 0) {
			query.RollSet = new RollSet (db.Rolls.GetRolls (1)[0]);
			UpdateQuery ();
		}
	}

#if false
	public void ImportUdi (string udi)
	{
		/* probably a camera we need to contruct on of our gphoto2 uris */
		Hal.Device dev = new Hal.Device (Core.HalContext, udi);
		string mount_point = dev.GetPropertyString ("volume.mount_point");
		int bus = dev.GetPropertyInt ("usb.bus_number");
		int device = dev.GetPropertyInt ("usb.linux.device_number");
		System.Console.WriteLine ("dev = {1} exists = {2} mount_point = {0} {3},{4}", mount_point, dev, dev.Exists, bus, device);

		if (! dev.Exists || mount_point != null) {
			ImportFile (mount_point);
		} else {
			string gphoto_uri = String.Format ("gphoto2:usb:{0},{1}", bus.ToString ("d3") , device.ToString ("d3"));
			System.Console.WriteLine ("gphoto_uri = {0}", gphoto_uri);
			ImportCamera (gphoto_uri);
		} 
			
	}
#endif

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
				if (icon_view.Selection.Contains (item))
					AttachTags (tag_selection_widget.TagHighlight, SelectedIds());
				else 
					AttachTags (tag_selection_widget.TagHighlight, new int [] {item});
			}
			break;
		case (uint)TargetType.UriList:

			/* 
			 * If the drop is coming from inside f-spot then we don't want to import 
			 */
			if (source != null)
				return;

			UriList list = new UriList (args.SelectionData);
			ImportUriList (list, (args.Context.Action & Gdk.DragAction.Copy) != 0);
			break;
#if ENABLE_REPARENTING
		case (uint)TargetType.PhotoList:
			int p_item = icon_view.CellAtPosition (args.X + (int) icon_view.Hadjustment.Value, 
							     args.Y + (int) icon_view.Vadjustment.Value);

			if (p_item >= 0)
			{
				PhotoVersionCommands.Reparent cmd = new PhotoVersionCommands.Reparent ();
				
				cmd.Execute (db.Photos, SelectedPhotos(), query.Photos [p_item], GetToplevel (null));
				UpdateQuery ();
			}
	
			break;
#endif
		}

		Gtk.Drag.Finish (args.Context, true, false, args.Time);
	}

	//
	// IconView event handlers
	// 

	void HandleDoubleClicked (Widget widget, BrowsableEventArgs args)
	{
		switch (ViewMode) {
		case ModeType.IconView:
			icon_view.FocusCell = args.Items[0];
			SetViewMode (ModeType.PhotoView);
			break;
		case ModeType.PhotoView:
			SetViewMode (ModeType.IconView);
			break;
		}
	}

	public void HandleCommonPhotoCommands (object sender, Gtk.KeyPressEventArgs args) {
		bool alt = ModifierType.Mod1Mask == (args.Event.State & ModifierType.Mod1Mask);
		bool shift = ModifierType.ShiftMask == (args.Event.State & ModifierType.ShiftMask);

		if (args.RetVal == null)
			args.RetVal = false;
		
		switch (args.Event.Key) {
		case Gdk.Key.Delete:
			if (shift)
				HandleDeleteCommand (sender, args);
			else
				HandleRemoveCommand (sender, args);
			break;
		case Gdk.Key.Key_0:
			if (alt)
				HandleRatingMenuSelected (0);
			break;
		case Gdk.Key.Key_1:
			if (alt)
				HandleRatingMenuSelected (1);
			break;
		case Gdk.Key.Key_2:
			if (alt)
				HandleRatingMenuSelected (2);
			break;
		case Gdk.Key.Key_3:
			if (alt)
				HandleRatingMenuSelected (3);
			break;
		case Gdk.Key.Key_4:
			if (alt)
				HandleRatingMenuSelected (4);
			break;
		case Gdk.Key.Key_5:
			if (alt)
				HandleRatingMenuSelected (5);
			break;
		default:
			return; //do not set the RetVal to true
		}
		args.RetVal = true;
	}

	void HandleIconViewKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
	{
		HandleCommonPhotoCommands (sender, args);
		if ((bool)args.RetVal)
			return;

		switch (args.Event.Key) {
		case Gdk.Key.F:
		case Gdk.Key.f:
			HandleViewFullscreen (sender, args);
			args.RetVal = true;
			break;
		}
	}

	//
	// FullScreenView event handlers.
	//

	void HandleFullScreenViewKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
	{
		HandleCommonPhotoCommands (sender, args);
		if ((bool)args.RetVal)
			// this will hide any panels again that might have appeared above the fullscreen view
			fsview.Present ();
	}

	//
	// PhotoView event handlers.
	//

	void HandlePhotoViewKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
	{
		HandleCommonPhotoCommands (sender, args);
		if ((bool)args.RetVal)
			return;

		switch (args.Event.Key) {
		case Gdk.Key.F:
		case Gdk.Key.f:
			HandleViewFullscreen (sender, args);
			args.RetVal = true;
			break;
		case Gdk.Key.Escape:
			SetViewMode (ModeType.IconView);
			args.RetVal = true;
			break;
		}
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
	// RatingMenu commands
	//
	
	public void HandleRatingMenuSelected (int r) 
	{
		if (view_mode == ModeType.PhotoView)
			this.photo_view.UpdateRating(r);

		Photo p;
		db.BeginTransaction ();
		int [] selected_photos = SelectedIds ();
		foreach (int num in selected_photos) {
			p = query [num] as Photo;
			p.Rating = (uint) r;
		}
		query.Commit (selected_photos);
		db.CommitTransaction ();
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
		db.BeginTransaction ();
		AddTagExtended (SelectedIds (), new Tag [] {t});
		db.CommitTransaction ();
		query_widget.PhotoTagsChanged (new Tag[] {t});
	}
	
	public void HandleRequireTag (object sender, EventArgs args)
 	{
        ShowQueryWidget ();
		query_widget.Require (tag_selection_widget.TagHighlight);
 	}
 
	public void HandleUnRequireTag (object sender, EventArgs args)
	{
		query_widget.UnRequire (tag_selection_widget.TagHighlight);
 	}

	public void HandleRemoveTagMenuSelected (Tag t)
	{
		db.BeginTransaction ();
		RemoveTags (SelectedIds (), new Tag [] {t});
		db.CommitTransaction ();
		query_widget.PhotoTagsChanged (new Tag [] {t});
	}

	//
	// Main menu commands
	//

	void HandleOpenCommand (object sender, EventArgs e)
	{
		new FSpot.SingleView ();
	}

	void HandleImportCommand (object sender, EventArgs e)
	{
		db.Sync = false;
		ImportCommand command = new ImportCommand (main_window);
		if (command.ImportFromFile (db.Photos, null) > 0) {
			query.RollSet = new RollSet (db.Rolls.GetRolls (1)[0]);
			UpdateQuery ();
		}
		db.Sync = true;		
	}

	void HandleImportFromCameraCommand (object sender, EventArgs e)
	{
		ImportCamera (null);
	}

	public void ImportCamera (string camera_device)
	{
		GPhotoCamera cam = new GPhotoCamera();

		try {
			int num_cameras = cam.DetectCameras();
			int selected_cam = 0;

			if (num_cameras < 1) {
				HigMessageDialog md = new HigMessageDialog (main_window, DialogFlags.DestroyWithParent, 
					MessageType.Warning, ButtonsType.Ok, 
					Catalog.GetString ("No cameras detected."),
					Catalog.GetString ("F-Spot was unable to find any cameras attached to this system." + 
								      "  Double check that the camera is connected and has power")); 

				md.Run ();
				md.Destroy ();
				return;
			} else if (num_cameras == 1) {
				selected_cam = 0;
			} else {
				bool found = false;
				if (camera_device != null) {
					string port = camera_device.Remove (0, "gphoto2:".Length);
					for (int i = 0; i < num_cameras; i++)
						if (cam.CameraList.GetValue (i) == port) {
							selected_cam = i;
							found = true;
							break;
						}
				}
				
				if (!found) {
					FSpot.CameraSelectionDialog camselect = new FSpot.CameraSelectionDialog (cam.CameraList);
					selected_cam = camselect.Run ();
				}
			}

			if (selected_cam >= 0) {
				cam.SelectCamera (selected_cam);	
				cam.InitializeCamera ();

				FSpot.CameraFileSelectionDialog selector = new FSpot.CameraFileSelectionDialog (cam, db);
				if (selector.Run() > 0)
					query.RollSet = new RollSet (db.Rolls.GetRolls (1)[0]);
				UpdateQuery ();
			}
		}
		catch (GPhotoException ge) {
			System.Console.WriteLine (ge.ToString ());
			HigMessageDialog md = new HigMessageDialog (main_window, DialogFlags.DestroyWithParent, 
				MessageType.Error, ButtonsType.Ok, 
				Catalog.GetString ("Error connecting to camera"),
				String.Format (Catalog.GetString ("Received error \"{0}\" while connecting to camera"), 
				ge.Message));

			md.Run ();
			md.Destroy ();
		} finally {
			cam.ReleaseGPhotoResources ();
		}
	}
#if GTK_2_10
	void HandlePageSetupActivated (object o, EventArgs e)
	{
		FSpot.Global.PageSetup = Print.RunPageSetupDialog (this.Window, FSpot.Global.PageSetup, null);
	}
#endif
	
	void HandlePrintCommand (object sender, EventArgs e)
	{
#if !GTK_2_10
		new FSpot.PrintDialog (SelectedPhotos ());
#else
		FSpot.PrintOperation print = new FSpot.PrintOperation (SelectedPhotos ());
		print.Run (PrintOperationAction.PrintDialog, null);
#endif
	}

	private Gtk.Dialog info_display_window;
	public void HandleInfoDisplayDestroy (object sender, EventArgs args)
	{
		info_display_window = null;
	}

	public void HandlePreferences (object sender, EventArgs args)
	{
		PreferenceDialog.Show ();
	}

	public void HandleManageExtensions (object sender, EventArgs args)
	{
		Mono.Addins.Gui.AddinManagerWindow.Run (main_window);
	}
	
	void HandleViewDirectory (object sender, EventArgs args)
	{
		Gtk.Window win = new Gtk.Window ("Directory View");
		FSpot.Widgets.IconView view = new FSpot.Widgets.IconView (new FSpot.DirectoryCollection (System.IO.Directory.GetCurrentDirectory ()));
		new FSpot.PreviewPopup (view);

		view.DisplayTags = false;

		Gtk.ScrolledWindow scrolled = new ScrolledWindow ();
		win.Add (scrolled);
		scrolled.Add (view);
		win.ShowAll ();
	}

	void HandleViewSelection (object sender, EventArgs args)
	{
		Gtk.Window win = new Gtk.Window ("This is a window");
		Gtk.ScrolledWindow scroll = new Gtk.ScrolledWindow ();
	
		win.Add (scroll);
		scroll.Add (new TrayView (icon_view.Selection));
		win.ShowAll ();
	}

	private void TestDisplay ()
	{
		Gtk.Window win = new Gtk.Window ("hello");
		VBox box = new VBox ();
		box.PackStart (new FSpot.Widgets.ImageDisplay (new BrowsablePointer (new FSpot.PhotoArray (SelectedPhotos ()), 0)));
		win.Add (box);
		win.ShowAll ();
	}

	void HandleSendMailCommand (object sender, EventArgs args)
	{
		//TestDisplay ();
		new FSpot.SendEmail (new FSpot.PhotoArray (SelectedPhotos ()));
	}

	public static void HandleHelp (object sender, EventArgs args)
	{
		GnomeUtil.ShowHelp ("f-spot.xml", null, FSpot.Global.HelpDirectory, Toplevel.Window.Screen);
	}

	public static void HandleAbout (object sender, EventArgs args)
	{
		FSpot.UI.Dialog.AboutDialog.ShowUp ();
	}

	void HandleTagSizeChange (object sender, EventArgs args)
	{
		RadioMenuItem choice = sender as RadioMenuItem;
	
		//Get this callback twice. Once for the active going menuitem,
		//once for the inactive leaving one. Ignore the inactive.
		if (!choice.Active)
			return;

		int old_size = TagsIconSize;
		
		if (choice == tag_icon_hidden) {
			TagsIconSize = (int) Tag.IconSize.Hidden;
		} else if (choice == tag_icon_small) {
			TagsIconSize = (int) Tag.IconSize.Small;
		} else if (choice == tag_icon_medium) {
			TagsIconSize = (int) Tag.IconSize.Medium;
		} else if (choice == tag_icon_large) {
			TagsIconSize = (int) Tag.IconSize.Large;
		} else {
			return;
		}
		
		if (old_size != TagsIconSize) {
			tag_selection_widget.ColumnsAutosize();
			Preferences.Set (Preferences.TAG_ICON_SIZE, TagsIconSize);
		}
	}

	public void HandleArrangeByTime (object sender, EventArgs args)
	{
		if (group_selector.Adaptor is TimeAdaptor)
			return;

		group_selector.Adaptor.GlassSet -= HandleAdaptorGlassSet;
		group_selector.Adaptor.Changed -= HandleAdaptorChanged;
		group_selector.Adaptor = new FSpot.TimeAdaptor (query, reverse_order.Active);

		group_selector.Mode = FSpot.GroupSelector.RangeType.Min;
		group_selector.Adaptor.GlassSet += HandleAdaptorGlassSet;
		group_selector.Adaptor.Changed += HandleAdaptorChanged;

		if (sender != month)
			month.Active = true;

		//update the selection in the Timeline
		if (query.Range != null)
			group_selector.SetLimitsToDates(query.Range.Start, query.Range.End);
	}

	public void HandleArrangeByDirectory (object sender, EventArgs args)
	{
		if (group_selector.Adaptor is DirectoryAdaptor)
			return;

		group_selector.Adaptor.GlassSet -= HandleAdaptorGlassSet;
		group_selector.Adaptor.Changed -= HandleAdaptorChanged;
		group_selector.Adaptor = new FSpot.DirectoryAdaptor (query, reverse_order.Active); 	

		group_selector.Mode = FSpot.GroupSelector.RangeType.Min;
		group_selector.Adaptor.GlassSet += HandleAdaptorGlassSet;
		group_selector.Adaptor.Changed += HandleAdaptorChanged;

		if (sender != directory)
			directory.Active = true;
	}
	
	public void HandleReverseOrder (object sender, EventArgs args)
	{
		Gtk.CheckMenuItem item = sender as Gtk.CheckMenuItem;

		if (group_selector.Adaptor.OrderAscending == item.Active)
			return;
		
		group_selector.Adaptor.OrderAscending = item.Active;
		query.TimeOrderAsc = item.Active;

		// FIXME this is blah...we need UIManager love here
		if (item != reverse_order)
			reverse_order.Active = item.Active;
		
		//update the selection in the timeline
		if ( query.Range != null && group_selector.Adaptor is TimeAdaptor) {
			group_selector.SetLimitsToDates(query.Range.Start, query.Range.End);
			
		}

	}

	// Called when the user clicks the X button	
	void HandleDeleteEvent (object sender, DeleteEventArgs args)
	{
		Close();
		args.RetVal = true;
	}

	void HandleCloseCommand (object sender, EventArgs args)
	{
		Close();
	}
	
	public void Close ()
	{
		int x, y, width, height;
		main_window.GetPosition (out x, out y);
		main_window.GetSize (out width, out height);

		bool maximized = ((main_window.GdkWindow.State & Gdk.WindowState.Maximized) > 0);
		Preferences.Set (Preferences.MAIN_WINDOW_MAXIMIZED, maximized);

		if (!maximized) {
			Preferences.Set (Preferences.MAIN_WINDOW_X,		x);
			Preferences.Set (Preferences.MAIN_WINDOW_Y,		y);
			Preferences.Set (Preferences.MAIN_WINDOW_WIDTH,		width);
			Preferences.Set (Preferences.MAIN_WINDOW_HEIGHT,	height);
		}

		Preferences.Set (Preferences.SHOW_TOOLBAR,		toolbar.Visible);
		Preferences.Set (Preferences.SHOW_SIDEBAR,		info_vbox.Visible);
		Preferences.Set (Preferences.SHOW_TIMELINE,		display_timeline.Active);
		Preferences.Set (Preferences.SHOW_FILMSTRIP,		display_filmstrip.Active);
		Preferences.Set (Preferences.SHOW_TAGS,			icon_view.DisplayTags);
		Preferences.Set (Preferences.SHOW_DATES,		icon_view.DisplayDates);
		Preferences.Set (Preferences.SHOW_RATINGS,		icon_view.DisplayRatings);

		Preferences.Set (Preferences.GROUP_ADAPTOR,		(group_selector.Adaptor is DirectoryAdaptor) ? 1 : 0);
		Preferences.Set (Preferences.GROUP_ADAPTOR_ORDER_ASC,   group_selector.Adaptor.OrderAscending);
		Preferences.Set (Preferences.GLASS_POSITION,		group_selector.GlassPosition);
		
		Preferences.Set (Preferences.SIDEBAR_POSITION,		main_hpaned.Position);
		Preferences.Set (Preferences.ZOOM,			icon_view.Zoom);

		tag_selection_widget.SaveExpandDefaults ();

		this.Window.Destroy ();
	}
	
	void HandleCreateVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Create cmd = new PhotoVersionCommands.Create ();

		cmd.Execute (db.Photos, CurrentPhoto, GetToplevel (null));
//		if (cmd.Execute (db.Photos, CurrentPhoto, GetToplevel (null))) {
//			query.MarkChanged (ActiveIndex (), true, false);
//		}
	}

	void HandleDeleteVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Delete cmd = new PhotoVersionCommands.Delete ();

		cmd.Execute (db.Photos, CurrentPhoto, GetToplevel (null));
//		if (cmd.Execute (db.Photos, CurrentPhoto, GetToplevel (null))) {
//			query.MarkChanged (ActiveIndex (), true, true);
//		}
	}

	void HandlePropertiesCommand (object obje, EventArgs args)
	{
		Photo [] photos = SelectedPhotos ();
		
	        long length = 0;

		foreach (Photo p in photos) {
			System.IO.FileInfo fi = new System.IO.FileInfo (p.DefaultVersionUri.LocalPath);

			length += fi.Length;
		}

		Console.WriteLine ("{0} Selected Photos : Total length = {1} - {2}kB - {3}MB", photos.Length, length, length / 1024, length / (1024 * 1024));
	}
		
	void HandleRenameVersionCommand (object obj, EventArgs args)
	{
		PhotoVersionCommands.Rename cmd = new PhotoVersionCommands.Rename ();

		cmd.Execute (db.Photos, CurrentPhoto, main_window);
//		if (cmd.Execute (db.Photos, CurrentPhoto, main_window)) {
//			query.MarkChanged (ActiveIndex (), true, false);
//		}
	}
	
	public void HandleCreateTagAndAttach (object sender, EventArgs args)
	{
		Tag new_tag = CreateTag (sender, args);

		if (new_tag != null)
			HandleAttachTagMenuSelected (new_tag);
	}

	public void HandleCreateNewCategoryCommand (object sender, EventArgs args)
	{
		Tag new_tag = CreateTag (sender, args);
		
		if (new_tag != null) {
			tag_selection_widget.ScrollTo (new_tag);
			tag_selection_widget.TagHighlight = new Tag [] {new_tag};
		}
	}

	public Tag CreateTag (object sender, EventArgs args)
	{
		TagCommands.Create command = new TagCommands.Create (db.Tags, GetToplevel (sender));
		return command.Execute (TagCommands.TagType.Category, tag_selection_widget.TagHighlight);
	}

	public void HandleAttachTagCommand (object obj, EventArgs args)
	{
		AttachTags (tag_selection_widget.TagHighlight, SelectedIds ());
	}

	void AttachTags (Tag [] tags, int [] ids) 
	{
		db.BeginTransaction ();
		AddTagExtended (ids, tags);
		db.CommitTransaction ();
		query_widget.PhotoTagsChanged (tags);
	}

	public void HandleRemoveTagCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight;

		db.BeginTransaction ();
		RemoveTags (SelectedIds (), tags);
		db.CommitTransaction ();
		query_widget.PhotoTagsChanged (tags);
	}

	public void HandleEditSelectedTag (object sender, EventArgs ea)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight;
		if (tags.Length != 1)
			return;

		HandleEditSelectedTagWithTag (tags [0]);
	}

	public void HandleEditSelectedTagWithTag (Tag tag)
	{
		if (tag == null)
			return;
		
		TagCommands.Edit command = new TagCommands.Edit (db, main_window);
		command.Execute (tag);
	}

	public void HandleMergeTagsCommand (object obj, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight;
		if (tags.Length < 2)
			return;
		
		// Translators, The singular case will never happen here.
		string header = Catalog.GetPluralString ("Merge the selected tag",
								    "Merge the {0} selected tags?", tags.Length);
		header = String.Format (header, tags.Length);

		// If a tag with children tags is selected for merging, we
		// should also merge its children..
		ArrayList all_tags = new ArrayList (tags.Length);
		foreach (Tag tag in tags) {
			if (! all_tags.Contains (tag))
				all_tags.Add (tag);
			else
				continue;

			if (! (tag is Category))
				continue;

			(tag as Category).AddDescendentsTo (all_tags);
		}

		// debug..
		tags = (Tag []) all_tags.ToArray (typeof (Tag));
		System.Array.Sort (tags, new TagRemoveComparer ());

		foreach (Tag tag in tags) {
			System.Console.WriteLine ("tag: {0}", tag.Name);
		}

		string msg = Catalog.GetString("This operation will merge the selected tags and any sub-tags into a single tag.");

		string ok_caption = Catalog.GetString ("_Merge Tags");
		
		if (ResponseType.Ok != HigMessageDialog.RunHigConfirmation(main_window, 
									   DialogFlags.DestroyWithParent, 
									   MessageType.Warning, 
									   header, 
									   msg, 
									   ok_caption))
			return;
		
		// The surviving tag is the last tag, as it is definitely not a child of any other the
		// other tags.  removetags will contain the tags to be merged.
		Tag survivor = tags[tags.Length - 1];
		
		Tag [] removetags = new Tag [tags.Length - 1];
		Array.Copy (tags, 0, removetags, 0, tags.Length - 1);

		// Add the surviving tag to all the photos with the other tags
		Photo [] photos = db.Photos.Query (removetags);
		foreach (Photo p in photos) {
			p.AddTag (survivor);
		}

		// Remove the defunct tags, which removes them from the photos, commits
		// the photos, and removes the tags from the TagStore
		db.BeginTransaction ();
		db.Photos.Remove (removetags);
		db.CommitTransaction ();

		HandleEditSelectedTagWithTag (survivor);
	}

	void HandleAdjustTime (object sender, EventArgs args)
	{
		PhotoList list = new PhotoList (Selection.Items);
		list.Sort (new Photo.CompareDateName ());
		new TimeDialog (db, list);
	}

	public void HideLoupe ()
	{
		loupe_menu_item.Active = false;
	}

	void HandleLoupe (object sender, EventArgs args)
	{
		// Don't steal characters from any text entries
		if (Window.Focus is Gtk.Entry && Gtk.Global.CurrentEvent is Gdk.EventKey) {
			Window.Focus.ProcessEvent (Gtk.Global.CurrentEvent);
			return;
		}
		
		photo_view.View.ShowHideLoupe ();
	}

	void HandleSharpen (object sender, EventArgs args)
	{
		// Don't steal characters from any text entries
		if (Window.Focus is Gtk.Entry && Gtk.Global.CurrentEvent is Gdk.EventKey) {
			Window.Focus.ProcessEvent (Gtk.Global.CurrentEvent);
			return;
		}
		
		photo_view.View.ShowSharpener ();
	}

	void HandleDisplayToolbar (object sender, EventArgs args)
	{
		if (display_toolbar.Active)
			toolbar.Show ();
		else
			toolbar.Hide ();
	}

	void HandleDisplayTags (object sender, EventArgs args)
	{
		icon_view.DisplayTags = !icon_view.DisplayTags;
	}
	
	void HandleDisplayDates (object sender, EventArgs args)
	{
		// Peg the icon_view's value to the MenuItem's active state,
		// as icon_view.DisplayDates's get won't always be equal to it's true value
		// because of logic to hide dates when zoomed way out.
		icon_view.DisplayDates = display_dates_menu_item.Active;
	}

	void HandleDisplayRatings (object sender, EventArgs args)
	{
		icon_view.DisplayRatings = display_ratings_menu_item.Active;
	}

	void HandleDisplayGroupSelector (object sender, EventArgs args)
	{
		if (group_selector.Visible)
			group_selector.Hide ();
		else
			group_selector.Show ();
	}

	void HandleDisplayFilmstrip (object sender, EventArgs args)
	{
		photo_view.FilmStripVisibility = display_filmstrip.Active;
		if (view_mode == ModeType.PhotoView)
			photo_view.QueueDraw ();
	}

	void HandleDisplayInfoSidebar (object sender, EventArgs args)
	{
		if (info_vbox.Visible)
			info_vbox.Hide ();
		else
			info_vbox.Show ();
	}

	void HandleViewSlideShow (object sender, EventArgs args)
	{
		HandleViewFullscreen (sender, args);
		fsview.PlayPause ();
	}

	void HandleToggleViewBrowse (object sender, EventArgs args)
	{
		if (view_mode == ModeType.IconView)
			browse_button.Active = true;
		else if (browse_button.Active)
			SetViewMode (ModeType.IconView);
	}

	void HandleToggleViewPhoto (object sender, EventArgs args)
	{
		if (view_mode == ModeType.PhotoView)
			edit_button.Active = true;
		else if (edit_button.Active)
			SetViewMode (ModeType.PhotoView);
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
		int active = (selection.Count > 0 ? SelectedIds() [0] : 0);
		if (fsview == null) {
			fsview = new FSpot.FullScreenView (query, main_window);
			fsview.Destroyed += HandleFullScreenViewDestroy;
			fsview.KeyPressEvent += HandleFullScreenViewKeyPressEvent;
			fsview.View.Item.Index = active;
		} else {
			// FIXME this needs to be another mode like PhotoView and IconView mode.
			fsview.View.Item.Index = active;
		}
		
		fsview.Show ();
	}

	void HandleFullScreenViewDestroy (object sender, EventArgs args)
	{
		JumpTo (fsview.View.Item.Index);
		fsview = null;
	}
	
	void HandleZoomScaleValueChanged (object sender, System.EventArgs args)
	{
		switch (view_mode) {
		case ModeType.PhotoView:
			photo_view.View.ZoomChanged -= HandleZoomChanged;
			photo_view.NormalizedZoom = zoom_scale.Value;
			photo_view.View.ZoomChanged += HandleZoomChanged;
			break;
		case ModeType.IconView:
			icon_view.ZoomChanged -= HandleZoomChanged;
			icon_view.Zoom = zoom_scale.Value;
			icon_view.ZoomChanged += HandleZoomChanged;
			break;
		}
		
		zoom_in.Sensitive = (zoom_scale.Value != 1.0);
		zoom_out.Sensitive = (zoom_scale.Value != 0.0);
	}
	
	void HandleQueryChanged (IBrowsableCollection sender)
	{
		if (find_untagged.Active != query.Untagged)
			find_untagged.Active = query.Untagged;
		
		clear_date_range.Sensitive = (query.Range != null);
		clear_rating_filter.Sensitive = (query.RatingRange != null);
		update_status_label = true;
		GLib.Idle.Add (UpdateStatusLabel);
	}

	bool update_status_label;
	private bool UpdateStatusLabel ()
	{
		update_status_label = false;
		int total_photos = Database.Photos.TotalPhotos;
		if (total_photos != query.Count)
			status_label.Text = String.Format (Catalog.GetPluralString ("{0} Photo out of {1}", "{0} Photos out of {1}", query.Count), query.Count, total_photos);
		else
			status_label.Text = String.Format (Catalog.GetPluralString ("{0} Photo", "{0} Photos", query.Count), query.Count);
	
		if ((selection != null) && (selection.Count > 0))
			status_label.Text += String.Format (Catalog.GetPluralString (" ({0} selected)", " ({0} selected)", selection.Count), selection.Count);
		status_label.UseMarkup = true;
		return update_status_label;
	}
	
	void HandleZoomChanged (object sender, System.EventArgs args)
	{
		zoom_scale.ValueChanged -= HandleZoomScaleValueChanged;

		double zoom = .5;
		switch (view_mode) {
		case ModeType.PhotoView:
			zoom = photo_view.NormalizedZoom;
			zoom_scale.Value = zoom;
			break;
		case ModeType.IconView:
			zoom = icon_view.Zoom;
			if (zoom == 0.0 || zoom == 100.0 || zoom != zoom_scale.Value)
				zoom_scale.Value = zoom;

			break;
		}
		
		zoom_in.Sensitive = (zoom != 1.0);
		zoom_out.Sensitive = (zoom != 0.0);
		
		zoom_scale.ValueChanged += HandleZoomScaleValueChanged;
	}

	void HandleZoomOut (object sender, ButtonPressEventArgs args)
	{
		ZoomOut ();
	}
	
	void HandleZoomOut (object sender, EventArgs args)
	{
		ZoomOut ();
	}
	
	void HandleZoomIn (object sender, ButtonPressEventArgs args)
	{
		ZoomIn ();
	}
	
	void HandleZoomIn (object sender, EventArgs args)
	{
		ZoomIn ();
	}
	
	private void ZoomOut ()
	{
		switch (view_mode) {
		case ModeType.PhotoView:
			photo_view.ZoomOut ();
			break;
		case ModeType.IconView:
			icon_view.ZoomOut ();
			break;
		}
	}
	
	private void ZoomIn ()
	{
		switch (view_mode) {
		case ModeType.PhotoView:
			double old_zoom = photo_view.Zoom;
			try {
				photo_view.ZoomIn ();
			} catch {
				photo_view.Zoom = old_zoom;
			}
			
			break;
		case ModeType.IconView:
			icon_view.ZoomIn ();
			break;
		}
	}

	public void DeleteException (Exception e, string fname)
	{
		string ok_caption = Catalog.GetString ("_Ok");
		string error = Catalog.GetString ("Error Deleting Picture");
		string msg;

		if (e is UnauthorizedAccessException)
			msg = String.Format (
				Catalog.GetString ("No permission to delete the file:{1}{0}"), 
				fname, Environment.NewLine).Replace ("_", "__");
		else
			msg = String.Format (
				Catalog.GetString ("An error of type {0} occurred while deleting the file:{2}{1}"),
				e.GetType (), fname.Replace ("_", "__"), Environment.NewLine);
		
		HigMessageDialog.RunHigConfirmation (
			main_window, DialogFlags.DestroyWithParent, MessageType.Error,
			error, msg, ok_caption);
	}

	public Gtk.Window GetToplevel (object sender)
	{
		Widget wsender = sender as Widget;
		Gtk.Window toplevel = null;

		if (wsender != null && !(wsender is MenuItem))
			toplevel = (Gtk.Window) wsender.Toplevel;
		else if (fsview != null)
			toplevel = fsview;
		else 
			toplevel = main_window;

		return toplevel;
	}

	public void HandleDeleteCommand (object sender, EventArgs args)
	{
   		Photo[] photos = SelectedPhotos();
   		string header = Catalog.GetPluralString ("Delete the selected photo permanently?", 
								    "Delete the {0} selected photos permanently?", 
								    photos.Length);
		header = String.Format (header, photos.Length);
		string msg = Catalog.GetPluralString ("This deletes all versions of the selected photo from your drive.", 
								 "This deletes all versions of the selected photos from your drive.", 
								 photos.Length);
		string ok_caption = Catalog.GetPluralString ("_Delete photo", "_Delete photos", photos.Length);
		
		if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(GetToplevel (sender), 
									   DialogFlags.DestroyWithParent, 
									   MessageType.Warning, 
									   header, msg, ok_caption)) {                              
			
			uint timer = Log.DebugTimerStart ();
			foreach (Photo photo in photos) {
				foreach (uint id in photo.VersionIds) {
					try {
						photo.DeleteVersion (id, true);
					} catch (Exception e) {
						DeleteException (e, photo.VersionUri (id).ToString ());
					}
				}
			}
			db.Photos.Remove (photos);
			
			UpdateQuery ();
			Log.DebugTimerPrint (timer, "HandleDeleteCommand took {0}");
		}
	}

	public void HandleRemoveCommand (object sender, EventArgs args)
	{
   		Photo[] photos = SelectedPhotos();
		if (photos.Length == 0) 
			return;

   		string header = Catalog.GetPluralString ("Remove the selected photo from F-Spot?",
								    "Remove the {0} selected photos from F-Spot?", 
								    photos.Length);

		header = String.Format (header, photos.Length);
		string msg = Catalog.GetString("If you remove photos from the F-Spot catalog all tag information will be lost. The photos remain on your computer and can be imported into F-Spot again.");
		string ok_caption = Catalog.GetString("_Remove from Catalog");
		if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(GetToplevel (sender), DialogFlags.DestroyWithParent, 
									   MessageType.Warning, header, msg, ok_caption)) {                              
			db.Photos.Remove (photos);
			UpdateQuery ();
		}
	}

	void HandleSelectAllCommand (object sender, EventArgs args)
	{
		icon_view.SelectAllCells ();
		UpdateStatusLabel();
	}

	void HandleSelectNoneCommand (object sender, EventArgs args)
	{
		icon_view.Selection.Clear ();
		UpdateStatusLabel();
	}

	// This ConnectBefore is needed because otherwise the editability of the name
	// column will steal returns, spaces, and clicks if the tag name is focused
	[GLib.ConnectBefore]
	public void HandleTagSelectionKeyPress (object sender, Gtk.KeyPressEventArgs args)
	{
		args.RetVal = true;

		switch (args.Event.Key) {
		case Gdk.Key.Delete:
 			HandleDeleteSelectedTagCommand (sender, (EventArgs) args);
			break;
		
		case Gdk.Key.space:
		case Gdk.Key.Return:
		case Gdk.Key.KP_Enter:
            ShowQueryWidget ();
            query_widget.Include (tag_selection_widget.TagHighlight);
			break;

		case Gdk.Key.F2:
			tag_selection_widget.EditSelectedTagName ();
			break;
		
		default:
			args.RetVal = false;
			break;
		}
	}

	public void HandleDeleteSelectedTagCommand (object sender, EventArgs args)
	{
		Tag [] tags = this.tag_selection_widget.TagHighlight;

		System.Array.Sort (tags, new TagRemoveComparer ());
	
		//How many pictures are associated to these tags?
		Db db = MainWindow.Toplevel.Database;
		FSpot.PhotoQuery count_query = new FSpot.PhotoQuery(db.Photos);
		count_query.Terms = FSpot.OrTerm.FromTags(tags);
		int associated_photos = count_query.Count;

		string header;
		if (tags.Length == 1)
			header = String.Format (Catalog.GetString ("Delete tag \"{0}\"?"), tags [0].Name.Replace ("_", "__"));
		else
			header = String.Format (Catalog.GetString ("Delete the {0} selected tags?"), tags.Length);
		
		header = String.Format (header, tags.Length);
		string msg = String.Empty;
		if (associated_photos > 0) {
			string photodesc = Catalog.GetPluralString ("photo", "photos", associated_photos);
			msg = String.Format( 
				Catalog.GetPluralString("If you delete this tag, the association with {0} {1} will be lost.",
							"If you delete these tags, the association with {0} {1} will be lost.",
							tags.Length),
				associated_photos, photodesc);
		}
		string ok_caption = Catalog.GetPluralString ("_Delete tag", "_Delete tags", tags.Length);
		
		if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation(main_window, 
									   DialogFlags.DestroyWithParent, 
									   MessageType.Warning, 
									   header, 
									   msg, 
									   ok_caption)) {                              
			try { 				
				db.Photos.Remove (tags);
			} catch (InvalidTagOperationException e) {
				System.Console.WriteLine ("this is something or another");

				// A Category is not empty. Can not delete it.
				string error_msg = Catalog.GetString ("Tag is not empty");
				string error_desc = String.Format (Catalog.GetString ("Can not delete tags that have tags within them.  " + 
												 "Please delete tags under \"{0}\" first"),
								   e.Tag.Name.Replace ("_", "__"));
				
				HigMessageDialog md = new HigMessageDialog (main_window, DialogFlags.DestroyWithParent, 
									    Gtk.MessageType.Error, ButtonsType.Ok, 
									    error_msg,
									    error_desc);
				md.Run ();
				md.Destroy ();
			}
		}
	}

	void HandleUpdateThumbnailCommand (object sender, EventArgs args)
	{
		ThumbnailCommand command = new ThumbnailCommand (main_window);

		int [] selected_ids = SelectedIds ();
		if (command.Execute (SelectedPhotos (selected_ids)))
#if MONO_1_9_0
			query.MarkChanged (selected_ids, new PhotoChanges {DataChanged = true});
#else
		{
			PhotoChanges changes = new PhotoChanges ();
			changes.DataChanged = true;
			query.MarkChanged (selected_ids, changes);
		}
#endif
	}

	public void HandleRotate90Command (object sender, EventArgs args)
	{
		// Don't steal characters from any text entries
		if (Window.Focus is Gtk.Entry && Gtk.Global.CurrentEvent is Gdk.EventKey) {
			Window.Focus.ProcessEvent (Gtk.Global.CurrentEvent);
			return;
		}
		
		RotateSelectedPictures (GetToplevel (sender), RotateDirection.Clockwise);
	}

	public void HandleRotate270Command (object sender, EventArgs args)
	{
		// Don't steal characters from any text entries
		if (Window.Focus is Gtk.Entry && Gtk.Global.CurrentEvent is Gdk.EventKey) {
			Window.Focus.ProcessEvent (Gtk.Global.CurrentEvent);
			return;
		}

		RotateSelectedPictures (GetToplevel (sender), RotateDirection.Counterclockwise);
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

			paths.Append (System.IO.Path.GetFullPath (p.DefaultVersionUri.LocalPath));
		}
		
		String data = paths.ToString ();
		primary.Text = data;
		clipboard.Text = data;
	}

	void HandleSetAsBackgroundCommand (object sender, EventArgs args)
	{
#if !NOGCONF
		Photo current = CurrentPhoto;

		if (current == null)
			return;

		GnomeUtil.SetBackgroundImage (current.DefaultVersionUri.LocalPath);
#endif
	}

	void HandleSetDateRange (object sender, EventArgs args) {
		DateCommands.Set set_command = new DateCommands.Set (query, main_window);
		set_command.Execute ();
		//update the TimeLine
		if (group_selector.Adaptor is TimeAdaptor && query.Range != null) 
			group_selector.SetLimitsToDates(query.Range.Start, query.Range.End);
	}

	public void HandleClearDateRange (object sender, EventArgs args) {
		if (group_selector.Adaptor is FSpot.TimeAdaptor) {
			group_selector.ResetLimits();
		}
		query.Range = null;
	}

	void HandleSelectLastRoll (object sender, EventArgs args) {
		query.RollSet = new RollSet (db.Rolls.GetRolls (1));
	}

	void HandleSelectRolls (object sender, EventArgs args) {
		new LastRolls (query, db.Rolls, main_window);
	}

	void HandleClearRollFilter (object sender, EventArgs args) {
		query.RollSet = null;
	}

	void HandleSetRatingFilter (object sender, EventArgs args) {
		RatingFilter.Set set_command = new RatingFilter.Set (query, main_window);
		set_command.Execute ();
	}

	public void HandleClearRatingFilter (object sender, EventArgs args) {
		query.RatingRange = null;
	}

	void HandleFindUntagged (object sender, EventArgs args) {
		if (query.Untagged == find_untagged.Active)
			return;

		query.Untagged = !query.Untagged;
	}
	
	void OnPreferencesChanged (object sender, NotifyEventArgs args)
	{
		LoadPreference (args.Key);
	}

	void LoadPreference (String key)
	{
		switch (key) {
		case Preferences.MAIN_WINDOW_MAXIMIZED:
			if (Preferences.Get<bool> (key))
				main_window.Maximize ();
			else
				main_window.Unmaximize ();
			break;

		case Preferences.MAIN_WINDOW_X:
		case Preferences.MAIN_WINDOW_Y:
			main_window.Move(Preferences.Get<int> (Preferences.MAIN_WINDOW_X),
					 Preferences.Get<int> (Preferences.MAIN_WINDOW_Y));
			break;
		
		case Preferences.MAIN_WINDOW_WIDTH:
		case Preferences.MAIN_WINDOW_HEIGHT:
			main_window.Resize(Preferences.Get<int> (Preferences.MAIN_WINDOW_WIDTH),
					   Preferences.Get<int> (Preferences.MAIN_WINDOW_HEIGHT));

			break;
		
		case Preferences.SHOW_TOOLBAR:
			if (display_toolbar.Active != Preferences.Get<bool> (key))
				display_toolbar.Active = Preferences.Get<bool> (key);
			break;
		
		case Preferences.SHOW_SIDEBAR:
			if (display_sidebar.Active != Preferences.Get<bool> (key))
				display_sidebar.Active = Preferences.Get<bool> (key);
			break;
		
		case Preferences.SHOW_TIMELINE:
			if (display_timeline.Active != Preferences.Get<bool> (key))
				display_timeline.Active = Preferences.Get<bool> (key);
			break;
		
		case Preferences.SHOW_FILMSTRIP:
			if (display_filmstrip.Active != Preferences.Get<bool> (key))
				display_filmstrip.Active = Preferences.Get<bool> (key);
			break;
		
		case Preferences.SHOW_TAGS:
			if (display_tags_menu_item.Active != Preferences.Get<bool> (key))
				display_tags_menu_item.Active = Preferences.Get<bool> (key);
			break;
		
		case Preferences.SHOW_DATES:
			if (display_dates_menu_item.Active != Preferences.Get<bool> (key))
				display_dates_menu_item.Active = Preferences.Get<bool> (key);
				//display_dates_menu_item.Toggle ();
			break;
		
		case Preferences.SHOW_RATINGS:
			if (display_ratings_menu_item.Active != Preferences.Get<bool> (key))
				display_ratings_menu_item.Active = Preferences.Get<bool> (key);
			break;
		
		case Preferences.GROUP_ADAPTOR:
			if (Preferences.Get<int> (key) == 1)
				directory.Active = true;
			break;

		case Preferences.GROUP_ADAPTOR_ORDER_ASC:
			group_selector.Adaptor.OrderAscending = Preferences.Get<bool> (key);
			reverse_order.Active = Preferences.Get<bool> (key);
			query.TimeOrderAsc = group_selector.Adaptor.OrderAscending;
			break;

		case Preferences.GLASS_POSITION:
			if (query.Count > 0) {
				// If the database has changed since this pref was saved, this could cause
				// an exception to be thrown.
				try {
					IBrowsableItem photo = group_selector.Adaptor.PhotoFromIndex (Preferences.Get<int> (key));
					
					if (photo != null)
						JumpTo (query.IndexOf (photo));
				} catch (Exception) {}
			}
			break;
		case Preferences.SIDEBAR_POSITION:
			if (main_hpaned.Position !=Preferences.Get<int> (key) )
				main_hpaned.Position = Preferences.Get<int> (key);
			break;

		case Preferences.TAG_ICON_SIZE:
			int s = Preferences.Get<int> (key);
			tag_icon_hidden.Active = (s == (int) Tag.IconSize.Hidden);
			tag_icon_small.Active = (s == (int) Tag.IconSize.Small);
			tag_icon_medium.Active = (s == (int) Tag.IconSize.Medium);
			tag_icon_large.Active = (s == (int) Tag.IconSize.Large);

			break;

		case Preferences.ZOOM:
			icon_view.Zoom = Preferences.Get<double> (key);
			break;
		
		case Preferences.METADATA_EMBED_IN_IMAGE:
			write_metadata =Preferences.Get<bool> (key) ;
			break;
		case Preferences.COLOR_MANAGEMENT_ENABLED:
			FSpot.ColorManagement.IsEnabled = Preferences.Get<bool> (key);
			break;
		case Preferences.COLOR_MANAGEMENT_USE_X_PROFILE:
			FSpot.ColorManagement.UseXProfile = Preferences.Get<bool> (key);
			break;
		case Preferences.GNOME_MAILTO_ENABLED:
			send_mail.Visible = Preferences.Get<bool> (key);
			break;
		}
	}

	// Version Id updates.

	void UpdateForVersionIdChange (uint version_id)
	{
		CurrentPhoto.DefaultVersionId = version_id;
		query.Commit (ActiveIndex ());
	}

	// Queries.

	public void UpdateQuery ()
	{
		main_window.GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Watch);
		main_window.GdkWindow.Display.Sync ();
		query.RequestReload ();
		main_window.GdkWindow.Cursor = null;
	}

	void HandleTagSelectionChanged (object obj, EventArgs args)
	{
		UpdateMenus ();
	}

	public bool TagIncluded (Tag tag)
	{
		return query_widget.TagIncluded (tag);
	}
	
	public bool TagRequired (Tag tag)
	{
		return query_widget.TagRequired (tag);
	}

	private void HandleQueryLogicChanged (object sender, EventArgs args)
	{
		HandleFindAddTagWith (null, null);
	}
	
	public void HandleIncludeTag (object sender, EventArgs args)
	{
		ShowQueryWidget ();
		query_widget.Include (tag_selection_widget.TagHighlight);
	}
	
	public void HandleUnIncludeTag (object sender, EventArgs args)
	{
		query_widget.UnInclude (tag_selection_widget.TagHighlight);
 	}
	
	void HandleFindByTag (object sender, EventArgs args)
	{
		UpdateFindByTagMenu ();
	}
	
	public void UpdateFindByTagMenu ()
	{
		if (query_widget.Visible) {
			query_widget.Close ();
		} else {
			ShowQueryWidget ();
		}
	}
	
	void HandleFindAddTagWith (object sender, EventArgs args)
	{
		if (find_add_tag_with.Submenu != null)
			find_add_tag_with.Submenu.Dispose ();
		
		Gtk.Menu submenu = FSpot.TermMenuItem.GetSubmenu (tag_selection_widget.TagHighlight);
		find_add_tag_with.Sensitive = (submenu != null);
		if (submenu != null) 
			find_add_tag_with.Submenu = submenu;	
	}
	
	public void HandleAddTagToTerm (object sender, EventArgs args)
	{
		MenuItem item = sender as MenuItem;
		
		int item_pos = 0;
		foreach (MenuItem i in (item.Parent as Menu).Children) {
			if (item == i) {
				break;
			}
			
			item_pos++;
		}
		// account for All and separator menu items
		item_pos -= 2;
		
		FSpot.Term parent_term = (FSpot.Term) FSpot.LogicWidget.Root.SubTerms [item_pos];
		
		if (FSpot.LogicWidget.Box != null) {
			FSpot.Literal after = parent_term.Last as FSpot.Literal;
			FSpot.LogicWidget.Box.InsertTerm (tag_selection_widget.TagHighlight, parent_term, after);
		}
	}
	
	void HandleFindTagIncluded (Tag t)
	{
		ShowQueryWidget ();
		query_widget.Include (new Tag [] {t});
 	}
	
	void HandleFindTagRequired (Tag t)
	{
		ShowQueryWidget ();
		query_widget.Require (new Tag [] {t});
	}

	//
	// Handle Main Menu 

	void UpdateMenus ()
	{
		int tags_selected = tag_selection_widget.Selection.CountSelectedRows ();
		bool tag_sensitive = tags_selected > 0;
		bool active_selection = selection.Count > 0;
		bool single_active = CurrentPhoto != null;
		
		if (!single_active) {
			version_menu_item.Sensitive = false;
			version_menu_item.Submenu = new Menu ();

			create_version_menu_item.Sensitive = false;
			delete_version_menu_item.Sensitive = false;
			rename_version_menu_item.Sensitive = false;

			sharpen.Sensitive = false;
			loupe_menu_item.Sensitive = false;
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
			versions_submenu.VersionIdChanged += delegate (PhotoVersionMenu menu) { UpdateForVersionIdChange (menu.VersionId);};
			version_menu_item.Submenu = versions_submenu;

			sharpen.Sensitive = (view_mode == ModeType.IconView ? false : true);
			loupe_menu_item.Sensitive = (view_mode == ModeType.IconView ? false : true);
		}

		set_as_background.Sensitive = single_active;
		adjust_time.Sensitive = active_selection;

		attach_tag.Sensitive = active_selection;
		remove_tag.Sensitive = active_selection;

		rotate_left.Sensitive = active_selection;
		rotate_right.Sensitive = active_selection;
		update_thumbnail.Sensitive = active_selection;
		delete_from_drive.Sensitive = active_selection;
		
		send_mail.Sensitive = active_selection;
		print.Sensitive = active_selection;
		select_none.Sensitive = active_selection;
		copy_location.Sensitive = active_selection;
		remove_from_catalog.Sensitive = active_selection;
		
		clear_rating_filter.Sensitive = (query.RatingRange != null);

		clear_roll_filter.Sensitive = (query.RollSet != null);
		
		delete_selected_tag.Sensitive = tag_sensitive;
		edit_selected_tag.Sensitive = tag_sensitive;


		attach_tag_to_selection.Sensitive = tag_sensitive && active_selection;
		remove_tag_from_selection.Sensitive = tag_sensitive && active_selection;
	
		export.Sensitive = active_selection;

		try {
			tools.Visible = (tools.Submenu as Menu).Children.Length > 0;
		} catch {
			tools.Visible = false;
		}

		if (rl_button != null) {
			if (selection.Count == 0) {
				rl_button.Sensitive = false;
				rl_button.SetTooltip (ToolTips, Catalog.GetString (String.Empty), null);
			} else {
				rl_button.Sensitive = true;

				string msg = Catalog.GetPluralString ("Rotate selected photo left",
								      "Rotate selected photos left", selection.Count);
				rl_button.SetTooltip (ToolTips, String.Format (msg, selection.Count), null);
			}
		}
		
		if (rr_button != null) {
			if (selection.Count == 0) {
				rr_button.Sensitive = false;
				rr_button.SetTooltip (ToolTips, Catalog.GetString (String.Empty), null);
			} else {
				rr_button.Sensitive = true;

				string msg = Catalog.GetPluralString ("Rotate selected photo right",
								      "Rotate selected photos right", selection.Count);
				rr_button.SetTooltip (ToolTips, String.Format (msg, selection.Count), null);
			}
		}

		//if (last_tags_selected_count != tags_selected) {
		((Gtk.Label)find_add_tag.Child).TextWithMnemonic = String.Format (
			Catalog.GetPluralString ("Find _Selected Tag", "Find _Selected Tags", tags_selected), tags_selected
		);

		((Gtk.Label)find_add_tag_with.Child).TextWithMnemonic = String.Format (
			Catalog.GetPluralString ("Find Selected Tag _With", "Find Selected Tags _With", tags_selected), tags_selected
		);

		find_add_tag.Sensitive = tag_sensitive;
		find_add_tag_with.Sensitive = tag_sensitive && find_add_tag_with.Submenu != null;

		//last_tags_selected_count = tags_selected;
		//}
	}

	void PopulateExtendableMenus (object o, EventArgs args)
	{
		try {
			if (export.Submenu != null)
				export.Submenu.Dispose ();
			if (tools.Submenu != null)
				tools.RemoveSubmenu ();

			export.Submenu = (Mono.Addins.AddinManager.GetExtensionNode ("/FSpot/Menus/Exports") as FSpot.Extensions.SubmenuNode).GetSubmenu ();
			export.Submenu.ShowAll ();

			tools.Submenu = (Mono.Addins.AddinManager.GetExtensionNode ("/FSpot/Menus/Tools") as FSpot.Extensions.SubmenuNode).GetSubmenu ();
			tools.Submenu.ShowAll ();

			tools.Visible = (tools.Submenu as Menu).Children.Length > 0;
		} catch {
			Console.WriteLine ("There's (maybe) something wrong with some of the installed extensions. You can try removing the directory addin-db-000 from ~/.gnome2/f-spot/");
			tools.Visible = false;
		}
	}

	public void HandleOpenWith (object sender, Gnome.Vfs.MimeApplication mime_application)
	{
		Photo[] selected = SelectedPhotos ();

		if (selected == null || selected.Length < 1)
			return;

		string header = Catalog.GetPluralString ("Create New Version?", "Create New Versions?", selected.Length); 
		string msg = String.Format (Catalog.GetPluralString (
				"Before launching {1}, should F-Spot create a new version of the selected photo to preserve the original?",
				"Before launching {1}, should F-Spot create new versions of the selected photos to preserve the originals?", selected.Length),
				selected.Length, mime_application.Name);

		// FIXME add cancel button? add help button?
		HigMessageDialog hmd = new HigMessageDialog(GetToplevel (sender), DialogFlags.DestroyWithParent, 
							    MessageType.Question, Gtk.ButtonsType.None,
							    header, msg);

		hmd.AddButton (Gtk.Stock.No, Gtk.ResponseType.No, false);
		//hmd.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, false);
		hmd.AddButton (Gtk.Stock.Yes, Gtk.ResponseType.Yes, true);

		Gtk.ResponseType response = Gtk.ResponseType.Cancel;

		try {
			response = (Gtk.ResponseType) hmd.Run();
		} finally {
			hmd.Destroy ();
		}

		if (response == Gtk.ResponseType.Cancel)
			return;

		bool create_new_versions = (response == Gtk.ResponseType.Yes);

		ArrayList errors = new ArrayList ();
		GLib.List uri_list = new GLib.List (typeof (string));
		foreach (Photo photo in selected) {
			try {
				if (create_new_versions) {
					uint version = photo.CreateNamedVersion (mime_application.Name, photo.DefaultVersionId, true);
					photo.DefaultVersionId = version;
				}
			} catch (Exception e) {
				errors.Add (new EditException (photo, e));
			}

			uri_list.Append (photo.DefaultVersionUri.ToString ());
		}

		// FIXME need to clean up the error dialog here.
		if (errors.Count > 0) {
			Dialog md = new EditExceptionDialog (GetToplevel (sender), errors.ToArray (typeof (EditException)) as EditException []);
			md.Run ();
			md.Destroy ();
		}

		if (create_new_versions)
			db.Photos.Commit (selected);

		mime_application.Launch (uri_list);
	}

	public void GetWidgetPosition(Widget widget, out int x, out int y)
    {
		main_window.GdkWindow.GetOrigin(out x, out y);
		
		x += widget.Allocation.X;
		y += widget.Allocation.Y;
 	}

	// Tag typing ...

	private void UpdateTagEntryFromSelection ()
	{
		if (!tagbar.Visible)
			return;
		tag_entry.UpdateFromSelection (SelectedPhotos ());
	}

	public void HandlePossibleTagTyping (object sender, Gtk.KeyPressEventArgs args)
	{
		if (Selection.Count == 0 || tagbar.Visible && tag_entry.HasFocus)
			return;

#if !ALLOW_TAG_TYPING_WITHOUT_HOTKEY
		if (args.Event.Key != Gdk.Key.t)
			return;
#endif

#if ALLOW_TAG_TYPING_WITHOUT_HOTKEY
		char c = System.Convert.ToChar (Gdk.Keyval.ToUnicode ((uint) args.Event.Key));
		if (! System.Char.IsLetter (c))
			return;
#endif
		
#if ALLOW_TAG_TYPING_WITHOUT_HOTKEY
		tag_entry.Text += c;
#endif

		tagbar.Show ();
		UpdateTagEntryFromSelection ();
		tag_entry.GrabFocus ();
		tag_entry.SelectRegion (-1, -1);
	}

	// "Activate" means the user pressed the enter key
	public void HandleTagEntryActivate (object sender, EventArgs args)
	{
	       if (view_mode == ModeType.IconView) {
		       icon_view.GrabFocus ();
	       } else {
		       photo_view.QueueDraw ();
		       photo_view.View.GrabFocus ();
	       }
	}

	private void HandleTagEntryTagsAttached (object o, string [] new_tags)
	{
		int [] selected_photos = SelectedIds ();
		if (selected_photos == null || new_tags == null || new_tags.Length == 0)
			return;

		Category default_category = null;
		Tag [] selection = tag_selection_widget.TagHighlight;
		if (selection.Length > 0) {
			if (selection [0] is Category)
				default_category = (Category) selection [0];
			else
				default_category = selection [0].Category;
		}
		Tag [] tags = new Tag [new_tags.Length];
		int i = 0;
		db.BeginTransaction ();
		foreach (string tagname in new_tags) {
			Tag t = db.Tags.GetTagByName (tagname);
			if (t == null) {
				t = db.Tags.CreateCategory (default_category, tagname) as Tag;
				db.Tags.Commit (t);
			}
			tags [i++] = t;
		}
		AddTagExtended (selected_photos, tags);
		db.CommitTransaction ();
	}

	private void HandleTagEntryRemoveTags (object o, Tag [] remove_tags)
	{
		int [] selected_photos = SelectedIds ();
		if (selected_photos == null || remove_tags == null || remove_tags.Length == 0)
			return;

		db.BeginTransaction ();
		RemoveTags (selected_photos, remove_tags);
		db.CommitTransaction ();
	}

	private void HideTagbar ()
	{
		if (! tagbar.Visible)
			return;
		
		UpdateTagEntryFromSelection ();

		// Cancel any pending edits...
		tagbar.Hide ();

		if (view_mode == ModeType.IconView)
			icon_view.GrabFocus ();
		else {
			photo_view.QueueDraw ();
			photo_view.View.GrabFocus ();
		}

		tag_entry.ClearTagCompletions ();
	}

	public void HandleTagBarCloseButtonPressed (object sender, EventArgs args)
	{
		HideTagbar ();
	}

	public void HandleTagEntryKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
	{
		args.RetVal = false;

		if (args.Event.Key == Gdk.Key.Escape) { 
			HideTagbar ();
			args.RetVal = true;
		}
	}

	public string [] SelectedMimeTypes ()
	{
		ArrayList mimes = new ArrayList ();

		foreach (Photo p in SelectedPhotos ()) {
			string mime = Gnome.Vfs.MimeType.GetMimeTypeForUri (p.DefaultVersionUri.ToString ());

			if (! mimes.Contains (mime))
				mimes.Add (mime);
		}

		return mimes.ToArray (typeof (string)) as string [];
	}

	private void ShowQueryWidget () {
		if (find_bar.Visible) {
			find_bar.Entry.Text = String.Empty;
			find_bar.Hide ();
		}
		
		query_widget.ShowBar ();
		return;
	}

	public void HideSidebar (object o, EventArgs args) {
		display_sidebar.Active = false;
	}
	
	public void HandleKeyPressEvent (object sender, Gtk.KeyPressEventArgs args)
	{
		bool ctrl = ModifierType.ControlMask == (args.Event.State & ModifierType.ControlMask);

		if ((ctrl && args.Event.Key == Gdk.Key.F) || args.Event.Key == Gdk.Key.slash) {
			if (!find_bar.Visible) {
				if (query_widget.Visible) {
					query_widget.Close ();
				}
				
				find_bar.ShowAll();
			}

			// Grab the focus even if it's already shown
			find_bar.Entry.GrabFocus ();
			args.RetVal = true;
			return;
		}
		
		args.RetVal = false;
	}
}
