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

				if (photo != null) {
					exif_info = new ExifData (photo.DefaultVersionPath);
					exif_info.Assemble ();
				} else {
					exif_info = null;
				}
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
			string value =  r.ToString ("x2") + g.ToString ("x2") + b.ToString ("x2");
			return value;
		}

		protected override void OnUrlRequested (string url, Gtk.HTMLStream stream)
		{
			if (url == "exif:thumbnail") {
				byte [] data = exif_info.Data;
				
				stream.Write (data, data.Length);
				stream.Close (Gtk.HTMLStreamStatus.Ok);
			}
		}

		private void Update ()
		{
			Gtk.HTMLStream stream = this.Begin ("text/html; charset=utf-8");
			
			string bg = Color (this.Style.Background (Gtk.StateType.Active));
			string fg = Color (this.Style.Foreground (Gtk.StateType.Active));

			if (exif_info != null) {
				stream.Write ("<table width=100% cellspacing=0 cellpadding=3>");
				stream.Write ("<tr><td colspan=2 align=\"center\" bgcolor=\"" + bg + "\"><img center src=\"exif:thumbnail\"></td></tr>");
				foreach (ExifTag tag in exif_info.Tags) {
					stream.Write ("<tr><td bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
					stream.Write (ExifUtil.GetTagTitle (tag));
					stream.Write ("</font></td><td>");
					if (exif_info.LookupString (tag) != "")
						stream.Write (exif_info.LookupString (tag));
					stream.Write ("</td><tr>");
				}
				stream.Write ("</table>");
			}

			End (stream, Gtk.HTMLStreamStatus.Ok);
		}
	}
}
