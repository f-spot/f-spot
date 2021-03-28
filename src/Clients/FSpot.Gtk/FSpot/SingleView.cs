//
// SingleView.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephane Delcroix <stephane@delcroix.org>
//   Larry Ewing <lewing@src.gnome.org>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2006-2010 Stephane Delcroix
// Copyright (C) 2005-2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Gtk;
using Gdk;

using System;
using System.Collections.Generic;

using Mono.Addins;
using Mono.Unix;

using Hyena;

using FSpot.Extensions;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Widgets;
using FSpot.Platform;
using FSpot.Core;
using FSpot.Settings;
using FSpot.Thumbnail;

namespace FSpot {
	public class SingleView {
		ToolButton rr_button, rl_button;
		Sidebar sidebar;
		Gtk.ScrolledWindow directory_scrolled;

#pragma warning disable 649
		[GtkBeans.Builder.Object]  Gtk.HBox toolbar_hbox;
		[GtkBeans.Builder.Object]  Gtk.VBox info_vbox;
		[GtkBeans.Builder.Object]  Gtk.ScrolledWindow image_scrolled;

		[GtkBeans.Builder.Object]  Gtk.CheckMenuItem side_pane_item;
		[GtkBeans.Builder.Object]  Gtk.CheckMenuItem toolbar_item;
		[GtkBeans.Builder.Object]  Gtk.CheckMenuItem filenames_item;

		[GtkBeans.Builder.Object]  Gtk.MenuItem export;

		[GtkBeans.Builder.Object]  Gtk.Scale zoom_scale;

		[GtkBeans.Builder.Object]  Label status_label;

		[GtkBeans.Builder.Object]  ImageMenuItem rotate_left;
		[GtkBeans.Builder.Object]  ImageMenuItem rotate_right;

		[GtkBeans.Builder.Object] Gtk.Window single_view;
#pragma warning restore 649

		public Gtk.Window Window {
			get {
				return single_view;
			}
		}

		PhotoImageView image_view;
		SelectionCollectionGridView directory_view;
		SafeUri uri;

		UriCollection collection;

		FullScreenView fsview;

