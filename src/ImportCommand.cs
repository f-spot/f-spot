using GLib;
using Gdk;
using Gnome;
using Gtk;
using GtkSharp;
using System.Collections;
using System.IO;
using System;

public class ImportCommand : FSpot.GladeDialog {
	internal class SourceItem : ImageMenuItem {
		public ImportSource Source;

		public SourceItem (ImportSource source) : base (source.Name)
		{
			this.Source = source;
			
			Gdk.Pixbuf icon = source.Icon;
			if (icon != null)
				this.Image = new Gtk.Image (icon);
		}
	} 

	internal class BrowseSource : ImportSource {
		public BrowseSource ()
		{
			this.Name = Mono.Posix.Catalog.GetString ("Select Folder");
			this.Icon = PixbufUtils.LoadThemeIcon ("stock_folder", 32);
		}

		public BrowseSource (string name, string icon)
		{
			this.Name = name;
			this.Icon = PixbufUtils.LoadThemeIcon (icon, 32);
		}
	}

	internal class VfsSource : ImportSource {
		public string uri;
		public bool SuggestCopy = false;

		public VfsSource (string uri)
		{ 
			string [] components = uri.Split (new char [] { '/' });
			this.Name = components [components.Length - 1];
			this.uri = uri;
			
			this.Icon = PixbufUtils.LoadThemeIcon ("stock_folder", 32);
		}

		public virtual bool Contains (string path)
		{
			return false;
		}

		protected VfsSource () {}
	}

	internal class VolumeSource : VfsSource {
		public Gnome.Vfs.Volume Volume;
		public string mount_point;

		public VolumeSource (Gnome.Vfs.Volume vol)
		{
			this.Volume = vol;
			this.Name = vol.DisplayName.Replace ("_", "__");
			mount_point = new Uri (vol.ActivationUri).LocalPath;
			uri = mount_point;
			SuggestCopy = true;

                        if (this.Icon == null)
				this.Icon = PixbufUtils.LoadThemeIcon (vol.Icon, 32);
			
			if (this.IsiPodPhoto)
				this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-ipod", 32);

                        if (this.Icon == null)			if (this.Icon == null && this.IsCamera)
				this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-media-cf", 32);

			try {
				if (this.Icon == null)
					this.Icon = new Gdk.Pixbuf (vol.Icon);
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}

		private bool IsCamera {
			get {
				try {
					return (Directory.Exists (System.IO.Path.Combine (mount_point, "DCIM")));
				} catch {
					return false;
				}
			}
		}

		private bool IsiPodPhoto {
			get {
				try {
					return (Directory.Exists (System.IO.Path.Combine (mount_point, "Photos")) &&
						Directory.Exists (System.IO.Path.Combine (mount_point, "iPod_Control")));
				} catch {
					return false;
				}
			}
		}
	}

	//internal classs FolderSource : ImportSource {
	//	private string path;
	//}

	internal class DriveSource : ImportSource {
		public Gnome.Vfs.Drive Drive;
		
		public DriveSource (Gnome.Vfs.Drive drive) 
		{
			this.Name = drive.DisplayName.Replace ("_", "__");
			this.Drive = drive;

			if (drive.IsMounted) {
				this.Icon = PixbufUtils.LoadThemeIcon (drive.MountedVolume.Icon, 32);
				//this.Sensitive = drive.MountedVolume.IsMounted;
			} else {
				this.Icon = PixbufUtils.LoadThemeIcon (drive.Icon, 32);
			}
		}
	}

	internal class CameraSource : ImportSource {
		GPhotoCamera cam;
		int CameraIndex;
		
		public CameraSource (GPhotoCamera cam, int index)
		{
			this.cam = cam;
			this.CameraIndex = index;

			//this.Name = String.Format ("{0} ({1})", cam.CameraList.GetName (index), cam.CameraList.GetValue (index));
			this.Name = String.Format ("{0}", cam.CameraList.GetName (index));
			this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-camera", 32);
			if (this.Icon == null)
				this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-media-cf", 32);
		}
	}

	internal abstract class ImportSource {
		public object Backend;
		public Gdk.Pixbuf Icon;
		public string Name;
	}
	
	private class SourceMenu : Gtk.Menu {
		public int source_count;

