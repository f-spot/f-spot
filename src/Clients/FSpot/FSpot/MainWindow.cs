//
// MainWindow.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2006-2010 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Linq;
using System.Collections.Generic;

using Gdk;
using Gtk;

using Mono.Addins;
using Mono.Unix;

using Hyena;
using Hyena.Widgets;

using FSpot.Core;
using FSpot.Database;
using FSpot.Database.Jobs;
using FSpot.Extensions;
using FSpot.Query;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Platform;
using FSpot.Import;
using FSpot.Settings;

namespace FSpot
{
	public class MainWindow
	{
		public Sidebar Sidebar { get; set; }
		public Gtk.Window Window => main_window;
		public Gtk.ToggleAction ReverseOrderAction => reverse_order;

		TagSelectionWidget tag_selection_widget;
		Gtk.ScrolledWindow tag_selection_scrolled;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.Window main_window;

		[GtkBeans.Builder.Object] Gtk.HPaned main_hpaned;
		[GtkBeans.Builder.Object] Gtk.VBox view_vbox;

		[GtkBeans.Builder.Object] Gtk.VBox toolbar_vbox;

		[GtkBeans.Builder.Object] Gtk.ScrolledWindow icon_view_scrolled;
		[GtkBeans.Builder.Object] Box photo_box;
		[GtkBeans.Builder.Object] Notebook view_notebook;

		[GtkBeans.Builder.Object] Label status_label;

		[GtkBeans.Builder.Object] Gtk.UIManager uimanager;
		// Photo
		[GtkBeans.Builder.Object] Gtk.Action create_version_menu_item;
		[GtkBeans.Builder.Object] Gtk.Action delete_version_menu_item;
		[GtkBeans.Builder.Object] Gtk.Action detach_version_menu_item;
		[GtkBeans.Builder.Object] Gtk.Action rename_version_menu_item;

		[GtkBeans.Builder.Object] Gtk.Action tools;
		[GtkBeans.Builder.Object] Gtk.Action export;
		[GtkBeans.Builder.Object] Gtk.Action pagesetup_menu_item;
		[GtkBeans.Builder.Object] Gtk.Action print;
		[GtkBeans.Builder.Object] Gtk.Action send_mail;

		// Edit
		[GtkBeans.Builder.Object] Gtk.Action copy;
		[GtkBeans.Builder.Object] Gtk.Action select_none;
		[GtkBeans.Builder.Object] Gtk.Action rotate_left;
		[GtkBeans.Builder.Object] Gtk.Action rotate_right;

		[GtkBeans.Builder.Object] Gtk.Action sharpen;
		[GtkBeans.Builder.Object] Gtk.Action adjust_time;

		[GtkBeans.Builder.Object] Gtk.Action update_thumbnail;
		[GtkBeans.Builder.Object] Gtk.Action delete_from_drive;
		[GtkBeans.Builder.Object] Gtk.Action remove_from_catalog;
		[GtkBeans.Builder.Object] Gtk.Action set_as_background;

		[GtkBeans.Builder.Object] Gtk.Action attach_tag;
		[GtkBeans.Builder.Object] Gtk.Action remove_tag;

		// View
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_toolbar;
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_sidebar;
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_timeline;
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_filmstrip;
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_dates_menu_item;
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_tags_menu_item;
		[GtkBeans.Builder.Object] Gtk.ToggleAction display_ratings_menu_item;

		[GtkBeans.Builder.Object] Gtk.Action zoom_in;
		[GtkBeans.Builder.Object] Gtk.Action zoom_out;
		[GtkBeans.Builder.Object] Gtk.ToggleAction loupe_menu_item;

		[GtkBeans.Builder.Object] Gtk.RadioAction tag_icon_hidden;
		[GtkBeans.Builder.Object] Gtk.RadioAction tag_icon_small;
		[GtkBeans.Builder.Object] Gtk.RadioAction tag_icon_medium;
		[GtkBeans.Builder.Object] Gtk.RadioAction tag_icon_large;

		[GtkBeans.Builder.Object] Gtk.ToggleAction reverse_order;

		// Find
		[GtkBeans.Builder.Object] Gtk.Action clear_date_range;
		[GtkBeans.Builder.Object] Gtk.Action clear_rating_filter;

		[GtkBeans.Builder.Object] Gtk.ToggleAction find_untagged;

		[GtkBeans.Builder.Object] Gtk.Action clear_roll_filter;

		// Tags
		[GtkBeans.Builder.Object] Gtk.Action edit_selected_tag;
		[GtkBeans.Builder.Object] Gtk.Action delete_selected_tag;

		[GtkBeans.Builder.Object] Gtk.Action attach_tag_to_selection;
		[GtkBeans.Builder.Object] Gtk.Action remove_tag_from_selection;

		// Other Widgets
		[GtkBeans.Builder.Object] Scale zoom_scale;

		[GtkBeans.Builder.Object] VBox info_vbox;

		[GtkBeans.Builder.Object] Gtk.HBox tagbar;
		[GtkBeans.Builder.Object] Gtk.VBox tag_entry_container;
		[GtkBeans.Builder.Object] Gtk.VBox sidebar_vbox;
#pragma warning restore 649

		TagEntry tag_entry;

		Gtk.Toolbar toolbar;

		FindBar find_bar;

		PhotoVersionMenu versions_submenu;

		Gtk.ToggleToolButton browse_button;
		Gtk.ToggleToolButton edit_button;

		QueryView icon_view;

		PhotoView photo_view;

		public PhotoView PhotoView {
			get { return photo_view; }
		}

		FullScreenView fsview;
		PhotoQuery query;
		GroupSelector group_selector;
		QueryWidget query_widget;

		PreviewPopup preview_popup;

		ToolButton rl_button;
		ToolButton rr_button;

		Label count_label;

		Gtk.ToolButton display_next_button;
		Gtk.ToolButton display_previous_button;

		bool write_metadata = false;

		Gdk.Cursor watch = new Gdk.Cursor (Gdk.CursorType.Watch);

		// Tag Icon Sizes
		public int TagsIconSize
		{
			get { return (int) Tag.TagIconSize; }
			set { Tag.TagIconSize = (Settings.IconSize) value; }
		}

		static TargetEntry[] tag_target_table = {
				DragDropTargets.TagListEntry
		};

		const int PHOTO_IDX_NONE = -1;

		public Db Database { get; set; }

		public ModeType ViewMode { get; set; }

		public MainSelection Selection { get; set; }

		public InfoBox InfoBox { get; set; }

		static TargetList iconSourceTargetList = new TargetList ();
		static TargetList iconDestTargetList = new TargetList ();

		static MainWindow ()
		{
			iconSourceTargetList.AddTargetEntry (DragDropTargets.PhotoListEntry);
			iconSourceTargetList.AddTargetEntry (DragDropTargets.TagQueryEntry);
			iconSourceTargetList.AddUriTargets ((uint)DragDropTargets.TargetType.UriList);
			iconSourceTargetList.AddTargetEntry (DragDropTargets.RootWindowEntry);

			iconDestTargetList.AddTargetEntry (DragDropTargets.PhotoListEntry);
			iconDestTargetList.AddTargetEntry (DragDropTargets.TagListEntry);
			iconDestTargetList.AddUriTargets ((uint)DragDropTargets.TargetType.UriList);
		}

