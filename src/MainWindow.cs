using Gtk;
using System;
using System.Collections;

public class MainWindow : Gtk.Window {
	Db db;

	private TagSelectionWidget tag_selection_widget;
	private InfoBox info_box;
	private IconView icon_view;
	private PhotoView photo_view;
	private Notebook view_notebook;
	private PhotoQuery query;


	// Commands.

	private void HandleImportCommand (object obj, EventArgs args)
	{
		ImportCommand command = new ImportCommand ();
		command.ImportFromFile (db.Photos);
		UpdateQuery ();
	}

	private void HandleCloseCommand (object obj, EventArgs args)
	{
		// FIXME
		// Should use Application.Quit(), but for that to work we need to terminate the threads
		// first too.
		Environment.Exit (0);
	}

	private MenuBar CreateMenuBar ()
	{
		MenuBar menu_bar = new MenuBar ();

		Menu file_menu = new Menu ();
		MenuItem file_item = new MenuItem ("_Photo");
		file_item.Submenu = file_menu;
		menu_bar.Append (file_item);

		MenuItem import_item = new MenuItem ("_Import...");
		import_item.Activated += new EventHandler (HandleImportCommand);
		file_menu.Append (import_item);

		MenuItem close_item = new MenuItem ("_Close...");
		close_item.Activated += new EventHandler (HandleCloseCommand);
		file_menu.Append (close_item);

		return menu_bar;
	}


	// Switching mode.

	private void SwitchToIconViewMode ()
	{
		view_notebook.CurrentPage = 0;
	}

	private void SwitchToPhotoViewMode (int photo_num)
	{
		view_notebook.CurrentPage = 1;
		photo_view.CurrentPhoto = photo_num;
	}


	// IconView events.

	private void HandleSelectionChanged (IconView view)
	{
		int [] selection = icon_view.Selection;

		if (selection.Length != 1)
			info_box.Photo = null;
		else
			info_box.Photo = query.Photos [selection [0]];
	}

	private void HandleDoubleClicked (IconView icon_view, int clicked_item)
	{
		SwitchToPhotoViewMode (clicked_item);
	}


	// Queries.

	private void UpdateQuery ()
	{
		query.Tags = tag_selection_widget.TagSelection;
	}

	private void OnTagSelectionChanged (object obj)
	{
		UpdateQuery ();
	}


	// Constructor.

	public MainWindow (Db db)
		: base (WindowType.Toplevel)
	{
		this.db = db;

		Title = "F-Spot";
		SetDefaultSize (850, 600);

		VBox vbox = new VBox (false, 0);
		Add (vbox);

		vbox.PackStart (CreateMenuBar (), false, true, 0);

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

		photo_view = new PhotoView (query);
		// FIXME GTK# should let me pass a null for the second argument here.
		view_notebook.AppendPage (photo_view, new Label ("foo"));

		vbox.ShowAll ();

		tag_selection_widget.SelectionChanged += new TagSelectionWidget.SelectionChangedHandler (OnTagSelectionChanged);
		UpdateQuery ();
	}
}
