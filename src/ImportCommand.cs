using GLib;
using Gdk;
using Gnome;
using Gtk;
using GtkSharp;
using System.Collections;
using System.IO;
using System;

public class ImportCommand : FSpot.GladeDialog {
	private class VolumeItem : Gtk.ImageMenuItem {
		public Gnome.Vfs.Volume Volume;
		
		public VolumeItem (Gnome.Vfs.Volume vol) : base (vol.DisplayName)
		{
			this.Volume = vol;
			this.Image = new Gtk.Image (vol.Icon);

			Gdk.Pixbuf icon = PixbufUtils.LoadThemeIcon (vol.Icon, 32);
			if (icon != null)
				this.Image = new Gtk.Image (icon);

			
		}

	}

	private class DriveItem : Gtk.ImageMenuItem {
		public Gnome.Vfs.Drive Drive;
		
		public DriveItem (Gnome.Vfs.Drive drive) : base (drive.DisplayName)
		{
			this.Drive = drive;

			Gdk.Pixbuf icon;
			if (drive.IsMounted) {
				icon = PixbufUtils.LoadThemeIcon (drive.MountedVolume.Icon, 32);
				this.Sensitive = drive.MountedVolume.IsMounted;
			} else {
				icon = PixbufUtils.LoadThemeIcon (drive.Icon, 32);
			}

			if (icon != null)
				this.Image = new Gtk.Image (icon);
		}
	}
	
	private class SourceMenu : Gtk.Menu {
		public SourceMenu () {
			Gnome.Vfs.VolumeMonitor monitor = Gnome.Vfs.VolumeMonitor.Get ();

			foreach (Gnome.Vfs.Drive drive in monitor.ConnectedDrives) {
				 this.Append (new DriveItem (drive));
			 }

			 foreach (Gnome.Vfs.Volume vol in monitor.MountedVolumes) {
				 System.Console.WriteLine ("{0} - {1} - {2} {3} {4}",
							   vol.DisplayName, 
							   vol.Icon, 
							   vol.VolumeType.ToString (), 
							   vol.ActivationUri, 
							   vol.IsMounted);

				 if (vol.Drive != null)
					 System.Console.WriteLine (vol.Drive.DeviceType.ToString ());
								   
				if (vol.IsUserVisible)
					this.Append (new VolumeItem (vol));
			}

			this.ShowAll ();
		}
	}

	private class PhotoGrid : Table {
		const int NUM_COLUMNS = 5;
		const int NUM_ROWS = 4;

		const int CELL_WIDTH = 128;
		const int CELL_HEIGHT = 96;

		const int PADDING = 3;

		Gtk.Image [] image_widgets;

		int position = -1;
		
		/*
		 * This controls whether or wraps as new photos are added.
		 */
		public bool Scroll = false;

		public PhotoGrid () : base (NUM_ROWS, NUM_COLUMNS, true)
		{
			image_widgets = new Gtk.Image [NUM_ROWS * NUM_COLUMNS];

			int i = 0;
			for (uint j = 0; j < NUM_ROWS; j++) {
				for (uint k = 0; k < NUM_COLUMNS; k ++) {
					Gtk.Image image_widget = new Gtk.Image ();

					image_widget = new Gtk.Image ();
					image_widget.SetSizeRequest (CELL_WIDTH, CELL_HEIGHT);

					Attach (image_widget, k, k + 1, j, j + 1, 0, 0, PADDING, PADDING);

					image_widgets [i] = image_widget;

					i ++;
				}
			}
		}

		private void ScrollIfNeeded ()
		{
			if (position < NUM_COLUMNS * NUM_ROWS)
				return;

			for (int i = 0; i < NUM_COLUMNS * (NUM_ROWS - 1); i ++) {
				image_widgets [i].Pixbuf = image_widgets [i + NUM_COLUMNS].Pixbuf;
			}

			for (int i = NUM_COLUMNS * (NUM_ROWS - 1); i < NUM_COLUMNS * NUM_ROWS; i ++) {
				// FIXME: Lame, apparently I can't set the Pixbuf property to null.
				// GTK# bug?
				image_widgets [i].Pixbuf = new Pixbuf (Colorspace.Rgb, false, 8, 1, 1);
			}

			position -= NUM_COLUMNS;
		}

