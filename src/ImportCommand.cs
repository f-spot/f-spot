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
	}
}
