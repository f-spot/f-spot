/*
 * ImportCommand.cs
 *
 * Author(s)
 * 	Ettore Perazzoli
 * 	Larry Ewing
 * 	Miguel de Icaza
 * 	Nat Friedman
 * 	Gabriel Burt
 * 	Markus Lindqvist <markus.lindqvist@iki.fi>
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
		public Uri uri;

		public VfsSource (Uri uri)
		{ 
			string [] components = uri.Segments;
			this.Name = components [components.Length - 1];
			if (this.Name == String.Empty)
				this.Name = components [components.Length - 2];

			this.uri = uri;
			
			this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "stock_folder", 32, (Gtk.IconLookupFlags)0);
		}

		protected VfsSource () {}
	}

	internal class MountSource : VfsSource
	{
		public GLib.Mount Mount;
		public string mount_point;

		public MountSource (GLib.Mount mount)
		{
			this.Mount = mount;
			this.Name = mount.Name;

			try {
				mount_point = mount.Root.Uri.LocalPath;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
			}

			uri = mount.Root.Uri;
			
			
			if (this.IsIPodPhoto)
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "multimedia-player", 32, (Gtk.IconLookupFlags)0);

			if (this.Icon == null && this.IsCamera)
				this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, "media-flash", 32, (Gtk.IconLookupFlags)0);

			if (this.Icon == null) {
				if (mount.Icon is GLib.ThemedIcon) {
					this.Icon = GtkUtil.TryLoadIcon (FSpot.Global.IconTheme, (mount.Icon as GLib.ThemedIcon).Names, 32, (Gtk.IconLookupFlags)0);
				} else {
					// TODO
					throw new Exception ("Unloadable icon type");
				}
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
				try {
					return (Directory.Exists (Path.Combine (mount_point, "Photos")) &&
						Directory.Exists (Path.Combine (mount_point, "iPod_Control")));
				} catch {
					return false;
				}
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

		private static GLib.VolumeMonitor monitor = GLib.VolumeMonitor.Default;

		public SourceMenu (ImportCommand command) {
			this.command = command;
			source_count = 0;
			
			SourceItem item = new SourceItem (new BrowseSource ());
			item.Activated += HandleActivated;
			this.Append (item);
			this.Append (new Gtk.SeparatorMenuItem ());

			// Add external hard drives to the menu
			foreach (GLib.Mount mount in monitor.Mounts) {
				 ImportSource source = new MountSource (mount);
				 item = new SourceItem (source);
				 item.Activated += HandleActivated;
				 this.Append (item);
				 source_count++;
			}

			// FIXME This crashes every time, replace by gvfs https://bugzilla.gnome.org/show_bug.cgi?id=618773
			//GPhotoCamera cam = new GPhotoCamera ();
			//cam.DetectCameras ();
			//int camera_count = cam.CameraList.Count;

			if (/*camera_count > 0*/false) {
				/*source_count += camera_count;
				for (int i = 0; i < camera_count; i++) {
					string handle = cam.CameraList.GetValue (i);
					if (camera_count == 1 || handle != "usb:") {
						if (handle.StartsWith ("disk:")) {
							string path = handle.Substring ("disk:".Length);

							if (FindItemPosition (UriUtils.PathToFileUri (path)) != -1)
								continue;
						}
			
						ImportSource source = new CameraSource (cam, i);
						item = new SourceItem (source);
						item.Activated += HandleActivated;
						this.Append (item);
					}
				}*/
			} else {
				ImportSource source = new BrowseSource (Catalog.GetString ("(No Cameras Detected)"),
									"camera-photo");
				item = new SourceItem (source);
				item.Activated += HandleActivated;
				item.Sensitive = false;
				this.Append (item);
			}

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
		
		public int FindItemPosition (Uri uri)
		{
			Gtk.Widget [] children = this.Children;
			System.Console.WriteLine ("looking for {0}", uri);
			for (int i = 0; i < children.Length; i++) {
				if (children [i] is SourceItem) {
					VfsSource vfs = ((SourceItem)(children [i])).Source as VfsSource;
					if (vfs != null && (vfs.uri == uri || uri.ToString () == (vfs.uri + "/dcim")))
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

	public Uri ImportUri { get; private set; }
	
	private SourceItem Source {
		set {
			if (store == null || collection == null)
				return;
			
			SourceItem item = value;
			
			this.Cancel ();
			this.copy = copy_check.Active;

			if (!item.Sensitive)
				return;

			if (item.Source is BrowseSource) {
				Uri uri = ChooseUri ();
				
				if (uri != null) {
					SourceItem uri_item = new SourceItem (new VfsSource (uri));
					menu.Prepend (uri_item);
					uri_item.ShowAll ();
					source_option_menu.SetHistory (0);
					ImportUri = uri;
				}
			} else if (item.Source is VfsSource) {
				VfsSource vfs = item.Source as VfsSource;
				ImportUri = vfs.uri;
			} else if (item.Source is CameraSource) {
				CameraSource csource = item.Source as CameraSource;
				string port = "gphoto2:" + csource.Port;
				this.Cancel ();
				this.Dialog.Destroy ();
				App.Instance.Organizer.ImportCamera (port);
			}

			idle_start.Start ();
		}
	}

	public ImportCommand (Gtk.Window mw)
	{
		main_window = mw;
		step = new FSpot.Delay (new GLib.IdleHandler (Step));
		idle_start = new FSpot.Delay (new IdleHandler (Start));
		loading_string = Catalog.GetString ("Importing {0} of {1}");
	}

	private void HandleDialogResponse (object obj, ResponseArgs args)
	{
		if (args.ResponseId != ResponseType.Ok) {
			this.Cancel ();
			this.Dialog.Destroy ();
			return;
		}

		AllowFinish = false;
		OptionsEnabled = false;
		if (total > 0) {
			UpdateProgressBar (1, total);
			step.Start ();

			while (total > 0 && this.Step ()) {
				System.DateTime start_time = System.DateTime.Now;
				System.TimeSpan span = start_time - start_time;

				while (Application.EventsPending () && span.TotalMilliseconds < 100) {
					span = System.DateTime.Now - start_time;
					Application.RunIteration ();
				}
			}
		}
	}

	private void UpdateProgressBar (int count, int total)
	{
		if (progress_bar == null)
			return;

		if (count > 0)
			progress_bar.Show ();
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

		if (status_info.Count > 0 && status_info.Count % 25 == 0)
			System.GC.Collect ();

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
			if (ok_button != null)
				ok_button.Sensitive = value;
		}
	}

	public bool OptionsEnabled
	{
		set {
			if (source_option_menu != null) source_option_menu.Sensitive = value;
			if (copy_check != null) copy_check.Sensitive = value;
			if (recurse_check != null) recurse_check.Sensitive = value;
			if (duplicate_check != null) duplicate_check.Sensitive = value;
			if (tagentry_box != null) tagentry_box.Sensitive = value;
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
	
	void SavePreferences ()
	{
		Preferences.Set(Preferences.IMPORT_COPY_FILES, copy_check.Active);
		Preferences.Set(Preferences.IMPORT_INCLUDE_SUBFOLDERS, recurse_check.Active);
		Preferences.Set(Preferences.IMPORT_CHECK_DUPLICATES, duplicate_check.Active);
	}
	
	public Uri ChooseUri ()
	{
		Uri uri = null;

		FileChooserDialog file_chooser =
			new FileChooserDialog (Catalog.GetString ("Import"), this.Dialog,
					FileChooserAction.SelectFolder,
					Stock.Cancel, ResponseType.Cancel,
					Stock.Open, ResponseType.Ok);

		file_chooser.SelectMultiple = false;
		file_chooser.LocalOnly = false;

		if (ImportUri != null)
			file_chooser.SetCurrentFolderUri (ImportUri.ToString ());
		else
			file_chooser.SetFilename (FSpot.Global.HomeDirectory);

		int response = file_chooser.Run ();

		if ((ResponseType) response == ResponseType.Ok) {
			uri = new Uri (file_chooser.Uri);
		}

		file_chooser.Destroy ();
		return uri;
	}

	private void HandleRecurseToggled (object sender, System.EventArgs args)
	{
		this.Cancel ();
		this.Dialog.Sensitive = false;
	       
		idle_start.Start ();
	}
	
	private void LoadPreferences ()
	{
		if (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_WIDTH) > 0)
			this.Dialog.Resize (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_WIDTH), FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_HEIGHT));

		if (FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_PANE_POSITION) > 0)
			import_hpaned.Position = FSpot.Preferences.Get<int> (FSpot.Preferences.IMPORT_WINDOW_PANE_POSITION);

		copy_check.Active = Preferences.Get<bool> (Preferences.IMPORT_COPY_FILES);
		recurse_check.Active = Preferences.Get<bool> (Preferences.IMPORT_INCLUDE_SUBFOLDERS);
		duplicate_check.Active = Preferences.Get<bool> (Preferences.IMPORT_CHECK_DUPLICATES);
	}

	public int ImportFromUri (PhotoStore store, Uri uri)
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

		//source_option_menu.Changed += HandleSourceChanged;
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
				System.Console.WriteLine (e);
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

	public void Cancel ()
	{
		idle_start.Stop ();
		step.Stop ();
		if (importer != null) {
			importer.Cancel ();
			importer = null;
		}
	}

	private bool Start ()
	{
		if (Dialog != null)
			Dialog.Sensitive = true;

		if (ImportUri == null)
			return false;

		Uri [] uriimport =  {ImportUri};
		
		if (copy_check != null)
			copy = copy_check.Active;
		
		bool recurse = true;
		if (recurse_check != null)
			recurse = recurse_check.Active;

		bool detect_duplicates = false;
		if (duplicate_check != null)
			detect_duplicates = duplicate_check.Active;
		
		importer = new FileImportBackend (store, uriimport, copy, recurse, detect_duplicates, null, Dialog);

		collection.Clear ();
		AllowFinish = false;

		var info = importer.Prepare ();
		total = info.Count;

		AllowFinish = true;
		collection.AddAll (info);

		return false;
	}

	public int ImportFromUris (PhotoStore store, Uri [] uris, bool copy)
	{
		return ImportFromUris (store, uris, null, copy);
	}
	
	public int ImportFromUris (PhotoStore store, Uri [] uris, Tag [] tags)
	{
		return ImportFromUris (store, uris, tags, false);
	}
	
	public int ImportFromUris (PhotoStore store, Uri [] uris, Tag [] tags, bool copy)
	{
		collection = new FSpot.PhotoList (new Photo [0]);
		int count = DoImport (new FileImportBackend (store, uris, copy, true, tags, main_window ));

		Finish ();

		return count;
	}
}

public class StepStatusInfo {
	private Photo photo;
	private int count;
	private bool is_duplicate;

	public Photo Photo {
		get  {
			return photo; 
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

	public StepStatusInfo (Photo photo, int count, bool is_duplicate)
	{
		this.photo = photo;
		this.count = count;
		this.is_duplicate = is_duplicate;
	}
 
	public StepStatusInfo (Photo photo, int count)
		: this (photo, count, false)
	{ }
}