		public void AddThumbnail (Pixbuf thumbnail)
		{
			Pixbuf scaled_thumbnail;

			if (thumbnail.Width <= CELL_WIDTH && thumbnail.Height <= CELL_HEIGHT) {
				scaled_thumbnail = thumbnail;
			} else {
				int thumbnail_width, thumbnail_height;

				PixbufUtils.Fit (thumbnail, CELL_WIDTH, CELL_HEIGHT, false,
						 out thumbnail_width, out thumbnail_height);
				scaled_thumbnail = thumbnail.ScaleSimple (thumbnail_width, thumbnail_height,
									  Gdk.InterpType.Bilinear);
			}

			position = (position + 1) % (NUM_COLUMNS * NUM_ROWS);
			if (Scroll)
				ScrollIfNeeded ();

			image_widgets [position].Pixbuf = scaled_thumbnail;
			if (Scroll)
				position ++;
		}
	}


	[Glade.Widget] Gtk.Entry import_folder_entry;
	[Glade.Widget] Gtk.OptionMenu tag_option_menu;
	[Glade.Widget] Gtk.CheckButton attach_check;
	[Glade.Widget] Gtk.CheckButton recurse_check;
	[Glade.Widget] Gtk.Image tag_image;
	[Glade.Widget] Gtk.Label tag_label;
	
	Tag tag_selected;

	Gtk.Dialog dialog;
	PhotoGrid grid;
	ProgressBar progress_bar;
	Gtk.Window main_window;
	string import_path;

	bool cancelled;

	public ImportCommand (Gtk.Window mw)
	{
		main_window = mw;
	}

	private void HandleDialogResponse (object obj, ResponseArgs args)
	{
		cancelled = true;
	}

	private void CreateDisplayDialog ()
	{
		dialog = new Gtk.Dialog ();
		dialog.AddButton (Gtk.Stock.Cancel, 0);

		grid = new PhotoGrid ();
		progress_bar = new ProgressBar ();

		dialog.VBox.PackStart (grid, true, true, 0);
		dialog.VBox.PackStart (progress_bar, false, true, 0);

		dialog.ShowAll ();

		dialog.Response += new ResponseHandler (HandleDialogResponse);
	}

	private void UpdateProgressBar (int count, int total)
	{
		progress_bar.Text = String.Format ("Importing {0} of {1}", count, total);
		progress_bar.Fraction = (double) count / System.Math.Max (total, 1);
	}

	private int DoImport (FileImportBackend importer)
	{
		int total = importer.Prepare ();
		
		CreateDisplayDialog ();
		UpdateProgressBar (0, total);

		cancelled = false;
		bool ongoing = true;
		while (ongoing && total > 0) {
			Photo photo;
			Pixbuf thumbnail;
			int count;

			while (Application.EventsPending ())
				Application.RunIteration ();

			if (cancelled)
				break;

			ongoing = importer.Step (out photo, out thumbnail, out count);
	
			if (thumbnail == null){
				Console.WriteLine ("Could not import file");
				continue;
			}

			grid.AddThumbnail (thumbnail);
			UpdateProgressBar (count, total);
		}

		if (cancelled)
			importer.Cancel ();
		else
			importer.Finish ();

		dialog.Destroy ();
		dialog = null;
		grid = null;
		progress_bar = null;

		if (cancelled)
			return 0;
		else
			return total;
	}
	
	public string ImportPath {
		get {
			return import_path;
		}
	}
	
	public void HandleTagToggled (object o, EventArgs args) 
	{
		tag_option_menu.Sensitive = attach_check.Active;
	}

	public void HandleImportBrowse (object o, EventArgs args) 
	{
	
		CompatFileChooserDialog file_selector =
			new CompatFileChooserDialog ("Import", this.Dialog,
						     CompatFileChooserDialog.Action.SelectFolder);

		file_selector.SelectMultiple = false;
		file_selector.Filename = import_folder_entry.Text;

		int response = file_selector.Run ();

		if ((ResponseType) response == ResponseType.Ok) {
			import_path = file_selector.Filename;
			import_folder_entry.Text = file_selector.Filename;
		}

		file_selector.Destroy ();
		
	}
	
