/*
 * ImportCommand.cs
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing
 * 	Miguel de Icaza
 * 	Nat Friedman
 * 	Gabriel Burt
 *
 * This is free software. See COPYING for details.
 */

using GLib;
using Gdk;
using Gtk;
using System.Collections;
using System.IO;
using System;
using Mono.Unix;

using FSpot;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Widgets;

public class ImportCommand : GladeDialog
{
	internal class SourceItem : ImageMenuItem
	{
		public ImportSource Source;

		public SourceItem (ImportSource source) : base (source.Name.Replace ("_", "__"))
		{
			this.Source = source;
	
			Gdk.Pixbuf icon = source.Icon;
			if (icon != null)
				this.Image = new Gtk.Image (icon);
		}
	} 

	internal class BrowseSource : ImportSource
	{
		public BrowseSource ()
		{
			this.Name = Catalog.GetString ("Select Folder");
			this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "stock_folder", 32, (Gtk.IconLookupFlags)0);
		}

		public BrowseSource (string name, string icon)
		{
			this.Name = name;
			this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, icon, 32, (Gtk.IconLookupFlags)0);
		}
	}

	internal class VfsSource : ImportSource
	{
		public string uri;

		public VfsSource (string uri)
		{ 
			string [] components = uri.Split (new char [] { '/' });
			this.Name = components [components.Length - 1];
			if (this.Name == String.Empty)
				this.Name = components [components.Length - 2];

			this.uri = uri;
			
			this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "stock_folder", 32, (Gtk.IconLookupFlags)0);
		}

		public virtual bool Contains (string path)
		{
			return false;
		}

		protected VfsSource () {}
	}

	internal class VolumeSource : VfsSource
	{
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
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, vol.Icon, 32, (Gtk.IconLookupFlags)0);
			
			if (this.IsIPodPhoto)
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "multimedia-player", 32, (Gtk.IconLookupFlags)0);

			if (this.Icon == null && this.IsCamera)
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "media-flash", 32, (Gtk.IconLookupFlags)0);

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

	internal class DriveSource : ImportSource
	{
		public Gnome.Vfs.Drive Drive;
		
		public DriveSource (Gnome.Vfs.Drive drive) 
		{
			this.Name = drive.DisplayName;
			this.Drive = drive;

			if (drive.IsMounted) {
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, drive.MountedVolume.Icon, 32, (Gtk.IconLookupFlags)0);
				//this.Sensitive = drive.MountedVolume.IsMounted;
			} else {
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, drive.Icon, 32, (Gtk.IconLookupFlags)0);
			}
		}
	}

	internal class CameraSource : ImportSource
	{
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
			this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "camera-photo", 32, (Gtk.IconLookupFlags)0);
			if (this.Icon == null)
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "media-flash", 32, (Gtk.IconLookupFlags)0);
		}

		public string Port {
			get {
				return cam.CameraList.GetValue (CameraIndex);
			}
		}
	}

	internal abstract class ImportSource {
		public Gdk.Pixbuf Icon;
		public string Name;
	}
	
	private class SourceMenu : Gtk.Menu
	{
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
	 	StepStatusInfo status_info;		
		bool ongoing = true;

		if (importer == null)
			return false;
		
		try {
			// FIXME this is really just an incredibly ugly way of dealing
			// with the recursive DoImport loops we sometimes get into
			ongoing = importer.Step (out status_info);
		} catch (ImportException e){
			System.Console.WriteLine (e);
			return false;
		}

		if (!status_info.IsDuplicate && (status_info.Photo == null || status_info.Thumbnail == null)) {
			Console.WriteLine ("Could not import file");
		} else {
			//icon_scrolled.Visible = true;
		 	if (!status_info.IsDuplicate)
				collection.Add (status_info.Photo);
			//grid.AddThumbnail (thumbnail);

		}

		if (status_info.Thumbnail != null)
			status_info.Thumbnail.Dispose ();
		
		if (status_info.Count < total)
			UpdateProgressBar (status_info.Count + 1, total);

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

		FileChooserDialog file_chooser =
			new FileChooserDialog (Catalog.GetString ("Import"), this.Dialog,
					FileChooserAction.SelectFolder,
					Stock.Cancel, ResponseType.Cancel,
					Stock.Open, ResponseType.Ok);

		file_chooser.SelectMultiple = false;

		if (ImportPath != null)
			file_chooser.SetFilename (ImportPath);
		else
			file_chooser.SetFilename (FSpot.Global.HomeDirectory);

		int response = file_chooser.Run ();

		if ((ResponseType) response == ResponseType.Ok) {
			path = file_chooser.Filename;
		}

		file_chooser.Destroy ();
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

 		if (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_WIDTH) > 0)
 			this.Dialog.Resize (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_WIDTH), FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_HEIGHT));

 		if (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_PANE_POSITION) > 0)
			import_hpaned.Position = FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_PANE_POSITION);

	        AllowFinish = false;
		
		this.Dialog.DefaultResponse = ResponseType.Ok;
		
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
		Db db = MainWindow.Toplevel.Database;	
		db.BeginTransaction ();
		foreach (string tagname in new_tags) {
			Tag t = db.Tags.GetTagByName (tagname);
			if (t == null) {
				// Note for translators: 'Import Tags' is no command, it means 'Tags used in Import'
				Category default_category = db.Tags.GetTagByName (Catalog.GetString ("Import Tags")) as Category;
				if (default_category == null) {
					default_category = db.Tags.CreateCategory (null, Catalog.GetString ("Import Tags"), false);
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

		bool detect_duplicates = false;
		if (duplicate_check != null)
		 	detect_duplicates = duplicate_check.Active;
		
//		importer = new FileImportBackend (store, pathimport, copy, recurse, null);
		importer = new FileImportBackend (store, pathimport, copy, recurse, detect_duplicates, null, Dialog);
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

public class StepStatusInfo {
	private Photo photo;
	private Pixbuf thumbnail;
	private int count;
	private bool is_duplicate;

	public Photo Photo {
		get  {
			return photo; 
		} 
	}

	public Pixbuf Thumbnail {
		get {
			return thumbnail; 
		} 
	}

	public int Count {
		get {
			return count; 
		} 
	}

	public bool IsDuplicate {
		get {
			return is_duplicate; 
		} 
	}

	public StepStatusInfo (Photo photo, Pixbuf thumbnail, int count, bool is_duplicate)
	{
		this.photo = photo;
		this.thumbnail = thumbnail;
		this.count = count;
		this.is_duplicate = is_duplicate;
	}
 
	public StepStatusInfo (Photo photo, Pixbuf thumbnail, int count)
		: this (photo, thumbnail, count, false)
	{ }
}