		public MainWindow (Db db)
		{
			foreach (ServiceNode service in AddinManager.GetExtensionNodes ("/FSpot/Services")) {
				try {
					service.Initialize ();
					service.Start ();
				} catch (Exception e) {
					Log.WarningFormat ("Something went wrong while starting the {0} extension.", service.Id);
					Log.DebugException (e);
				}
			}

			Database = db;

			var builder = new GtkBeans.Builder ("main_window.ui");
			builder.Autoconnect (this);

			//Set the global DefaultColormap. Allows transparency according
			//to the theme (work on murrine engine)
			Gdk.Colormap colormap = ((Widget)main_window).Screen.RgbaColormap;
			if (colormap == null) {
				Log.Debug ("Your screen doesn't support alpha channels!");
				colormap = ((Widget)main_window).Screen.RgbColormap;
			}
			Gtk.Widget.DefaultColormap = colormap;

			LoadPreference (Preferences.MAIN_WINDOW_WIDTH);
			LoadPreference (Preferences.MAIN_WINDOW_X);
			LoadPreference (Preferences.MAIN_WINDOW_MAXIMIZED);
			main_window.ShowAll ();

			LoadPreference (Preferences.SIDEBAR_POSITION);
			LoadPreference (Preferences.METADATA_EMBED_IN_IMAGE);

			pagesetup_menu_item.Activated += HandlePageSetupActivated;

			toolbar = new Gtk.Toolbar ();
			toolbar_vbox.PackStart (toolbar);

			ToolButton import_button = GtkUtil.ToolButtonFromTheme ("gtk-add", Catalog.GetString ("Import Photos…"), true);
			import_button.Clicked += (o, args) => StartImport (null);
			import_button.TooltipText = Catalog.GetString ("Import new photos");
			toolbar.Insert (import_button, -1);

			toolbar.Insert (new SeparatorToolItem (), -1);

			rl_button = GtkUtil.ToolButtonFromTheme ("object-rotate-left", Catalog.GetString ("Rotate Left"), false);
			rl_button.Clicked += HandleRotate270Command;
			toolbar.Insert (rl_button, -1);

			rr_button = GtkUtil.ToolButtonFromTheme ("object-rotate-right", Catalog.GetString ("Rotate Right"), false);
			rr_button.Clicked += HandleRotate90Command;
			toolbar.Insert (rr_button, -1);

			toolbar.Insert (new SeparatorToolItem (), -1);

			browse_button = new ToggleToolButton ();
			browse_button.Label = Catalog.GetString ("Browsing");
			browse_button.IconName = "mode-browse";
			browse_button.IsImportant = true;
			browse_button.Toggled += HandleToggleViewBrowse;
			browse_button.TooltipText = Catalog.GetString ("Browse your photo library");
			toolbar.Insert (browse_button, -1);

			edit_button = new ToggleToolButton ();
			edit_button.Label = Catalog.GetString ("Editing");
			edit_button.IconName = "mode-image-edit";
			edit_button.IsImportant = true;
			edit_button.Toggled += HandleToggleViewPhoto;
			edit_button.TooltipText = Catalog.GetString ("Edit your photos");
			toolbar.Insert (edit_button, -1);

			toolbar.Insert (new SeparatorToolItem (), -1);

			ToolButton fs_button = GtkUtil.ToolButtonFromTheme ("view-fullscreen", Catalog.GetString ("Fullscreen"), false);
			fs_button.Clicked += HandleViewFullscreen;
			fs_button.TooltipText = Catalog.GetString ("View photos fullscreen");
			toolbar.Insert (fs_button, -1);

			ToolButton ss_button = GtkUtil.ToolButtonFromTheme ("media-playback-start", Catalog.GetString ("Slideshow"), false);
			ss_button.Clicked += HandleViewSlideShow;
			ss_button.TooltipText = Catalog.GetString ("View photos in a slideshow");
			toolbar.Insert (ss_button, -1);

			SeparatorToolItem white_space = new SeparatorToolItem ();
			white_space.Draw = false;
			white_space.Expand = true;
			toolbar.Insert (white_space, -1);

			ToolItem label_item = new ToolItem ();
			count_label = new Label (string.Empty);
			label_item.Child = count_label;
			toolbar.Insert (label_item, -1);

			display_previous_button = new ToolButton (Stock.GoBack);
			toolbar.Insert (display_previous_button, -1);
			display_previous_button.TooltipText = Catalog.GetString ("Previous photo");
			display_previous_button.Clicked += HandleDisplayPreviousButtonClicked;

			display_next_button = new ToolButton (Stock.GoForward);
			toolbar.Insert (display_next_button, -1);
			display_next_button.TooltipText = Catalog.GetString ("Next photo");
			display_next_button.Clicked += HandleDisplayNextButtonClicked;

			Sidebar = new Sidebar ();
			ViewModeChanged += Sidebar.HandleMainWindowViewModeChanged;
			sidebar_vbox.Add (Sidebar);

			tag_selection_scrolled = new Gtk.ScrolledWindow ();
			tag_selection_scrolled.ShadowType = ShadowType.In;

			tag_selection_widget = new TagSelectionWidget (Database.Tags);
			tag_selection_scrolled.Add (tag_selection_widget);

			Sidebar.AppendPage (tag_selection_scrolled, Catalog.GetString ("Tags"), "tag");

			AddinManager.AddExtensionNodeHandler ("/FSpot/Sidebar", OnSidebarExtensionChanged);

			Sidebar.Context = ViewContext.Library;

			Sidebar.CloseRequested += HideSidebar;
			Sidebar.Show ();

			InfoBox = new InfoBox ();
			ViewModeChanged += InfoBox.HandleMainWindowViewModeChanged;
			InfoBox.VersionChanged += (info_box, version) => {
				UpdateForVersionChange (version);
			};
			sidebar_vbox.PackEnd (InfoBox, false, false, 0);

			InfoBox.Context = ViewContext.Library;

			tag_selection_widget.Selection.Changed += HandleTagSelectionChanged;
			tag_selection_widget.KeyPressEvent += HandleTagSelectionKeyPress;
			tag_selection_widget.ButtonPressEvent += HandleTagSelectionButtonPressEvent;
			tag_selection_widget.PopupMenu += HandleTagSelectionPopupMenu;
			tag_selection_widget.RowActivated += HandleTagSelectionRowActivated;

			LoadPreference (Preferences.TAG_ICON_SIZE);

			try {
				query = new PhotoQuery (Database.Photos);
			} catch (Exception e) {
				//FIXME assume any exception here is due to a corrupt db and handle that.
				new RepairDbDialog (e, Database.Repair (), main_window);
				query = new PhotoQuery (Database.Photos);
			}

			UpdateStatusLabel ();
			query.Changed += HandleQueryChanged;

			Database.Photos.ItemsChanged += HandleDbItemsChanged;
			Database.Tags.ItemsChanged += HandleTagsChanged;
			Database.Tags.ItemsAdded += HandleTagsChanged;
			Database.Tags.ItemsRemoved += HandleTagsChanged;

			group_selector = new GroupSelector ();
			group_selector.Adaptor = new TimeAdaptor (query, Preferences.Get<bool> (Preferences.GROUP_ADAPTOR_ORDER_ASC));

			group_selector.ShowAll ();

			if (zoom_scale != null)
				zoom_scale.ValueChanged += HandleZoomScaleValueChanged;

			view_vbox.PackStart (group_selector, false, false, 0);
			view_vbox.ReorderChild (group_selector, 0);

			find_bar = new FindBar (query, tag_selection_widget.Model);
			view_vbox.PackStart (find_bar, false, false, 0);
			view_vbox.ReorderChild (find_bar, 1);
			main_window.KeyPressEvent += HandleKeyPressEvent;

			query_widget = new QueryWidget (query, Database);
			query_widget.Logic.Changed += HandleQueryLogicChanged;
			view_vbox.PackStart (query_widget, false, false, 0);
			view_vbox.ReorderChild (query_widget, 2);

			MenuItem findByTag = uimanager.GetWidget ("/ui/menubar1/find/find_by_tag") as MenuItem;
			query_widget.Hidden += (s, e) => {
				((Gtk.Label)findByTag.Child).TextWithMnemonic = Catalog.GetString ("Show _Find Bar");
			};
			query_widget.Shown += (s, e) => {
				((Gtk.Label)findByTag.Child).TextWithMnemonic = Catalog.GetString ("Hide _Find Bar");
			};

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

			preview_popup = new PreviewPopup (icon_view);

			icon_view.DragBegin += HandleIconViewDragBegin;
			icon_view.DragEnd += HandleIconViewDragEnd;
			icon_view.DragDataGet += HandleIconViewDragDataGet;
			icon_view.DragMotion += HandleIconViewDragMotion;
			icon_view.DragDrop += HandleIconViewDragDrop;
			// StartDrag is fired by IconView
			icon_view.StartDrag += HandleIconViewStartDrag;

			TagMenu tag_menu = new TagMenu (null, Database.Tags);
			tag_menu.NewTagHandler += (s, e) => {
				HandleCreateTagAndAttach (this, null);
			};
			tag_menu.TagSelected += HandleAttachTagMenuSelected;
			tag_menu.Populate ();
			(uimanager.GetWidget ("/ui/menubar1/edit2/attach_tag") as MenuItem).Submenu = tag_menu;

			PhotoTagMenu pmenu = new PhotoTagMenu ();
			pmenu.TagSelected += HandleRemoveTagMenuSelected;
			(uimanager.GetWidget ("/ui/menubar1/edit2/remove_tag") as MenuItem).Submenu = pmenu;

			Gtk.Drag.DestSet (icon_view, DestDefaults.All, (TargetEntry[])iconDestTargetList,
				DragAction.Copy | DragAction.Move);

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
			tag_entry = new TagEntry (Database.Tags);
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
			LoadPreference (Preferences.GROUP_ADAPTOR_ORDER_ASC);
			LoadPreference (Preferences.FILMSTRIP_ORIENTATION);

			Selection = new MainSelection (this);
			Selection.Changed += HandleSelectionChanged;
			Selection.ItemsChanged += HandleSelectionItemsChanged;
			Selection.Changed += Sidebar.HandleSelectionChanged;
			Selection.ItemsChanged += Sidebar.HandleSelectionItemsChanged;

			AddinManager.ExtensionChanged += PopulateExtendableMenus;
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

			// When the icon_view is loaded, set it's initial scroll position
			icon_view.SizeAllocated += HandleIconViewReady;

			export.Activated += HandleExportActivated;
			UpdateToolbar ();

			(uimanager.GetWidget ("/ui/menubar1/file1/close1") as MenuItem).Hide ();

			Banshee.Kernel.Scheduler.Resume ();
		}

		void HandleDisplayNextButtonClicked (object sender, EventArgs args)
		{
			PhotoView.View.Item.MoveNext ();
		}

		void HandleDisplayPreviousButtonClicked (object sender, EventArgs args)
		{
			PhotoView.View.Item.MovePrevious ();
		}

		void OnSidebarExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			// FIXME: No sidebar page removal yet!
			if (args.Change == ExtensionChange.Add)
				Sidebar.AppendPage ((args.ExtensionNode as SidebarPageNode).GetPage ());
		}

		Photo CurrentPhoto {
			get {
				int active = ActiveIndex ();
				if (active >= 0)
					return query [active] as Photo;

				return null;
			}
		}

		// Index into the PhotoQuery.  If -1, no photo is selected or multiple photos are selected.
		int ActiveIndex ()
		{
			return Selection.Count == 1 ? SelectedIds () [0] : PHOTO_IDX_NONE;
		}

		// Switching mode.
		public enum ModeType
		{
			IconView,
			PhotoView
		};

		public event EventHandler ViewModeChanged;

