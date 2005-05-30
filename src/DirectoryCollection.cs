namespace FSpot {
	public class DirectoryCollection : IBrowsableCollection {
		string path;
		FileBrowsableItem [] items;

		public DirectoryCollection (string path)
		{
			this.path = path;
			LoadItems ();
		}

		public int Count {
			get {
				return items.Length;
			}
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

		public int IndexOf (IBrowsableItem item)
		{
			return System.Array.IndexOf (items, item);
		}

		void LoadItems () {
			// FIXME this should probably actually throw and exception
			// if the directory doesn't exist.
			if (System.IO.Directory.Exists (path)) {
				System.Collections.ArrayList images = new System.Collections.ArrayList ();

				System.IO.DirectoryInfo info = new System.IO.DirectoryInfo (path);
				System.IO.FileInfo [] files = info.GetFiles ();
				foreach (System.IO.FileInfo f in files) {
					if (System.IO.Path.GetExtension (f.FullName).ToLower () == ".png") {
						System.Console.WriteLine (f.FullName);
					
						images.Add (new FileBrowsableItem (f.FullName));
					}
				}
				
				items = images.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem [];
			} else {
				items = new FileBrowsableItem [0];
			}
		}

	}

	public class FileBrowsableItem : IBrowsableItem {
		ImageFile img;
		public FileBrowsableItem (string path)
		{
			this.img = ImageFile.Create (path);
		}
		
		public Tag [] Tags {
			get {
				return null;
			}
		}

		public System.DateTime Time {
			get {
				return img.Date ();
			}
		}
		
		public System.Uri DefaultVersionUri {
			get {
				return UriList.PathToFileUri (img.Path);
			}
		}

		public string Description {
			get {
				if (img is JpegFile) 
					return ((JpegFile)img).Description;
				else
					return null;
			}
		}	

		public string Name {
			get {
				return System.IO.Path.GetFileName (img.Path);
			}
		}
	}
}
