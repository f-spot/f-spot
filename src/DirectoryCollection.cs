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

		public bool Contains (IBrowsableItem item)
		{
			return IndexOf (item) >= 0;
		}

		// IBrowsableCollection
		public IBrowsableItem [] Items {
			get {
				return items;
			}
		}

		public IBrowsableItem this [int index] {
			get {
				return items [index];
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

		public void MarkChanged (int num)
		{
			if (this.ItemChanged != null)
				this.ItemChanged (this, num);
		}

		void LoadItems () {
			// FIXME this should probably actually throw and exception
			// if the directory doesn't exist.
			if (System.IO.Directory.Exists (path)) {
				System.Collections.ArrayList images = new System.Collections.ArrayList ();

				System.IO.DirectoryInfo info = new System.IO.DirectoryInfo (path);
				System.IO.FileInfo [] files = info.GetFiles ();
				foreach (System.IO.FileInfo f in files) {
					switch (System.IO.Path.GetExtension (f.FullName)) {
					case ".jpeg":
					case ".jpg":
					case ".png":
					case ".cr2":
					case ".nef":
					case ".tiff":
					case ".tif":
					case ".dng":
					case ".crw":
					case ".ppm":
						System.Console.WriteLine (f.FullName);
						images.Add (new FileBrowsableItem (f.FullName));
						break;
					}
				}
				
				items = images.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem [];
			} else if (System.IO.File.Exists (path)) {
				items = new FileBrowsableItem [] { new FileBrowsableItem (path) };
			} else {
				items = new FileBrowsableItem [0];
			}
		}

	}

	public class FileBrowsableItem : IBrowsableItem {
		ImageFile img;
		string path;
		bool attempted;

		public FileBrowsableItem (string path)
		{
			this.path = path;
		}
		
		protected ImageFile Image {
			get {
				if (!attempted) {
					img = ImageFile.Create (path);
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

		public System.DateTime Time {
			get {
				return Image.Date ();
			}
		}
		
		public System.Uri DefaultVersionUri {
			get {
				return UriList.PathToFileUri (path);
			}
		}

		public string Description {
			get {
				if (Image is JpegFile) 
					return ((JpegFile)Image).Description;
				else
					return null;
			}
		}	

		public string Name {
			get {
				return System.IO.Path.GetFileName (Image.Path);
			}
		}
	}
}