		public SingleView (SafeUri [] uris)
		{
			uri = uris [0];
			Log.Debug ("uri: " + uri);

			var builder = new GtkBeans.Builder ("single_view.ui");
			builder.Autoconnect (this);

			LoadPreference (Preferences.ViewerWidth);
			LoadPreference (Preferences.ViewerMaximized);

			Gtk.Toolbar toolbar = new Gtk.Toolbar ();
			toolbar_hbox.PackStart (toolbar);

			rl_button = GtkUtil.ToolButtonFromTheme ("object-rotate-left", Catalog.GetString ("Rotate Left"), true);
			rl_button.Clicked += HandleRotate270Command;
			rl_button.TooltipText = Catalog.GetString ("Rotate photo left");
			toolbar.Insert (rl_button, -1);

			rr_button = GtkUtil.ToolButtonFromTheme ("object-rotate-right", Catalog.GetString ("Rotate Right"), true);
			rr_button.Clicked += HandleRotate90Command;
			rr_button.TooltipText = Catalog.GetString ("Rotate photo right");
			toolbar.Insert (rr_button, -1);

			toolbar.Insert (new SeparatorToolItem (), -1);

			ToolButton fs_button = GtkUtil.ToolButtonFromTheme ("view-fullscreen", Catalog.GetString ("Fullscreen"), true);
			fs_button.Clicked += HandleViewFullscreen;
			fs_button.TooltipText = Catalog.GetString ("View photos fullscreen");
			toolbar.Insert (fs_button, -1);

			ToolButton ss_button = GtkUtil.ToolButtonFromTheme ("media-playback-start", Catalog.GetString ("Slideshow"), true);
			ss_button.Clicked += HandleViewSlideshow;
			ss_button.TooltipText = Catalog.GetString ("View photos in a slideshow");
			toolbar.Insert (ss_button, -1);

			collection = new UriCollection (uris);

			TargetList targetList = new TargetList();
			targetList.AddTextTargets((uint)DragDropTargets.TargetType.PlainText);
			targetList.AddUriTargets((uint)DragDropTargets.TargetType.UriList);

			directory_view = new SelectionCollectionGridView (collection);
			directory_view.Selection.Changed += HandleSelectionChanged;
			directory_view.DragDataReceived += HandleDragDataReceived;
			Gtk.Drag.DestSet (directory_view, DestDefaults.All, (TargetEntry[])targetList,
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

			App.Instance.Container.Resolve<IThumbnailLoader> ().OnPixbufLoaded += delegate { directory_view.QueueDraw (); };

			image_view = new PhotoImageView (collection);
			GtkUtil.ModifyColors (image_view);
			GtkUtil.ModifyColors (image_scrolled);
			image_view.ZoomChanged += HandleZoomChanged;
			image_view.Item.Changed += HandleItemChanged;
			image_view.ButtonPressEvent += HandleImageViewButtonPressEvent;
			image_view.DragDataReceived += HandleDragDataReceived;
			Gtk.Drag.DestSet (image_view, DestDefaults.All, (TargetEntry[])targetList,
					DragAction.Copy | DragAction.Move);
			image_scrolled.Add (image_view);

			Window.ShowAll ();

			zoom_scale.ValueChanged += HandleZoomScaleValueChanged;

			LoadPreference (Preferences.ViewerShowToolbar);
			LoadPreference (Preferences.ViewerInterpolation);
			LoadPreference (Preferences.ViewerTransparency);
			LoadPreference (Preferences.ViewerTransColor);

			ShowSidebar = collection.Count > 1;

			LoadPreference (Preferences.ViewerShawFilenames);

			Preferences.SettingChanged += OnPreferencesChanged;
			Window.DeleteEvent += HandleDeleteEvent;

			collection.Changed += HandleCollectionChanged;

			// wrap the methods to fit to the delegate
			image_view.Item.Changed += delegate (object sender, BrowsablePointerChangedEventArgs old) {
					BrowsablePointer pointer = sender as BrowsablePointer;
					if (pointer == null)
						return;
					IPhoto [] item = {pointer.Current};
					sidebar.HandleSelectionChanged (new PhotoList (item));
			};

			image_view.Item.Collection.ItemsChanged += sidebar.HandleSelectionItemsChanged;

			UpdateStatusLabel ();

			if (collection.Count > 0)
				directory_view.Selection.Add (0);

			export.Submenu = (Mono.Addins.AddinManager.GetExtensionNode ("/FSpot/Menus/Exports") as FSpot.Extensions.SubmenuNode).GetMenuItem (this).Submenu;
			export.Submenu.ShowAll ();
			export.Activated += HandleExportActivated ;
		}

		void OnSidebarExtensionChanged (object s, ExtensionNodeEventArgs args) {
			// FIXME: No sidebar page removal yet!
			if (args.Change == ExtensionChange.Add)
				sidebar.AppendPage ((args.ExtensionNode as SidebarPageNode).GetPage ());
		}

		void HandleExportActivated (object o, EventArgs e)
		{
			FSpot.Extensions.ExportMenuItemNode.SelectedImages = () => new PhotoList(directory_view.Selection.Items);
		}

		public void HandleCollectionChanged (IBrowsableCollection collection)
		{
			if (collection.Count > 0 && directory_view.Selection.Count == 0) {
				Log.Debug ("Added selection");
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

		SafeUri CurrentUri
		{
			get {
				return uri;
			}
			set {
				uri = value;
				collection.Clear ();
				collection.LoadItems (new SafeUri[] { uri });
			}
		}

		void HandleRotate90Command (object sender, EventArgs args)
		{
			RotateCommand command = new RotateCommand (Window);
			if (command.Execute (RotateDirection.Clockwise, new List<Photo> { image_view.Item.Current as Photo }))
				collection.MarkChanged (image_view.Item.Index, FullInvalidate.Instance);
		}

		void HandleRotate270Command (object sender, EventArgs args)
		{
			RotateCommand command = new RotateCommand (Window);
			if (command.Execute (RotateDirection.Counterclockwise, new List<Photo> { image_view.Item.Current as Photo}))
				collection.MarkChanged (image_view.Item.Index, FullInvalidate.Instance);
		}

		void HandleSelectionChanged (IBrowsableCollection selection)
		{

			if (selection.Count > 0) {
				image_view.Item.Index = ((FSpot.Widgets.SelectionCollection)selection).Ids[0];

				zoom_scale.Value = image_view.NormalizedZoom;
			}
			UpdateStatusLabel ();
		}

		void HandleItemChanged (object sender, BrowsablePointerChangedEventArgs old)
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
			IPhoto current = image_view.Item.Current;

			if (current == null)
				return;

			throw new NotImplementedException ("HandlSetAsBackgroundCommand");
		}

        void HandleViewToolbar(object sender, EventArgs args)
		{
			ShowToolbar = toolbar_item.Active;
		}

		void HandleHideSidePane (object sender, EventArgs args)
		{
			ShowSidebar = false;
		}

        void HandleViewSidePane(object sender, EventArgs args)
		{
			ShowSidebar = side_pane_item.Active;
		}

		void HandleViewSlideshow (object sender, EventArgs args)
		{
			HandleViewFullscreen (sender, args);
			fsview.PlayPause ();
		}

        void HandleViewFilenames(object sender, EventArgs args)
		{
			directory_view.DisplayFilenames = filenames_item.Active;
			UpdateStatusLabel ();
		}

        void HandleAbout(object sender, EventArgs args)
		{
			FSpot.UI.Dialog.AboutDialog.ShowUp ();
		}

        void HandleNewWindow(object sender, EventArgs args)
		{
			/* FIXME this needs to register witth the core */
			new SingleView (new SafeUri[] {uri});
		}

        void HandlePreferences(object sender, EventArgs args)
		{
			SingleView.PreferenceDialog.Show ();
		}

        void HandleOpenFolder(object sender, EventArgs args)
		{
			Open (FileChooserAction.SelectFolder);
		}

        void HandleOpen(object sender, EventArgs args)
		{
			Open (FileChooserAction.Open);
		}

		void Open (FileChooserAction action)
		{
			string title = Catalog.GetString ("Open");

			if (action == FileChooserAction.SelectFolder)
				title = Catalog.GetString ("Select Folder");

			FileChooserDialog chooser = new FileChooserDialog (title,
									   Window,
									   action);

			chooser.AddButton (Stock.Cancel, ResponseType.Cancel);
			chooser.AddButton (Stock.Open, ResponseType.Ok);

			chooser.SetUri (uri.ToString ());
			int response = chooser.Run ();

			if ((ResponseType) response == ResponseType.Ok)
				CurrentUri = new SafeUri (chooser.Uri, true);


			chooser.Destroy ();
		}

		void HandleViewFullscreen (object sender, System.EventArgs args)
		{
			if (fsview != null)
				fsview.Destroy ();

			fsview = new FSpot.FullScreenView (collection, Window);
			fsview.Destroyed += HandleFullScreenViewDestroy;

			fsview.View.Item.Index = image_view.Item.Index;
			fsview.Show ();
		}

		void HandleFullScreenViewDestroy (object sender, EventArgs args)
		{
			directory_view.Selection.Clear ();
			if (fsview.View.Item.IsValid)
				directory_view.Selection.Add (fsview.View.Item.Index);
			fsview = null;
		}

		public void HandleZoomOut (object sender, EventArgs args)
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

		public void HandleZoomIn (object sender, EventArgs args)
		{
			image_view.ZoomIn ();
		}

		void HandleZoomScaleValueChanged (object sender, EventArgs args)
		{
			image_view.NormalizedZoom = zoom_scale.Value;
		}

		void HandleZoomChanged (object sender, EventArgs args)
		{
			zoom_scale.Value = image_view.NormalizedZoom;

			// FIXME something is broken here
			//zoom_in.Sensitive = (zoom_scale.Value != 1.0);
			//zoom_out.Sensitive = (zoom_scale.Value != 0.0);
		}

		void HandleImageViewButtonPressEvent (object sender, ButtonPressEventArgs args)
		{
			if (args.Event.Type != EventType.ButtonPress || args.Event.Button != 3)
				return;

			var popup_menu = new Gtk.Menu ();
			bool has_item = image_view.Item.Current != null;

			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Rotate _Left"), "object-rotate-left", delegate { HandleRotate270Command(Window, null); }, has_item);
			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Rotate _Right"), "object-rotate-right", delegate { HandleRotate90Command (Window, null); }, has_item);
			GtkUtil.MakeMenuSeparator (popup_menu);
			GtkUtil.MakeMenuItem (popup_menu, Catalog.GetString ("Set as Background"), HandleSetAsBackgroundCommand, has_item);

			popup_menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
		}

		void HandleDeleteEvent (object sender, DeleteEventArgs args)
		{
			SavePreferences ();
			Window.Destroy ();
			args.RetVal = true;
		}

		void HandleDragDataReceived (object sender, DragDataReceivedArgs args)
		{
			if (args.Info == (uint)FSpot.DragDropTargets.TargetType.UriList
			    || args.Info == (uint)FSpot.DragDropTargets.TargetType.PlainText) {

				/*
				 * If the drop is coming from inside f-spot then we don't want to import
				 */
				if (Gtk.Drag.GetSourceWidget (args.Context) != null)
					return;

				UriList list = args.SelectionData.GetUriListData ();
				collection.LoadItems (list.ToArray());

				Gtk.Drag.Finish (args.Context, true, false, args.Time);
			    }
		}

		void UpdateStatusLabel ()
		{
			IPhoto item = image_view.Item.Current;
			var sb = new System.Text.StringBuilder();
			if (filenames_item.Active && item != null)
				sb.Append (System.IO.Path.GetFileName (item.DefaultVersion.Uri.LocalPath) + "  -  ");

			sb.AppendFormat (Catalog.GetPluralString ("{0} Photo", "{0} Photos", collection.Count), collection.Count);
			status_label.Text = sb.ToString ();
		}

		void HandleFileClose (object sender, EventArgs args)
		{
			SavePreferences ();
			Window.Destroy ();
		}

		void SavePreferences  ()
		{
			int width, height;
			Window.GetSize (out width, out height);

			bool maximized = ((Window.GdkWindow.State & Gdk.WindowState.Maximized) > 0);
			Preferences.Set (Preferences.ViewerMaximized, maximized);

			if (!maximized) {
				Preferences.Set (Preferences.ViewerWidth,	width);
				Preferences.Set (Preferences.ViewerHeight,	height);
			}

			Preferences.Set (Preferences.ViewerShowToolbar,	toolbar_hbox.Visible);
			Preferences.Set (Preferences.ViewerShawFilenames, filenames_item.Active);
		}

        void HandleFileOpen(object sender, EventArgs args)
		{
			var file_selector = new FileChooserDialog ("Open", Window, FileChooserAction.Open);

			file_selector.SetUri (uri.ToString ());
			int response = file_selector.Run ();

			if ((Gtk.ResponseType) response == Gtk.ResponseType.Ok) {
				var l = new List<SafeUri> ();
				foreach (var s in file_selector.Uris)
					l.Add (new SafeUri (s));
				new FSpot.SingleView (l.ToArray ());
			}

			file_selector.Destroy ();
		}

		void OnPreferencesChanged (object sender, NotifyEventArgs args)
		{
			LoadPreference (args.Key);
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case Preferences.ViewerMaximized:
				if (Preferences.Get<bool> (key))
					Window.Maximize ();
				else
					Window.Unmaximize ();
				break;

			case Preferences.ViewerWidth:
			case Preferences.ViewerHeight:
				int width, height;
				width = Preferences.Get<int> (Preferences.ViewerWidth);
				height = Preferences.Get<int> (Preferences.ViewerHeight);

				if( width == 0 || height == 0 )
					break;

				Window.SetDefaultSize(width, height);

				Window.ReshowWithInitialSize();
				break;

			case Preferences.ViewerShowToolbar:
				if (toolbar_item.Active != Preferences.Get<bool> (key))
					toolbar_item.Active = Preferences.Get<bool> (key);

				toolbar_hbox.Visible = Preferences.Get<bool> (key);
				break;

			case Preferences.ViewerInterpolation:
				if (Preferences.Get<bool> (key))
					image_view.Interpolation = Gdk.InterpType.Bilinear;
				else
					image_view.Interpolation = Gdk.InterpType.Nearest;
				break;

			case Preferences.ViewerShawFilenames:
				if (filenames_item.Active != Preferences.Get<bool> (key))
					filenames_item.Active = Preferences.Get<bool> (key);
				break;

			case Preferences.ViewerTransparency:
				if (Preferences.Get<string> (key) == "CHECK_PATTERN")
					image_view.CheckPattern = CheckPattern.Dark;
				else if (Preferences.Get<string> (key) == "COLOR")
					image_view.CheckPattern = new CheckPattern (Preferences.Get<string> (Preferences.ViewerTransColor));
				else // NONE
					image_view.CheckPattern = new CheckPattern (image_view.Style.BaseColors [(int)Gtk.StateType.Normal]);
				break;

			case Preferences.ViewerTransColor:
				if (Preferences.Get<string> (Preferences.ViewerTransparency) == "COLOR")
					image_view.CheckPattern = new CheckPattern (Preferences.Get<string> (key));
				break;
			}
		}

		public class PreferenceDialog : BuilderDialog
		{
#pragma warning disable 649
			[GtkBeans.Builder.Object] CheckButton interpolation_check;
			[GtkBeans.Builder.Object] ColorButton color_button;
			[GtkBeans.Builder.Object] RadioButton as_background_radio;
			[GtkBeans.Builder.Object] RadioButton as_check_radio;
			[GtkBeans.Builder.Object] RadioButton as_color_radio;
#pragma warning restore 649

			public PreferenceDialog () : base ("viewer_preferences.ui", "viewer_preferences")
			{
				LoadPreference (Preferences.ViewerInterpolation);
				LoadPreference (Preferences.ViewerTransparency);
				LoadPreference (Preferences.ViewerTransColor);
				Preferences.SettingChanged += OnPreferencesChanged;
				Destroyed += HandleDestroyed;
			}

            void InterpolationToggled(object sender, EventArgs args)
			{
				Preferences.Set (Preferences.ViewerInterpolation, interpolation_check.Active);
			}

            void HandleTransparentColorSet(object sender, EventArgs args)
			{
				Preferences.Set (Preferences.ViewerTransColor,
						"#" +
						(color_button.Color.Red / 256 ).ToString("x").PadLeft (2, '0') +
						(color_button.Color.Green / 256 ).ToString("x").PadLeft (2, '0') +
						(color_button.Color.Blue / 256 ).ToString("x").PadLeft (2, '0'));
			}

            void HandleTransparencyToggled(object sender, EventArgs args)
			{
				if (as_background_radio.Active)
					Preferences.Set (Preferences.ViewerTransparency, "NONE");
				else if (as_check_radio.Active)
					Preferences.Set (Preferences.ViewerTransparency, "CHECK_PATTERN");
				else if (as_color_radio.Active)
					Preferences.Set (Preferences.ViewerTransparency, "COLOR");
			}

			static PreferenceDialog prefs;
			public static new void Show ()
			{
				if (prefs == null)
					prefs = new PreferenceDialog ();

				prefs.Present ();
			}

			void OnPreferencesChanged (object sender, NotifyEventArgs args)
			{
				LoadPreference (args.Key);
			}

            void HandleClose(object sender, EventArgs args)
			{
				Destroy ();
			}

			void HandleDestroyed (object sender, EventArgs args)
			{
				prefs = null;
			}

			void LoadPreference (string key)
			{

				switch (key) {
				case Preferences.ViewerInterpolation:
					interpolation_check.Active = Preferences.Get<bool> (key);
					break;
				case Preferences.ViewerTransparency:
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
				case Preferences.ViewerTransColor:
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
