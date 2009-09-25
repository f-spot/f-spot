using Gtk;
using Gdk;
using System;
using Mono.Addins;
using Mono.Unix;

using FSpot.Extensions;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Widgets;
using FSpot.Platform;

namespace FSpot {
	public class SingleView {
		[Glade.Widget] Gtk.HBox toolbar_hbox;
		[Glade.Widget] Gtk.VBox info_vbox;
		[Glade.Widget] Gtk.ScrolledWindow image_scrolled;
		[Glade.Widget] Gtk.HPaned info_hpaned;

		Gtk.ScrolledWindow directory_scrolled;

		[Glade.Widget] Gtk.CheckMenuItem side_pane_item;
		[Glade.Widget] Gtk.CheckMenuItem toolbar_item;
		[Glade.Widget] Gtk.CheckMenuItem filenames_item;
		
		[Glade.Widget] Gtk.MenuItem zoom_in;
		[Glade.Widget] Gtk.MenuItem zoom_out;

		[Glade.Widget] Gtk.MenuItem export;

		[Glade.Widget] Gtk.Scale zoom_scale;

		[Glade.Widget] Label status_label;

		[Glade.Widget] ImageMenuItem rotate_left;
		[Glade.Widget] ImageMenuItem rotate_right;

		ToolButton rr_button, rl_button;

		Sidebar sidebar;

		protected Glade.XML xml;
		private Gtk.Window window;
		PhotoImageView image_view;
		FSpot.Widgets.IconView directory_view;
		private Uri uri;
		
		UriCollection collection;
		
		FullScreenView fsview;

		private static Gtk.Tooltips toolTips = new Gtk.Tooltips ();

		public SingleView () : this (FSpot.Global.HomeDirectory) {}


		public SingleView (string path) : this (UriUtils.PathToFileUri (path)) 
		{
		}

		public SingleView (Uri uri) : this (new Uri [] { uri })
		{
		}

		public SingleView (UriList list) : this (list.ToArray ())
		{
		}

