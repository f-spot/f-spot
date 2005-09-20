using System;
using SemWeb;

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
		
		private static string Color (Gdk.Color color)
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

			stream.Write ("<table width=100% cellpadding=5 cellspacing=0>");
			bool empty = true;

			if (exif_info != null) {
				foreach (Exif.ExifContent content in exif_info.GetContents ()) {
					Exif.ExifEntry [] entries = content.GetEntries ();
					if (entries.Length > 0) {
						empty = false;
						break;
					}
				}

				if (exif_info.Data.Length > 0)
					stream.Write ("<tr><td colspan=2 align=\"center\" bgcolor=\"" + ig + "\"><img center src=\"exif:thumbnail\"></td></tr>");

				int i = 0;
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
				}
			}

			if (photo != null) {
				ImageFile img = ImageFile.Create (photo.DefaultVersionPath);
				if (img is SemWeb.StatementSource) {
					StatementSource source = (StatementSource)img;
					empty = false;
					stream.Write ("<tr><th align=left bgcolor=\"" + ig + "\" colspan=2>" 
						      + Mono.Posix.Catalog.GetString ("Extended Metadata") + "</th><tr>");
					MetadataStore store = new MetadataStore ();
					source.Select (store);
					foreach (Statement stmt in store) {
						if (stmt.Subject.Uri == null)
							continue;

						string predicate = stmt.Predicate.ToString ();
						string path = System.IO.Path.GetDirectoryName (predicate);
						string title = System.IO.Path.GetFileName (predicate);

						stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
						stream.Write (title);
						stream.Write ("</font></td><td>");
						
						string s = stmt.Object.ToString ();
						if (stmt.Object is SemWeb.Literal) {
							s = ((SemWeb.Literal)(stmt.Object)).Value;
						} else {
							MemoryStore substore = store.Select (new Statement ((Entity)stmt.Object, null, null, null));
							s = "";
							foreach (Statement sub in substore) {
								if (sub.Object is Literal) {
									s += ((Literal)(sub.Object)).Value + "<br>";
								}
							}
						}

						if (s != null && s != "")
							stream.Write (s);

						stream.Write ("</td><tr>");
					}
				}
			}
			
			if (empty) {
				string msg = String.Format ("<tr><td valign=top align=center bgcolor=\"{0}\">" 
							    + "<b>{1}</b></td></tr>", ig,
							    Mono.Posix.Catalog.GetString ("No EXIF info available"));
				stream.Write (msg);
			}

			stream.Write ("</table>");
			End (stream, Gtk.HTMLStreamStatus.Ok);
		}

		private class StreamSink : SemWeb.StatementSink
		{
			Gtk.HTMLStream stream;
			SemWeb.StatementSource source;
			InfoDisplay info;

			public StreamSink (SemWeb.StatementSource source, Gtk.HTMLStream stream, InfoDisplay info)
			{
				this.stream = stream;
				this.info = info;
				this.source = source;
			}

			public bool Add (SemWeb.Statement stmt)
			{
				string predicate = stmt.Predicate.ToString ();
				string path = System.IO.Path.GetDirectoryName (predicate);
				string title = System.IO.Path.GetFileName (predicate);
				string bg = InfoDisplay.Color (info.Style.Background (Gtk.StateType.Active));
				string fg = InfoDisplay.Color (info.Style.Foreground (Gtk.StateType.Active));

				//if (MetadataStore.Namespaces.GetPrefix (path) != null) {
				if (stmt.Object is Literal) {
					stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
					stream.Write (title);
					stream.Write ("</font></td><td>");

					string s = stmt.Object.ToString ();
					if (stmt.Object is SemWeb.Literal) {
						s = ((SemWeb.Literal)(stmt.Object)).Value;
					} 
					/*
					else {
						MemoryStore store = source.Select (stmt.Invert (), new SelectPartialFilter (true, false, false, false));
						s = "";
						foreach (Statement sub in store) {
							s += sub.Object.ToString () + "/n";
						}
					}
					*/
					if (s != null && s != "")
						stream.Write (s);
					stream.Write ("</td><tr>");
				}
				return true;
			}

		}
	}
}

