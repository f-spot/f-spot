namespace FSpot {
	public class DirectoryCollection : IBrowsableCollection {
		string path;
		FileBrowsableItem [] items;

		public DirectoryCollection (string path)
		{
			this.path = path;
			LoadItems ();
		}

		// IBrowsableCollection
		public IBrowsableItem [] Items {
			get {
				return items;
			}
		}

		public event FSpot.IBrowsableCollectionChangedHandler Changed;
		public event FSpot.IBrowsableCollectionItemChangedHandler ItemChanged;

		// Methods
		public string Path {
			get {
				return path;
			}
			set {
				path = value;
				LoadItems ();
			}
		}

		void LoadItems () {
			System.Collections.ArrayList images = new System.Collections.ArrayList ();

			System.IO.DirectoryInfo info = new System.IO.DirectoryInfo (path);
			System.IO.FileInfo [] files = info.GetFiles ();
			foreach (System.IO.FileInfo f in files) {
				System.Console.WriteLine (f.FullName);

				images.Add (new FileBrowsableItem (f.FullName));
			}
			
			items = images.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem [];
		}

	}

	public class FileBrowsableItem : IBrowsableItem {
		string path;
		public FileBrowsableItem (string path)
		{
			this.path = path;
		}
		
		public Tag [] Tags {
			get {
				return null;
			}
		}

		public System.DateTime Time {
			get {
				return System.IO.File.GetLastWriteTime (path);
			}
		}
		
		public System.Uri DefaultVersionUri {
			get {
				return UriList.PathToFileUri (path);
			}
		}

		public string Description {
			get {
				ExifData data = new ExifData (path);
				if (data != null)
					return data.LookupString (ExifTag.ImageDescription);
				else
					return null;
			}
		}	

		public string Name {
			get {
				return System.IO.Path.GetFileName (path);
			}
		}
	}
}
