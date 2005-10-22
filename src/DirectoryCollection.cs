using System;
using System.IO;
using System.Collections;

namespace FSpot {
	public class DirectoryCollection : FileCollection {
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
			}
		}

		void Load () {
			// FIXME this should probably actually throw and exception
			// if the directory doesn't exist.

			if (Directory.Exists (path)) {
				DirectoryInfo info = new DirectoryInfo (path);

				LoadItems (info.GetFiles ());
			} else if (File.Exists (path)) {
				items = new FileBrowsableItem [] { new FileBrowsableItem (path) };
			} else {
				items = new FileBrowsableItem [0];
			}
		}
	}

	public class FileCollection : IBrowsableCollection {
		protected FileBrowsableItem [] items;

		protected FileCollection ()
		{

		}

		public FileCollection (FileInfo [] files)
		{
			LoadItems (files);
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

		public int IndexOf (IBrowsableItem item)
		{
			return Array.IndexOf (items, item);
		}

		public void MarkChanged (int num)
		{
			if (this.ItemChanged != null)
				this.ItemChanged (this, num);
		}


		protected void LoadItems (FileInfo [] files) {
			ArrayList images = new ArrayList ();

			foreach (FileInfo f in files) {
				if (FSpot.ImageFile.HasLoader (f.FullName)) {
					Console.WriteLine (f.FullName);
					images.Add (new FileBrowsableItem (f.FullName));
				}
			}
				
			items = images.ToArray (typeof (FileBrowsableItem)) as FileBrowsableItem [];
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

		public DateTime Time {
			get {
				return Image.Date;
			}
		}
		
		public Uri DefaultVersionUri {
			get {
				return UriList.PathToFileUri (path);
			}
		}

		public string Description {
			get {
				if (Image != null)
					return Image.Description;
				else 
					return null;
			}
		}	

		public string Name {
			get {
				return Path.GetFileName (Image.Path);
			}
		}
	}
}
