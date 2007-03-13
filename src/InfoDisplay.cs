using System;
using SemWeb;
using System.IO;

using Mono.Unix;

namespace FSpot {
	public class InfoDialog : Gtk.Dialog {
		InfoDisplay info_display;
		public InfoDisplay InfoDisplay {
			get { return info_display; }
		}

		public InfoDialog (Gtk.Window parent) : base (Catalog.GetString ("Metadata Browser"),
											    parent,
											    Gtk.DialogFlags.NoSeparator | Gtk.DialogFlags.DestroyWithParent)
		{
			info_display = new InfoDisplay ();
			SetDefaultSize (400, 400);
			Gtk.ScrolledWindow scrolled = new Gtk.ScrolledWindow ();
			VBox.PackStart (scrolled);
			scrolled.Add (info_display);
		}
	}


	public class InfoDisplay : Gtk.HTML {
		public InfoDisplay () 
		{

		}

		private Exif.ExifData exif_info;

		private IBrowsableItem photo;
		public IBrowsableItem Photo {
			get {
				return photo;
			}
			set {
				photo = value;

				if (exif_info != null)
					exif_info.Dispose ();

				if (photo != null) {
					if (File.Exists (photo.DefaultVersionUri.LocalPath))
						exif_info = new Exif.ExifData (photo.DefaultVersionUri.LocalPath);
				} else {
					exif_info = null;
				}
				this.Update ();
			}
		}

