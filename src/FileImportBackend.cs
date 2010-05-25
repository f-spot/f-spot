using Gdk;
using Gtk;
using System.Collections;
using System.Collections.Generic;
using System;
using FSpot;
using FSpot.Utils;
using FSpot.Xmp;
using FSpot.UI.Dialog;
using System.IO;
using Mono.Unix;
using Hyena;

public class ImportException : System.Exception {
	public ImportException (string msg) : base (msg)
	{
	}
}

public class FileImportBackend : ImportBackend {
	PhotoStore store;
	RollStore rolls = FSpot.App.Instance.Database.Rolls;
	TagStore tag_store = FSpot.App.Instance.Database.Tags;
	bool recurse;
	bool copy;
	bool detect_duplicates;
	SafeUri [] base_paths;
	Tag [] tags;
	Gtk.Window parent;

	bool cancel = false;
	int count;
	int duplicate_count;
	XmpTagsImporter xmptags;

	List<IBrowsableItem> import_info;
	Stack directories;

	private class ImportInfo : IBrowsableItem {
		public ImportInfo (SafeUri original)
		{
			DefaultVersion = new ImportInfoVersion () {
				BaseUri = original.GetBaseUri (),
				Filename = original.GetFilename ()
			};

			try {
				using (FSpot.ImageFile img = FSpot.ImageFile.Create (original)) {
					Time = img.Date;
				}
			} catch (Exception) {
				Time = DateTime.Now;
			}
		}

		public IBrowsableItemVersion DefaultVersion { get; private set; }
		public SafeUri DestinationUri { get; set; }

		public System.DateTime Time { get; private set; }
        
		public Tag [] Tags { get { throw new NotImplementedException (); } }
		public string Description { get { throw new NotImplementedException (); } }
		public string Name { get { throw new NotImplementedException (); } }
		public uint Rating { get { return 0; } }

		internal uint PhotoId { get; set; }
	}

	private class ImportInfoVersion : IBrowsableItemVersion {
		public string Name { get { return String.Empty; } }
		public bool IsProtected { get { return true; } }
		public SafeUri BaseUri { get; set; }
		public string Filename { get; set; }

		public SafeUri Uri { get { return BaseUri.Append (Filename); } }
	}

	public override List<IBrowsableItem> Prepare ()
	{
		if (import_info != null)
			throw new ImportException ("Busy");

		import_info = new List<IBrowsableItem> ();

		foreach (var uri in base_paths) {
			var enumerator = new RecursiveFileEnumerator (uri, recurse, true);
			foreach (var file in enumerator) {
				if (FSpot.ImageFile.HasLoader (new SafeUri (file.Uri, true)))
					import_info.Add (new ImportInfo (new SafeUri(file.Uri, true)));
			}
		}	

		directories = new Stack ();
		xmptags = new XmpTagsImporter (store, tag_store);

		roll = rolls.Create ();
		Photo.ResetMD5Cache ();

		return import_info;
	}
	

	public static SafeUri UniqueName (string path, string filename)
	{
		int i = 1;
		string dest = System.IO.Path.Combine (path, filename);

		while (System.IO.File.Exists (dest)) {
			string numbered_name = String.Format ("{0}-{1}{2}", 
							      System.IO.Path.GetFileNameWithoutExtension (filename),
							      i++,
							      System.IO.Path.GetExtension (filename));
			
			dest = System.IO.Path.Combine (path, numbered_name);
		}
		
		return new SafeUri ("file://"+dest, true);
	}
	
	public static SafeUri ChooseLocation (SafeUri uri)
	{
		return ChooseLocation (uri, null);
	}

	private static SafeUri ChooseLocation (SafeUri uri, Stack created_directories)
	{
		string name = uri.GetFilename ();
		DateTime time;
		using (FSpot.ImageFile img = FSpot.ImageFile.Create (uri)) {
			time = img.Date;
		}

		string dest_dir = String.Format ("{0}{1}{2}{1}{3:D2}{1}{4:D2}",
						 FSpot.Global.PhotoDirectory,
						 System.IO.Path.DirectorySeparatorChar,
						 time.Year,
						 time.Month,
						 time.Day);
		
		if (!System.IO.Directory.Exists (dest_dir)) {
			System.IO.DirectoryInfo info;
			// Split dest_dir into constituent parts so we can clean up each individual directory in
			// event of a cancel.
			if (created_directories != null) {
				string [] parts = dest_dir.Split (new char [] {System.IO.Path.DirectorySeparatorChar});
				string nextPath = String.Empty;
				for (int i = 0; i < parts.Length; i++) {
					if (i == 0)
						nextPath += parts [i];
					else
						nextPath += System.IO.Path.DirectorySeparatorChar + parts [i];
					if (nextPath.Length > 0) {
						info = new System.IO.DirectoryInfo (nextPath);
						// only add the directory path if it didn't already exist and we haven't already added it.
						if (!info.Exists && !created_directories.Contains (nextPath))
							created_directories.Push (nextPath);
					}
				}
			}
			
			info = System.IO.Directory.CreateDirectory (dest_dir);
		}

		// If the destination we'd like to use is the file itself return that
		if ("file://" + Path.Combine (dest_dir, name) == uri.ToString ())
			return uri;
		 
		var dest = UniqueName (dest_dir, name);
		
		return dest;
	}