		public SingleView (Uri [] uris) 
		{
			string glade_name = "single_view";
			this.uri = uris [0];
			
			xml = new Glade.XML (null, "f-spot.glade", glade_name, "f-spot");
			xml.Autoconnect (this);
			window = (Gtk.Window) xml.GetWidget (glade_name);
		
			LoadPreference (Preferences.VIEWER_WIDTH);
			LoadPreference (Preferences.VIEWER_MAXIMIZED);

			Gtk.Toolbar toolbar = new Gtk.Toolbar ();
			toolbar_hbox.PackStart (toolbar);
		
			rl_button = GtkUtil.ToolButtonFromTheme ("object-rotate-left", Catalog.GetString ("Rotate Left"), true);
			rl_button.Clicked += HandleRotate270Command;
			rl_button.SetTooltip (toolTips, Catalog.GetString ("Rotate photo left"), null);
			toolbar.Insert (rl_button, -1);

			rr_button = GtkUtil.ToolButtonFromTheme ("object-rotate-right", Catalog.GetString ("Rotate Right"), true);
			rr_button.Clicked += HandleRotate90Command;
			rr_button.SetTooltip (toolTips, Catalog.GetString ("Rotate photo right"), null);
			toolbar.Insert (rr_button, -1);

			toolbar.Insert (new SeparatorToolItem (), -1);

			ToolButton fs_button = GtkUtil.ToolButtonFromTheme ("view-fullscreen", Catalog.GetString ("Fullscreen"), true);
			fs_button.Clicked += HandleViewFullscreen;
			fs_button.SetTooltip (toolTips, Catalog.GetString ("View photos fullscreen"), null);
			toolbar.Insert (fs_button, -1);

			ToolButton ss_button = GtkUtil.ToolButtonFromTheme ("media-playback-start", Catalog.GetString ("Slideshow"), true);
			ss_button.Clicked += HandleViewSlideshow;
			ss_button.SetTooltip (toolTips, Catalog.GetString ("View photos in a slideshow"), null);
			toolbar.Insert (ss_button, -1);

			collection = new UriCollection (uris);

			TargetEntry [] dest_table = {
				FSpot.DragDropTargets.UriListEntry,
				FSpot.DragDropTargets.PlainTextEntry
			};
			
			directory_view = new FSpot.Widgets.IconView (collection);
			directory_view.Selection.Changed += HandleSelectionChanged;
			directory_view.DragDataReceived += HandleDragDataReceived;
			Gtk.Drag.DestSet (directory_view, DestDefaults.All, dest_table, 
					DragAction.Copy | DragAction.Move); 
			directory_view.DisplayTags = false;
			directory_view.DisplayDates = false;
			directory_view.DisplayRatings = false;

			directory_scrolled = new ScrolledWindow();
			directory_scrolled.Add (directory_view);

			sidebar = new Sidebar ();

			info_vbox.Add (sidebar);
			sidebar.AppendPage (directory_scrolled, Catalog.GetString ("Folder"), "gtk-directory");

			AddinManager.AddExtensionNodeHandler ("/FSpot/Sidebar", OnSidebarExtensionChanged);
 		
			sidebar.Context = ViewContext.Single;

			sidebar.CloseRequested += HandleHideSidePane;
			sidebar.Show ();

			ThumbnailGenerator.Default.OnPixbufLoaded += delegate { directory_view.QueueDraw (); };

			image_view = new PhotoImageView (collection);
			GtkUtil.ModifyColors (image_view);
			GtkUtil.ModifyColors (image_scrolled);
			image_view.ZoomChanged += HandleZoomChanged;
			image_view.Item.Changed += HandleItemChanged;
			image_view.ButtonPressEvent += HandleImageViewButtonPressEvent;
			image_view.DragDataReceived += HandleDragDataReceived;
			Gtk.Drag.DestSet (image_view, DestDefaults.All, dest_table,
					DragAction.Copy | DragAction.Move); 
			image_scrolled.Add (image_view);
			
			Window.ShowAll ();

			zoom_scale.ValueChanged += HandleZoomScaleValueChanged;
		
			LoadPreference (Preferences.VIEWER_SHOW_TOOLBAR);
 			LoadPreference (Preferences.VIEWER_INTERPOLATION);
			LoadPreference (Preferences.VIEWER_TRANSPARENCY);
			LoadPreference (Preferences.VIEWER_TRANS_COLOR);

			ShowSidebar = collection.Count > 1;

			LoadPreference (Preferences.VIEWER_SHOW_FILENAMES);

			Preferences.SettingChanged += OnPreferencesChanged;
			window.DeleteEvent += HandleDeleteEvent;
			
			collection.Changed += HandleCollectionChanged;

			// wrap the methods to fit to the delegate
			image_view.Item.Changed += delegate (object sender, BrowsablePointerChangedEventArgs old) {
					BrowsablePointer pointer = sender as BrowsablePointer;
					if (pointer == null)
						return;
					IBrowsableItem [] item = {pointer.Current};
					PhotoArray item_array = new PhotoArray (item);
					sidebar.HandleSelectionChanged (item_array);
			};
			
			image_view.Item.Collection.ItemsChanged += sidebar.HandleSelectionItemsChanged;

			UpdateStatusLabel ();
			
			if (collection.Count > 0)
				directory_view.Selection.Add (0);

			export.Submenu = (Mono.Addins.AddinManager.GetExtensionNode ("/FSpot/Menus/Exports") as FSpot.Extensions.SubmenuNode).GetMenuItem (this).Submenu;
			export.Submenu.ShowAll ();
			export.Activated += HandleExportActivated ;
		}

		private void OnSidebarExtensionChanged (object s, ExtensionNodeEventArgs args) {
			// FIXME: No sidebar page removal yet!
			if (args.Change == ExtensionChange.Add)
				sidebar.AppendPage ((args.ExtensionNode as SidebarPageNode).GetSidebarPage ());
		}

