using System;
using System.IO;
using System.Collections;

namespace FSpot {
	public class DirectoryCollection : UriCollection {
		string path;

		public DirectoryCollection (string path)
		{
			this.path = path;
			Load ();
		}

		// Methods
		public string Path {
			get {
				return path;
			}
			set {
				path = value;
				Load ();
				System.Console.WriteLine ("XXXXX after load");
			}
		}

		void Load () {
			// FIXME this should probably actually throw and exception
			// if the directory doesn't exist.

			if (Directory.Exists (path)) {
				DirectoryInfo info = new DirectoryInfo (path);
				LoadItems (info.GetFiles ());
			} else if (File.Exists (path)) {
				list.Clear ();
				Add (new FileBrowsableItem (path));
			}
		}
	}

	public class UriCollection : PhotoList {
		protected UriCollection () : base (new IBrowsableItem [0])
		{
		}

		public UriCollection (FileInfo [] files) : this ()
		{
			LoadItems (files);
		}

		public UriCollection (Uri [] uri) : this ()
		{
			LoadItems (uri);
		}

		public void LoadItems (Uri [] uris)
		{
			foreach (Uri uri in uris) {

				if (FSpot.ImageFile.HasLoader (uri)) {
					Console.WriteLine (uri.ToString ());
					Add (new FileBrowsableItem (uri));
				} else {
					Gnome.Vfs.FileInfo info = new Gnome.Vfs.FileInfo (uri.ToString ());
					
					if (info.Type == Gnome.Vfs.FileType.Directory)
						new DirectoryLoader (this, uri);
				}
			}
		}
		
		private class DirectoryLoader 
		{
			UriCollection collection;
			Uri uri;

			public DirectoryLoader (UriCollection collection, System.Uri uri)
			{
				this.collection = collection;
				this.uri = uri;
				Gnome.Vfs.Directory.GetEntries (uri.ToString (),
								Gnome.Vfs.FileInfoOptions.Default,
								20, 
								(int)Gnome.Vfs.Async.Priority.Default,
								InfoLoaded);
			}
			
			private void InfoLoaded (Gnome.Vfs.Result result, Gnome.Vfs.FileInfo []info, uint entries_read)
			{
				if (result != Gnome.Vfs.Result.Ok && result != Gnome.Vfs.Result.ErrorEof)
					return;

				ArrayList items = new ArrayList ();

				for (int i = 0; i < entries_read; i++) {
					Gnome.Vfs.Uri vfs = new Gnome.Vfs.Uri (uri.ToString ());
					vfs = vfs.AppendFileName (info [i].Name);
					Uri file = new Uri (vfs.ToString ());
					System.Console.WriteLine ("tesing uri = {0}", file.ToString ());
					
					if (FSpot.ImageFile.HasLoader (file))
						items.Add (new FileBrowsableItem (file));
				}
				
				collection.Add (items.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem []);
			}
		}

		protected void LoadItems (FileInfo [] files) 
		{
			ArrayList items = new ArrayList ();
			foreach (FileInfo f in files) {
				if (FSpot.ImageFile.HasLoader (f.FullName)) {
					Console.WriteLine (f.FullName);
					items.Add (new FileBrowsableItem (f.FullName));
				}
			}

			list = items;
			this.Reload ();
		}
	}

	public class FileBrowsableItem : IBrowsableItem {
		ImageFile img;
		Uri uri;
		bool attempted;
		
		public FileBrowsableItem (Uri uri)
		{
			this.uri = uri;
		}

		public FileBrowsableItem (string path)
		{
			this.uri = UriList.PathToFileUri (path);
		}
		
		protected ImageFile Image {
			get {
				if (!attempted) {
					img = ImageFile.Create (uri);
					attempted = true;
				}

				return img;
			}
		}

		public Tag [] Tags {
			get {
				return null;
			}
		}

		public DateTime Time {
			get {
				return Image.Date;
			}
		}
		
		public Uri DefaultVersionUri {
			get {
				return uri;
			}
		}

		public string Description {
			get {
				try {
					if (Image != null)
						return Image.Description;

				} catch (System.Exception e) {
					System.Console.WriteLine (e);
				}

				return null;
			}
		}	

		public string Name {
			get {
				return Path.GetFileName (Image.Uri.AbsolutePath);
			}
		}
	}
}
