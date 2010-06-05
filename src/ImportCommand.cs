public class ImportCommand : GladeDialog
{
	
	[Glade.Widget] Gtk.HBox tagentry_box;
	[Glade.Widget] Gtk.OptionMenu source_option_menu;
	[Glade.Widget] Gtk.ScrolledWindow icon_scrolled;
	[Glade.Widget] Gtk.ScrolledWindow photo_scrolled;
	[Glade.Widget] Gtk.CheckButton duplicate_check;
	[Glade.Widget] Gtk.CheckButton recurse_check;
	[Glade.Widget] Gtk.CheckButton copy_check;
	[Glade.Widget] Gtk.Button ok_button;
	[Glade.Widget] Gtk.Image tag_image;
	[Glade.Widget] Gtk.Label tag_label;
	[Glade.Widget] Gtk.EventBox frame_eventbox;
	[Glade.Widget] ProgressBar progress_bar;
	[Glade.Widget] Gtk.HPaned import_hpaned;
	
	ArrayList tags_selected;

	FSpot.Widgets.TagEntry tag_entry;

	Gtk.Window main_window;
	FSpot.PhotoList collection;
	bool copy;
	SourceMenu menu;

	int total;
	PhotoStore store;
	FSpot.Delay step;
	
	PhotoImageView photo_view;
	ImportBackend importer;
	FSpot.Widgets.IconView tray;

	FSpot.Delay idle_start; 

	string loading_string;

	public SafeUri ImportUri { get; private set; }
	
	private SourceItem Source;

	private bool Step ()
	{	
		StepStatusInfo status_info;		
		bool ongoing = true;

		if (importer == null)
			return false;
		
		try {
			// FIXME this is really just an incredibly ugly way of dealing
			// with the recursive DoImport loops we sometimes get into
			ongoing = importer.Step (out status_info);
		} catch (ImportException e){
			Hyena.Log.Exception (e);
			return false;
		}

		if (status_info.Count > 0 && status_info.Count % 25 == 0)
			System.GC.Collect ();

		if (status_info.Count < total)
			UpdateProgressBar (status_info.Count + 1, total);

		if (ongoing && total > 0)
			return true;
		else {
			Hyena.Log.Debug ("Stopping");
			if (progress_bar != null)
				progress_bar.Text = Catalog.GetString ("Done Loading");
			
			AllowFinish = true;
			return false;
		}
	}

	private int DoImport (ImportBackend imp)
	{
		if (collection == null)
			return 0;

		this.importer = imp;
		AllowFinish = false;
		
		var info = importer.Prepare ();
		total = info.Count;

		if (total > 0)
			UpdateProgressBar (1, total);
		
		collection.Clear ();
		collection.AddAll (info);

		while (total > 0 && this.Step ()) {
			System.DateTime start_time = System.DateTime.Now;
			System.TimeSpan span = start_time - start_time;

			while (Application.EventsPending () && span.TotalMilliseconds < 100) {
				span = System.DateTime.Now - start_time;
				Application.RunIteration ();
			}
		}

		return total;
	}

	public int ImportFromUri (PhotoStore store, SafeUri uri)
	{
		this.store = store;
		this.CreateDialog ("import_dialog");
		this.Dialog.TransientFor = main_window;
		this.Dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;
		this.Dialog.Response += HandleDialogResponse;
		this.Dialog.DefaultResponse = ResponseType.Ok;

		AllowFinish = false;
		
		LoadPreferences ();
		
		//import_folder_entry.Activated += HandleEntryActivate;
		duplicate_check.Toggled += HandleRecurseToggled;
		recurse_check.Toggled += HandleRecurseToggled;
		copy_check.Toggled += HandleRecurseToggled;

		menu = new SourceMenu (this);
		source_option_menu.Menu = menu;

		collection = new FSpot.PhotoList (new Photo [0]);
		tray = new ScalingIconView (collection);
		tray.Selection.Changed += HandleTraySelectionChanged;
		icon_scrolled.SetSizeRequest (400, 200);
		icon_scrolled.Add (tray);
		//icon_scrolled.Visible = false;
		tray.DisplayTags = false;
		tray.Show ();

		photo_view = new PhotoImageView (collection);
		photo_scrolled.Add (photo_view);
		photo_scrolled.SetSizeRequest (200, 200);
		photo_view.Show ();

		//GtkUtil.ModifyColors (frame_eventbox);
		GtkUtil.ModifyColors (photo_scrolled);
		GtkUtil.ModifyColors (photo_view);

		photo_view.Pixbuf = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "f-spot", 128, (Gtk.IconLookupFlags)0);
		photo_view.ZoomFit (false);
			
		tag_entry = new FSpot.Widgets.TagEntry (App.Instance.Database.Tags, false);
		tag_entry.UpdateFromTagNames (new string []{});
		tagentry_box.Add (tag_entry);

		tag_entry.Show ();

		this.Dialog.Show ();
        progress_bar.Hide ();

		source_option_menu.Changed += menu.HandleSourceSelectionChanged;
		if (uri != null) {
			ImportUri = uri;
			int i = menu.FindItemPosition (uri);

			var file = FileFactory.NewForUri (uri);

			if (i > 0) {
				source_option_menu.SetHistory ((uint)i);
			} else if (file.QueryExists (null)) {
				SourceItem uri_item = new SourceItem (new VfsSource (uri));
				menu.Prepend (uri_item);
				uri_item.ShowAll ();
				ImportUri = uri;
				source_option_menu.SetHistory (0);
			} 
			idle_start.Start ();
		}
						
		ResponseType response = (ResponseType) this.Dialog.Run ();
		
		while (response == ResponseType.Ok) {
			try {
				var file = FileFactory.NewForUri (uri);
				if (file.QueryExists (null))
					break;
			} catch (System.Exception e){
				Hyena.Log.Exception (e);
				break;
			}

			HigMessageDialog md = new HigMessageDialog (this.Dialog,
			        DialogFlags.DestroyWithParent,
				MessageType.Error,
				ButtonsType.Ok,
				Catalog.GetString ("Directory does not exist."),
				String.Format (Catalog.GetString ("The directory you selected \"{0}\" does not exist.  " + 
								  "Please choose a different directory"), ImportUri));

			md.Run ();
			md.Destroy ();

			response = (Gtk.ResponseType) this.Dialog.Run ();
		}

		if (response == ResponseType.Ok) {
			this.UpdateTagStore (tag_entry.GetTypedTagNames ());
			SavePreferences ();
			this.Finish ();

			if (tags_selected != null && tags_selected.Count > 0) {
				for (int i = 0; i < collection.Count; i++) {
					Photo p = collection [i] as Photo;
					
					if (p == null)
						continue;
					
					p.AddTag ((Tag [])tags_selected.ToArray(typeof(Tag)));
					store.Commit (p);
				}
			}

			int width, height;
			this.Dialog.GetSize (out width, out height);

			FSpot.Preferences.Set (FSpot.Preferences.IMPORT_WINDOW_WIDTH, width);
			FSpot.Preferences.Set (FSpot.Preferences.IMPORT_WINDOW_HEIGHT, height);
			FSpot.Preferences.Set (FSpot.Preferences.IMPORT_WINDOW_PANE_POSITION, import_hpaned.Position);

			this.Dialog.Destroy ();
			return collection.Count;
		} else {
			this.Cancel ();
			//this.Dialog.Destroy();
			return 0;
		}
	}

	private void UpdateTagStore (string [] new_tags)
	{
		if (new_tags == null || new_tags.Length == 0)
			return;

		tags_selected = new ArrayList ();
		Db db = App.Instance.Database;	
		db.BeginTransaction ();
		foreach (string tagname in new_tags) {
			Tag t = db.Tags.GetTagByName (tagname);
			if (t == null) {
				Category default_category = db.Tags.GetTagByName (Catalog.GetString ("Imported Tags")) as Category;
				if (default_category == null) {
					default_category = db.Tags.CreateCategory (null, Catalog.GetString ("Imported Tags"), false);
					default_category.ThemeIconName = "gtk-new"; 
				}
				t = db.Tags.CreateCategory (default_category, tagname, false) as Tag;
				db.Tags.Commit (t);
			}

			tags_selected.Add (t);
		}
		db.CommitTransaction ();
		
		ArrayList tagnames = new ArrayList ();
		foreach (Tag t in tags_selected)
			tagnames.Add (t.Name);
		tag_entry.UpdateFromTagNames ((string [])tagnames.ToArray(typeof(string)));
	}
}