		protected override void OnLinkClicked (string url)
		{
                        if (url.StartsWith ("/")) {
                                // do a lame job of creating a URI out of local paths
                                url = "file://" + url;
                        }
                        
                        GnomeUtil.UrlShow (null, url);
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

		private static string Escape (string value)
		{
			if (value == null) 
				return "(null)";

			value = value.Replace ("&", "&amp;");
			value = value.Replace (">", "&gt;");
			value = value.Replace ("<", "&lt;");
			value = value.Replace ("\r\n", "<br>");
			value = value.Replace ("\n", "<br>");
			return value;
		}

		private static string GetExportUrl (ExportItem export)
		{
			switch (export.ExportType) {
			case ExportStore.FlickrExportType:
				string[] split_token = export.ExportToken.Split (':');
				return String.Format ("http://www.{0}/photos/{1}/{2}/", split_token[2],
                                                      split_token[0], split_token[3]);
			case ExportStore.FolderExportType:
				Gnome.Vfs.Uri uri = new Gnome.Vfs.Uri (export.ExportToken);
				return (uri.HasParent) ? uri.Parent.ToString () : export.ExportToken;
			case ExportStore.Gallery2ExportType:
				string[] split_item = export.ExportToken.Split (':');
				return String.Format ("{0}:{1}?g2_itemId={2}",split_item[0], split_item[1], split_item[2]);
			case ExportStore.OldFolderExportType:	//This is obsolete and meant to be removed once db reach rev4
			case ExportStore.PicasaExportType:
			case ExportStore.SmugMugExportType:
				return export.ExportToken;
			default:
				return null;
			}
		}

		private static string GetExportLabel (ExportItem export)
		{
			switch (export.ExportType) {
			case ExportStore.FlickrExportType:
				string[] split_token = export.ExportToken.Split (':');
				return String.Format ("Flickr ({0})", split_token[1]);
			case ExportStore.OldFolderExportType:	//Obsolete, remove after db rev4
				return Catalog.GetString ("Folder");
			case ExportStore.FolderExportType:
				return Catalog.GetString ("Folder");
			case ExportStore.PicasaExportType:
				return Catalog.GetString ("Picasaweb");
			case ExportStore.SmugMugExportType:
				return Catalog.GetString ("SmugMug");
			case ExportStore.Gallery2ExportType:
				return Catalog.GetString ("Gallery2");
			default:
				return null;
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
			bool missing = false;
			System.Exception error = null;

			if (exif_info != null) {
				foreach (Exif.ExifContent content in exif_info.GetContents ()) {
					Exif.ExifEntry [] entries = content.GetEntries ();
					if (entries.Length > 0) {
						empty = false;
						break;
					}
				}

				if (exif_info.Data.Length > 0)
					stream.Write (String.Format ("<tr><td colspan=2 align=\"center\" bgcolor=\"{0}\">" + 
								     "<img center src=\"exif:thumbnail\"></td></tr>", ig));

				int i = 0;
				foreach (Exif.ExifContent content in exif_info.GetContents ()) {
					Exif.ExifEntry [] entries = content.GetEntries ();
					
					i++;
					if (entries.Length < 1)
						continue;
					
					stream.Write ("<tr><th align=left bgcolor=\"" + ig + "\" colspan=2>" 
						      + Exif.ExifUtil.GetIfdNameExtended ((Exif.Ifd)i - 1) + "</th><tr>");
					
					foreach (Exif.ExifEntry entry in entries) {
						stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><small><font color=\"" + fg + "\">");
						if (entry.Title != null)
							stream.Write (entry.Title);
						else
							stream.Write ("&lt;Unknown Tag ID=" + entry.Tag.ToString () + "&gt;");
						stream.Write ("</font></small></td><td>");
						string s = entry.Value;
						if (s != null && s != String.Empty)
							stream.Write (s);
						stream.Write ("</td><tr>");
					}
				}
			}
			
			if (photo != null) {
				MetadataStore store = new MetadataStore ();
				try {
					using (ImageFile img = ImageFile.Create (photo.DefaultVersionUri)) {
						if (img is SemWeb.StatementSource) {
							StatementSource source = (StatementSource)img;
							source.Select (store);
						}
					}
				} catch (System.IO.FileNotFoundException) {
					missing = true;
				} catch (System.Exception e){
					// Sometimes we don't get the right exception, check for the file
					if (!System.IO.File.Exists (photo.DefaultVersionUri.LocalPath)) {
						missing = true;
					} else {
						// if the file is there but we still got an exception display it.
						error = e;
					}
				}
				
				if (store.StatementCount > 0) {
#if false
					using (System.IO.Stream xmpstream = System.IO.File.OpenWrite ("tmp.xmp")) {
						xmpstream.Length = 0;
						FSpot.Xmp.XmpFile file;

						file = new FSpot.Xmp.XmpFile ();
						store.Select (file);
						file.Save (xmpstream);
					}
#endif
					empty = false;
					stream.Write ("<tr><th align=left bgcolor=\"" + ig + "\" colspan=2>" 
						      + Catalog.GetString ("Extended Metadata") + "</th></tr>");
					
					foreach (Statement stmt in store) {
						// Skip anonymous subjects because they are
						// probably part of a collection
						if (stmt.Subject.Uri == null && store.SelectSubjects (null, stmt.Subject).Length > 0)
							continue;
						
						string title;
						string value;

						Description.GetDescription (store, stmt, out title, out value);

						stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><small><font color=\"" + fg + "\">");
						stream.Write (title);
						stream.Write ("</font></small></td><td width=100%>");
						
					        if (value != null)
							value = Escape (value);
						else {
							MemoryStore substore = store.Select (new Statement ((Entity)stmt.Object, null, null, null)).Load();
							WriteCollection (substore, stream);
						}
						
						if (value != null && value != String.Empty)
							stream.Write (value);
						
						stream.Write ("</td></tr>");
					}
				}
	
				if (Core.Database != null && photo is Photo) {
					stream.Write ("<tr><th align=left bgcolor=\"" + ig + "\" colspan=2>" + Catalog.GetString ("Exported Locations") + "</th></tr>");
	
					Photo p = photo as Photo;
					foreach (ExportItem export in Core.Database.Exports.GetByImageId (p.Id, p.DefaultVersionId)) {
						string url = GetExportUrl (export);
						string label = GetExportLabel (export);
						if (url == null || label == null)
							continue;
	                                        
						stream.Write ("<tr colspan=2><td width=100%>");
						stream.Write (String.Format ("<a href=\"{0}\">{1}</a>", url, label));
						stream.Write ("</font></small></td></tr>");
					}
				}
			}
			
			if (empty) {
				string msg;
				if (photo == null) {
					// FIXME we should pass the full selection to the info display and let it
					// handle multiple items however it wants.
					msg = String.Format ("<tr><td valign=top align=center bgcolor=\"{0}\">" 
							     + "<b>{1}</b></td></tr>", ig,
							     Catalog.GetString ("No active photo"));
				} else if (missing) {
					string text = String.Format (Catalog.GetString ("The photo \"{0}\" does not exist"), photo.DefaultVersionUri);
					msg = String.Format ("<tr><td valign=top align=center bgcolor=\"{0}\">" 
							     + "<b>{1}</b></td></tr>", ig, text);
				} else {
					msg = String.Format ("<tr><td valign=top align=center bgcolor=\"{0}\">" 
							     + "<b>{1}</b></td></tr>", ig,
							     Catalog.GetString ("No metadata available"));

					if (error != null) {
						String.Format ("<pre>{0}</pre>", error);
					}
				}
				stream.Write (msg);
			}

			stream.Write ("</table>");
			End (stream, Gtk.HTMLStreamStatus.Ok);
		}

