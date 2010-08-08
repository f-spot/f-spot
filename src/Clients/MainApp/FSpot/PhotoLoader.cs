using System;

using FSpot.Core;
using FSpot.Platform;
using FSpot.Imaging;
using Hyena;

namespace FSpot {
	[Obsolete ("nuke or rename this")]
	public class PhotoLoader {
		public PhotoQuery query;

		public Gdk.Pixbuf Load (int index) {
			return Load (query, index);
		}

		static public Gdk.Pixbuf Load (IBrowsableCollection collection, int index)
		{
			IBrowsableItem item = collection [index];
			return Load (item);
		}

		static public Gdk.Pixbuf Load (IBrowsableItem item)
		{
			using (var img = ImageFile.Create (item.DefaultVersion.Uri)) {
				Gdk.Pixbuf pixbuf = img.Load ();
				return pixbuf;
			}
		}

		static public Gdk.Pixbuf LoadAtMaxSize (IBrowsableItem item, int width, int height)
		{
			using (var img = ImageFile.Create (item.DefaultVersion.Uri)) {
				Gdk.Pixbuf pixbuf = img.Load (width, height);
				return pixbuf;
			}
		}

		public PhotoLoader (PhotoQuery query)
		{
			this.query = query;
		}
	}
}