		public void SetViewMode (ModeType value)
		{
			if (ViewMode == value)
				return;

			ViewMode = value;
			switch (ViewMode) {
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

				photo_view.View.GrabFocus ();
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
				bool state = ViewMode == ModeType.IconView;

				if (browse_button.Active != state)
					browse_button.Active = state;
			}

			if (edit_button != null) {
				bool state = ViewMode == ModeType.PhotoView;

				if (edit_button.Active != state)
					edit_button.Active = state;
			}

			if (ViewMode == ModeType.PhotoView) {
				display_previous_button.Visible = true;
				display_next_button.Visible = true;
				count_label.Visible = true;

				bool valid = photo_view.View.Item.IsValid;
				bool prev = valid && photo_view.View.Item.Index > 0;
				bool next = valid && photo_view.View.Item.Index < query.Count - 1;

				display_previous_button.Sensitive = prev;
				display_next_button.Sensitive = next;

				if (Query == null)
					count_label.Text = string.Empty;
				else
					// Note for translators: This indicates the current photo is photo {0} of {1} out of photos
					count_label.Text = string.Format (Catalog.GetString ("{0} of {1}"), Query.Count == 0 ? 0 : photo_view.View.Item.Index + 1, Query.Count == 0 ? 0 : Query.Count);
			} else {
				display_previous_button.Visible = false;
				display_next_button.Visible = false;
				count_label.Visible = false;
			}
		}

		void HandleExportActivated (object o, EventArgs e)
		{
			ExportMenuItemNode.SelectedImages = () => new PhotoList (SelectedPhotos ());
		}

		void HandleDbItemsChanged (object sender, DbItemEventArgs<Photo> args)
		{
			foreach (Photo p in args.Items.Where(p => p != null).Where(p => write_metadata)) {
				SyncMetadataJob.Create (Database.Jobs, p);
			}

			if (args is PhotoEventArgs && (args as PhotoEventArgs).Changes.TimeChanged)
				query.RequestReload ();
		}

		void HandleTagsChanged (object sender, DbItemEventArgs<Tag> args)
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

		int [] SelectedIds ()
		{
			int[] ids = new int [0];

			if (fsview != null && fsview.View.Item.IsValid)
				ids = new int [] { fsview.View.Item.Index };
			else {
				switch (ViewMode) {
				case ModeType.IconView:
					ids = icon_view.Selection.Ids;
					break;
				default:
				case ModeType.PhotoView:
					if (photo_view.Item.IsValid)
						ids = new [] { photo_view.Item.Index };
					break;
				}
			}

			return ids;
		}