		private void WriteCollection (MemoryStore substore, Gtk.HTMLStream stream)
		{
			string type = null;

			foreach (Statement stmt in substore) {
				if (stmt.Predicate.Uri == MetadataStore.Namespaces.Resolve ("rdf:type")) {
					string prefix;
					MetadataStore.Namespaces.Normalize (stmt.Object.Uri, out prefix, out type);
				}
			}
			
			stream.Write ("<table cellpadding=5 cellspacing=0 width=100%>");
			foreach (Statement sub in substore) {
				if (sub.Object is Literal) {
					string title;
					string value = ((Literal)(sub.Object)).Value;
					string vc = String.Empty;
					
					Description.GetDescription (substore, sub, out title, out value);

					if (type != "Alt")
						vc = " bgcolor=" + Color (Style.Backgrounds [(int)Gtk.StateType.Normal]);

					if (type == null)
						 stream.Write (String.Format ("<tr bgcolor={3}><td bgcolor={2}>{0}</td><td width=100%>{1}</td></tr>",  
								     Escape (title), 
								     Escape (value),
								     Color (Style.MidColors [(int)Gtk.StateType.Normal]),
								     Color (Style.Backgrounds [(int)Gtk.StateType.Normal])));
					else 
						stream.Write (String.Format ("<tr><td{1}>{0}</td></tr>", 
								    Escape (value),
								    vc));
				} else {
					if (type == null) {
						stream.Write ("<tr><td>");
						MemoryStore substore2 = substore.Select (new Statement ((Entity)sub.Object, null, null, null)).Load();
						if (substore.StatementCount > 0)
							WriteCollection (substore2, stream);
						stream.Write ("</tr><td>");
					}
				}
			}
			stream.Write ("</table>");
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
				string predicate = stmt.Predicate.Uri;
				string title = System.IO.Path.GetFileName (predicate);
				string bg = InfoDisplay.Color (info.Style.Background (Gtk.StateType.Active));
				string fg = InfoDisplay.Color (info.Style.Foreground (Gtk.StateType.Active));

				//if (MetadataStore.Namespaces.GetPrefix (path) != null) {
				if (stmt.Object is Literal) {
					stream.Write ("<tr><td valign=top align=right bgcolor=\""+ bg + "\"><font color=\"" + fg + "\">");
					stream.Write (title);
					stream.Write ("</font></td><td>");

					string s = ((SemWeb.Literal)(stmt.Object)).Value;
					/*
					else {
						MemoryStore store = source.Select (stmt.Invert (), new SelectPartialFilter (true, false, false, false));
						s = "";
						foreach (Statement sub in store) {
							s += sub.Object.ToString () + "/n";
						}
					}
					*/
					if (s != null && s != String.Empty)
						stream.Write (s);
					stream.Write ("</td><tr>");
				}
				return true;
			}
		}
	}
}

