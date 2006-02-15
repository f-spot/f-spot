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

	public class FileCollection : PhotoList {
		protected FileCollection () : base (new IBrowsableItem [0])
		{
		}

		public FileCollection (FileInfo [] files) : this ()
		{
			LoadItems (files);
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
