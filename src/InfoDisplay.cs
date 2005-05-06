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
				bool empty = true;
				foreach (Exif.ExifContent content in exif_info.GetContents ()) {
					Exif.ExifEntry [] entries = content.GetEntries ();
					if (entries.Length > 0) {
						empty = false;
						break;
					}
				}
				
				stream.Write ("<table width=100% cellpadding=5 cellspacing=0>");
				if (exif_info.Data.Length > 0)
					stream.Write ("<tr><td colspan=2 align=\"center\" bgcolor=\"" + ig + "\"><img center src=\"exif:thumbnail\"></td></tr>");
				if (!empty) {
					foreach (Exif.ExifContent content in exif_info.GetContents ()) {
						Exif.ExifEntry [] entries = content.GetEntries ();
						
						i++;
						if (entries.Length < 1)
							continue;
						
						stream.Write ("<tr><th align=left bgcolor=\"" + ig + "\" colspan=2>" 
							      + Exif.ExifUtil.GetIfdNameExtended ((Exif.Ifd)i - 1) + "</th><tr>");
						
						foreach (Exif.ExifEntry entry in entries) {
							stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
							if (entry.Title != null)
								stream.Write (entry.Title);
							else
								stream.Write ("&lt;Unknown Tag ID=" + entry.Tag.ToString () + "&gt;");
							stream.Write ("</font></td><td>");
							string s = entry.Value;
							if (s != null && s != "")
							stream.Write (s);
							stream.Write ("</td><tr>");
						}
						
					}
				} else {
					stream.Write ("<table width=100% cellspacing=10 cellpadding=5 >");
					string msg = String.Format ("<tr><td valign=top align=center bgcolor=\"{0}\">" 
								    + "<b>{1}</b></td></tr>", ig,
								    Mono.Posix.Catalog.GetString ("No EXIF info available"));
					stream.Write (msg);
					stream.Write ("</table>");
				}
				End (stream, Gtk.HTMLStreamStatus.Ok);
			}
		}
	}
}