		void HandleExportActivated (object o, EventArgs e)
		{
			FSpot.Extensions.ExportMenuItemNode.SelectedImages = delegate () {return new FSpot.PhotoArray (directory_view.Selection.Items); };
		}

		public void HandleCollectionChanged (IBrowsableCollection collection)
		{
			if (collection.Count > 0 && directory_view.Selection.Count == 0) {
				Console.WriteLine ("Added selection");
				directory_view.Selection.Add (0);
			}

			if (collection.Count > 1)
				ShowSidebar = true;

			rotate_left.Sensitive = rotate_right.Sensitive = rr_button.Sensitive = rl_button.Sensitive = collection.Count != 0;

			UpdateStatusLabel ();
		}

		public bool ShowSidebar {
			get {
				return info_vbox.Visible;
			}
			set {
				info_vbox.Visible = value;
				if (side_pane_item.Active != value)
					side_pane_item.Active = value;
			}
		}
		
		public bool ShowToolbar {
			get {
				return toolbar_hbox.Visible;
			}
			set {
				toolbar_hbox.Visible = value;
				if (toolbar_item.Active != value)
					toolbar_item.Active = value;
			}
		}

		private Uri CurrentUri
		{
			get { 
			 	return this.uri; 
			}
			set {
			 	this.uri = value;
				collection.Clear ();
				collection.LoadItems (new Uri[] { this.uri });
			}
		}

		void HandleRotate90Command (object sender, System.EventArgs args) 
		{
			RotateCommand command = new RotateCommand (this.Window);
			if (command.Execute (RotateDirection.Clockwise, new IBrowsableItem [] { image_view.Item.Current }))
				collection.MarkChanged (image_view.Item.Index, FullInvalidate.Instance);
		}

		void HandleRotate270Command (object sender, System.EventArgs args) 
		{
			RotateCommand command = new RotateCommand (this.Window);
			if (command.Execute (RotateDirection.Counterclockwise, new IBrowsableItem [] { image_view.Item.Current }))
				collection.MarkChanged (image_view.Item.Index, FullInvalidate.Instance);
		}		

		private void HandleSelectionChanged (FSpot.IBrowsableCollection selection) 
		{
			
			if (selection.Count > 0) {
				image_view.Item.Index = ((FSpot.Widgets.IconView.SelectionCollection)selection).Ids[0];

				zoom_scale.Value = image_view.NormalizedZoom;
			}
			UpdateStatusLabel ();
		}

		private void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs old)
		{
			BrowsablePointer pointer = sender as BrowsablePointer;
			if (pointer == null)
				return;

			directory_view.FocusCell = pointer.Index;
			directory_view.Selection.Clear ();
			if (collection.Count > 0) {
				directory_view.Selection.Add (directory_view.FocusCell);
				directory_view.ScrollTo (directory_view.FocusCell);
			}
		}

		void HandleSetAsBackgroundCommand (object sender, EventArgs args)
		{
			IBrowsableItem current = image_view.Item.Current;

			if (current == null)
				return;

			Desktop.SetBackgroundImage (current.DefaultVersionUri.LocalPath);
		}

		private void HandleViewToolbar (object sender, System.EventArgs args)
		{
			ShowToolbar = toolbar_item.Active;
		}
		
		private void HandleHideSidePane (object sender, System.EventArgs args) 
		{
			ShowSidebar = false;
		}

		private void HandleViewSidePane (object sender, System.EventArgs args)
		{
			ShowSidebar = side_pane_item.Active;
		}

		private void HandleViewSlideshow (object sender, System.EventArgs args)
		{
			HandleViewFullscreen (sender, args);
			fsview.PlayPause ();
		}
	
		private void HandleViewFilenames (object sender, System.EventArgs args)
		{
			directory_view.DisplayFilenames = filenames_item.Active; 
			UpdateStatusLabel ();
		}

		private void HandleAbout (object sender, System.EventArgs args)
		{
			FSpot.UI.Dialog.AboutDialog.ShowUp ();
		}

		private void HandleNewWindow (object sender, System.EventArgs args)
		{
			/* FIXME this needs to register witth the core */
			new SingleView (uri);
		}

