/*
 * Copyright (C) 2005 Alessandro Gervaso <gervystar@gervystar.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

//This should be used to export the selected pics to an original gallery
//located on a VFS location.
using System;
using System.IO;
using System.Runtime.InteropServices;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;

namespace FSpot {
	public class FolderExport : GladeDialog {
		IPhotoCollection selection;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Entry uri_entry;
		[Glade.Widget] Gtk.Entry name_entry;
		[Glade.Widget] Gtk.Entry description_entry;

		//[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton open_check;
		
		[Glade.Widget] Gtk.RadioButton static_radio;
		[Glade.Widget] Gtk.RadioButton original_radio;
		[Glade.Widget] Gtk.RadioButton none_radio;

		[Glade.Widget] Gtk.SpinButton size_spin;

		Gnome.Vfs.Uri dest;
		
		int photo_index;
		bool open;
		bool scale;
		int size;
		
		string description;
		string gallery_name = "Gallery";
		// FIME this needs to be a real temp directory
		string gallery_path = Path.Combine (Path.GetTempPath (), "f-spot-original-" + System.DateTime.Now.Ticks.ToString ());

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		
		public FolderExport (IPhotoCollection selection) : base ("folder_export_dialog")
		{
			Gnome.Vfs.ModuleCallbackFullAuthentication auth = new Gnome.Vfs.ModuleCallbackFullAuthentication ();
			auth.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleAuth);
			auth.SetDefault ();
			auth.Push ();
			
			Gnome.Vfs.ModuleCallbackAuthentication mauth = new Gnome.Vfs.ModuleCallbackAuthentication ();
			mauth.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleAuth);
			mauth.SetDefault ();
			mauth.Push ();
			
			Gnome.Vfs.ModuleCallbackSaveAuthentication sauth = new Gnome.Vfs.ModuleCallbackSaveAuthentication ();
			sauth.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleAuth);
			sauth.SetDefault ();
			sauth.Push ();
			
			Gnome.Vfs.ModuleCallbackStatusMessage msg = new Gnome.Vfs.ModuleCallbackStatusMessage ();
			msg.Callback += new Gnome.Vfs.ModuleCallbackHandler (HandleMsg);
			msg.SetDefault ();
			msg.Push ();
			
			this.selection = selection;
			
			IconView view = (IconView) new IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);
			name_entry.Text = gallery_name;
			uri_entry.Text = System.IO.Path.Combine (FSpot.Global.HomeDirectory, "public_html");
			if (!System.IO.Directory.Exists (uri_entry.Text))
				uri_entry.Text = FSpot.Global.HomeDirectory;

			Dialog.ShowAll ();

			//LoadHistory ();
			Dialog.Response += HandleResponse;
		}

		public void HandleSizeActive (object sender, System.EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		public void Upload ()
		{
			// FIXME use mkstemp

			Gnome.Vfs.Result result = Gnome.Vfs.Result.Ok;

			try {
				Dialog.Destroy ();
				
				Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (Path.Combine (gallery_path, gallery_name));
				Gnome.Vfs.Uri target = dest.Clone();
				target = target.AppendFileName(source.ExtractShortName ());

				if (dest.IsLocal)
					gallery_path = Gnome.Vfs.Uri.GetLocalPathFromUri (dest.ToString ());

				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Building Gallery");
				progress_dialog.Fraction = 1.0;

				FolderGallery gallery;
				if (static_radio.Active) {
					gallery = new HtmlGallery (selection, gallery_path, gallery_name);
				} else if (original_radio.Active) {
					gallery = new OriginalGallery (selection, gallery_path, gallery_name);
				} else {
					gallery = new FolderGallery (selection, gallery_path, gallery_name);
				}

				if (scale)
					gallery.SetScale (size);
				
				gallery.Description = description;

				gallery.Generate ();

				// we've created the structure, now if the destination was local we are done
				// otherwise we xfer 
				if (!dest.IsLocal) {
					Console.WriteLine(target);
					Gnome.Vfs.XferProgressCallback cb = new Gnome.Vfs.XferProgressCallback (Progress);
					System.Console.WriteLine ("Xfering {0} to {1}", source.ToString (), target.ToString ());
					result = Gnome.Vfs.Xfer.XferUri (source, target, 
									 Gnome.Vfs.XferOptions.Default, 
									 Gnome.Vfs.XferErrorMode.Abort, 
									 Gnome.Vfs.XferOverwriteMode.Replace, 
									 cb);
				}

				if (result == Gnome.Vfs.Result.Ok) {

					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Transfer Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;

				} else {
					progress_dialog.ProgressText = result.ToString ();
					progress_dialog.Message = Mono.Posix.Catalog.GetString ("Error While Transferring");
				}

				if (open)
					GnomeUtil.UrlShow (null, target.ToString ());

			} catch (System.Exception e) {
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error Transferring");
			} finally {
				// if the destination isn't local then we want to remove the temp directory we
				// created.
				if (!dest.IsLocal)
					System.IO.Directory.Delete (gallery_path, true);
			}
		}
		
		private int Progress (Gnome.Vfs.XferProgressInfo info)
		{
			progress_dialog.ProgressText = info.Phase.ToString ();

			if (info.BytesTotal > 0) {
				progress_dialog.Fraction = info.BytesCopied / (double)info.BytesTotal;
			}
			
			switch (info.Status) {
			case Gnome.Vfs.XferProgressStatus.Vfserror:
				progress_dialog.Message = Mono.Posix.Catalog.GetString ("Error: Error while transferring, Aborting");
				return (int)Gnome.Vfs.XferErrorAction.Abort;
			case Gnome.Vfs.XferProgressStatus.Overwrite:
				progress_dialog.ProgressText = Mono.Posix.Catalog.GetString ("Error: File Already Exists, Aborting");
				return (int)Gnome.Vfs.XferOverwriteAction.Abort;
			default:
				return 1;
			}

		}

		private void HandleMsg (Gnome.Vfs.ModuleCallback cb)
		{
			Gnome.Vfs.ModuleCallbackStatusMessage msg = cb as Gnome.Vfs.ModuleCallbackStatusMessage;
			System.Console.WriteLine ("{0}", msg.Message);
		}

		private void HandleAuth (Gnome.Vfs.ModuleCallback cb)
		{
			Gnome.Vfs.ModuleCallbackFullAuthentication fcb = cb as Gnome.Vfs.ModuleCallbackFullAuthentication;
			System.Console.Write ("Enter your username ({0}): ", fcb.Username);
			string username = System.Console.ReadLine ();
			System.Console.Write ("Enter your password : ");
			string passwd = System.Console.ReadLine ();
			
			if (username.Length > 0)
				fcb.Username = username;
			fcb.Password = passwd;
		}

		private void HandleResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId != Gtk.ResponseType.Ok) {
				Dialog.Destroy ();
				return;
			}

			dest = new Gnome.Vfs.Uri (uri_entry.Text);
			open = open_check.Active;
			scale = scale_check.Active;
			gallery_name = name_entry.Text;

			if (description_entry != null)
				description = description_entry.Text;

			if (scale)
				size = size_spin.ValueAsInt;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
			command_thread.Name = Mono.Posix.Catalog.GetString ("Transfering Pictures");

			//FIXME: get the files/dirs count in a cleaner way than (* 5 + 2(zip) + 9)
			// selection * 5 (original, mq, lq, thumbs, comments)
			// 2: zipfiles
			// 9: directories + info.txt + .htaccess
			// this should actually be 1 anyway, because we transfer just one dir 
			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, 1);
			progress_dialog.Start ();
		}
	}

	internal class FolderGallery 
	{
		protected IPhotoCollection collection;
		protected string gallery_name;
		protected string gallery_path;
		protected bool scale;
		protected int size;
		protected string description;
		protected System.Uri destination;

		protected ScaleRequest [] requests;
		
		protected string [] pixbuf_keys = {"quality"};
		protected string [] pixbuf_values = {"75"};

		protected struct ScaleRequest {
			public string Name;
			public int Width;
			public int Height;
			public bool Skip;
			public ScaleRequest (string name, int width, int height, bool skip)
			{
				this.Name = name != null ? name : "";
				this.Width = width;
				this.Height = height;
				this.Skip = skip;
			}

			public static ScaleRequest Default = new ScaleRequest ("", 0, 0, false);

			public bool AvoidScale (int size) { 
				return (size < this.Width && size < this.Height && this.Skip);
			}
		}
		
		internal FolderGallery (IPhotoCollection selection, string path, string gallery_name)
		{
			this.collection = selection;
			this.gallery_name = gallery_name;
			this.gallery_path = Path.Combine (path, gallery_name);
			this.requests = new ScaleRequest [] { ScaleRequest.Default };
		}

		public virtual void Generate ()
		{
			MakeDir (gallery_path);

			for (int i = 0; i < collection.Photos.Length; i++)
			{
				ProcessImage (i);
			}
		}
		
		protected virtual string ImageName (int image_num)
		{
			return System.IO.Path.GetFileName (collection.Photos [image_num].DefaultVersionPath); 
		}

		public void ProcessImage (int image_num)
		{
			Photo photo = collection.Photos [image_num];
			string photo_path = photo.DefaultVersionPath;
			string path;
			ScaleRequest req;

			req = requests [0];
			
			MakeDir (SubdirPath (req.Name));
			path = SubdirPath (req.Name, ImageName (image_num));
			
			if (!scale)
				File.Copy(photo_path, path, true);
			else 
				PixbufUtils.Resize (photo_path, path, size, true); 
			
			Gdk.Pixbuf img = null;
			Gdk.Pixbuf scaled = null;
			
			for (int i = 1; i < requests.Length; i++) {
				req = requests [i];
				if (scale && req.AvoidScale (size))
					continue;

				if (img == null)
					scaled = PixbufUtils.LoadAtMaxSize (photo_path, req.Width, req.Height);
				else
					scaled = PixbufUtils.ScaleToMaxSize (img, req.Width, req.Height, false);
				
				MakeDir (SubdirPath (req.Name));
				path = SubdirPath (req.Name, ImageName (image_num));
				scaled.Savev (path, "jpeg", pixbuf_keys, pixbuf_values);
				
				if (img != null)
					img.Dispose ();
			}

			if (scaled != null)
				scaled.Dispose ();
		}

		protected string MakeDir (string path)
		{
			try {
				Directory.CreateDirectory (path);
			} catch {
				Console.WriteLine ("Error in creating directory " + path);
			}
			return path;
		}

		protected string SubdirPath (string subdir)
		{
			return SubdirPath (subdir, null);
		}
		
		protected string SubdirPath (string subdir, string file)
		{
			string path = Path.Combine (gallery_path, subdir);
			if (file != null)
				path = Path.Combine (path, file);

			return path;
		}

		public string GalleryPath {
			get {
				return gallery_path;
			}
		}

		public string Description {
			get {
				return description;
			}
			set {
				description = value;
			}
		}
		
		public Uri Destination {
			get {
				return destination;
			}
			set {
				this.destination = value;
			}
		}

		public void SetScale (int size) {
			requests [0].Width = size;
			requests [0].Height = size;
		}
	}

	class OriginalGallery : FolderGallery
	{
		public OriginalGallery (IPhotoCollection selection, string path, string name) : base (selection, path, name) 
		{ 
			requests = new ScaleRequest [] { new ScaleRequest ("hq", 0, 0, false),
							 new ScaleRequest ("mq", 800, 600, true),
							 new ScaleRequest ("lq", 640, 480, false),
							 new ScaleRequest ("thumbs", 120, 90, false) };
		}

		public override void Generate ()
		{
			base.Generate ();
			MakeDir (SubdirPath ("comments"));
			MakeDir (SubdirPath ("zip"));
			CreateHtaccess();
			CreateInfo();
			SetTime ();
		}
		
		protected override string ImageName (int photo_index)
		{
			return String.Format ("img-{0}.jpg", photo_index);
		}

		private string AlternateName (int photo_index, string extention) 
		{
			return System.IO.Path.GetFileNameWithoutExtension (ImageName (photo_index)) + extention;
		}

		private void SetTime ()
		{
			try {
				int i = 0;
				foreach (Photo photo in collection.Photos) {
					CreateComments (photo.DefaultVersionPath, i);
					i++;
				}

				Directory.SetLastWriteTimeUtc(gallery_path, collection.Photos [0].Time);

				if (System.IO.Directory.Exists (SubdirPath ("mq")))
				    CreateZipFile("mq");

				if (System.IO.Directory.Exists (SubdirPath ("hq")))
				    CreateZipFile("hq");
			
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			} 
		}

		private void CreateComments(string photo_path, int photo_index)
		{
			StreamWriter comment = File.CreateText(SubdirPath  ("comments", "img-" + photo_index + ".txt"));
			comment.WriteLine("<span>image " + photo_index + "</span>\n");
			comment.Close();
		}

		private void CreateZipFile(string img_quality)
		{
			string[] filenames = Directory.GetFiles(SubdirPath (img_quality));
			Crc32 crc = new Crc32();
			ZipOutputStream s = new ZipOutputStream(File.Create(SubdirPath ("zip", img_quality + ".zip")));
			
			s.SetLevel(0);
			foreach (string file in filenames) {
				FileStream fs = File.OpenRead(file);
			
				byte[] buffer = new byte[fs.Length];
				fs.Read(buffer, 0, buffer.Length);
				ZipEntry entry = new ZipEntry(Path.GetFileName(file));
			
				entry.DateTime = DateTime.Now;
			
				// set Size and the crc, because the information
				// about the size and crc should be stored in the header
				// if it is not set it is automatically written in the footer.
				// (in this case size == crc == -1 in the header)
				// Some ZIP programs have problems with zip files that don't store
				// the size and crc in the header.
				entry.Size = fs.Length;
				fs.Close();
			
				crc.Reset();
				crc.Update(buffer);
			
				entry.Crc  = crc.Value;
			
				s.PutNextEntry(entry);
			
				s.Write(buffer, 0, buffer.Length);
			
			}
		
			s.Finish();
			s.Close();
		}

		private void CreateHtaccess()
		{
			StreamWriter htaccess = File.CreateText(Path.Combine (gallery_path,".htaccess"));
			htaccess.Write("<Files info.txt>\n\tdeny from all\n</Files>\n");
			htaccess.Close();
		}

		private void CreateInfo()
		{
			StreamWriter info = File.CreateText(Path.Combine (gallery_path, "info.txt"));
			info.WriteLine("date|" + collection.Photos[0].Time.Date.ToString ("dd.MM.yyyy"));
			info.Close();
		}
	}

	class HtmlGallery : FolderGallery 
	{
		int current;
		int perpage = 16;
		string stylesheet = "f-spot-simple-white.css";
		
		public HtmlGallery (IPhotoCollection selection, string path, string name) : base (selection, path, name) 
		{ 
			requests = new ScaleRequest [] { new ScaleRequest ("hq", 0, 0, false),
							 new ScaleRequest ("mq", 480, 320, false),
							 new ScaleRequest ("thumbs", 120, 90, false) };
		}
		
		public override void Generate ()
		{
			if (collection.Photos.Length == 0)
				return;
			
			base.Generate ();
			
			Photo [] photos = collection.Photos;
			
			int i;
			for (i = 0; i < photos.Length; i++)
				SavePhotoHtmlIndex (i);
			
			for (i = 0; i < PageCount; i++)
				SaveHtmlIndex (i);
			
			MakeDir (SubdirPath ("style"));
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
			System.IO.Stream s = assembly.GetManifestResourceStream (stylesheet);
			System.IO.Stream fs = System.IO.File.Open (SubdirPath ("style", stylesheet), System.IO.FileMode.Create);

			byte [] buffer = new byte [8192];
			int n;
			while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
				fs.Write (buffer, 0,  n);						    
			
			fs.Close ();
		}
		
		public int PageCount {
			get {
				return 	(int) System.Math.Ceiling (collection.Items.Length / (double)perpage);
			}
		}
		
		public string PhotoThumbPath (int item) 
		{
			return System.IO.Path.Combine (requests [2].Name, ImageName (item));
		}
		
		public string PhotoWebPath (int item)
		{
			return System.IO.Path.Combine (requests [1].Name, ImageName (item));	
		}
		
		public string PhotoOriginalPath (int item)
		{
			return System.IO.Path.Combine (requests [0].Name, ImageName (item));
		}
		
		public string PhotoIndexPath (int item)
		{
			return (System.IO.Path.GetFileNameWithoutExtension (ImageName (item)) + ".html");
		}
		
		public static void WritePageNav (System.Web.UI.HtmlTextWriter writer, string id, string url, string name)
		{
			writer.AddAttribute ("id", id);
			writer.RenderBeginTag ("div");				
			 
			writer.AddAttribute ("href", url);
			writer.RenderBeginTag ("a");
			writer.Write (name);
			writer.RenderEndTag ();
			
			writer.RenderEndTag ();
		}
		
		public void SavePhotoHtmlIndex (int i)
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (PhotoIndexPath (i)));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			//writer.Indent = 4;
			
			//writer.Write ("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">");
			writer.RenderBeginTag ("html");
			
			WriteHeader (writer);
			
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (description);
			writer.RenderEndTag ();

			if (i > 0)
				WritePageNav (writer, "prev", PhotoIndexPath (i - 1), Mono.Posix.Catalog.GetString("Prev"));

			WritePageNav (writer, "index", IndexPath (i / perpage), Mono.Posix.Catalog.GetString("Index"));
			
			if (i < collection.Photos.Length -1)
				WritePageNav (writer, "next", PhotoIndexPath (i + 1), Mono.Posix.Catalog.GetString("Next"));

			writer.RenderEndTag ();
			
			writer.AddAttribute ("class", "photo");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", PhotoOriginalPath (i));
			writer.RenderBeginTag ("a");
			
			writer.AddAttribute ("src", PhotoWebPath (i));
			writer.RenderBeginTag ("img");
			writer.RenderEndTag ();
			writer.RenderEndTag (); // a
			
			writer.RenderBeginTag ("div");
			writer.Write (collection.Photos [i].Description);
			writer.RenderEndTag ();

			writer.RenderEndTag ();

			WriteFooter (writer);
			
			writer.RenderEndTag (); //body
			writer.RenderEndTag (); // html

			writer.Close ();
			stream.Close ();
		}				

		public static string IndexPath (int page_num)
		{
			if (page_num == 0)
				return "index.html";
			else
				return String.Format ("index{0}.html", page_num);
		}
		
		static string IndexTitle (int page)
		{
			return String.Format ("{0}", page);
		}

		public void WriteHeader (System.Web.UI.HtmlTextWriter writer)
		{
			writer.RenderBeginTag ("head");
			writer.RenderBeginTag ("title");
			writer.Write (gallery_name);
			writer.RenderEndTag ();
			
			writer.AddAttribute ("type", "text/css");
			writer.AddAttribute ("media", "screen");
			writer.RenderBeginTag ("style");
			writer.Write (String.Format ("@import url( {0} );", SubdirPath ("style", stylesheet)));
			writer.RenderEndTag ();
			writer.RenderEndTag ();
		}
		
		public static void WriteFooter (System.Web.UI.HtmlTextWriter writer)
		{
			writer.AddAttribute ("class", "footer");
			writer.RenderBeginTag ("div");
			
			writer.Write (Mono.Posix.Catalog.GetString ("Gallery generated by "));
			
			writer.AddAttribute ("href", "http://www.gnome.org/projects/f-spot");
			writer.RenderBeginTag ("a");
			writer.Write (String.Format ("{0} {1}", FSpot.Defines.PACKAGE, FSpot.Defines.VERSION));
			writer.RenderEndTag ();

			writer.RenderEndTag ();
		}

		public void SaveHtmlIndex (int page_num)
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (IndexPath (page_num)));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			//writer.Indent = 4;

			//writer.Write ("<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\">");
			writer.RenderBeginTag ("html");
			WriteHeader (writer);
			
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (description);
			writer.RenderEndTag ();
			
			writer.RenderBeginTag ("div");
			writer.Write (Mono.Posix.Catalog.GetString ("Pages:"));
			writer.RenderEndTag ();
			
			int i;
			for (i = 0; i < PageCount; i++) {
				writer.AddAttribute ("class", i == page_num ? "navipage-current" : "navipage");
				writer.RenderBeginTag ("div");
				
				writer.AddAttribute ("href", IndexPath (i));
				writer.RenderBeginTag ("a");
				writer.Write (IndexTitle (i));
				writer.RenderEndTag ();
				
				writer.RenderEndTag ();
			}
			writer.RenderEndTag ();
			
			writer.AddAttribute ("class", "thumbs");
			writer.RenderBeginTag ("div");
			
			int start = page_num * perpage;
			int end = Math.Min (start + perpage, collection.Photos.Length);
			for (i = start; i < end; i++) {
				writer.AddAttribute ("href", PhotoIndexPath (i));
				writer.RenderBeginTag ("a");
				
				writer.AddAttribute  ("src", PhotoThumbPath (i));
				writer.RenderBeginTag ("img");
				writer.RenderEndTag ();
				
				writer.RenderEndTag ();
			}
			
			WriteFooter (writer);
			
			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html
			
			writer.Close ();
			stream.Close ();
		}
	}
}
