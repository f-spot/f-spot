using GLib;
using Gdk;
using Gnome;
using Gtk;
using GtkSharp;
using System.Collections;
using System.IO;
using System;
using Mono.Unix;

public class ImportCommand : FSpot.GladeDialog {
	internal class SourceItem : ImageMenuItem {
		public ImportSource Source;

		public SourceItem (ImportSource source) : base (source.Name.Replace ("_", "__"))
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
			this.Name = Catalog.GetString ("Select Folder");
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

		public VfsSource (string uri)
		{ 
			string [] components = uri.Split (new char [] { '/' });
			this.Name = components [components.Length - 1];
			if (this.Name == String.Empty)
				this.Name = components [components.Length - 2];

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
			this.Name = vol.DisplayName;

			try {
				mount_point = new Uri (vol.ActivationUri).LocalPath;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}

			uri = mount_point;
			
                        if (this.Icon == null)
				this.Icon = PixbufUtils.LoadThemeIcon (vol.Icon, 32);
			
			if (this.IsIPodPhoto)
				this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-ipod", 32);

			if (this.Icon == null && this.IsCamera)
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
					return (Directory.Exists (Path.Combine (mount_point, "DCIM")));
				} catch {
					return false;
				}
			}
		}

		private bool IsIPodPhoto {
			get {
				if (Volume.DeviceType != Gnome.Vfs.DeviceType.MusicPlayer 
				    && Volume.DeviceType != Gnome.Vfs.DeviceType.Apple)
					return false;

				try {
					return (Directory.Exists (Path.Combine (mount_point, "Photos")) &&
						Directory.Exists (Path.Combine (mount_point, "iPod_Control")));
				} catch {
					return false;
				}
			}
		}
	}

	internal class DriveSource : ImportSource {
		public Gnome.Vfs.Drive Drive;
		
		public DriveSource (Gnome.Vfs.Drive drive) 
		{
			this.Name = drive.DisplayName;
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

#if LONG_NAMES
			this.Name = String.Format ("{0} ({1})", cam.CameraList.GetName (index), cam.CameraList.GetValue (index));
#else
			this.Name = String.Format ("{0}", cam.CameraList.GetName (index));
#endif
			this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-camera", 32);
			if (this.Icon == null)
				this.Icon = PixbufUtils.LoadThemeIcon ("gnome-dev-media-cf", 32);
		}

		public string Port {
			get {
				return cam.CameraList.GetValue (CameraIndex);
			}
		}
	}

	internal abstract class ImportSource {
		public object Backend;
		public Gdk.Pixbuf Icon;
		public string Name;
	}
	
	private class SourceMenu : Gtk.Menu {
		public int source_count;
		ImportCommand command;

		private static Gnome.Vfs.VolumeMonitor monitor = Gnome.Vfs.VolumeMonitor.Get ();

		public SourceMenu (ImportCommand command) {
			this.command = command;
			source_count = 0;
			
			SourceItem item = new SourceItem (new BrowseSource ());
			item.Activated += HandleActivated;
			this.Append (item);
			this.Append (new Gtk.SeparatorMenuItem ());

			// Add external hard drives to the menu
			foreach (Gnome.Vfs.Volume vol in monitor.MountedVolumes) {
				 if (!vol.IsUserVisible || vol.DeviceType == Gnome.Vfs.DeviceType.Unknown)
					 continue;
				
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
				 item = new SourceItem (source);
				 item.Activated += HandleActivated;
				 this.Append (item);
				 source_count++;

			}


			GPhotoCamera cam = new GPhotoCamera ();
			cam.DetectCameras ();
			int camera_count = cam.CameraList.Count ();

			if (camera_count > 0) {
				source_count += camera_count;
				for (int i = 0; i < camera_count; i++) {
					string handle = cam.CameraList.GetValue (i);
					if (camera_count == 1 || handle != "usb:") {
						if (handle.StartsWith ("disk:")) {
							string path = handle.Substring ("disk:".Length);

							if (FindItemPosition (path) != -1)
								continue;
						}
			
						ImportSource source = new CameraSource (cam, i);
						item = new SourceItem (source);
						item.Activated += HandleActivated;
						this.Append (item);
					}
				}
			} else {
				ImportSource source = new BrowseSource (Catalog.GetString ("(No Cameras Detected)"),
									"emblem-camera");
				item = new SourceItem (source);
				item.Activated += HandleActivated;
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

		private void HandleActivated (object sender, EventArgs args)
		{
			command.Source = (SourceItem) sender;
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
	
	[Glade.Widget] Gtk.HBox tagentry_box;
	[Glade.Widget] Gtk.OptionMenu source_option_menu;
	[Glade.Widget] Gtk.ScrolledWindow icon_scrolled;
	[Glade.Widget] Gtk.ScrolledWindow photo_scrolled;
	[Glade.Widget] Gtk.CheckButton recurse_check;
	[Glade.Widget] Gtk.CheckButton copy_check;
	[Glade.Widget] Gtk.Button ok_button;
	[Glade.Widget] Gtk.Image tag_image;
	[Glade.Widget] Gtk.Label tag_label;
	[Glade.Widget] Gtk.EventBox frame_eventbox;
	[Glade.Widget] ProgressBar progress_bar;
	
	ArrayList tags_selected;

	FSpot.Widgets.TagEntry tag_entry;

	Gtk.Window main_window;
	FSpot.PhotoList collection;
	bool copy;
	SourceMenu menu;

	int total;
	PhotoStore store;
	FSpot.Delay step;
	
	FSpot.PhotoImageView photo_view;
	ImportBackend importer;
	IconView tray;

	FSpot.Delay idle_start; 

	string loading_string;

	string import_path;
	public string ImportPath {
		get { return import_path; }
	}
	
	private SourceItem Source {
		set {
			if (store == null || collection == null)
				return;
			
			SourceItem item = value;
			
			this.Cancel ();
			this.copy = copy_check.Active;
			AllowFinish = false;

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
				SetImportPath (vfs.uri);
			} else if (item.Source is CameraSource) {
				CameraSource csource = item.Source as CameraSource;
				string port = "gphoto2:" + csource.Port;
				this.Cancel ();
				this.Dialog.Destroy ();
				MainWindow.Toplevel.ImportCamera (port);
			}

			idle_start.Start ();
		}
	}

	public ImportCommand (Gtk.Window mw)
	{
		main_window = mw;
		step = new FSpot.Delay (new GLib.IdleHandler (Step));
		idle_start = new FSpot.Delay (new IdleHandler (Start));
		loading_string = Catalog.GetString ("Loading {0} of {1}");
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

		progress_bar.Text = String.Format (loading_string, count, total);
		progress_bar.Fraction = (double) count / System.Math.Max (total, 1);
	}

	private void HandleTraySelectionChanged (FSpot.IBrowsableCollection coll) 
	{
		if (tray.Selection.Count > 0)
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
		
		try {
			// FIXME this is really just an incredibly ugly way of dealing
			// with the recursive DoImport loops we sometimes get into
			ongoing = importer.Step (out photo, out thumbnail, out count);
		} catch (ImportException e){
			System.Console.WriteLine (e);
			return false;
		}

		if (photo == null || thumbnail == null) {
			Console.WriteLine ("Could not import file");
		} else {
			//icon_scrolled.Visible = true;
			collection.Add (photo);
		
			//grid.AddThumbnail (thumbnail);

		}

		if (thumbnail != null)
			thumbnail.Dispose ();
		
		if (count < total)
			UpdateProgressBar (count + 1, total);

		if (ongoing && total > 0)
			return true;
		else {
			System.Console.WriteLine ("Stopping");
			if (progress_bar != null)
				progress_bar.Text = Catalog.GetString ("Done Loading");
			
			AllowFinish = true;
			return false;
		}
	}

	public bool AllowFinish
	{
		set {
			if (this.ok_button != null)
				this.ok_button.Sensitive = value;
		}
	}

	private int DoImport (ImportBackend imp)
	{
		if (collection == null)
			return 0;

		this.importer = imp;
		AllowFinish = false;
		
		total = importer.Prepare ();

		if (total > 0)
			UpdateProgressBar (1, total);
		
		collection.Clear ();
		collection.Capacity = total;

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
	
	public void Finish ()
	{
		if (idle_start.IsPending || step.IsPending) {
			AllowFinish = false;
			return;
		}
		
		if (importer != null)
			importer.Finish ();
		
		importer = null;
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
			file_selector.Filename = FSpot.Global.HomeDirectory;

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

//	private void HandleTagMenuSelected (Tag t) 
//	{
//		tag_selected = t;
//		//tag_image.Pixbuf = t.Icon;
//		//tag_label.Text = t.Name;
//	}

	private void HandleRecurseToggled (object sender, System.EventArgs args)
	{
		this.Cancel ();
		this.Dialog.Sensitive = false;
	       
		idle_start.Start ();
	}

	public int ImportFromFile (PhotoStore store, string path)
	{
		this.store = store;
		this.CreateDialog ("import_dialog");
		
		this.Dialog.TransientFor = main_window;
		this.Dialog.WindowPosition = Gtk.WindowPosition.CenterOnParent;
		this.Dialog.Response += HandleDialogResponse;

	        AllowFinish = false;
		
		this.Dialog.DefaultResponse = ResponseType.Ok;
		
		//import_folder_entry.Activated += HandleEntryActivate;
		recurse_check.Toggled += HandleRecurseToggled;
		copy_check.Toggled += HandleRecurseToggled;

		menu = new SourceMenu (this);
		source_option_menu.Menu = menu;

		collection = new FSpot.PhotoList (new Photo [0]);
		tray = new FSpot.ScalingIconView (collection);
		tray.Selection.Changed += HandleTraySelectionChanged;
		icon_scrolled.SetSizeRequest (200, 200);
		icon_scrolled.Add (tray);
		//icon_scrolled.Visible = false;
		tray.DisplayTags = false;
		tray.Show ();

		photo_view = new FSpot.PhotoImageView (collection);
		photo_scrolled.Add (photo_view);
		photo_scrolled.SetSizeRequest (200, 200);
		photo_view.Show ();

		//FSpot.Global.ModifyColors (frame_eventbox);
		FSpot.Global.ModifyColors (photo_scrolled);
		FSpot.Global.ModifyColors (photo_view);

		photo_view.Pixbuf = PixbufUtils.LoadFromAssembly ("f-spot-48.png");
		photo_view.Fit = true;
			
		tag_entry = new FSpot.Widgets.TagEntry (MainWindow.Toplevel.Database.Tags, false);
		tag_entry.UpdateFromTagNames (new string []{});
		tagentry_box.Add (tag_entry);

		tag_entry.Show ();

		this.Dialog.Show ();
		//source_option_menu.Changed += HandleSourceChanged;
		if (path != null) {
			SetImportPath (path);
			int i = menu.FindItemPosition (path);

			if (i > 0) {
				source_option_menu.SetHistory ((uint)i);
			} else if (Directory.Exists (path)) {
				SourceItem path_item = new SourceItem (new VfsSource (path));
				menu.Prepend (path_item);
				path_item.ShowAll ();
				SetImportPath (path);
				source_option_menu.SetHistory (0);
			} 
			idle_start.Start ();
		}
						
		ResponseType response = (ResponseType) this.Dialog.Run ();
		
		while (response == ResponseType.Ok) {
			try {
				if (Directory.Exists (this.ImportPath))
					break;
			} catch (System.Exception e){
				System.Console.WriteLine (e);
				break;
			}

			HigMessageDialog md = new HigMessageDialog (this.Dialog,
			        DialogFlags.DestroyWithParent,
				MessageType.Error,
				ButtonsType.Ok,
				Catalog.GetString ("Directory does not exist."),
				String.Format (Catalog.GetString ("The directory you selected \"{0}\" does not exist.  " + 
								  "Please choose a different directory"), this.ImportPath));

			md.Run ();
			md.Destroy ();

			response = (Gtk.ResponseType) this.Dialog.Run ();
		}

		if (response == ResponseType.Ok) {
			this.UpdateTagStore (tag_entry.GetTypedTagNames ());
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
		Db db = MainWindow.Toplevel.Database;	
		db.BeginTransaction ();
		foreach (string tagname in new_tags) {
			Tag t = db.Tags.GetTagByName (tagname);
			if (t == null) {
				Category default_category = db.Tags.GetTagByName (Catalog.GetString ("Import Tags")) as Category;
				if (default_category == null) {
					default_category = db.Tags.CreateCategory (null, Catalog.GetString ("Import Tags"));
					default_category.StockIconName = "f-spot-imported-xmp-tags.png"; 
				}
				t = db.Tags.CreateCategory (default_category, tagname) as Tag;
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

	public void Cancel ()
	{
		idle_start.Stop ();
		step.Stop ();
		if (importer != null) {
			importer.Cancel ();
			importer = null;
		}
		
		if (collection == null || collection.Count == 0)
			return;
		
		Photo [] photos = new Photo [collection.Count];
		for (int i = 0; i < collection.Count; i++)
			photos [i] = (Photo) collection [i];

		store.Remove (photos);
	}

	public bool Start ()
	{
		if (Dialog != null)
			Dialog.Sensitive = true;

		if (import_path == null)
			return false;

		string [] pathimport =  {ImportPath};
		//this.Dialog.Destroy();
		
		if (copy_check != null)
			copy = copy_check.Active;
		
		bool recurse = true;
		if (recurse_check != null)
			recurse = recurse_check.Active;
		
//		importer = new FileImportBackend (store, pathimport, copy, recurse, null);
		importer = new FileImportBackend (store, pathimport, copy, recurse, null, Dialog);
		AllowFinish = false;
		
		total = importer.Prepare ();
		
		if (total > 0)
			UpdateProgressBar (1, total);
		
		collection.Clear ();
		collection.Capacity = total;

		if (total > 0)
			step.Start ();
	       
		return false;
	}

	public int ImportFromPaths (PhotoStore store, string [] paths, bool copy)
	{
		return ImportFromPaths (store, paths, null, copy);
	}
	
	public int ImportFromPaths (PhotoStore store, string [] paths, Tag [] tags)
	{
		return ImportFromPaths (store, paths, tags, false);
	}
	
	public int ImportFromPaths (PhotoStore store, string [] paths, Tag [] tags, bool copy)
	{
		collection = new FSpot.PhotoList (new Photo [0]);
		int count = DoImport (new FileImportBackend (store, paths, copy, true, tags, main_window ));

		Finish ();

		return count;
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