		public SourceMenu () {
			source_count = 0;
			Gnome.Vfs.VolumeMonitor monitor = Gnome.Vfs.VolumeMonitor.Get ();
			
			this.Append (new SourceItem (new BrowseSource ()));

			this.Append (new Gtk.SeparatorMenuItem ());

			foreach (Gnome.Vfs.Volume vol in monitor.MountedVolumes) {
				System.Console.WriteLine ("{0} - {1} - {2} {3} {4} {5} {6}",
							  vol.DisplayName, 
							   vol.Icon, 
							  vol.VolumeType.ToString (), 
							  vol.ActivationUri, 
							  vol.IsUserVisible,
							  vol.IsMounted,
							  vol.DeviceType);
				
				 if (vol.Drive != null)
					 System.Console.WriteLine (vol.Drive.DeviceType.ToString ());
				 
				 ImportSource source = new VolumeSource (vol);
#if true
				 SourceItem item = new SourceItem (source);
				 if (!vol.IsUserVisible || vol.DeviceType == Gnome.Vfs.DeviceType.Unknown) {
					 item.Sensitive = false;
					 continue;
				 }
				 this.Append (item);
				 source_count++;
#else
				 
				 this.Append (new SourceItem (source));
#endif
			}


			GPhotoCamera cam = new GPhotoCamera ();
			cam.DetectCameras ();
			
			if (cam.CameraList.Count () > 0)
				this.Append (new Gtk.SeparatorMenuItem ());
			
			source_count += cam.CameraList.Count ();
			for (int i = 0; i < cam.CameraList.Count (); i++) {
				ImportSource source = new CameraSource (cam, i);
				this.Append (new SourceItem (source));
			}

			if (source_count == 0) {
				ImportSource source = new BrowseSource (Mono.Posix.Catalog.GetString ("(No Cameras Detected)"),
									"emblem-camera");
				SourceItem item = new SourceItem (source);
				item.Sensitive = false;
				this.Append (item);
			}
			/*
			this.Append (new Gtk.SeparatorMenuItem ());
			
			foreach (Gnome.Vfs.Drive drive in monitor.ConnectedDrives) {
				ImportSource source = new DriveSource (drive);
				
				Gtk.ImageMenuItem item = new SourceItem (source);
				item.Sensitive = drive.IsMounted;
				this.Append (item);
			}
			*/

			this.ShowAll ();
		}

		public int SourceCount {
			get {
				return source_count;
			}
		}

		public int FindItemPosition (SourceItem source)
		{
			Gtk.Widget [] children = this.Children;
			for (int i = 0; i < children.Length; i++) {
				if (children [i] == source) {
					return i;
				}
			}
			return -1;
		}
		
		public int FindItemPosition (string path)
		{
			Gtk.Widget [] children = this.Children;
			System.Console.WriteLine ("looking for {0}", path);
			for (int i = 0; i < children.Length; i++) {
				if (children [i] is SourceItem) {
					VfsSource vfs = ((SourceItem)(children [i])).Source as VfsSource;
					if (vfs != null && (vfs.uri == path || path == (vfs.uri + "/dcim")))
						return i;
				}
			}
			return -1;
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


			if (scaled_thumbnail != thumbnail)
				scaled_thumbnail.Dispose ();
			
		}
	}


	[Glade.Widget] Gtk.OptionMenu tag_option_menu;
	[Glade.Widget] Gtk.OptionMenu source_option_menu;
	[Glade.Widget] Gtk.ScrolledWindow icon_scrolled;
	[Glade.Widget] Gtk.ScrolledWindow photo_scrolled;
	[Glade.Widget] Gtk.CheckButton attach_check;
	[Glade.Widget] Gtk.CheckButton recurse_check;
	[Glade.Widget] Gtk.Button ok_button;
	[Glade.Widget] Gtk.Image tag_image;
	[Glade.Widget] Gtk.Label tag_label;
	[Glade.Widget] Gtk.EventBox frame_eventbox;
	[Glade.Widget] ProgressBar progress_bar;
	
	Tag tag_selected;

	PhotoGrid grid;
	Gtk.Window main_window;
	string import_path;
	FSpot.PhotoList collection;
	bool cancelled;
	bool copy;

	int total;
	PhotoStore store;

	FSpot.Delay step;
	
	FSpot.PhotoImageView photo_view;
	IconView tray;
	ImportBackend importer;

	public ImportCommand (Gtk.Window mw)
	{
		main_window = mw;
		step = new FSpot.Delay (10, new GLib.IdleHandler (Step));
	}

	private void HandleDialogResponse (object obj, ResponseArgs args)
	{
		if (args.ResponseId != ResponseType.Ok) {
			this.Cancel ();
			this.Dialog.Destroy ();
			return;
		}
	}

