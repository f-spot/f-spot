using GtkSharp;
using Gtk;

namespace FSpot {
	public class InfoDisplay : HTML {
		public InfoDisplay () 
		{

		}

		private ExifData exif_info;

		private Photo photo;
		public Photo Photo {
			get {
				return photo;
			}
			set {
				photo = value;

				if (exif_info != null)
					exif_info.Dispose ();

				exif_info = new ExifData (photo.DefaultVersionPath);
				exif_info.Assemble ();
				this.Update ();
			}
		}
		
		private void Update ()
		{
			ExifTag [] tags = exif_info.Tags;
			HTMLStream stream = this.Begin ("text/html; charset=utf-8");
			stream.Write ("<table width=100%>");
			System.Console.WriteLine (tags.Length);
			foreach (ExifTag tag in tags) {
				stream.Write ("<tr><td bgcolor=\"cccccc\">");
				stream.Write (ExifUtil.GetTagName (tag));
				stream.Write ("</td><td>");
				stream.Write (exif_info.LookupString (tag));
				stream.Write ("</td><tr>");
			}
			stream.Write ("</table>");
			End (stream, HTMLStreamStatus.Ok);
		}
	}
}