	public override bool Step (out StepStatusInfo status_info)
	{
		Photo photo = null;
		bool is_duplicate = false;

		if (import_info == null)
			throw new ImportException ("Prepare() was not called");

		if (this.count == import_info.Count)
			throw new ImportException ("Already finished");

		// FIXME Need to get the EXIF info etc.
		ImportInfo info = (ImportInfo)import_info [this.count];
		bool needs_commit = false;
		bool abort = false;
		try {
			var destination = info.DefaultVersion.Uri;
			if (copy)
				destination = ChooseLocation (info.DefaultVersion.Uri, directories);

			// Don't copy if we are already home
			if (info.DefaultVersion.Uri == destination) {
				info.DestinationUri = destination;

				if (detect_duplicates)
					photo = store.CheckForDuplicate (destination);

				if (photo == null)
					photo = store.Create (info.DestinationUri, roll.Id);
				else
				 	is_duplicate = true;
			} else {
				var file = GLib.FileFactory.NewForUri (info.DefaultVersion.Uri);
				var new_file = GLib.FileFactory.NewForUri (destination);
				file.Copy (new_file, GLib.FileCopyFlags.AllMetadata, null, null);
				info.DestinationUri = destination;

				if (detect_duplicates)
					photo = store.CheckForDuplicate (destination);

				if (photo == null)
				{
					photo = store.Create (info.DestinationUri,
					                      info.DefaultVersion.Uri,
					                      roll.Id);
				}
				else
				{
					is_duplicate = true; 
					new_file.Delete (null);
				}
			} 

			if (!is_duplicate)
			{
				if (tags != null) {
					foreach (Tag t in tags) {
						photo.AddTag (t);
					}
					needs_commit = true;
				}

				needs_commit |= xmptags.Import (photo, info.DestinationUri, info.DefaultVersion.Uri);

				if (needs_commit)
					store.Commit(photo);

                info.PhotoId = photo.Id;
			}
		} catch (System.Exception e) {
			System.Console.WriteLine ("Error importing {0}{2}{1}", info.DestinationUri.ToString (), e.ToString (), Environment.NewLine);
			photo = null;

			HigMessageDialog errordialog = new HigMessageDialog (parent,
									     Gtk.DialogFlags.Modal | Gtk.DialogFlags.DestroyWithParent,
									     Gtk.MessageType.Error,
									     Gtk.ButtonsType.Cancel,
									     Catalog.GetString ("Import error"),
									     String.Format(Catalog.GetString ("Error importing {0}{2}{2}{1}"), info.DefaultVersion.Uri.ToString (), e.Message, Environment.NewLine ));
			errordialog.AddButton (Catalog.GetString ("Skip"), Gtk.ResponseType.Reject, false);
			ResponseType response = (ResponseType) errordialog.Run ();
			errordialog.Destroy ();
			if (response == ResponseType.Cancel)
				abort = true;
		}

		this.count ++;

		if (is_duplicate)
			this.duplicate_count ++;

		status_info = new StepStatusInfo (photo, this.count, is_duplicate);

		if (cancel) {
			abort = true;
		}

		return (!abort && count != import_info.Count);
	}

	public override void Cancel ()
	{
		if (import_info == null)
			throw new ImportException ("Not doing anything");

		foreach (var item in import_info) {
            ImportInfo info = item as ImportInfo;
			
			if (info.DefaultVersion.Uri != info.DestinationUri && info.DestinationUri != null) {
				try {
					var file = GLib.FileFactory.NewForUri (info.DestinationUri);
					if (file.QueryExists (null))
						file.Delete (null);
				} catch (System.ArgumentNullException) {
					// Do nothing, since if DestinationUri == null, we do not have to remove it
				} catch (System.Exception e) {
					System.Console.WriteLine (e);
				}
			}
			
			if (info.PhotoId != 0)
				store.Remove (store.Get (info.PhotoId));
		}
		
		// clean up all the directories we created.
		if (copy) {
			string path;
			System.IO.DirectoryInfo info;
			while (directories.Count > 0) {
				path = directories.Pop () as string;
				info = new System.IO.DirectoryInfo (path);
				// double check we aren't trying to delete a directory that still contains something!
				if (info.Exists && info.GetFiles().Length == 0 && info.GetDirectories().Length == 0)
					info.Delete ();
			}
		}
		// Clean up just created tags
		xmptags.Cancel();

		rolls.Remove (roll);
	}

	public override void Finish ()
	{
		if (import_info == null)
			throw new ImportException ("Not doing anything");

		foreach (ImportInfo info in import_info) {
			if (info.PhotoId != 0) 
				FSpot.ThumbnailGenerator.Default.Request (store.Get (info.PhotoId).DefaultVersion.Uri, 0, 256, 256);
		}

		import_info = null;
		xmptags.Finish();
		Photo.ResetMD5Cache ();

		if (count == duplicate_count)
			rolls.Remove (roll);

		count = duplicate_count = 0;
		//rolls.EndImport();    // Clean up the imported session.
	}

	public FileImportBackend (PhotoStore store, SafeUri [] base_paths, bool recurse, Gtk.Window parent) : this (store, base_paths, false, recurse, false, null, parent) {}

	public FileImportBackend (PhotoStore store, SafeUri [] base_paths, bool copy, bool recurse, Tag [] tags, Gtk.Window parent) : this (store, base_paths, copy, recurse, false, null, parent) {}

	public FileImportBackend (PhotoStore store, SafeUri [] base_paths, bool copy, bool recurse, bool detect_duplicates, Tag [] tags, Gtk.Window parent)
	{
		this.store = store;
		this.copy = copy;
		this.base_paths = base_paths;
		this.recurse = recurse;
		this.detect_duplicates = detect_duplicates;
		this.tags = tags;
		this.parent = parent;
	}
}