	private void UpdateProgressBar (int count, int total)
	{
		if (progress_bar == null)
			return;

		progress_bar.Text = String.Format ("Importing {0} of {1}", count, total);
		progress_bar.Fraction = (double) count / System.Math.Max (total, 1);
	}

	private void HandleTraySelectionChanged (FSpot.IBrowsableCollection collection) 
	{
		if (collection.Count > 0)
			photo_view.Item.Index = tray.Selection.Ids[0];
	}

	private bool Step ()
	{			
		Photo photo;
		Pixbuf thumbnail;
		int count;
		bool ongoing = true;

		if (importer == null)
			return false;
		
		ongoing = importer.Step (out photo, out thumbnail, out count);
		
		if (thumbnail == null) {
			Console.WriteLine ("Could not import file");
		} else {
			//icon_scrolled.Visible = true;
			collection.Add (photo);
		
			//grid.AddThumbnail (thumbnail);
			UpdateProgressBar (count, total);
			thumbnail.Dispose ();
		}

		if (ongoing && total > 0)
			return true;
		else 
			return false;
	}

	private int DoImport (ImportBackend imp)
	{
		if (collection == null)
			return 0;

		this.importer = imp;
		//this.ok_button.Sensitive = false;

		total = importer.Prepare ();
		UpdateProgressBar (0, total);
		
		collection.Clear ();
		collection.Capacity = total;

		cancelled = false;
		FSpot.ThumbnailGenerator.Default.PushBlock ();

		while (total > 0 && this.Step ()) {
			while (Application.EventsPending ())
				Application.RunIteration ();
		}

		FSpot.ThumbnailGenerator.Default.PopBlock ();
		
		if (importer != null)
			importer.Finish ();
		
		importer = null;

		//ThumbnailGenerator.Default.PopBlock ();
		if (cancelled)
			return 0;
		else {
			//ok_button.Sensitive = true;
			return total;
		}
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
		string path = ChoosePath ();
		if (path != null) {
			SetImportPath (path);
		}
	}
	
	public string ChoosePath ()
	{
		string path = null;

		CompatFileChooserDialog file_selector =
			new CompatFileChooserDialog ("Import", this.Dialog,
						     CompatFileChooserDialog.Action.SelectFolder);

		file_selector.SelectMultiple = false;

		if (ImportPath != null)
			file_selector.Filename = ImportPath;
		else
			file_selector.Filename = System.Environment.GetEnvironmentVariable ("HOME");

		int response = file_selector.Run ();

		if ((ResponseType) response == ResponseType.Ok) {
			path = file_selector.Filename;
		}

		file_selector.Destroy ();
		return path;
	}
	
	public void SetImportPath (string path)
	{
		import_path = path;
	}

	private void HandleTagMenuSelected (Tag t) 
	{
		tag_selected = t;
		//tag_image.Pixbuf = t.Icon;
		//tag_label.Text = t.Name;
	
	}

	private void HandleSourceChanged (object sender, EventArgs args)
	{
		if (store == null || collection == null)
			return;
		
		this.Cancel ();
		this.copy = false;
		//this.ok_button.Sensitive = false;

		Gtk.OptionMenu option = (Gtk.OptionMenu) sender;
		Gtk.Menu menu = (Gtk.Menu)(option.Menu);
		SourceItem item =  (SourceItem)(menu.Active);
		System.Console.WriteLine ("item {0}", item);

		if (!item.Sensitive)
			return;

		if (item.Source is BrowseSource) {
			string path = ChoosePath ();
			
			if (path != null) {
				SourceItem path_item = new SourceItem (new VfsSource (path));
				menu.Prepend (path_item);
				path_item.ShowAll ();
				//option.SetHistory (0);
				SetImportPath (path);
			}
		} else if (item.Source is VfsSource) {
			VfsSource vfs = item.Source as VfsSource;

			// If the paths are the Same no need to reload.
			if (vfs is VolumeSource)
				copy = true;
			
			SetImportPath (vfs.uri);
		}

		Start ();
	}

	private void HandleRecurseToggled (object sender, System.EventArgs args)
	{
		this.Cancel ();
		while (Application.EventsPending ())
			Application.RunIteration ();
		this.Start ();
	}

