using Gdk;
using Gtk;
using GtkSharp;
using Glade;
using System;
using System.Collections;

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
	PhotoVersionMenu versions_submenu;
	
	InfoBox info_box;
	IconView icon_view;
	PhotoView photo_view;
	PhotoQuery query;
	
	// Index into the PhotoQuery.  If -1, no photo is selected or multiple photos are selected.
	const int PHOTO_IDX_NONE = -1;
	private int current_photo_idx = PHOTO_IDX_NONE;

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

		tag_selection_widget.SelectionChanged += new TagSelectionWidget.SelectionChangedHandler (OnTagSelectionChanged);

		window1.ShowAll ();
	}

	//
	// Commands
	//
	private void RotateSelectedPictures (RotateCommand.Direction direction)
	{
		RotateCommand command = new RotateCommand (window1);

		switch (mode) {
		case ModeType.IconView:
			if (query.Photos.Length != 0) {
				Photo [] photo_list = new Photo [icon_view.Selection.Length];

				int i = 0;
				foreach (int num in icon_view.Selection)
					photo_list [i ++] = query.Photos [num];

				if (command.Execute (direction, photo_list)) {
					foreach (int num in icon_view.Selection)
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

		if (selection.Length != 1) {
			current_photo_idx = -1;
			info_box.Photo = null;
		} else {
			current_photo_idx = selection [0];
			info_box.Photo = CurrentPhoto;
		}

		UpdateMenus ();
	}

	void HandleDoubleClicked (IconView icon_view, int clicked_item)
	{
		SwitchToPhotoViewMode (clicked_item);
	}


	// PhotoView events.

	void HandlePhotoViewPhotoChanged (PhotoView sender)
	{
		current_photo_idx = photo_view.CurrentPhoto;
		info_box.Photo = CurrentPhoto;
		UpdateMenus ();
	}

	void HandlePhotoViewButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		if (args.Event.type == EventType.TwoButtonPress && args.Event.button == 1)
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

	void HandleCloseCommand (object sender, EventArgs e)
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
				
				foreach (Tag tag in tags) {
					photo.AddTag (tag);	
				}	
				db.Photos.Commit (photo);
				icon_view.InvalidateCell (num);
			}
		}
		break;	
		case ModeType.PhotoView:
			Photo photo = query.Photos [photo_view.CurrentPhoto];
			
			foreach (Tag tag in tags) {
				photo.AddTag (tag);	
			}	
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

				foreach (Tag tag in tags)
					photo.RemoveTag (tag);
				db.Photos.Commit (photo);
				icon_view.InvalidateCell (num);
			}
		}
		break;	
		case ModeType.PhotoView:
			Photo photo = query.Photos [photo_view.CurrentPhoto];
			foreach (Tag tag in tags)
				photo.RemoveTag (tag);
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

	// Toolbar commands.

	void HandleRotate90ToolbarButtonClicked (object sender, EventArgs args)
	{
		RotateSelectedPictures (RotateCommand.Direction.Clockwise);
	}

	void HandleRotate270ToolbarButtonClicked (object sender, EventArgs args)
	{
	        Console.Write ("hello");
		RotateSelectedPictures (RotateCommand.Direction.Counterclockwise);
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

	void UpdateMenus ()
	{
		if (CurrentPhoto == null) {
			version_menu_item.Sensitive = false;
			version_menu_item.Submenu = new Menu ();

			create_version_menu_item.Sensitive = false;
			delete_version_menu_item.Sensitive = false;
			rename_version_menu_item.Sensitive = false;
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
		}
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

#if false
public class MainWindow2 : Gtk.Window {
	Db db;

	TagSelectionWidget tag_selection_widget;
	InfoBox info_box;
	IconView icon_view;
	PhotoView photo_view;
	Notebook view_notebook;
	PhotoQuery query;

	// Commands.

	void RotateSelectedPictures (RotateCommand.Direction direction)
	{
		RotateCommand command = new RotateCommand ();

		switch (mode) {
		case ModeType.IconView:
			if (query.Photos.Length != 0) {
				Photo [] photo_list = new Photo [icon_view.Selection.Length];

				int i = 0;
				foreach (int num in icon_view.Selection)
					photo_list [i ++] = query.Photos [num];

				if (command.Execute (direction, photo_list)) {
					foreach (int num in icon_view.Selection)
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


	// Menu commands.

	void HandleImportCommand (object obj, EventArgs args)
	{
		ImportCommand command = new ImportCommand ();
		command.ImportFromFile (db.Photos);
		UpdateQuery ();
	}

	void HandleCloseCommand (object obj, EventArgs args)
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
		TagCommands.Create command = new TagCommands.Create (db.Tags, this);
		if (command.Execute (TagCommands.TagType.Category))
			tag_selection_widget.Update ();
	}

	// Toolbar commands.

	void HandleRotate90ToolbarButtonClicked ()
	{
		RotateSelectedPictures (RotateCommand.Direction.Clockwise);
	}

	void HandleRotate270ToolbarButtonClicked ()
	{
		RotateSelectedPictures (RotateCommand.Direction.Counterclockwise);
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


	// Menus.

	MenuItem version_menu_item;
	PhotoVersionMenu versions_submenu;
	MenuItem create_version_menu_item;
	MenuItem delete_version_menu_item;
	MenuItem rename_version_menu_item;

	MenuBar CreateMenuBar ()
	{
		MenuBar menu_bar = new MenuBar ();

		Menu photo_menu = new Menu ();
		MenuItem photo_item = new MenuItem ("_Photo");
		photo_item.Submenu = photo_menu;
		menu_bar.Append (photo_item);

		MenuItem import_item = new MenuItem ("_Import...");
		import_item.Activated += new EventHandler (HandleImportCommand);
		photo_menu.Append (import_item);

		photo_menu.Append (new MenuItem ());

		version_menu_item = new MenuItem ("Version");
		photo_menu.Append (version_menu_item);

		create_version_menu_item = PhotoVersionMenu.NewCreateVersionMenuItem ();
		create_version_menu_item.Activated += new EventHandler (HandleCreateVersionCommand);
		photo_menu.Append (create_version_menu_item);

		delete_version_menu_item = PhotoVersionMenu.NewDeleteVersionMenuItem ();
		photo_menu.Append (delete_version_menu_item);
		delete_version_menu_item.Activated += new EventHandler (HandleDeleteVersionCommand);

		rename_version_menu_item = PhotoVersionMenu.NewRenameVersionMenuItem ();
		rename_version_menu_item.Activated += new EventHandler (HandleRenameVersionCommand);
		photo_menu.Append (rename_version_menu_item);

		photo_menu.Append (new MenuItem ());

		MenuItem close_item = new MenuItem ("_Close");
		close_item.Activated += new EventHandler (HandleCloseCommand);
		photo_menu.Append (close_item);

		Menu tags_menu = new Menu ();
		MenuItem tags_item = new MenuItem ("_Tags");
		tags_item.Submenu = tags_menu;
		menu_bar.Append (tags_item);

		MenuItem create_tag_item = new MenuItem ("Create New _Tag...");
		create_tag_item.Activated += new EventHandler (HandleCreateNewTagCommand);
		tags_menu.Append (create_tag_item);

		MenuItem create_category_item = new MenuItem ("Create New _Category...");
		create_category_item.Activated += new EventHandler (HandleCreateNewCategoryCommand);
		tags_menu.Append (create_category_item);

		return menu_bar;
	}

	void UpdateMenus ()
	{
		if (CurrentPhoto == null) {
			version_menu_item.Sensitive = false;
			version_menu_item.Submenu = new Menu ();

			create_version_menu_item.Sensitive = false;
			delete_version_menu_item.Sensitive = false;
			rename_version_menu_item.Sensitive = false;
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
		}
	}


	// Toolbar.

	// FIXME this should all respect the GNOME toolbar settings and stuff...

	Toolbar CreateToolbar ()
	{
		Toolbar toolbar = new Toolbar ();

		toolbar.AppendItem ("Rotate 90°", "Rotate by 90 degrees clockwise.", "",
				    new Gtk.Image ("f-spot-rotate-90", IconSize.LargeToolbar),
				    new SignalFunc (HandleRotate90ToolbarButtonClicked));
		toolbar.AppendItem ("Rotate 270°", "Rotate by 90 degrees counterclockwise.", "",
				    new Gtk.Image ("f-spot-rotate-270", IconSize.LargeToolbar),
				    new SignalFunc (HandleRotate270ToolbarButtonClicked));

		toolbar.Show ();

		return toolbar;
	}


	// IconView events.

	void HandleSelectionChanged (IconView view)
	{
		int [] selection = icon_view.Selection;

		if (selection.Length != 1) {
			current_photo_idx = -1;
			info_box.Photo = null;
		} else {
			current_photo_idx = selection [0];
			info_box.Photo = CurrentPhoto;
		}

		UpdateMenus ();
	}

	void HandleDoubleClicked (IconView icon_view, int clicked_item)
	{
		SwitchToPhotoViewMode (clicked_item);
	}


	// PhotoView events.

	void HandlePhotoViewPhotoChanged (PhotoView sender)
	{
		current_photo_idx = photo_view.CurrentPhoto;
		info_box.Photo = CurrentPhoto;
		UpdateMenus ();
	}

	void HandlePhotoViewButtonPressEvent (object sender, ButtonPressEventArgs args)
	{
		if (args.Event.type == EventType.TwoButtonPress && args.Event.button == 1)
			SwitchToIconViewMode ();
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


	// Constructor.

	public MainWindow2 (Db db)
		: base (Gtk.WindowType.Toplevel)
	{
		this.db = db;

		Title = "F-Spot";
		SetDefaultSize (850, 600);

		VBox vbox = new VBox (false, 0);
		Add (vbox);

		vbox.PackStart (CreateMenuBar (), false, true, 0);
		vbox.PackStart (CreateToolbar (), false, true, 0);

		HPaned paned = new HPaned ();
		paned.Position = 200;
		vbox.PackStart (paned, true, true, 0);

		VBox left_vbox = new VBox (false, 3);
		paned.Pack1 (left_vbox, false, true);

		tag_selection_widget = new TagSelectionWidget (db.Tags);
		ScrolledWindow tag_selection_scrolled = new ScrolledWindow (null, null);
		tag_selection_scrolled.Add (tag_selection_widget);
		tag_selection_scrolled.SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
		tag_selection_scrolled.ShadowType = ShadowType.In;
		left_vbox.PackStart (tag_selection_scrolled, true, true, 0);

		info_box = new InfoBox ();
		info_box.VersionIdChanged += new InfoBox.VersionIdChangedHandler (HandleInfoBoxVersionIdChange);
		left_vbox.PackStart (info_box, false, true, 0);

		view_notebook = new Notebook ();
		view_notebook.ShowTabs = false;
		view_notebook.ShowBorder = false;
		paned.Pack2 (view_notebook, true, false);

		query = new PhotoQuery (db.Photos);

		icon_view = new IconView (query);
		ScrolledWindow icon_view_scrolled = new ScrolledWindow (null, null);
		icon_view_scrolled.Add (icon_view);
		icon_view_scrolled.ShadowType = ShadowType.In;
		icon_view_scrolled.SetPolicy (PolicyType.Never, PolicyType.Always);
		icon_view.SelectionChanged += new IconView.SelectionChangedHandler (HandleSelectionChanged);
		icon_view.DoubleClicked += new IconView.DoubleClickedHandler (HandleDoubleClicked);
		// FIXME GTK# should let me pass a null for the second argument here.
		view_notebook.AppendPage (icon_view_scrolled, new Label ("foo"));

		photo_view = new PhotoView (query, db.Photos);
		photo_view.PhotoChanged += new PhotoView.PhotoChangedHandler (HandlePhotoViewPhotoChanged);
		photo_view.ButtonPressEvent += new ButtonPressEventHandler (HandlePhotoViewButtonPressEvent);
		// FIXME GTK# should let me pass a null for the second argument here.
		view_notebook.AppendPage (photo_view, new Label ("foo"));

		vbox.ShowAll ();

		tag_selection_widget.SelectionChanged += new TagSelectionWidget.SelectionChangedHandler (OnTagSelectionChanged);

		UpdateQuery ();
		UpdateMenus ();
	}
}
#endif
