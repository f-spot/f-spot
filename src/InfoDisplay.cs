using System;

namespace FSpot {
	public class InfoDisplay : Gtk.HTML {
		public InfoDisplay () 
		{

		}

		private Exif.ExifData exif_info;

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
					exif_info = new Exif.ExifData (photo.DefaultVersionPath);
				} else {
					exif_info = null;
				}
				this.Update ();
			}
		}
		
		protected override void OnStyleSet (Gtk.Style previous)
		{
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
				
				if (data.Length > 0) {
					stream.Write (data, data.Length);
					stream.Close (Gtk.HTMLStreamStatus.Ok);
				} else 
					stream.Close (Gtk.HTMLStreamStatus.Error);
			} 
		}

		private void Update ()
		{
			Gtk.HTMLStream stream = this.Begin (null, "text/html; charset=utf-8", Gtk.HTMLBeginFlags.Scroll);
			
			string bg = Color (this.Style.Background (Gtk.StateType.Active));
			string fg = Color (this.Style.Foreground (Gtk.StateType.Active));
			string ig = Color (this.Style.Base (Gtk.StateType.Active));

			int i = 0;
			if (exif_info != null) {
				stream.Write ("<table width=100% cellspacing=0 cellpadding=3>");
				stream.Write ("<tr><td colspan=2 align=\"center\" bgcolor=\"" + ig + "\"><img center src=\"exif:thumbnail\"></td></tr>");
				foreach (Exif.ExifContent content in exif_info.GetContents ()) {
					stream.Write ("<tr><th align=center colspan=2>" + Exif.ExifUtil.GetIfdName ((Exif.ExifIfd)i++) + "</th><tr>");
					foreach (Exif.ExifEntry entry in content.GetEntries ()) {
						stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
						stream.Write (entry.Title);
						stream.Write ("</font></td><td>");
						string s = entry.Value;
						if (s != null && s != "")
							stream.Write (s);
						stream.Write ("</td><tr>");
					}
				}
				stream.Write ("</table>");
			}

			End (stream, Gtk.HTMLStreamStatus.Ok);
		}
	}
}
