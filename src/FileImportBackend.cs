using Gdk;
using Gtk;
using Gnome;
using System.Collections;
using System.IO;
using System;

public class FileImportBackend : ImportBackend {
	PhotoStore store;
	bool recurse;
	string base_path;

	int count;

	ArrayList file_paths;
	ArrayList imported_photos;

	private void GetListing (DirectoryInfo info)
	{
		FileInfo [] files = info.GetFiles ();

		foreach (FileInfo f in files) {
			string path = f.FullName;

			if (path.EndsWith (".jpg") || path.EndsWith (".JPG"))
				file_paths.Add (path);
		}

		if (recurse) {
			foreach (DirectoryInfo d in info.GetDirectories ())
				GetListing (d);
		}
	}

	public override int Prepare ()
	{
		if (file_paths != null)
			throw new Exception ("Busy");

		file_paths = new ArrayList ();

		try {
			GetListing (new DirectoryInfo (base_path));
		} catch {
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
		photo = store.Create (DateTime.Now, file_paths [this.count] as string, out thumbnail);
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

	public FileImportBackend (PhotoStore store, string base_path, bool recurse)
	{
		this.store = store;
		this.base_path = base_path;
		this.recurse = recurse;
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