		private void HandlePreferences (object sender, System.EventArgs args)
		{
			SingleView.PreferenceDialog.Show ();
		}

		private void HandleOpenFolder (object sender, System.EventArgs args)
		{
			Open (FileChooserAction.SelectFolder);
		}

		private void HandleOpen (object sender, System.EventArgs args)
		{
			Open (FileChooserAction.Open);
		}

		private void Open (FileChooserAction action)
		{
			string title = Catalog.GetString ("Open");

			if (action == FileChooserAction.SelectFolder)
				title = Catalog.GetString ("Select Folder");

			FileChooserDialog chooser = new FileChooserDialog (title,
									   window,
									   action);

			chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
			chooser.AddButton (Stock.Open, ResponseType.Ok);

			chooser.SetUri (uri.ToString ());
			int response = chooser.Run ();

			if ((ResponseType) response == ResponseType.Ok)
				CurrentUri = new System.Uri (chooser.Uri);
			

			chooser.Destroy ();
		}

		private bool SlideShow ()
		{
			IBrowsableItem [] items = new IBrowsableItem [collection.Count];
			for (int i = 0; i < collection.Count; i++) {
				items [i] = collection [i];
			}

			FSpot.FullSlide full = new FSpot.FullSlide (Window, items);
			full.Play ();
			this.Window.GdkWindow.Cursor = null;
			return false;
		}

		private void HandleViewFullscreen (object sender, System.EventArgs args)
		{
			if (fsview != null)
				fsview.Destroy ();

			fsview = new FSpot.FullScreenView (collection, window);
			fsview.Destroyed += HandleFullScreenViewDestroy;

			fsview.View.Item.Index = image_view.Item.Index;
			fsview.Show ();
		}
		
		private void HandleFullScreenViewDestroy (object sender, System.EventArgs args)
		{
			directory_view.Selection.Clear ();
			if (fsview.View.Item.IsValid) 
				directory_view.Selection.Add (fsview.View.Item.Index);
			fsview = null;
		}
		
		public void HandleZoomOut (object sender, System.EventArgs args)
		{
			image_view.ZoomOut ();
		}

		public void HandleZoomOut (object sender, Gtk.ButtonPressEventArgs args)
		{
			image_view.ZoomOut ();
		}

		public void HandleZoomIn (object sender, Gtk.ButtonPressEventArgs args)
		{
			image_view.ZoomIn ();
		}

		public void HandleZoomIn (object sender, System.EventArgs args)
		{
			image_view.ZoomIn ();
		}

		private void HandleZoomScaleValueChanged (object sender, System.EventArgs args)
		{
			image_view.NormalizedZoom = zoom_scale.Value;
		}

		private void HandleZoomChanged (object sender, System.EventArgs args)
		{
			zoom_scale.Value = image_view.NormalizedZoom;

			// FIXME something is broken here
			//zoom_in.Sensitive = (zoom_scale.Value != 1.0);
			//zoom_out.Sensitive = (zoom_scale.Value != 0.0);
		}

