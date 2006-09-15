using Gdk;
using Gtk;
using Gnome;
using System.Collections;
using System;
using FSpot;
using FSpot.Xmp;
using System.IO;

public class ImportException : System.Exception {
	public ImportException (string msg) : base (msg)
	{
	}
}

public class FileImportBackend : ImportBackend {
	PhotoStore store;
	TagStore tag_store = FSpot.Core.Database.Tags;
	bool recurse;
	bool copy;
	string [] base_paths;
	Tag [] tags;

	int count;
	XmpTagsImporter xmptags;

	ArrayList import_info;
	Stack directories;

	private class ImportInfo {
		string original_path;
		public string destination_path;
		public Photo Photo;
	       
		public string OriginalPath {
			get { return original_path; }
		}
		
		public string DestinationPath {
			get { return destination_path; }
			set { destination_path = value; }
		}
		
		public ImportInfo (string original)
		{
			original_path = original;
		        destination_path= null;
			Photo = null;
		}
	}
	
	private void AddPath (string path)
	{
		if (FSpot.ImageFile.HasLoader (path))
			import_info.Add (new ImportInfo (path));
	}

	private void GetListing (System.IO.DirectoryInfo info)
	{
		try {
			GetListing (info, info.GetFiles (), recurse);
		} catch (System.UnauthorizedAccessException e) {
			System.Console.WriteLine ("Unable to access directory {0}", info.FullName);
		} catch (System.Exception e) {
			System.Console.WriteLine ("{0}", e.ToString ());
		}
	}

	private void GetListing (System.IO.DirectoryInfo dirinfo, System.IO.FileInfo [] files, bool recurse)
	{
		System.Console.WriteLine ("Scanning {0}", dirinfo.FullName);
		Hashtable exiting_entries = new Hashtable ();

		foreach (Photo p in store.Query (dirinfo)) {
			foreach (uint id in p.VersionIds) {
				string name;
				if (id == Photo.OriginalVersionId)
				        name = p.Name;
				else 
					name = (new System.IO.FileInfo (p.GetVersionPath (id))).Name;

				exiting_entries [name] = p;
			}
		}
	
		foreach (System.IO.FileInfo f in files) {
			if (! exiting_entries.Contains (f.Name)) {
				AddPath (f.FullName);
			}
		}

		if (recurse) {
			foreach (System.IO.DirectoryInfo d in dirinfo.GetDirectories ()){
				if (!d.Name.StartsWith ("."))
					GetListing (d);
			}
		}
	}

	public override int Prepare ()
	{
		if (import_info != null)
			throw new ImportException ("Busy");

		import_info = new ArrayList ();

		foreach (string path in base_paths) {
			try {	
				if (System.IO.Directory.Exists (path))
					GetListing (new System.IO.DirectoryInfo (path));
				else if (System.IO.File.Exists (path))
					GetListing (new System.IO.DirectoryInfo (System.IO.Path.GetDirectoryName (path)), 
						    new System.IO.FileInfo [] { new System.IO.FileInfo (path)}, false);
			} catch (Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}	

		directories = new Stack ();
		xmptags = new XmpTagsImporter (store, tag_store);

		return import_info.Count;
	}

	public static string UniqueName (string path, string filename)
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
		
		return dest;
	}
	
	public static string ChooseLocation (string path)
	{
		return ChooseLocation (path, null);
	}