	public int ImportFromFile (PhotoStore store, string path)
	{
		this.store = store;
		this.CreateDialog ("import_dialog");
		
		this.Dialog.TransientFor = main_window;
		this.Dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;
		this.Dialog.Response += HandleDialogResponse;

		MenuItem attach_item = new MenuItem (Mono.Posix.Catalog.GetString ("Select Tag"));
		TagMenu tagmenu = new TagMenu (null, MainWindow.Toplevel.Database.Tags);
		
		this.Dialog.DefaultResponse = ResponseType.Ok;
		
		//import_folder_entry.Activated += HandleEntryActivate;

		tagmenu.TagSelected += HandleTagMenuSelected;
		tagmenu.ShowAll ();
		tagmenu.Populate (true);
		tagmenu.Prepend (attach_item);

		//		this.ok_button.Sensitive = false;
		
		recurse_check.Toggled += HandleRecurseToggled;

		tag_option_menu.Menu = tagmenu;
		SourceMenu menu = new SourceMenu ();
		source_option_menu.Menu = menu;

		collection = new FSpot.PhotoList (new Photo [0]);
		tray = new TrayView (collection);
		tray.Selection.Changed += HandleTraySelectionChanged;
		icon_scrolled.SetSizeRequest (200, 480);
		icon_scrolled.Add (tray);
		//icon_scrolled.Visible = false;
		tray.Show ();

		photo_view = new FSpot.PhotoImageView (collection);
		photo_scrolled.Add (photo_view);
		photo_scrolled.SetSizeRequest (200, 480);
		photo_view.Show ();

		//FSpot.Global.ModifyColors (frame_eventbox);
		FSpot.Global.ModifyColors (photo_scrolled);
		FSpot.Global.ModifyColors (photo_view);

		photo_view.Pixbuf = PixbufUtils.LoadFromAssembly ("f-spot-logo.png");
		photo_view.Fit = true;
			
		tag_selected = null;
		if (attach_check != null) {
			attach_check.Toggled += HandleTagToggled;
			HandleTagToggled (null, null);
		}				

		this.Dialog.Show ();
		source_option_menu.Changed += HandleSourceChanged;
		if (path != null) {
			SetImportPath (path);
			int i = menu.FindItemPosition (path);
			if (i > 0)
				source_option_menu.SetHistory ((uint)i);
		}
						
		ResponseType response = (ResponseType) this.Dialog.Run ();
		
		while (response == ResponseType.Ok) {
			if (System.IO.Directory.Exists (this.ImportPath))
			    break;

			HigMessageDialog md = new HigMessageDialog (this.Dialog,
								    DialogFlags.DestroyWithParent,
								    MessageType.Error,
								    ButtonsType.Ok,
								    Mono.Posix.Catalog.GetString ("Directory does not exist."),
									    String.Format (Mono.Posix.Catalog.GetString ("The directory you selected \"{0}\" does not exist.  Please choose a different directory"), this.ImportPath));
			md.Run ();
			md.Destroy ();

			response = (Gtk.ResponseType) this.Dialog.Run ();
		}

		if (response == ResponseType.Ok) {
			if (attach_check.Active && tag_selected != null) {
				for (int i = 0; i < collection.Count; i++) {
					Photo p = collection [i] as Photo;
					
					if (p == null)
						continue;
					
					p.AddTag (tag_selected);
					store.Commit (p);
				}
			}

			this.Dialog.Destroy ();
			return collection.Count;
		} else {
			this.Cancel ();
			//this.Dialog.Destroy();
			return 0;
		}
	}

	public void Cancel ()
	{
		if (importer != null) {
			importer.Cancel ();
			importer = null;
		}
		
		if (collection == null || collection.Count == 0)
			return;
		
		// FIXME this should be a transaction or a multiple remove.
		for (int i = 0; i < collection.Count; i++) {
			store.Remove ((Photo)(collection [i]));
		}
	}

	public int Start ()
	{
		if (import_path == null)
			return 0;

		string [] pathimport =  {ImportPath};
		//this.Dialog.Destroy();
		
		bool recurse = true;
		if (recurse_check != null)
			recurse = recurse_check.Active;
		
		if (collection == null)
			return 0;

		return DoImport (new FileImportBackend (store, pathimport, copy, recurse, null));
	}

	public int ImportFromPaths (PhotoStore store, string [] paths)
	{
		return ImportFromPaths (store, paths, null);
	}

	public int ImportFromPaths (PhotoStore store, string [] paths, Tag [] tags)
	{
		collection = new FSpot.PhotoList (new Photo [0]);
		return DoImport (new FileImportBackend (store, paths, false, true, tags));
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