		public class MainSelection : IBrowsableCollection
		{
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
					switch (win.ViewMode) {
					case ModeType.PhotoView:
						return win.photo_view.Item.IsValid ? 1 : 0;
					case ModeType.IconView:
						return win.icon_view.Selection.Count;
					}
					return 0;
				}
			}

			public int IndexOf (IPhoto item)
			{
				switch (win.ViewMode) {
				case ModeType.PhotoView:
					return item == win.photo_view.Item.Current ? 0 : -1;
				case ModeType.IconView:
					return win.icon_view.Selection.IndexOf (item);
				}
				return -1;
			}

			public bool Contains (IPhoto item)
			{
				switch (win.ViewMode) {
				case ModeType.PhotoView:
					return item == win.photo_view.Item.Current;
				case ModeType.IconView:
					return win.icon_view.Selection.Contains (item);
				}
				return false;
			}

			public void MarkChanged ()
			{
				Changed?.Invoke (this);
			}

			public void MarkChanged (int index, IBrowsableItemChanges changes)
			{
				throw new NotImplementedException ("I didn't think you'd find me");
			}

			public IPhoto this [int index] {
				get {
					switch (win.ViewMode) {
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

			public IEnumerable<IPhoto> Items {
				get {
					switch (win.ViewMode) {
					case ModeType.PhotoView:
						if (win.photo_view.Item.IsValid)
							return new IPhoto [] { win.photo_view.Item.Current };

						break;
					case ModeType.IconView:
						return win.icon_view.Selection.Items;
					}
					return new IPhoto [0];
				}
			}

			void HandleQueryItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
			{
				// FIXME for now we only listen to changes directly from the query
				// when we are in PhotoView mode because we presume that we'll get
				// proper notification from the icon view selection in icon view mode
				if (win.ViewMode != ModeType.PhotoView || ItemsChanged == null)
					return;

				foreach (int item in args.Items.Where(item => win.photo_view.Item.Index == item)) {
					ItemsChanged (this, new BrowsableEventArgs (item, args.Changes));
					break;
				}
			}

			void HandlePhotoChanged (PhotoView sender)
			{
				if (win.ViewMode == ModeType.PhotoView)
					Changed?.Invoke (this);
			}

			public void HandleSelectionChanged (IBrowsableCollection collection)
			{
				if (win.ViewMode == ModeType.IconView)
					Changed?.Invoke (this);


			}

			void HandleSelectionItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
			{
				if (win.ViewMode == ModeType.IconView)
					ItemsChanged?.Invoke (this, args);
			}

			public event IBrowsableCollectionChangedHandler Changed;
			public event IBrowsableCollectionItemsChangedHandler ItemsChanged;
		}

		void HandleSelectionChanged (IBrowsableCollection collection)
		{
			UpdateMenus ();
			UpdateTagEntryFromSelection ();
			UpdateStatusLabel ();
			UpdateToolbar ();

			InfoBox.Photos = SelectedPhotos ();
		}

		void HandleSelectionItemsChanged (IBrowsableCollection collection, BrowsableEventArgs args)
		{
			UpdateMenus ();
			UpdateTagEntryFromSelection ();
			photo_view.UpdateTagView ();
			InfoBox.Photos = SelectedPhotos ();
		}


		//
		// Selection Interface
		//

		Photo [] SelectedPhotos (int[] selected_ids)
		{
			Photo[] photo_list = new Photo [selected_ids.Length];

			int i = 0;
			foreach (int num in selected_ids)
				photo_list [i++] = query [num] as Photo;

			return photo_list;
		}

		public Photo [] SelectedPhotos ()
		{
			return SelectedPhotos (SelectedIds ());
		}

		public PhotoQuery Query => query;

		//
		// Commands
		//

		void RotateSelectedPictures (Gtk.Window parent, RotateDirection direction)
		{
			RotateCommand command = new RotateCommand (parent);

			int[] selected_ids = SelectedIds ();
			if (command.Execute (direction, SelectedPhotos (selected_ids)))
				query.MarkChanged (selected_ids, InvalidateData.Instance);
		}

		//
		// Tag Selection Drag Handlers
		//

		public void AddTagExtended (int[] nums, Tag[] tags)
		{
			foreach (int num in nums)
				(query [num] as Photo).AddTag (tags);
			query.Commit (nums);

			foreach (Tag t in tags) {
				if (t.Icon != null || t.IconWasCleared)
					continue;
				// FIXME this needs a lot more work.
				Pixbuf icon = null;
				try {
					var tmp = PhotoLoader.LoadAtMaxSize (query [nums [0]], 128, 128);
					icon = PixbufUtils.TagIconFromPixbuf (tmp);
					tmp.Dispose ();
				} catch {
					icon = null;
				}

				t.Icon = icon;
				Database.Tags.Commit (t);
			}
		}

		public void SetFolderQuery (IEnumerable<SafeUri> uriList)
		{
			ShowQueryWidget ();
			query_widget.SetFolders (uriList);
		}

		public void RemoveTags (int[] nums, Tag[] tags)
		{
			foreach (int num in nums)
				(query [num] as Photo).RemoveTag (tags);
			query.Commit (nums);
		}

		void HandleTagSelectionButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				var popup = new TagPopup ();
				popup.Activate (args.Event, tag_selection_widget.TagAtPosition (args.Event.X, args.Event.Y),
					tag_selection_widget.TagHighlight);
				args.RetVal = true;
			}
		}

		void HandleTagSelectionPopupMenu (object sender, PopupMenuArgs args)
		{
			var popup = new TagPopup ();
			popup.Activate (null, null, tag_selection_widget.TagHighlight);
			args.RetVal = true;
		}

		void HandleTagSelectionRowActivated (object sender, RowActivatedArgs args)
		{
			ShowQueryWidget ();
			query_widget.Include (new Tag [] { tag_selection_widget.TagByPath (args.Path) });
		}

		void JumpTo (int index)
		{
			switch (ViewMode) {
			case ModeType.PhotoView:
				photo_view.Item.Index = index;
				break;
			case ModeType.IconView:
				icon_view.ScrollTo (index);
				icon_view.Throb (index);
				break;
			}
		}

		void HandleAdaptorGlassSet (GroupAdaptor sender, int index)
		{
			JumpTo (index);
		}

		void HandleAdaptorChanged (GroupAdaptor sender)
		{
			UpdateGlass ();
		}

		/// <summary>
		/// Keep the glass temporal slider in sync with the user's scrolling in the icon_view
		/// </summary>
		void UpdateGlass ()
		{
			// If people cant see the timeline don't update it.
			if (!display_timeline.Active)
				return;

			int cell_num = icon_view.TopLeftVisibleCell ();
			if (cell_num == -1 /*|| cell_num == lastTopLeftCell*/)
				return;

			IPhoto photo = icon_view.Collection [cell_num];
			/*
			 * FIXME this is a lame hack to get around a delegate chain.  This should
			 * actually operate directly on the adaptor not on the selector but I don't have
			 * time to fix it right now.
			 */
			if (!group_selector.GlassUpdating) {
				group_selector.SetPosition (group_selector.Adaptor.IndexFromPhoto (photo));
			}
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

		public void HandleIconViewStartDrag (object sender, StartDragArgs args)
		{
			Gtk.Drag.Begin (icon_view, iconSourceTargetList,
				DragAction.Copy | DragAction.Move, (int)args.Button, args.Event);
		}

		public void HandleIconViewDragBegin (object sender, DragBeginArgs args)
		{
			var photos = SelectedPhotos ();

			if (photos.Length > 0) {
				int len = Math.Min (photos.Length, 4);
				int size = 48;
				int border = 2;
				int csize = size / 2 + len * size / 2 + 2 * border;

				Pixbuf container = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, csize, csize);
				container.Fill (0x00000000);

				bool use_icon = false;

				while (len-- > 0) {
					FSpot.PixbufCache.CacheEntry entry = icon_view.Cache.Lookup (photos [len].DefaultVersion.Uri);

					Pixbuf thumbnail = null;
					if (entry != null) {
						Cms.Profile screen_profile;
						if (FSpot.ColorManagement.Profiles.TryGetValue (Preferences.Get<string> (Preferences.COLOR_MANAGEMENT_DISPLAY_PROFILE), out screen_profile)) {
							thumbnail = entry.Pixbuf.Copy ();
							FSpot.ColorManagement.ApplyProfile (thumbnail, screen_profile);
						} else
							thumbnail = entry.ShallowCopyPixbuf ();
					}

					if (thumbnail != null) {
						Pixbuf small = thumbnail.ScaleToMaxSize (size, size);

						int x = border + len * (size / 2) + (size - small.Width) / 2;
						int y = border + len * (size / 2) + (size - small.Height) / 2;
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

		void HandleIconViewDragEnd (object sender, DragEndArgs args)
		{
		}

		void HandleIconViewDragDataGet (object sender, DragDataGetArgs args)
		{
			if (args.Info == (uint)DragDropTargets.TargetType.UriList) {
				var uris = from p in SelectedPhotos ()
				                       select p.DefaultVersion.Uri;
				args.SelectionData.SetUriListData (new UriList (uris), args.Context.Targets [0]);
				return;
			}

			if (args.Info == DragDropTargets.PhotoListEntry.Info) {
				args.SelectionData.SetPhotosData (SelectedPhotos (), args.Context.Targets [0]);
				return;
			}

			if (args.Info == DragDropTargets.RootWindowEntry.Info) {
				HandleSetAsBackgroundCommand (null, null);
				return;
			}
		}

		void HandleIconViewDragDrop (object sender, DragDropArgs args)
		{
			args.RetVal = true;
		}

		void HandleIconViewDragMotion (object sender, DragMotionArgs args)
		{
			Gdk.Drag.Status (args.Context, args.Context.SuggestedAction, args.Time);
			args.RetVal = true;
		}

		public void ImportUriList (UriList list, bool copy)
		{
			// Drag'n drop import.
			var controller = new ImportDialogController (false);
			controller.StatusEvent += (evnt) => ThreadAssist.ProxyToMain (() => {
				if (evnt == ImportEvent.ImportFinished) {
					if (controller.PhotosImported > 0) {
						query.RollSet = new RollSet (Database.Rolls.GetRolls (1));
					}
				}
			});

			var source = new MultiImportSource (list.ToArray ());
			controller.ActiveSource = source;
			controller.CopyFiles = copy;
			controller.DuplicateDetect = true;
			controller.RecurseSubdirectories = true;
			controller.RemoveOriginals = false;

			var import_window = new ImportDialog (controller, Window);
			import_window.Show ();

			controller.StartImport ();
		}

		void HandleImportCommand (object obj, EventArgs args)
		{
			StartImport (null);
		}

		public void ImportFile (SafeUri uri)
		{
			StartImport (uri);
		}

		void StartImport (SafeUri uri)
		{
			var controller = new ImportDialogController (true);
			controller.StatusEvent += evnt => {
				if (evnt == ImportEvent.ImportFinished) {
					if (controller.PhotosImported > 0) {
						query.RollSet = new RollSet (Database.Rolls.GetRolls (1));
					}
				}
			};
			var import_window = new ImportDialog (controller, Window);
			import_window.Show ();
		}

		void HandleIconViewDragDataReceived (object sender, DragDataReceivedArgs args)
		{
			Widget source = Gtk.Drag.GetSourceWidget (args.Context);

			if (args.Info == DragDropTargets.TagListEntry.Info) {
				//
				// Translate the event args from viewport space to window space,
				// drag events use the viewport.  Owen sends his regrets.
				//
				int item = icon_view.CellAtPosition (args.X + (int)icon_view.Hadjustment.Value,
					           args.Y + (int)icon_view.Vadjustment.Value);

				//Console.WriteLine ("Drop cell = {0} ({1},{2})", item, args.X, args.Y);
				if (item >= 0) {
					if (icon_view.Selection.Contains (item))
						AttachTags (tag_selection_widget.TagHighlight, SelectedIds ());
					else
						AttachTags (tag_selection_widget.TagHighlight, new int [] { item });
				}

				Gtk.Drag.Finish (args.Context, true, false, args.Time);
				return;
			}

			if (args.Info == (uint)DragDropTargets.TargetType.UriList) {

				/*
				 * If the drop is coming from inside f-spot then we don't want to import
				 */
				if (source != null)
					return;

				UriList list = args.SelectionData.GetUriListData ();
				ImportUriList (list, (args.Context.Action & Gdk.DragAction.Copy) != 0);

				Gtk.Drag.Finish (args.Context, true, false, args.Time);
				return;
			}

			if (args.Info == DragDropTargets.PhotoListEntry.Info) {
				int p_item = icon_view.CellAtPosition (args.X + (int)icon_view.Hadjustment.Value,
					             args.Y + (int)icon_view.Vadjustment.Value);

				if (p_item >= 0) {
					if (icon_view.Selection.Contains (p_item)) //We don't want to reparent ourselves!
						return;
					PhotoVersionCommands.Reparent cmd = new PhotoVersionCommands.Reparent ();
					Photo[] photos_to_reparent = SelectedPhotos ();
					// Give feedback to user that something happened, and leave the parent selected after reparenting
					icon_view.Selection.Add (p_item);
					cmd.Execute (Database.Photos, photos_to_reparent, query.Photos [p_item], GetToplevel (null));
					UpdateQuery ();
				}
				Gtk.Drag.Finish (args.Context, true, false, args.Time);
				return;
			}
		}

		//
		// IconView event handlers
		//
		void HandleDoubleClicked (object sender, BrowsableEventArgs args)
		{
			var widget = sender as Widget;
			if (widget == null)
				return;

			switch (ViewMode) {
			case ModeType.IconView:
				icon_view.FocusCell = args.Items [0];
				SetViewMode (ModeType.PhotoView);
				break;
			case ModeType.PhotoView:
				SetViewMode (ModeType.IconView);
				break;
			}
		}

		public void HandleCommonPhotoCommands (object sender, Gtk.KeyPressEventArgs args)
		{
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
			case Gdk.Key.KP_0:
				if (alt)
					HandleRatingMenuSelected (0);
				break;
			case Gdk.Key.Key_1:
			case Gdk.Key.KP_1:
				if (alt)
					HandleRatingMenuSelected (1);
				break;
			case Gdk.Key.Key_2:
			case Gdk.Key.KP_2:
				if (alt)
					HandleRatingMenuSelected (2);
				break;
			case Gdk.Key.Key_3:
			case Gdk.Key.KP_3:
				if (alt)
					HandleRatingMenuSelected (3);
				break;
			case Gdk.Key.Key_4:
			case Gdk.Key.KP_4:
				if (alt)
					HandleRatingMenuSelected (4);
				break;
			case Gdk.Key.Key_5:
			case Gdk.Key.KP_5:
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
			main_window.GdkWindow.Cursor = watch;
			// FIXME: use gdk_display_flush() when available
			main_window.GdkWindow.Display.Sync ();
		}

		void HandlePhotoViewUpdateFinished (PhotoView sender)
		{
			main_window.GdkWindow.Cursor = null;
			// FIXME: use gdk_display_flush() when available
			main_window.GdkWindow.Display.Sync ();
		}

		#region PhotoView drag handlers
		void HandlePhotoViewDragDrop (object sender, DragDropArgs args)
		{
			//Widget source = Gtk.Drag.GetSourceWidget (args.Context);

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

			photo_view.View.GrabFocus ();
		}
		#endregion

		//
		// RatingMenu commands
		//
		public void HandleRatingMenuSelected (int r)
		{
			if (ViewMode == ModeType.PhotoView)
				photo_view.UpdateRating (r);

			Photo p;
			Database.BeginTransaction ();
			int[] selected_photos = SelectedIds ();
			foreach (int num in selected_photos) {
				p = query [num] as Photo;
				p.Rating = (uint)r;
			}
			query.Commit (selected_photos);
			Database.CommitTransaction ();
		}

		//
		// TagMenu commands.
		//
		public void HandleTagMenuActivate (object sender, EventArgs args)
		{
			MenuItem parent = sender as MenuItem ?? uimanager.GetWidget ("/ui/menubar1/edit2/remove_tag") as MenuItem;
			if (parent != null && parent.Submenu is PhotoTagMenu) {
				PhotoTagMenu menu = (PhotoTagMenu)parent.Submenu;
				menu.Populate (SelectedPhotos ());
			}
		}

		public void HandleAttachTagMenuSelected (Tag t)
		{
			Database.BeginTransaction ();
			AddTagExtended (SelectedIds (), new Tag [] { t });
			Database.CommitTransaction ();
			query_widget.PhotoTagsChanged (new Tag[] { t });
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
			Database.BeginTransaction ();
			RemoveTags (SelectedIds (), new[] { t });
			Database.CommitTransaction ();
			query_widget.PhotoTagsChanged (new[] { t });
		}

		//
		// Main menu commands
		//
		void HandlePageSetupActivated (object o, EventArgs e)
		{
			FSpot.Settings.Global.PageSetup = Print.RunPageSetupDialog (this.Window, FSpot.Settings.Global.PageSetup, null);
		}

		void HandlePrintCommand (object sender, EventArgs e)
		{
			FSpot.PrintOperation print = new FSpot.PrintOperation (SelectedPhotos ());
			print.Run (PrintOperationAction.PrintDialog, null);
		}

		public void HandlePreferences (object sender, EventArgs args)
		{
			var pref = new PreferenceDialog (GetToplevel (sender));
			pref.Run ();
			pref.Destroy ();
		}

		public void HandleManageExtensions (object sender, EventArgs args)
		{
			Mono.Addins.Gui.AddinManagerWindow.Run (main_window);
		}

		void HandleSendMailCommand (object sender, EventArgs args)
		{
			//TestDisplay ();
			new FSpot.SendEmail (new PhotoList (SelectedPhotos ()), Window);
		}

		public static void HandleHelp (object sender, EventArgs args)
		{
			GtkBeans.Global.ShowUri (App.Instance.Organizer.Window.Screen, "ghelp:f-spot");
		}

		public static void HandleAbout (object sender, EventArgs args)
		{
			FSpot.UI.Dialog.AboutDialog.ShowUp ();
		}

		void HandleTagSizeChange (object sender, EventArgs args)
		{
			RadioAction choice = sender as RadioAction;

			//Get this callback twice. Once for the active going menuitem,
			//once for the inactive leaving one. Ignore the inactive.
			if (!choice.Active)
				return;

			int old_size = TagsIconSize;

			if (choice == tag_icon_hidden) {
				TagsIconSize = (int)Settings.IconSize.Hidden;
			} else if (choice == tag_icon_small) {
				TagsIconSize = (int)Settings.IconSize.Small;
			} else if (choice == tag_icon_medium) {
				TagsIconSize = (int)Settings.IconSize.Medium;
			} else if (choice == tag_icon_large) {
				TagsIconSize = (int)Settings.IconSize.Large;
			} else {
				return;
			}

			if (old_size != TagsIconSize) {
				tag_selection_widget.ColumnsAutosize ();
				photo_view?.UpdateTagView ();
				Preferences.Set (Preferences.TAG_ICON_SIZE, TagsIconSize);
			}
		}

		public void HandleFilmstripHorizontal (object sender, EventArgs args)
		{
			if (photo_view.FilmstripOrientation == Orientation.Horizontal)
				return;
			(sender as Gtk.CheckMenuItem).Active = false;
			photo_view.PlaceFilmstrip (Orientation.Horizontal);
		}

		public void HandleFilmstripVertical (object sender, EventArgs args)
		{
			if (photo_view.FilmstripOrientation == Orientation.Vertical)
				return;
			(sender as Gtk.CheckMenuItem).Active = false;
			photo_view.PlaceFilmstrip (Orientation.Vertical);
		}

		public void HandleReverseOrder (object sender, EventArgs args)
		{
			var item = sender as ToggleAction;

			if (group_selector.Adaptor.OrderAscending == item.Active)
				return;

			group_selector.Adaptor.OrderAscending = item.Active;
			query.TimeOrderAsc = item.Active;

			// FIXME this is blah...we need UIManager love here
			if (item != reverse_order)
				reverse_order.Active = item.Active;

			//update the selection in the timeline
			if (query.Range != null && group_selector.Adaptor is TimeAdaptor)
				group_selector.SetLimitsToDates (query.Range.Start, query.Range.End);
		}

		// Called when the user clicks the X button
		void HandleDeleteEvent (object sender, DeleteEventArgs args)
		{
			Close ();
			args.RetVal = true;
		}

		void HandleCloseCommand (object sender, EventArgs args)
		{
			Close ();
		}

		public void Close ()
		{
			int x, y, width, height;
			main_window.GetPosition (out x, out y);
			main_window.GetSize (out width, out height);

			bool maximized = ((main_window.GdkWindow.State & Gdk.WindowState.Maximized) > 0);
			Preferences.Set (Preferences.MAIN_WINDOW_MAXIMIZED, maximized);

			if (!maximized) {
				Preferences.Set (Preferences.MAIN_WINDOW_X, x);
				Preferences.Set (Preferences.MAIN_WINDOW_Y, y);
				Preferences.Set (Preferences.MAIN_WINDOW_WIDTH, width);
				Preferences.Set (Preferences.MAIN_WINDOW_HEIGHT,	height);
			}

			Preferences.Set (Preferences.SHOW_TOOLBAR, toolbar.Visible);
			Preferences.Set (Preferences.SHOW_SIDEBAR, info_vbox.Visible);
			Preferences.Set (Preferences.SHOW_TIMELINE, display_timeline.Active);
			Preferences.Set (Preferences.SHOW_FILMSTRIP, display_filmstrip.Active);
			Preferences.Set (Preferences.SHOW_TAGS, icon_view.DisplayTags);
			Preferences.Set (Preferences.SHOW_DATES, icon_view.DisplayDates);
			Preferences.Set (Preferences.SHOW_RATINGS, icon_view.DisplayRatings);

			Preferences.Set (Preferences.GROUP_ADAPTOR_ORDER_ASC, group_selector.Adaptor.OrderAscending);
			Preferences.Set (Preferences.GLASS_POSITION, group_selector.GlassPosition);

			Preferences.Set (Preferences.SIDEBAR_POSITION, main_hpaned.Position);
			Preferences.Set (Preferences.ZOOM, icon_view.Zoom);

			tag_selection_widget.SaveExpandDefaults ();

			this.Window.Destroy ();

			photo_view.Dispose ();
			preview_popup.Dispose ();
		}

		void HandleCreateVersionCommand (object obj, EventArgs args)
		{
			PhotoVersionCommands.Create cmd = new PhotoVersionCommands.Create ();
			cmd.Execute (Database.Photos, CurrentPhoto, GetToplevel (null));
		}

		void HandleDeleteVersionCommand (object obj, EventArgs args)
		{
			PhotoVersionCommands.Delete cmd = new PhotoVersionCommands.Delete ();
			cmd.Execute (Database.Photos, CurrentPhoto, GetToplevel (null));
		}

		void HandleDetachVersionCommand (object obj, EventArgs args)
		{
			PhotoVersionCommands.Detach cmd = new PhotoVersionCommands.Detach ();
			cmd.Execute (Database.Photos, CurrentPhoto, GetToplevel (null));
			UpdateQuery ();
		}

		void HandleRenameVersionCommand (object obj, EventArgs args)
		{
			PhotoVersionCommands.Rename cmd = new PhotoVersionCommands.Rename ();
			cmd.Execute (Database.Photos, CurrentPhoto, main_window);
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
				tag_selection_widget.TagHighlight = new Tag [] { new_tag };
			}
		}

		public Tag CreateTag (object sender, EventArgs args)
		{
			CreateTagDialog dialog = new CreateTagDialog (Database.Tags);
			return dialog.Execute (CreateTagDialog.TagType.Category, tag_selection_widget.TagHighlight);
		}

		public void HandleAttachTagCommand (object obj, EventArgs args)
		{
			AttachTags (tag_selection_widget.TagHighlight, SelectedIds ());
		}

		void AttachTags (Tag[] tags, int[] ids)
		{
			Database.BeginTransaction ();
			AddTagExtended (ids, tags);
			Database.CommitTransaction ();
			query_widget.PhotoTagsChanged (tags);
		}

		public void HandleRemoveTagCommand (object obj, EventArgs args)
		{
			Tag[] tags = this.tag_selection_widget.TagHighlight;

			Database.BeginTransaction ();
			RemoveTags (SelectedIds (), tags);
			Database.CommitTransaction ();
			query_widget.PhotoTagsChanged (tags);
		}

		public void HandleEditSelectedTag (object sender, EventArgs ea)
		{
			Tag[] tags = this.tag_selection_widget.TagHighlight;
			if (tags.Length != 1)
				return;

			HandleEditSelectedTagWithTag (tags [0]);
		}

		public void HandleEditSelectedTagWithTag (Tag tag)
		{
			if (tag == null)
				return;

			EditTagDialog dialog = new EditTagDialog (Database, tag, main_window);
			if ((ResponseType)dialog.Run () == ResponseType.Ok) {
				bool name_changed = false;
				try {
					if (tag.Name != dialog.TagName) {
						tag.Name = dialog.TagName;
						name_changed = true;
					}
					tag.Category = dialog.TagCategory;
					Database.Tags.Commit (tag, name_changed);
				} catch (Exception ex) {
					Log.Exception (ex);
				}
			}

			dialog.Destroy ();
		}

		public void HandleMergeTagsCommand (object obj, EventArgs args)
		{
			Tag[] tags = tag_selection_widget.TagHighlight;
			if (tags.Length < 2)
				return;

			// Translators, The singular case will never happen here.
			string header = Catalog.GetPluralString ("Merge the selected tag",
				                "Merge the {0} selected tags?", tags.Length);
			header = string.Format (header, tags.Length);

			// If a tag with children tags is selected for merging, we
			// should also merge its children..
			List<Tag> all_tags = new List<Tag> (tags.Length);
			foreach (Tag tag in tags) {
				if (!all_tags.Contains (tag))
					all_tags.Add (tag);
				else
					continue;

				if (!(tag is Category))
					continue;

				(tag as Category).AddDescendentsTo (all_tags);
			}

			// debug..
			tags = all_tags.ToArray ();
			System.Array.Sort (tags, new TagRemoveComparer ());

			foreach (Tag tag in tags) {
				Log.Debug ("tag: {0}", tag.Name);
			}

			string msg = Catalog.GetString ("This operation will merge the selected tags and any sub-tags into a single tag.");

			string ok_caption = Catalog.GetString ("_Merge Tags");

			if (ResponseType.Ok != HigMessageDialog.RunHigConfirmation (main_window,
				    DialogFlags.DestroyWithParent,
				    MessageType.Warning,
				    header,
				    msg,
				    ok_caption))
				return;

			// The surviving tag is the last tag, as it is definitely not a child of any other the
			// other tags.  removetags will contain the tags to be merged.
			Tag survivor = tags [tags.Length - 1];

			var removetags = new Tag [tags.Length - 1];
			Array.Copy (tags, 0, removetags, 0, tags.Length - 1);

			// Add the surviving tag to all the photos with the other tags
			var photos = ObsoletePhotoQueries.Query (removetags);
			foreach (Photo p in photos) {
				p.AddTag (survivor);
			}

			// Remove the defunct tags, which removes them from the photos, commits
			// the photos, and removes the tags from the TagStore
			Database.BeginTransaction ();
			Database.Photos.Remove (removetags);
			Database.CommitTransaction ();

			HandleEditSelectedTagWithTag (survivor);
		}

		void HandleAdjustTime (object sender, EventArgs args)
		{
			PhotoList list = new PhotoList (Selection.Items);
			list.Sort (new IPhotoComparer.CompareDateName ());

			// HACK: force libgnomeui to be loaded by accessing some type
			// this resolves "Invalid object type `GnomeDateEdit'" on loading the AdjustTimeDialog
			var type = Gnome.DateEdit.GType;

			var dialog = new AdjustTimeDialog (Database, list);
			dialog.Run ();
			dialog.Destroy ();
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
			if (ViewMode == ModeType.PhotoView)
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
			if (ViewMode == ModeType.IconView)
				browse_button.Active = true;
			else if (browse_button.Active)
				SetViewMode (ModeType.IconView);
		}

		void HandleToggleViewPhoto (object sender, EventArgs args)
		{
			if (ViewMode == ModeType.PhotoView)
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
			int active = (Selection.Count > 0 ? SelectedIds () [0] : 0);
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
			switch (ViewMode) {
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

		bool UpdateStatusLabel ()
		{
			update_status_label = false;
			int total_photos = Database.Photos.TotalPhotos;
			if (total_photos != query.Count)
				status_label.Text = string.Format (Catalog.GetPluralString ("{0} photo out of {1}", "{0} photos out of {1}", query.Count), query.Count, total_photos);
			else
				status_label.Text = string.Format (Catalog.GetPluralString ("{0} photo", "{0} photos", query.Count), query.Count);

			if ((Selection != null) && (Selection.Count > 0))
				status_label.Text += string.Format (Catalog.GetPluralString (" ({0} selected)", " ({0} selected)", Selection.Count), Selection.Count);
			status_label.UseMarkup = true;
			return update_status_label;
		}

		void HandleZoomChanged (object sender, System.EventArgs args)
		{
			zoom_scale.ValueChanged -= HandleZoomScaleValueChanged;

			double zoom = .5;
			switch (ViewMode) {
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

		void ZoomOut ()
		{
			switch (ViewMode) {
			case ModeType.PhotoView:
				photo_view.ZoomOut ();
				break;
			case ModeType.IconView:
				icon_view.ZoomOut ();
				break;
			}
		}

		void ZoomIn ()
		{
			switch (ViewMode) {
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
				msg = string.Format (
					Catalog.GetString ("No permission to delete the file:{1}{0}"),
					fname, Environment.NewLine).Replace ("_", "__");
			else
				msg = string.Format (
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
				toplevel = (Gtk.Window)wsender.Toplevel;
			else if (fsview != null)
				toplevel = fsview;
			else
				toplevel = main_window;

			return toplevel;
		}

		public void HandleDeleteCommand (object sender, EventArgs args)
		{
			// Don't steal characters from any text entries
			if (Window.Focus is Gtk.Entry && Gtk.Global.CurrentEvent is Gdk.EventKey) {
				Window.Focus.ProcessEvent (Gtk.Global.CurrentEvent);
				return;
			}

			Photo[] photos = SelectedPhotos ();
			string header = Catalog.GetPluralString ("Delete the selected photo permanently?",
				                "Delete the {0} selected photos permanently?",
				                photos.Length);
			header = string.Format (header, photos.Length);
			string msg = Catalog.GetPluralString ("This deletes all versions of the selected photo from your drive.",
				             "This deletes all versions of the selected photos from your drive.",
				             photos.Length);
			string ok_caption = Catalog.GetPluralString ("_Delete photo", "_Delete photos", photos.Length);

			if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation (GetToplevel (sender),
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
				Database.Photos.Remove (photos);

				UpdateQuery ();
				Log.DebugTimerPrint (timer, "HandleDeleteCommand took {0}");
			}
		}

		public void HandleRemoveCommand (object sender, EventArgs args)
		{
			// Don't steal characters from any text entries
			if (Window.Focus is Gtk.Entry && Gtk.Global.CurrentEvent is Gdk.EventKey) {
				Window.Focus.ProcessEvent (Gtk.Global.CurrentEvent);
				return;
			}

			Photo[] photos = SelectedPhotos ();
			if (photos.Length == 0)
				return;

			string header = Catalog.GetPluralString ("Remove the selected photo from F-Spot?",
				                "Remove the {0} selected photos from F-Spot?",
				                photos.Length);

			header = string.Format (header, photos.Length);
			string msg = Catalog.GetString ("If you remove photos from the F-Spot catalog all tag information will be lost. The photos remain on your computer and can be imported into F-Spot again.");
			string ok_caption = Catalog.GetString ("_Remove from Catalog");
			if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation (GetToplevel (sender), DialogFlags.DestroyWithParent,
				    MessageType.Warning, header, msg, ok_caption)) {
				Database.Photos.Remove (photos);
				UpdateQuery ();
			}
		}

		void HandleSelectAllCommand (object sender, EventArgs args)
		{
			if (Window.Focus is Editable) {
				(Window.Focus as Editable).SelectRegion (0, -1); // select all in text box
				return;
			}

			icon_view.SelectAllCells ();
			UpdateStatusLabel ();
		}

		void HandleSelectNoneCommand (object sender, EventArgs args)
		{
			icon_view.Selection.Clear ();
			UpdateStatusLabel ();
		}

		void HandleSelectInvertCommand (object sender, EventArgs args)
		{
			icon_view.Selection.SelectionInvert ();
			UpdateStatusLabel ();
		}

		// This ConnectBefore is needed because otherwise the editability of the name
		// column will steal returns, spaces, and clicks if the tag name is focused
		[GLib.ConnectBefore]
		public void HandleTagSelectionKeyPress (object sender, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = true;

			switch (args.Event.Key) {
			case Gdk.Key.Delete:
				HandleDeleteSelectedTagCommand (sender, (EventArgs)args);
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
			Tag[] tags = this.tag_selection_widget.TagHighlight;

			Array.Sort (tags, new TagRemoveComparer ());

			//How many pictures are associated to these tags?
			Db db = App.Instance.Database;
			FSpot.PhotoQuery count_query = new FSpot.PhotoQuery (db.Photos);
			count_query.Terms = OrTerm.FromTags (tags);
			int associated_photos = count_query.Count;

			string header;
			if (tags.Length == 1)
				header = string.Format (Catalog.GetString ("Delete tag \"{0}\"?"), tags [0].Name.Replace ("_", "__"));
			else
				header = string.Format (Catalog.GetString ("Delete the {0} selected tags?"), tags.Length);

			header = string.Format (header, tags.Length);
			string msg = string.Empty;
			if (associated_photos > 0) {
				string photodesc = Catalog.GetPluralString ("photo", "photos", associated_photos);
				msg = string.Format (
					Catalog.GetPluralString ("If you delete this tag, the association with {0} {1} will be lost.",
						"If you delete these tags, the association with {0} {1} will be lost.",
						tags.Length),
					associated_photos, photodesc);
			}
			string ok_caption = Catalog.GetPluralString ("_Delete tag", "_Delete tags", tags.Length);

			if (ResponseType.Ok == HigMessageDialog.RunHigConfirmation (main_window,
				    DialogFlags.DestroyWithParent,
				    MessageType.Warning,
				    header,
				    msg,
				    ok_caption)) {
				try {
					db.Photos.Remove (tags);
				} catch (InvalidTagOperationException e) {
					Log.Debug ("this is something or another");

					// A Category is not empty. Can not delete it.
					string error_msg = Catalog.GetString ("Tag is not empty");
					string error_desc = string.Format (Catalog.GetString ("Can not delete tags that have tags within them.  " +
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

			int[] selected_ids = SelectedIds ();
			if (command.Execute (SelectedPhotos (selected_ids)))
				query.MarkChanged (selected_ids, InvalidateData.Instance);
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

		public void HandleCopy (object sender, EventArgs args)
		{
			Clipboard primary = Clipboard.Get (Atom.Intern ("PRIMARY", false));
			Clipboard clipboard = Clipboard.Get (Atom.Intern ("CLIPBOARD", false));

			if (Window.Focus is Editable) {
				(Window.Focus as Editable).CopyClipboard ();
				return;
			}

			TargetList targetList = new TargetList ();
			targetList.AddTextTargets ((uint)DragDropTargets.TargetType.PlainText);
			targetList.AddUriTargets ((uint)DragDropTargets.TargetType.UriList);
			targetList.Add (
				DragDropTargets.CopyFilesEntry.Target,
				(uint)DragDropTargets.CopyFilesEntry.Flags,
				(uint)DragDropTargets.CopyFilesEntry.Info);

			// use eager evaluation, because we want to copy the photos which are currently selected ...
			var uris = new UriList (from p in SelectedPhotos ()
			                                 select p.DefaultVersion.Uri);
			var paths = string.Join (" ",
				                     (from p in SelectedPhotos ()
				                                  select p.DefaultVersion.Uri.LocalPath).ToArray ()
			                     );

			clipboard.SetWithData ((TargetEntry[])targetList, delegate (Clipboard clip, SelectionData data, uint info) {

				if (info == (uint)DragDropTargets.TargetType.PlainText) {
					data.Text = paths;
					return;
				}

				if (info == (uint)DragDropTargets.TargetType.UriList) {
					data.SetUriListData (uris);
					return;
				}

				if (info == DragDropTargets.CopyFilesEntry.Info) {
					data.SetCopyFiles (uris);
					return;
				}

				Log.DebugFormat ("Unknown Selection Data Target (info: {0})", info);
			}, delegate {
			});

			primary.Text = paths;
		}

		void HandleSetAsBackgroundCommand (object sender, EventArgs args)
		{
			Photo current = CurrentPhoto;

			if (current == null)
				return;

			Desktop.SetBackgroundImage (current.DefaultVersion.Uri.LocalPath);
		}

		void HandleSetDateRange (object sender, EventArgs args)
		{
			var date_range_dialog = new DateRangeDialog (query.Range, main_window);
			if ((ResponseType)date_range_dialog.Run () == ResponseType.Ok)
				query.Range = date_range_dialog.Range;
			date_range_dialog.Destroy ();

			//update the TimeLine
			if (group_selector.Adaptor is TimeAdaptor && query.Range != null)
				group_selector.SetLimitsToDates (query.Range.Start, query.Range.End);
		}

		public void HandleClearDateRange (object sender, EventArgs args)
		{
			if (group_selector.Adaptor is FSpot.TimeAdaptor) {
				group_selector.ResetLimits ();
			}
			query.Range = null;
		}

		void HandleSelectLastRoll (object sender, EventArgs args)
		{
			query.RollSet = new RollSet (Database.Rolls.GetRolls (1));
		}

		void HandleSelectRolls (object sender, EventArgs args)
		{
			new LastRolls (query, Database.Rolls, main_window);
		}

		void HandleClearRollFilter (object sender, EventArgs args)
		{
			query.RollSet = null;
		}

		void HandleSetRatingFilter (object sender, EventArgs args)
		{
			new RatingFilterDialog (query, main_window);
		}

		public void HandleClearRatingFilter (object sender, EventArgs args)
		{
			query.RatingRange = null;
		}

		void HandleFindUntagged (object sender, EventArgs args)
		{
			if (query.Untagged == find_untagged.Active)
				return;

			query.Untagged = !query.Untagged;
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (string key)
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
				main_window.Move (Preferences.Get<int> (Preferences.MAIN_WINDOW_X),
					Preferences.Get<int> (Preferences.MAIN_WINDOW_Y));
				break;

			case Preferences.MAIN_WINDOW_WIDTH:
			case Preferences.MAIN_WINDOW_HEIGHT:
				if (Preferences.Get<int> (Preferences.MAIN_WINDOW_WIDTH) > 0 &&
				    Preferences.Get<int> (Preferences.MAIN_WINDOW_HEIGHT) > 0)
					main_window.Resize (Preferences.Get<int> (Preferences.MAIN_WINDOW_WIDTH),
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
				if (display_filmstrip.Active != Preferences.Get<bool> (key)) {
					display_filmstrip.Active = Preferences.Get<bool> (key);
				}
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
						IPhoto photo = group_selector.Adaptor.PhotoFromIndex (Preferences.Get<int> (key));

						if (photo != null)
							JumpTo (query.IndexOf (photo));
					} catch (Exception) {
					}
				}

				icon_view.GrabFocus ();
				break;
			case Preferences.SIDEBAR_POSITION:
				if (main_hpaned.Position != Preferences.Get<int> (key))
					main_hpaned.Position = Preferences.Get<int> (key);
				break;

			case Preferences.TAG_ICON_SIZE:
				int s = Preferences.Get<int> (key);
				tag_icon_hidden.Active = (s == (int)Settings.IconSize.Hidden);
				tag_icon_small.Active = (s == (int)Settings.IconSize.Small);
				tag_icon_medium.Active = (s == (int)Settings.IconSize.Medium);
				tag_icon_large.Active = (s == (int)Settings.IconSize.Large);

				break;

			case Preferences.ZOOM:
				icon_view.Zoom = Preferences.Get<double> (key);
				break;

			case Preferences.METADATA_EMBED_IN_IMAGE:
				write_metadata = Preferences.Get<bool> (key);
				break;
			case Preferences.GNOME_MAILTO_ENABLED:
				send_mail.Visible = Preferences.Get<bool> (key);
				break;
			}
		}

		// Version Id updates.
		void UpdateForVersionChange (IPhotoVersion version)
		{
			IPhotoVersionable versionable = CurrentPhoto;

			if (versionable != null) {
				versionable.SetDefaultVersion (version);
				query.Commit (ActiveIndex ());
			}
		}

		// Queries.
		public void UpdateQuery ()
		{
			main_window.GdkWindow.Cursor = watch;
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

		void HandleQueryLogicChanged (object sender, EventArgs args)
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
			MenuItem find_add_tag_with = uimanager.GetWidget ("/ui/menubar1/find/find_add_tag_with") as MenuItem;
			if (find_add_tag_with.Submenu != null)
				find_add_tag_with.Submenu.Dispose ();

			Gtk.Menu submenu = TermMenuItem.GetSubmenu (tag_selection_widget.TagHighlight);
			find_add_tag_with.Sensitive = (submenu != null);
			if (submenu != null)
				find_add_tag_with.Submenu = submenu;
		}

		public void HandleAddTagToTerm (object sender, EventArgs args)
		{
			MenuItem item = sender as MenuItem;

			if (item == null)
				return;

			int item_pos = (item.Parent as Menu).Children.Cast<MenuItem> ().TakeWhile (i => item != i).Count ();
			// account for All and separator menu items
			item_pos -= 2;

			Term parent_term = LogicWidget.Root.SubTerms [item_pos];

			if (LogicWidget.Box != null) {
				Literal after = parent_term.Last as Literal;
				LogicWidget.Box.InsertTerm (tag_selection_widget.TagHighlight, parent_term, after);
			}
		}

		//
		// Handle Main Menu
		void UpdateMenus ()
		{
			int tags_selected = tag_selection_widget.Selection.CountSelectedRows ();
			bool tag_sensitive = tags_selected > 0;
			bool active_selection = Selection.Count > 0;
			bool single_active = CurrentPhoto != null;
			MenuItem version_menu_item = uimanager.GetWidget ("/ui/menubar1/file1/version_menu_item") as MenuItem;

			if (!single_active) {
				version_menu_item.Sensitive = false;
				version_menu_item.Submenu = new Menu ();

				create_version_menu_item.Sensitive = false;
				delete_version_menu_item.Sensitive = false;
				detach_version_menu_item.Sensitive = false;
				rename_version_menu_item.Sensitive = false;

				sharpen.Sensitive = false;
				loupe_menu_item.Sensitive = false;
			} else {
				version_menu_item.Sensitive = true;
				create_version_menu_item.Sensitive = true;

				if (CurrentPhoto.DefaultVersionId == Photo.OriginalVersionId) {
					delete_version_menu_item.Sensitive = false;
					detach_version_menu_item.Sensitive = false;
					rename_version_menu_item.Sensitive = false;
				} else {
					delete_version_menu_item.Sensitive = true;
					detach_version_menu_item.Sensitive = true;
					rename_version_menu_item.Sensitive = true;
				}

				versions_submenu = new PhotoVersionMenu (CurrentPhoto);
				versions_submenu.VersionChanged += menu => UpdateForVersionChange (menu.Version);
				version_menu_item.Submenu = versions_submenu;

				sharpen.Sensitive = (ViewMode != ModeType.IconView);
				loupe_menu_item.Sensitive = (ViewMode != ModeType.IconView);
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
			copy.Sensitive = active_selection;
			remove_from_catalog.Sensitive = active_selection;

			clear_rating_filter.Sensitive = (query.RatingRange != null);

			clear_roll_filter.Sensitive = (query.RollSet != null);

			delete_selected_tag.Sensitive = tag_sensitive;
			edit_selected_tag.Sensitive = tag_sensitive;


			attach_tag_to_selection.Sensitive = tag_sensitive && active_selection;
			remove_tag_from_selection.Sensitive = tag_sensitive && active_selection;

			export.Sensitive = active_selection;

			MenuItem toolsmenu = uimanager.GetWidget ("/ui/menubar1/tools") as MenuItem;
			try {
				tools.Visible = (toolsmenu.Submenu as Menu).Children.Length > 0;
			} catch {
				tools.Visible = false;
			}

			if (rl_button != null) {
				if (Selection.Count == 0) {
					rl_button.Sensitive = false;
					rl_button.TooltipText = string.Empty;
				} else {
					rl_button.Sensitive = true;

					string msg = Catalog.GetPluralString ("Rotate selected photo left",
						             "Rotate selected photos left", Selection.Count);
					rl_button.TooltipText = string.Format (msg, Selection.Count);
				}
			}

			if (rr_button != null) {
				if (Selection.Count == 0) {
					rr_button.Sensitive = false;
					rr_button.TooltipText = string.Empty;
				} else {
					rr_button.Sensitive = true;

					string msg = Catalog.GetPluralString ("Rotate selected photo right",
						             "Rotate selected photos right", Selection.Count);
					rr_button.TooltipText = string.Format (msg, Selection.Count);
				}
			}

			//if (last_tags_selected_count != tags_selected) {
			MenuItem find_add_tag = uimanager.GetWidget ("/ui/menubar1/find/find_add_tag") as MenuItem;
			MenuItem find_add_tag_with = uimanager.GetWidget ("/ui/menubar1/find/find_add_tag_with") as MenuItem;

			((Gtk.Label)find_add_tag.Child).TextWithMnemonic = string.Format (
				Catalog.GetPluralString ("Find _Selected Tag", "Find _Selected Tags", tags_selected), tags_selected
			);

			((Gtk.Label)find_add_tag_with.Child).TextWithMnemonic = string.Format (
				Catalog.GetPluralString ("Find Selected Tag _With", "Find Selected Tags _With", tags_selected), tags_selected
			);

			find_add_tag.Sensitive = tag_sensitive;
			find_add_tag_with.Sensitive = tag_sensitive && find_add_tag_with.Submenu != null;

			//last_tags_selected_count = tags_selected;
			//}
		}

		void PopulateExtendableMenus (object o, EventArgs args)
		{
			var exportmenu = uimanager.GetWidget ("/ui/menubar1/file1/export") as MenuItem;
			var toolsmenu = uimanager.GetWidget ("/ui/menubar1/tools") as MenuItem;
			try {
				if (exportmenu.Submenu != null)
					exportmenu.Submenu.Dispose ();
				toolsmenu.Submenu = null;

				exportmenu.Submenu = (AddinManager.GetExtensionNode ("/FSpot/Menus/Exports") as SubmenuNode).GetSubmenu ();
				exportmenu.Submenu.ShowAll ();

				toolsmenu.Submenu = (AddinManager.GetExtensionNode ("/FSpot/Menus/Tools") as SubmenuNode).GetSubmenu ();
				toolsmenu.Submenu.ShowAll ();

				tools.Visible = (toolsmenu.Submenu as Menu).Children.Length > 0;
			} catch {
				Log.Warning ("There's (maybe) something wrong with some of the installed extensions. You can try removing the directory addin-db-000 from ~/.config/f-spot/");
				toolsmenu.Visible = false;
			}
		}

		public void HandleOpenWith (object sender, ApplicationActivatedEventArgs e)
		{
			GLib.AppInfo application = e.AppInfo;
			Photo[] selected = SelectedPhotos ();

			if (selected == null || selected.Length < 1)
				return;

			string header = Catalog.GetPluralString ("Create New Version?", "Create New Versions?", selected.Length);
			string msg = string.Format (Catalog.GetPluralString (
				             "Before launching {1}, should F-Spot create a new version of the selected photo to preserve the original?",
				             "Before launching {1}, should F-Spot create new versions of the selected photos to preserve the originals?", selected.Length),
				             selected.Length, application.Name);

			// FIXME add cancel button? add help button?
			HigMessageDialog hmd = new HigMessageDialog (GetToplevel (sender), DialogFlags.DestroyWithParent,
				                       MessageType.Question, Gtk.ButtonsType.None,
				                       header, msg);

			hmd.AddButton (Gtk.Stock.No, Gtk.ResponseType.No, false);
			//hmd.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, false);
			hmd.AddButton (Gtk.Stock.Yes, Gtk.ResponseType.Yes, true);

			bool support_xcf = false;
			;
			if (application.Id == "gimp.desktop")
				foreach (PixbufFormat format in Gdk.Pixbuf.Formats.Where(format => format.Name == "xcf"))
					support_xcf = true;

			//This allows creating a version with a .xcf extension.
			//There's no need to convert the file to xcf file format, gimp will take care of this
			if (support_xcf) {
				CheckButton cb = new CheckButton (Catalog.GetString ("XCF version"));
				cb.Active = Preferences.Get<bool> (Preferences.EDIT_CREATE_XCF_VERSION);
				hmd.VBox.Add (cb);
				cb.Toggled += (s, ea) => Preferences.Set (Preferences.EDIT_CREATE_XCF_VERSION, (s as CheckButton).Active);
				cb.Show ();
			}

			Gtk.ResponseType response = Gtk.ResponseType.Cancel;

			try {
				response = (Gtk.ResponseType)hmd.Run ();
			} finally {
				hmd.Destroy ();
			}

			bool create_xcf = false;
			if (support_xcf)
				create_xcf = Preferences.Get<bool> (Preferences.EDIT_CREATE_XCF_VERSION);

			Log.DebugFormat ("XCF ? {0}", create_xcf);

			if (response == Gtk.ResponseType.Cancel)
				return;

			bool create_new_versions = (response == Gtk.ResponseType.Yes);

			List<EditException> errors = new List<EditException> ();
			GLib.List uri_list = new GLib.List (typeof(string));
			foreach (Photo photo in selected) {
				try {
					if (create_new_versions) {
						uint version = photo.CreateNamedVersion (application.Name, create_xcf ? ".xcf" : null, photo.DefaultVersionId, true);
						photo.DefaultVersionId = version;
					}
				} catch (Exception ex) {
					errors.Add (new EditException (photo, ex));
				}

				uri_list.Append (photo.DefaultVersion.Uri.ToString ());
			}

			// FIXME need to clean up the error dialog here.
			if (errors.Count > 0) {
				Dialog md = new EditExceptionDialog (GetToplevel (sender), errors.ToArray ());
				md.Run ();
				md.Destroy ();
			}

			if (create_new_versions)
				Database.Photos.Commit (selected);

			try {
				application.LaunchUris (uri_list, null);
			} catch (Exception) {
				Log.ErrorFormat ("Failed to lauch {0}", application.Name);
			}
		}

		public void GetWidgetPosition (Widget widget, out int x, out int y)
		{
			main_window.GdkWindow.GetOrigin (out x, out y);

			x += widget.Allocation.X;
			y += widget.Allocation.Y;
		}

		// Tag typing ...

		void UpdateTagEntryFromSelection ()
		{
			if (!tagbar.Visible)
				return;
			tag_entry.UpdateFromSelection (SelectedPhotos ());
		}

		public void HandlePossibleTagTyping (object sender, Gtk.KeyPressEventArgs args)
		{
			if (Selection.Count == 0 || tagbar.Visible && tag_entry.HasFocus)
				return;

			if (args.Event.Key != Gdk.Key.t)
				return;

			tagbar.Show ();
			UpdateTagEntryFromSelection ();
			tag_entry.GrabFocus ();
			tag_entry.SelectRegion (-1, -1);
		}

		// "Activate" means the user pressed the enter key
		public void HandleTagEntryActivate (object sender, EventArgs args)
		{
			if (ViewMode == ModeType.IconView) {
				icon_view.GrabFocus ();
			} else {
				photo_view.QueueDraw ();
				photo_view.View.GrabFocus ();
			}
		}

		void HandleTagEntryTagsAttached (object o, string[] new_tags)
		{
			int[] selected_photos = SelectedIds ();
			if (selected_photos == null || new_tags == null || new_tags.Length == 0)
				return;

			Category default_category = null;
			Tag[] selection = tag_selection_widget.TagHighlight;
			if (selection.Length > 0) {
				if (selection [0] is Category)
					default_category = (Category)selection [0];
				else
					default_category = selection [0].Category;
			}
			Tag[] tags = new Tag [new_tags.Length];
			int i = 0;
			Database.BeginTransaction ();
			foreach (string tagname in new_tags) {
				Tag t = Database.Tags.GetTagByName (tagname);
				if (t == null) {
					t = Database.Tags.CreateCategory (default_category, tagname, true);
					Database.Tags.Commit (t);
				}
				tags [i++] = t;
			}
			AddTagExtended (selected_photos, tags);
			Database.CommitTransaction ();
		}

		void HandleTagEntryRemoveTags (object o, Tag[] remove_tags)
		{
			int[] selected_photos = SelectedIds ();
			if (selected_photos == null || remove_tags == null || remove_tags.Length == 0)
				return;

			Database.BeginTransaction ();
			RemoveTags (selected_photos, remove_tags);
			Database.CommitTransaction ();
		}

		void HideTagbar ()
		{
			if (!tagbar.Visible)
				return;

			UpdateTagEntryFromSelection ();

			// Cancel any pending edits...
			tagbar.Hide ();

			if (ViewMode == ModeType.IconView)
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

		public List<string> SelectedMimeTypes ()
		{
			var contents = new List<string> ();

			foreach (Photo p in SelectedPhotos ()) {
				string content;
				try {
					content = GLib.FileFactory.NewForUri (p.DefaultVersion.Uri).QueryInfo ("standard::content-type", GLib.FileQueryInfoFlags.None, null).ContentType;
				} catch (GLib.GException) {
					content = null;
				}

				if (!contents.Contains (content))
					contents.Add (content);
			}

			return contents;
		}

		void ShowQueryWidget ()
		{
			if (find_bar.Visible) {
				find_bar.Entry.Text = string.Empty;
				find_bar.Hide ();
			}

			query_widget.ShowBar ();
		}

		public void HideSidebar (object o, EventArgs args)
		{
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

					find_bar.ShowAll ();
				}

				// Grab the focus even if it's already shown
				find_bar.Entry.GrabFocus ();
				args.RetVal = true;
				return;
			}

			args.RetVal = false;
		}
	}
}