	public void HandleTagMenuSelected (Tag t) 
	{
		tag_selected = t;
		//tag_image.Pixbuf = t.Icon;
		//tag_label.Text = t.Name;
	
	}
	
	public void HandleEntryActivate (object sender, EventArgs args)
	{
		this.Dialog.Respond (Gtk.ResponseType.Ok);
	}

	public int ImportFromFile (PhotoStore store, string path)
	{
		this.CreateDialog ("import_dialog");
		
		this.Dialog.TransientFor = main_window;
		this.Dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;

		//Gtk.Menu menu = new Gtk.Menu();
		MenuItem attach_item = new MenuItem (Mono.Posix.Catalog.GetString ("Select Tag"));
		TagMenu tagmenu = new TagMenu (null, MainWindow.Toplevel.Database.Tags);
		
		this.Dialog.DefaultResponse = ResponseType.Ok;
		
		import_folder_entry.Activated += HandleEntryActivate;

		tagmenu.TagSelected += HandleTagMenuSelected;
		tagmenu.ShowAll ();
		tagmenu.Populate (true);
		tagmenu.Prepend (attach_item);
		
		tag_option_menu.Menu = tagmenu;
		//tag_option_menu.Menu = new SourceMenu ();

		tag_selected = null;
		if (attach_check != null) {
			attach_check.Toggled += HandleTagToggled;
			HandleTagToggled (null, null);
		}				

		if (path != null)
			import_folder_entry.Text = path;
		else 
			import_folder_entry.Text = System.Environment.GetEnvironmentVariable ("HOME");
						
		ResponseType response = (ResponseType) this.Dialog.Run ();
		
		while (response == ResponseType.Ok) {
			if (System.IO.Directory.Exists (import_folder_entry.Text))
			    break;

			HigMessageDialog md = new HigMessageDialog (this.Dialog,
								    DialogFlags.DestroyWithParent,
								    MessageType.Error,
								    ButtonsType.Ok,
								    Mono.Posix.Catalog.GetString ("Directory does not exist."),
									    String.Format (Mono.Posix.Catalog.GetString ("The directory you selected \"{0}\" does not exist.  Please choose a different diarectory"), import_folder_entry.Text));
			md.Run ();
			md.Destroy ();

			response = (Gtk.ResponseType) this.Dialog.Run ();
		}

		if (response == ResponseType.Ok) {
			string [] pathimport =  {import_folder_entry.Text};
			this.Dialog.Destroy();

			Tag [] tags = null;		       
			if (attach_check.Active && tag_selected != null)
				tags = new Tag [] {tag_selected};
			
			bool recurse = true;
			if (recurse_check != null)
				recurse = recurse_check.Active;

			return DoImport (new FileImportBackend (store, pathimport, recurse, tags));
				
		} else {
			this.Dialog.Destroy();
			return 0;
		}
	}

	public int ImportFromPaths (PhotoStore store, string [] paths)
	{
		return ImportFromPaths (store, paths, null);
	}

	public int ImportFromPaths (PhotoStore store, string [] paths, Tag [] tags)
	{
		return DoImport (new FileImportBackend (store, paths, true, tags));
	}
	
#if TEST_IMPORT_COMMAND

	private const string db_path = "/tmp/ImportCommandTest.db";
	private static string directory_path;

	private static bool OnIdleStartImport ()
	{
		Db db = new Db (db_path, true);

		ImportCommand command = new ImportCommand ();

		command.ImportFromPath (db.Photos, directory_path, true);

		Application.Quit ();
		return false;
	}

	public static void Main (string [] args)
	{
		Program program = new Program ("ImportCommandTest", "0.0", Modules.UI, args);

		try {
			File.Delete (db_path);
		} catch {}

		directory_path = args [0];

		Idle.Add (new IdleHandler (OnIdleStartImport));
		program.Run ();
	}

#endif
}
