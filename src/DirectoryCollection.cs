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
				items.Clear ();
				items.Add (new FileBrowsableItem (path)) ;
				Reload ();
			}
		}
	}

	public class FileCollection : IBrowsableCollection {
		protected ArrayList items;

		protected FileCollection ()
		{
			items = new ArrayList ();
		}

		public FileCollection (FileInfo [] files)
		{
			LoadItems (files);
		}

		public int Count {
			get {
				return items.Count;
			}
		}

		public bool Contains (IBrowsableItem item)
		{
			return Contains (item);
		}

		// IBrowsableCollection
		public IBrowsableItem [] Items {
			get {
				return items.ToArray (typeof (IBrowsableItem)) as IBrowsableItem [];
			}
		}

		public IBrowsableItem this [int index] {
			get { return (IBrowsableItem) items [index]; }
		}

		public event FSpot.IBrowsableCollectionChangedHandler Changed;
		public event FSpot.IBrowsableCollectionItemsChangedHandler ItemsChanged;

		public int IndexOf (IBrowsableItem item)
		{
			return IndexOf (item);
		}

		public void Reload ()
		{
			if (Changed != null)
				Changed (this);
		}
		
		public void MarkChanged (int num)
		{
			MarkChanged (new BrowsableArgs (num));
		}

		public void MarkChanged (BrowsableArgs args)
		{
			if (this.ItemsChanged != null)
				this.ItemsChanged (this, args);
		}

		protected void LoadItems (FileInfo [] files) {
			items.Clear ();
			foreach (FileInfo f in files) {
				if (FSpot.ImageFile.HasLoader (f.FullName)) {
					Console.WriteLine (f.FullName);
					items.Add (new FileBrowsableItem (f.FullName));
				}
			}

			this.Reload ();
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
