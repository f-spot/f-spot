using System;

namespace FSpot {
	public class InfoDisplay : Gtk.HTML {
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
		
		protected override void OnStyleSet (Gtk.Style previous)
		{
			base.OnStyleSet (previous);
			this.Update ();
		}
		
		private string Color (Gdk.Color color)
		{
			Byte r = (byte)(color.Red / 256);
			Byte b = (byte)(color.Blue / 256);
			Byte g = (byte)(color.Green / 256);
			string value =  r.ToString ("x") + g.ToString ("x") + b.ToString ("x");
			System.Console.WriteLine (value);
			return value;
		}

		private void Update ()
		{
			ExifTag [] tags = exif_info.Tags;
			Gtk.HTMLStream stream = this.Begin ("text/html; charset=utf-8");
			
			string bg = Color (this.Style.Base (Gtk.StateType.Insensitive));
			string fg = Color (this.Style.Text (Gtk.StateType.Insensitive));

			stream.Write ("<table width=100%>");
			System.Console.WriteLine (tags.Length);
			foreach (ExifTag tag in tags) {
				stream.Write ("<tr><td bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
				stream.Write (ExifUtil.GetTagTitle (tag));
				stream.Write ("</font></td><td>");
				if (exif_info.LookupString (tag) != "")
					stream.Write (exif_info.LookupString (tag));
				stream.Write ("</td><tr>");
			}
			stream.Write ("</table>");
			End (stream, Gtk.HTMLStreamStatus.Ok);
		}
	}
}