	public static string ChooseLocation (string path, Stack created_directories)
	{
		string name = System.IO.Path.GetFileName (path);
		FSpot.ImageFile img = FSpot.ImageFile.Create (path);
		DateTime time = img.Date;
		
		string dest_dir = String.Format ("{0}{1}{2}{1}{3}{1}{4}",
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
				string [] parts = dest_dir.Split (new char [] {'/'});
				string nextPath = "";
				for (int i = 0; i < parts.Length; i++) {
					if (i == 0)
						nextPath += parts [i];
					else
						nextPath += "/" + parts [i];
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
		if (Path.Combine (dest_dir, name) == path)
			return path;
		 
		string dest = UniqueName (dest_dir, name);
		
		return dest;
	}

	public override bool Step (out Photo photo, out Pixbuf thumbnail, out int count)
	{
		if (import_info == null)
			throw new ImportException ("Prepare() was not called");

		if (this.count == import_info.Count)
			throw new ImportException ("Already finished");

		// FIXME Need to get the EXIF info etc.
		ImportInfo info = (ImportInfo)import_info [this.count];
		bool needs_commit = false;

		try {
			string destination = info.OriginalPath;
			if (copy)
				destination = ChooseLocation (info.OriginalPath, directories);
			
			// Don't copy if we are already home
			if (info.OriginalPath == destination) {
				info.DestinationPath = destination;
				photo = store.Create (info.DestinationPath, out thumbnail);
			} else {
				System.IO.File.Copy (info.OriginalPath, destination);
				info.DestinationPath = destination;

				photo = store.Create (info.DestinationPath, info.OriginalPath, out thumbnail);

				try {
					File.SetAttributes (destination, File.GetAttributes (info.DestinationPath) & ~FileAttributes.ReadOnly);
					DateTime create = File.GetCreationTime (info.OriginalPath);
					File.SetCreationTime (info.DestinationPath, create);
					DateTime mod = File.GetLastWriteTime (info.OriginalPath);
					File.SetLastWriteTime (info.DestinationPath, mod);
				} catch (System.Exception e) {
					// we don't want an exception here to be fatal.
				}
			} 

			if (tags != null) {
				foreach (Tag t in tags) {
					photo.AddTag (t);
				}
				needs_commit = true;
			}

			needs_commit |= xmptags.Import (photo, info.DestinationPath, info.OriginalPath);

			if (needs_commit)
				store.Commit(photo);
			
			info.Photo = photo;
		} catch (System.Exception e) {
			System.Console.WriteLine ("Error importing {0}\n{1}", info.OriginalPath, e.ToString ());
			thumbnail = null;
			photo = null;
		}

		this.count ++;
		count = this.count;

		return count != import_info.Count;
	}

	public override void Cancel ()
	{
		if (import_info == null)
			throw new ImportException ("Not doing anything");

		foreach (ImportInfo info in import_info) {
			
			if (info.OriginalPath != info.DestinationPath) {
				try {
					System.IO.File.Delete (info.DestinationPath);
				} catch (System.Exception e) {
					System.Console.WriteLine (e);
				}
			}
			
			if (info.Photo != null)
				store.Remove (info.Photo);
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
	}

	public override void Finish ()
	{
		if (import_info == null)
			throw new ImportException ("Not doing anything");

		foreach (ImportInfo info in import_info) {
			if (info.Photo != null) 
				FSpot.ThumbnailGenerator.Default.Request (info.Photo.DefaultVersionUri, 0, 256, 256);
		}

		import_info = null;
		xmptags.Finish();
		count = 0;
	}

	public FileImportBackend (PhotoStore store, string [] base_paths, bool recurse) : this (store, base_paths, false, recurse, null) {}

	public FileImportBackend (PhotoStore store, string [] base_paths, bool copy, bool recurse, Tag [] tags)
	{
		this.store = store;
		this.copy = copy;
		this.base_paths = base_paths;
		this.recurse = recurse;
		this.tags = tags;
	}

#if TEST_FILE_IMPORT_BACKEND

	public static void Main (string [] args)
	{
		Program program = new Program ("FileImportTest", "0.0", Modules.UI, args);

		const string path = "/tmp/FileImportTest.db";

		try {
			File.Delete (path);
		} catch {}

		Db db = new Db (path, true);

		FileImportBackend import = new FileImportBackend (db.Photos, args [0],true);

		Console.WriteLine ("Preparing...");

		int total_count = import.Prepare();
		if (total_count == 0)
			Console.WriteLine ("(No pictures)");

		Console.WriteLine ("Prepared: {0} picture(s)", total_count);

		bool ongoing;
		do {
			Photo photo;
			Pixbuf thumbnail;
			int count;

			ongoing = import.Step (out photo, out thumbnail, out count);

			Console.WriteLine ("{0}/{1} - {2}", count, total_count, photo.Path);
		} while (ongoing);

		import.Finish ();
	}

#endif
}
