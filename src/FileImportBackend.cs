using Gdk;
using Gtk;
using Gnome;
using System.Collections;
using System;

public class FileImportBackend : ImportBackend {
	PhotoStore store;
	bool recurse;
	string [] base_paths;
	Tag [] tags;

	int count;

	ArrayList file_paths;
	ArrayList imported_photos;

	private void AddPath (string path)
	{
		if (path.ToLower().EndsWith (".jpg") || path.ToLower().EndsWith (".jpeg"))
			file_paths.Add (path);

	}
	
	private void GetListing (System.IO.DirectoryInfo info)
	{
		System.Console.WriteLine ("Scanning {0}", info.FullName);

		System.IO.FileInfo [] files = info.GetFiles ();
		Hashtable exiting_entries = new Hashtable ();

		foreach (Photo p in store.Query (info)) {
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
			if (exiting_entries [f.Name] == null) {
				AddPath (f.FullName);
			}
		}

		if (recurse) {
			foreach (System.IO.DirectoryInfo d in info.GetDirectories ()){
				if (!d.Name.StartsWith ("."))
					GetListing (d);
			}
		}
	}

	public override int Prepare ()
	{
		if (file_paths != null)
			throw new Exception ("Busy");

		file_paths = new ArrayList ();

		foreach (string path in base_paths) {
			try {	
				GetListing (new System.IO.DirectoryInfo (path));
			} catch (Exception e) {
				System.Console.WriteLine (e.ToString ());
				AddPath (path);
			}
		}	

		if (file_paths.Count == 0) {
			file_paths = null;
			return 0;
		}

		imported_photos = new ArrayList ();

		return file_paths.Count;
	}

	public override bool Step (out Photo photo, out Pixbuf thumbnail, out int count)
	{
		if (file_paths == null)
			throw new Exception ("Prepare() was not called");

		if (this.count == file_paths.Count)
			throw new Exception ("Already finished");

		// FIXME Need to get the EXIF info etc.
		photo = store.Create (file_paths [this.count] as string, out thumbnail);
		
		if (tags != null) {
			foreach (Tag t in tags) {
				photo.AddTag (t);
			}
			store.Commit(photo);
		}
		
		imported_photos.Add (photo);

		this.count ++;
		count = this.count;

		return count != file_paths.Count;
	}

	public override void Cancel ()
	{
		if (imported_photos == null)
			throw new Exception ("Not doing anything");

		foreach (Photo p in imported_photos)
			store.Remove (p);

		Finish ();
	}

	public override void Finish ()
	{
		if (file_paths == null)
			throw new Exception ("Not doing anything");

		file_paths = null;
		imported_photos = null;
		count = 0;
	}

	public FileImportBackend (PhotoStore store, string [] base_paths, bool recurse) : this (store, base_paths, recurse, null) {}

	public FileImportBackend (PhotoStore store, string [] base_paths, bool recurse, Tag [] tags)
	{
		this.store = store;
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

		FileImportBackend import = new FileImportBackend (db.Photos, args [0], true);

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