		private void HandleImageViewButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Type != EventType.ButtonPress || args.Event.Button != 3)
			 	return;

			Gtk.Menu popup_menu = new Gtk.Menu ();
			bool has_item = image_view.Item.Current != null;

			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Rotate _Left"), "object-rotate-left", delegate { HandleRotate270Command(window, null); }, has_item);
			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Rotate _Right"), "object-rotate-right", delegate { HandleRotate90Command (window, null); }, has_item);
			GtkUtil.MakeMenuSeparator (popup_menu);
			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Set as Background"), HandleSetAsBackgroundCommand, has_item);

			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
		}

		void HandleDeleteEvent (object sender, DeleteEventArgs args)
		{
			SavePreferences ();
			this.Window.Destroy ();
			args.RetVal = true;
		}

		void HandleDragDataReceived (object sender, DragDataReceivedArgs args) 
		{
			if (args.Info == FSpot.DragDropTargets.UriListEntry.Info
			    || args.Info == FSpot.DragDropTargets.PlainTextEntry.Info) {
				
				/* 
				 * If the drop is coming from inside f-spot then we don't want to import 
				 */
				if (Gtk.Drag.GetSourceWidget (args.Context) != null)
					return;
				
				UriList list = args.SelectionData.GetUriListData ();
				collection.LoadItems (list.ToArray());
				
				Gtk.Drag.Finish (args.Context, true, false, args.Time);
				
				return;
			}
		}

		private void UpdateStatusLabel ()
		{
			IBrowsableItem item = image_view.Item.Current;
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			if (filenames_item.Active && item != null)
				sb.Append (System.IO.Path.GetFileName (item.DefaultVersionUri.LocalPath) + "  -  ");

			sb.AppendFormat (Catalog.GetPluralString ("{0} Photo", "{0} Photos", collection.Count), collection.Count);
			status_label.Text = sb.ToString ();
		}

		private void HandleFileClose (object sender, System.EventArgs args)
		{
			SavePreferences ();
			this.Window.Destroy ();
		}

		private void SavePreferences  ()
		{
			int width, height;
			window.GetSize (out width, out height);
		
			bool maximized = ((window.GdkWindow.State & Gdk.WindowState.Maximized) > 0);
			Preferences.Set (Preferences.VIEWER_MAXIMIZED, maximized);
		
			if (!maximized) {
				Preferences.Set (Preferences.VIEWER_WIDTH,	width);
				Preferences.Set (Preferences.VIEWER_HEIGHT,	height);
			}
		
			Preferences.Set (Preferences.VIEWER_SHOW_TOOLBAR,	toolbar_hbox.Visible);
			Preferences.Set (Preferences.VIEWER_SHOW_FILENAMES, filenames_item.Active);
		}

		private void HandleFileOpen (object sender, System.EventArgs args)
		{
			string open = null;
			
			FileChooserDialog file_selector =
				new FileChooserDialog ("Open", this.Window,
						       FileChooserAction.Open);
			
			file_selector.SetUri (uri.ToString ());
			int response = file_selector.Run ();
			
			if ((Gtk.ResponseType) response == Gtk.ResponseType.Ok) {
				open = file_selector.Filename;
				new FSpot.SingleView (open);
			}
			
			file_selector.Destroy ();
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (String key)
		{
			switch (key) {
			case Preferences.VIEWER_MAXIMIZED:
				if (Preferences.Get<bool> (key))
					window.Maximize ();
				else
					window.Unmaximize ();
				break;

			case Preferences.VIEWER_WIDTH:
			case Preferences.VIEWER_HEIGHT:
				window.SetDefaultSize(Preferences.Get<int> (Preferences.VIEWER_WIDTH),
						      Preferences.Get<int> (Preferences.VIEWER_HEIGHT));

				window.ReshowWithInitialSize();
				break;
			
			case Preferences.VIEWER_SHOW_TOOLBAR:
				if (toolbar_item.Active != Preferences.Get<bool> (key))
					toolbar_item.Active = Preferences.Get<bool> (key);

				toolbar_hbox.Visible = Preferences.Get<bool> (key);
				break;

			case Preferences.VIEWER_INTERPOLATION:
				if (Preferences.Get<bool> (key))
					image_view.Interpolation = Gdk.InterpType.Bilinear;
				else
					image_view.Interpolation = Gdk.InterpType.Nearest;
				break;

			case Preferences.VIEWER_SHOW_FILENAMES:
				if (filenames_item.Active != Preferences.Get<bool> (key))
					filenames_item.Active = Preferences.Get<bool> (key);
				break;

			case Preferences.VIEWER_TRANSPARENCY:
				if (Preferences.Get<string> (key) == "CHECK_PATTERN")
					image_view.CheckPattern = CheckPattern.Dark;
				else if (Preferences.Get<string> (key) == "COLOR")
					image_view.CheckPattern = new CheckPattern (Preferences.Get<string> (Preferences.VIEWER_TRANS_COLOR));
				else // NONE
					image_view.CheckPattern = new CheckPattern (image_view.Style.BaseColors [(int)Gtk.StateType.Normal]);
				break;

			case Preferences.VIEWER_TRANS_COLOR:
				if (Preferences.Get<string> (Preferences.VIEWER_TRANSPARENCY) == "COLOR")
					image_view.CheckPattern = new CheckPattern (Preferences.Get<string> (key));
				break;
			}
		}

		public Gtk.Window Window {
			get { 
				return window;
			}
		}

		public class PreferenceDialog : GladeDialog {
			[Glade.Widget] private CheckButton interpolation_check;
			[Glade.Widget] private ColorButton color_button;
			[Glade.Widget] private RadioButton as_background_radio;
			[Glade.Widget] private RadioButton as_check_radio;
			[Glade.Widget] private RadioButton as_color_radio;

			public PreferenceDialog () : base ("viewer_preferences")
			{
				this.LoadPreference (Preferences.VIEWER_INTERPOLATION);
				this.LoadPreference (Preferences.VIEWER_TRANSPARENCY);
				this.LoadPreference (Preferences.VIEWER_TRANS_COLOR);
				Preferences.SettingChanged += OnPreferencesChanged;
				this.Dialog.Destroyed += HandleDestroyed;
			}

			void InterpolationToggled (object sender, System.EventArgs args)
			{
				Preferences.Set (Preferences.VIEWER_INTERPOLATION, interpolation_check.Active);
			}

			void HandleTransparentColorSet (object sender, System.EventArgs args)
			{
				Preferences.Set (Preferences.VIEWER_TRANS_COLOR, 
						"#" + 
						(color_button.Color.Red / 256 ).ToString("x").PadLeft (2, '0') +
						(color_button.Color.Green / 256 ).ToString("x").PadLeft (2, '0') +
						(color_button.Color.Blue / 256 ).ToString("x").PadLeft (2, '0'));
			}

			void HandleTransparencyToggled (object sender, System.EventArgs args)
			{
				if (as_background_radio.Active)
					Preferences.Set (Preferences.VIEWER_TRANSPARENCY, "NONE");
				else if (as_check_radio.Active)
					Preferences.Set (Preferences.VIEWER_TRANSPARENCY, "CHECK_PATTERN");
				else if (as_color_radio.Active)
					Preferences.Set (Preferences.VIEWER_TRANSPARENCY, "COLOR");
			}
			
			static PreferenceDialog prefs;
			public static void Show ()
			{
				if (prefs == null)
					prefs = new PreferenceDialog ();
				
				prefs.Dialog.Present ();
			}

			void OnPreferencesChanged (object sender, NotifyEventArgs args)
			{
				LoadPreference (args.Key);
			}

			void HandleClose (object sender, EventArgs args)
			{
				this.Dialog.Destroy ();
			}

			private void HandleDestroyed (object sender, EventArgs args)
			{
				prefs = null;
			}

			void LoadPreference (string key)
			{
		
				switch (key) {
				case Preferences.VIEWER_INTERPOLATION:
					interpolation_check.Active = Preferences.Get<bool> (key);
					break;
				case Preferences.VIEWER_TRANSPARENCY:
					switch (Preferences.Get<string> (key)) {
					case "COLOR":
						as_color_radio.Active = true;
						break;
					case "CHECK_PATTERN":
						as_check_radio.Active = true;
						break;
					default: //NONE
						as_background_radio.Active = true;
						break;
					}
					break;
				case Preferences.VIEWER_TRANS_COLOR:
					color_button.Color = new Gdk.Color (
						Byte.Parse (Preferences.Get<string> (key).Substring (1,2), System.Globalization.NumberStyles.AllowHexSpecifier),
						Byte.Parse (Preferences.Get<string> (key).Substring (3,2), System.Globalization.NumberStyles.AllowHexSpecifier),
						Byte.Parse (Preferences.Get<string> (key).Substring (5,2), System.Globalization.NumberStyles.AllowHexSpecifier));
					break;
				}
			}
		}
	}
}
