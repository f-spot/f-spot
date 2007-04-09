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

using Mono.Unix;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;

namespace FSpot {
	public class FolderExport : GladeDialog, FSpot.Extensions.IExporter {
		IBrowsableCollection selection;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Entry name_entry;
		[Glade.Widget] Gtk.Entry description_entry;

		//[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton rotate_check;
		[Glade.Widget] Gtk.CheckButton open_check;
		
		[Glade.Widget] Gtk.RadioButton static_radio;
		[Glade.Widget] Gtk.RadioButton original_radio;
		[Glade.Widget] Gtk.RadioButton plain_radio;

		[Glade.Widget] Gtk.SpinButton size_spin;
		
		[Glade.Widget] Gtk.HBox chooser_hbox;

		Gnome.Vfs.Uri dest;
		Gtk.FileChooserButton uri_chooser;
		
		int photo_index;
		bool open;
		bool scale;
		bool rotate;
		int size;
		
		string description;
		string gallery_name = "Gallery";
		// FIME this needs to be a real temp directory
		string gallery_path = Path.Combine (Path.GetTempPath (), "f-spot-original-" + System.DateTime.Now.Ticks.ToString ());

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		
		public FolderExport () : base ("folder_export_dialog")
		{}
		public void Run (IBrowsableCollection selection)
		{
			/*
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
			*/
			this.selection = selection;
			
			IconView view = (IconView) new IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);
			name_entry.Text = gallery_name;

			string uri_path = System.IO.Path.Combine (FSpot.Global.HomeDirectory, "Desktop");
			if (!System.IO.Directory.Exists (uri_path))
			        uri_path = FSpot.Global.HomeDirectory;

			uri_chooser = new Gtk.FileChooserButton (Catalog.GetString ("Select Export Folder"),
								 Gtk.FileChooserAction.SelectFolder);
			
			uri_chooser.LocalOnly = false;

			if (Preferences.Get (Preferences.EXPORT_FOLDER_URI) != null && Preferences.Get (Preferences.EXPORT_FOLDER_URI) as string != String.Empty)
				uri_chooser.SetUri (Preferences.Get (Preferences.EXPORT_FOLDER_URI) as string);
			else
				uri_chooser.SetFilename (uri_path);

			chooser_hbox.PackStart (uri_chooser);

			Dialog.ShowAll ();

			//LoadHistory ();
			Dialog.Response += HandleResponse;

			LoadPreference (Preferences.EXPORT_FOLDER_SCALE);
			LoadPreference (Preferences.EXPORT_FOLDER_SIZE);
			LoadPreference (Preferences.EXPORT_FOLDER_OPEN);
			LoadPreference (Preferences.EXPORT_FOLDER_ROTATE);
			LoadPreference (Preferences.EXPORT_FOLDER_METHOD);
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
				Dialog.Hide ();
				
				Gnome.Vfs.Uri source = new Gnome.Vfs.Uri (Path.Combine (gallery_path, gallery_name));
				Gnome.Vfs.Uri target = dest.Clone();
				target = target.AppendFileName(source.ExtractShortName ());

				if (dest.IsLocal)
					gallery_path = Gnome.Vfs.Uri.GetLocalPathFromUri (dest.ToString ());

				progress_dialog.Message = Catalog.GetString ("Building Gallery");
				progress_dialog.Fraction = 0.0;

				FolderGallery gallery;
				if (static_radio.Active) {
					gallery = new HtmlGallery (selection, gallery_path, gallery_name);
				} else if (original_radio.Active) {
					gallery = new OriginalGallery (selection, gallery_path, gallery_name);
				} else {
					gallery = new FolderGallery (selection, gallery_path, gallery_name);
				}

				if (scale) {
					System.Console.WriteLine ("setting scale to {0}", size);
					gallery.SetScale (size);
				} else {
					System.Console.WriteLine ("Exporting full size image");
				}

				if (rotate) {
					System.Console.WriteLine ("Exporting rotated image");
					gallery.SetRotate();
				}

				gallery.Description = description;

				gallery.GenerateLayout ();
				Filters.FilterSet filter_set = new Filters.FilterSet ();
				if (scale)
					filter_set.Add (new Filters.ResizeFilter ((uint) size));
				else if (rotate)
					filter_set.Add (new Filters.OrientationFilter ());
				filter_set.Add (new Filters.ChmodFilter ());
				filter_set.Add (new Filters.UniqueNameFilter (gallery_path));

				for (int photo_index = 0; photo_index < selection.Count; photo_index++)
				{
					try {	
						progress_dialog.Message = System.String.Format (Catalog.GetString ("Uploading picture \"{0}\""), selection[photo_index].Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						gallery.ProcessImage (photo_index, filter_set);
						progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"), photo_index, selection.Count);
					}
					catch (Exception e) {
						progress_dialog.Message = String.Format (Catalog.GetString ("Error uploading picture \"{0}\" to Gallery:{2}{1}"), 
							selection[photo_index].Name, e.Message, Environment.NewLine);
						progress_dialog.ProgressText = Catalog.GetString ("Error");

						if (progress_dialog.PerformRetrySkip ())
							photo_index--;
					}
		
				}

				//create the zip tarballs for original
				if (gallery is OriginalGallery && (bool)Preferences.Get(Preferences.EXPORT_FOLDER_INCLUDE_TARBALLS))
					(gallery as OriginalGallery).CreateZip ();

				// we've created the structure, now if the destination was local we are done
				// otherwise we xfer 
				if (!dest.IsLocal) {
					Console.WriteLine(target);
					System.Console.WriteLine ("Xfering {0} to {1}", source.ToString (), target.ToString ());
					result = Gnome.Vfs.Xfer.XferUri (source, target, 
									 Gnome.Vfs.XferOptions.Default, 
									 Gnome.Vfs.XferErrorMode.Abort, 
									 Gnome.Vfs.XferOverwriteMode.Replace, 
									 Progress);
				}

				if (result == Gnome.Vfs.Result.Ok) {

					progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Catalog.GetString ("Transfer Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;

				} else {
					progress_dialog.ProgressText = result.ToString ();
					progress_dialog.Message = Catalog.GetString ("Error While Transferring");
				}

				if (open) {
					GnomeUtil.UrlShow (null, target.ToString ());
				}

				// Save these settings for next time
				Preferences.Set (Preferences.EXPORT_FOLDER_SCALE, scale);
				Preferences.Set (Preferences.EXPORT_FOLDER_SIZE, size);
				Preferences.Set (Preferences.EXPORT_FOLDER_OPEN, open);
				Preferences.Set (Preferences.EXPORT_FOLDER_ROTATE, rotate);
				Preferences.Set (Preferences.EXPORT_FOLDER_METHOD, static_radio.Active ? "static" : original_radio.Active ? "original" : "folder" );
				Preferences.Set (Preferences.EXPORT_FOLDER_URI, uri_chooser.Uri);
			} catch (System.Exception e) {
				// Console.WriteLine (e);
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Catalog.GetString ("Error Transferring");
			} finally {
				// if the destination isn't local then we want to remove the temp directory we
				// created.
				if (!dest.IsLocal)
					System.IO.Directory.Delete (gallery_path, true);
				
				Gtk.Application.Invoke (delegate { Dialog.Destroy(); });

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
				progress_dialog.Message = Catalog.GetString ("Error: Error while transferring; Aborting");
				return (int)Gnome.Vfs.XferErrorAction.Abort;
			case Gnome.Vfs.XferProgressStatus.Overwrite:
				progress_dialog.ProgressText = Catalog.GetString ("Error: File Already Exists; Aborting");
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
				// FIXME this is to work around a bug in gtk+ where
				// the filesystem events are still listened to when
				// a FileChooserButton is destroyed but not finalized
				// and an event comes in that wants to update the child widgets.
				Dialog.Destroy ();
				uri_chooser.Dispose ();
				uri_chooser = null;
				return;
			}

			dest = new Gnome.Vfs.Uri (uri_chooser.Uri);
			open = open_check.Active;
			scale = scale_check.Active;
			rotate = rotate_check.Active;

			gallery_name = name_entry.Text;

			if (description_entry != null)
				description = description_entry.Text;

			if (scale)
				size = size_spin.ValueAsInt;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
			command_thread.Name = Catalog.GetString ("Transferring Pictures");

			//FIXME: get the files/dirs count in a cleaner way than (* 5 + 2(zip) + 9)
			// selection * 5 (original, mq, lq, thumbs, comments)
			// 2: zipfiles
			// 9: directories + info.txt + .htaccess
			// this should actually be 1 anyway, because we transfer just one dir 
			progress_dialog = new FSpot.ThreadProgressDialog (command_thread, 1);
			progress_dialog.Start ();
		}

		void LoadPreference (string key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			//System.Console.WriteLine ("Setting {0} to {1}", key, val);

			switch (key) {
			case Preferences.EXPORT_FOLDER_SCALE:
				if (scale_check.Active != (bool) val)
					scale_check.Active = (bool) val;
				break;

			case Preferences.EXPORT_FOLDER_SIZE:
				size_spin.Value = (double) (int) val;
				break;
			
			case Preferences.EXPORT_FOLDER_OPEN:
				if (open_check.Active != (bool) val)
					open_check.Active = (bool) val;
				break;
			
			case Preferences.EXPORT_FOLDER_ROTATE:
				if (rotate_check.Active != (bool) val)
					rotate_check.Active = (bool) val;
				break;
			case Preferences.EXPORT_FOLDER_METHOD:
				static_radio.Active = (string) val == "static";
				original_radio.Active = (string) val == "original";
				plain_radio.Active = (string) val == "folder";
				break;
			}
		}
	}

	internal class FolderGallery 
	{
		protected IBrowsableCollection collection;
		protected string gallery_name;
		protected string gallery_path;
		protected bool scale;
		protected int size;
		protected bool rotate;
		protected string description;
		protected string language;
		protected System.Uri destination;

		protected ScaleRequest [] requests;
		
		protected string [] pixbuf_keys = { "quality", null };
		protected string [] pixbuf_values = { "95", null };

		protected struct ScaleRequest {
			public string Name;
			public int Width;
			public int Height;
			public bool Skip;
			public bool CopyExif;

			public ScaleRequest (string name, int width, int height, bool skip) : this (name, width, height, skip, false) {}

			public ScaleRequest (string name, int width, int height, bool skip, bool exif)
			{
				this.Name = name != null ? name : String.Empty;
				this.Width = width;
				this.Height = height;
				this.Skip = skip;
				this.CopyExif = exif;
			}

			public static ScaleRequest Default = new ScaleRequest (String.Empty, 0, 0, false);

			public bool AvoidScale (int size) { 
				return (size < this.Width && size < this.Height && this.Skip);
			}
		}
		
		internal FolderGallery (IBrowsableCollection selection, string path, string gallery_name)
		{
			this.collection = selection;
			this.gallery_name = gallery_name;
			this.gallery_path = Path.Combine (path, gallery_name);
			this.requests = new ScaleRequest [] { ScaleRequest.Default };
		}

		public virtual void GenerateLayout ()
		{
			MakeDir (gallery_path);

		}

		protected virtual string ImageName (int image_num)
		{
			return System.IO.Path.GetFileName(FileImportBackend.UniqueName(gallery_path, System.IO.Path.GetFileName (collection [image_num].DefaultVersionUri.LocalPath))); 
		}

		public void ProcessImage (int image_num, Filters.FilterSet filter_set)
		{
			IBrowsableItem photo = collection [image_num];
			string photo_path = photo.DefaultVersionUri.LocalPath;
			string path;
			ScaleRequest req;

			req = requests [0];
			
			MakeDir (SubdirPath (req.Name));
			path = SubdirPath (req.Name, ImageName (image_num));
			
			using (Filters.FilterRequest request = new Filters.FilterRequest (photo.DefaultVersionUri)) {
				filter_set.Convert (request);
				if (request.Current.LocalPath == path)
					request.Preserve(request.Current);
				else
					File.Copy (request.Current.LocalPath, path, true); 

				if (photo != null && photo is Photo && Core.Database != null) {
					Core.Database.Exports.Create ((photo as Photo).Id, (photo as Photo).DefaultVersionId,
								      ExportStore.FolderExportType,
								      // FIXME this is wrong, the final path is the one
								      // after the Xfer.
								      UriList.PathToFileUriEscaped (path).ToString ());
				}

				using (Exif.ExifData data = new Exif.ExifData (photo_path)) {
					for (int i = 1; i < requests.Length; i++) {
						
						req = requests [i];
						if (scale && req.AvoidScale (size))
							continue;
				
						Filters.FilterSet req_set = new Filters.FilterSet ();
						req_set.Add (new Filters.ResizeFilter ((uint)Math.Max (req.Width, req.Height)));
						if ((bool)Preferences.Get (Preferences.EXPORT_FOLDER_SHARPEN)) {
							if (req.Name == "lq")
								req_set.Add (new Filters.SharpFilter (0.1, 2, 4));
							if (req.Name == "thumbs")
								req_set.Add (new Filters.SharpFilter (0.1, 2, 5));
						}
						using (Filters.FilterRequest tmp_req = new Filters.FilterRequest (photo.DefaultVersionUri)) {
							req_set.Convert (tmp_req);
							MakeDir (SubdirPath (req.Name));
							path = SubdirPath (req.Name, ImageName (image_num));
							System.IO.File.Copy (tmp_req.Current.LocalPath, path, true);
						}
						
					}
				}
			}
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

		public string Language {
			get {
				if (language == null)
					language=GetLanguage();
				return language;
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
			this.scale = true;
			this.size = size;
			requests [0].Width = size;
			requests [0].Height = size;
		}

		public void SetRotate () {
			this.rotate = true;
		}

		private string GetLanguage()
		{
			string language;
 
			if ((language = Environment.GetEnvironmentVariable ("LC_ALL")) == null)
				if ((language = Environment.GetEnvironmentVariable ("LC_MESSAGES")) == null)
					if ((language = Environment.GetEnvironmentVariable ("LANG")) == null)
						language = "en";
 
			if (language.IndexOf('.') >= 0)
				language = language.Substring(0,language.IndexOf('.'));
			if (language.IndexOf('@') >= 0)
				language = language.Substring(0,language.IndexOf('@'));
			language = language.Replace('_','-');
 
			return language;
		}
	}

	class OriginalGallery : FolderGallery
	{
		public OriginalGallery (IBrowsableCollection selection, string path, string name) : base (selection, path, name) 
		{ 
			requests = new ScaleRequest [] { new ScaleRequest ("hq", 0, 0, false),
							 new ScaleRequest ("mq", 800, 600, true),
							 new ScaleRequest ("lq", 640, 480, false, true),
							 new ScaleRequest ("thumbs", 120, 120, false) };
		}

		public override void GenerateLayout ()
		{
			base.GenerateLayout ();
			MakeDir (SubdirPath ("comments"));
			CreateHtaccess();
			CreateInfo();
			SetTime ();
		}
		
		protected override string ImageName (int photo_index)
		{
			return String.Format ("img-{0}.jpg", photo_index + 1);
		}

		private string AlternateName (int photo_index, string extension) 
		{
			return System.IO.Path.GetFileNameWithoutExtension (ImageName (photo_index)) + extension;
		}

		private void SetTime ()
		{
			try {
				for (int i = 0; i < collection.Count; i++)
					CreateComments (collection [i].DefaultVersionUri.LocalPath, i);

				Directory.SetLastWriteTimeUtc(gallery_path, collection [0].Time);
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			} 
		}

		internal void CreateZip () 
		{
			MakeDir (SubdirPath ("zip"));
			try {
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
			StreamWriter comment = File.CreateText(SubdirPath  ("comments", photo_index + 1 + ".txt"));
			comment.Write("<span>photo " + (photo_index + 1) + "</span> ");
			comment.Write (collection [photo_index].Description + Environment.NewLine);
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
			htaccess.Write("<Files info.txt>" + Environment.NewLine + "\tdeny from all" + Environment.NewLine+ "</Files>" + Environment.NewLine);
			htaccess.Close();
		}

		private void CreateInfo()
		{
			StreamWriter info = File.CreateText(Path.Combine (gallery_path, "info.txt"));
			info.WriteLine("name|" + gallery_name);
			info.WriteLine("date|" + collection [0].Time.Date.ToString ("dd.MM.yyyy"));
			info.WriteLine("description|" + description);
			info.Close();
		}
	}

	class HtmlGallery : FolderGallery 
	{
		int current;
		int perpage = 16;
		string stylesheet = "f-spot-simple.css";
		string altstylesheet = "f-spot-simple-white.css";
		string javascript = "f-spot.js";

		static string light = Catalog.GetString("Light");
		static string dark = Catalog.GetString("Dark");
		
		public HtmlGallery (IBrowsableCollection selection, string path, string name) : base (selection, path, name) 
		{ 
			requests = new ScaleRequest [] { new ScaleRequest ("hq", 0, 0, false),
							 new ScaleRequest ("mq", 480, 320, false),
							 new ScaleRequest ("thumbs", 120, 90, false) };
		}
		
		protected override string ImageName (int photo_index)
		{
			return String.Format ("img-{0}.jpg", photo_index + 1);
		}

		public override void GenerateLayout ()
		{
			if (collection.Count == 0)
				return;
			
			base.GenerateLayout ();
			
			IBrowsableItem [] photos = collection.Items;
			
			int i;
			for (i = 0; i < photos.Length; i++)
				SavePhotoHtmlIndex (i);
			
			for (i = 0; i < PageCount; i++)
				SaveHtmlIndex (i);
			
			MakeDir (SubdirPath ("style"));
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
			using (Stream s = assembly.GetManifestResourceStream (stylesheet)) {
				using (Stream fs = System.IO.File.Open (SubdirPath ("style", stylesheet), System.IO.FileMode.Create)) {

					byte [] buffer = new byte [8192];
					int n;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
						fs.Write (buffer, 0,  n);						    
					
				}
			}
			/* quick and stupid solution
			   this should have been iterated over an array of stylesheets, really
			*/
			using (Stream s = assembly.GetManifestResourceStream (altstylesheet)) {
				using (Stream fs = System.IO.File.Open (SubdirPath ("style", altstylesheet), System.IO.FileMode.Create)) {
					
					byte [] buffer = new byte [8192];
					int n = 0;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
						fs.Write (buffer, 0,  n);						    
					
				}
			}

			/* Javascript for persistant style change */
			MakeDir (SubdirPath ("script"));
			using (Stream s = assembly.GetManifestResourceStream (javascript)) {
				using (Stream fs = System.IO.File.Open (SubdirPath ("script", javascript), System.IO.FileMode.Create)) {

					byte [] buffer = new byte [8192];
					int n = 0;
					while ((n = s.Read (buffer, 0, buffer.Length)) != 0)
						fs.Write (buffer, 0,  n);						    
					
				}
			}
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
			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			
			WriteHeader (writer);
			
			writer.AddAttribute ("onload", "checkForTheme()");
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (gallery_name);
			writer.RenderEndTag ();

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			if (i > 0)
				// Abbreviation of previous	
				WritePageNav (writer, "prev", PhotoIndexPath (i - 1), Catalog.GetString("Prev"));

			WritePageNav (writer, "index", IndexPath (i / perpage), Catalog.GetString("Index"));
			
			if (i < collection.Count -1)
				WritePageNav (writer, "next", PhotoIndexPath (i + 1), Catalog.GetString("Next"));

			writer.RenderEndTag (); //navi
			
			writer.RenderEndTag (); //header
			
			writer.AddAttribute ("class", "photo");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", PhotoOriginalPath (i));
			writer.RenderBeginTag ("a");
			
			writer.AddAttribute ("src", PhotoWebPath (i));
			writer.AddAttribute ("alt", "#");
			writer.RenderBeginTag ("img");
			writer.RenderEndTag ();
			writer.RenderEndTag (); // a
			
			writer.AddAttribute ("id", "description");
			writer.RenderBeginTag ("div");
			writer.Write (collection [i].Description);
			writer.RenderEndTag ();

			writer.RenderEndTag ();
			
		  //Style Selection Box
			writer.AddAttribute ("id", "styleboxcontainer");
			writer.RenderBeginTag ("div");
			writer.AddAttribute ("id", "stylebox");
			writer.AddAttribute ("style", "display: none;");
			writer.RenderBeginTag ("div");
			writer.RenderBeginTag("ul");
			writer.RenderBeginTag("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", dark);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + dark + "')");
			writer.RenderBeginTag("a");
			writer.Write (dark);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderBeginTag("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", light);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + light + "')");
			writer.RenderBeginTag("a");
			writer.Write (light);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderEndTag (); //ul
			writer.RenderEndTag (); //div stylebox
			writer.RenderBeginTag ("div");
			writer.Write ("<span class=\"style_toggle\">"); 
			writer.Write ("<a href=\"javascript:toggle_stylebox()\">");
			writer.Write ("<span id=\"showlink\">" + Catalog.GetString ("Show Styles") + "</span><span id=\"hidelink\" ");
			writer.Write ("style=\"display:none;\">" + Catalog.GetString ("Hide Styles") + "</span></a></span>" + Environment.NewLine);
			writer.RenderEndTag (); //div toggle
			writer.RenderEndTag (); //div styleboxcontainer
			writer.RenderEndTag (); //container1	

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
			return String.Format ("{0}", page + 1);
		}

		public void WriteHeader (System.Web.UI.HtmlTextWriter writer)
		{
			writer.RenderBeginTag ("head");
			/* It seems HtmlTextWriter always uses UTF-8, unless told otherwise */
			writer.Write ("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />");
			writer.WriteLine ();
			writer.RenderBeginTag ("title");
			writer.Write (gallery_name);
			writer.RenderEndTag ();

			writer.Write ("<link type=\"text/css\" rel=\"stylesheet\" href=\"");
			writer.Write (String.Format ("{0}", "style/" + stylesheet));
			writer.Write ("\" title=\"" + dark + "\" media=\"screen\" />" + Environment.NewLine);

			writer.Write ("<link type=\"text/css\" rel=\"prefetch ") ;
			writer.Write ("alternate stylesheet\" href=\"");
			writer.Write (String.Format ("{0}", "style/" + altstylesheet));
			writer.Write ("\" title=\"" + light + "\" media=\"screen\" />" + Environment.NewLine);

			writer.Write ("<script src=\"script/" + javascript + "\"");
			writer.Write (" type=\"text/javascript\"></script>" + Environment.NewLine);

			writer.RenderEndTag ();
		}
		
		public static void WriteFooter (System.Web.UI.HtmlTextWriter writer)
		{
			writer.AddAttribute ("class", "footer");
			writer.RenderBeginTag ("div");
			
			writer.Write (Catalog.GetString ("Gallery generated by") + " ");
			
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
			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			WriteHeader (writer);
			
			writer.AddAttribute ("onload", "checkForTheme()");
			writer.RenderBeginTag ("body");
			

			
			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (gallery_name);
			writer.RenderEndTag (); //title div
			
			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "navilabel");
			writer.RenderBeginTag ("div");
			writer.Write (Catalog.GetString ("Page:"));
			writer.RenderEndTag (); //pages div
			
			int i;
			for (i = 0; i < PageCount; i++) {
				writer.AddAttribute ("class", i == page_num ? "navipage-current" : "navipage");
				writer.RenderBeginTag ("div");
				
				writer.AddAttribute ("href", IndexPath (i));
				writer.RenderBeginTag ("a");
				writer.Write (IndexTitle (i));
				writer.RenderEndTag (); //a
				
				writer.RenderEndTag (); //navipage
			}
			writer.RenderEndTag (); //navi
			writer.RenderEndTag (); //header
			
			writer.AddAttribute ("class", "thumbs");
			writer.RenderBeginTag ("div");
			
			int start = page_num * perpage;
			int end = Math.Min (start + perpage, collection.Count);
			for (i = start; i < end; i++) {
				writer.AddAttribute ("href", PhotoIndexPath (i));
				writer.RenderBeginTag ("a");
				
				writer.AddAttribute  ("src", PhotoThumbPath (i));
				writer.AddAttribute  ("alt", "#");
				writer.RenderBeginTag ("img");
				writer.RenderEndTag ();
				
				writer.RenderEndTag (); //a
			}
			
			writer.RenderEndTag (); //thumbs
			
			writer.AddAttribute ("id", "gallery_description");
			writer.RenderBeginTag ("div");
			writer.Write (description);
			writer.RenderEndTag (); //description
			
      //Style Selection Box
			writer.AddAttribute ("id", "styleboxcontainer");
			writer.RenderBeginTag ("div");
			writer.AddAttribute ("id", "stylebox");
			writer.AddAttribute ("style", "display: none;");
			writer.RenderBeginTag ("div");
			writer.RenderBeginTag("ul");
			writer.RenderBeginTag("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", dark);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + dark + "')");
			writer.RenderBeginTag("a");
			writer.Write (dark);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderBeginTag("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", light);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + light + "')");
			writer.RenderBeginTag("a");
			writer.Write (light);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderEndTag (); //ul
			writer.RenderEndTag (); //div stylebox
			writer.RenderBeginTag ("div");
			writer.Write ("<span class=\"style_toggle\">"); 
			writer.Write ("<a href=\"javascript:toggle_stylebox()\">");
			writer.Write ("<span id=\"showlink\">" + Catalog.GetString("Show Styles") + "</span><span id=\"hidelink\" ");
			writer.Write ("style=\"display:none;\">" + Catalog.GetString("Hide Styles") + "</span></a></span>" + Environment.NewLine);
			writer.RenderEndTag (); //div toggle
			writer.RenderEndTag (); //div styleboxcontainer
			writer.RenderEndTag (); //container1

			WriteFooter (writer);
			
			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html
			
			writer.Close ();
			stream.Close ();
		}
	}
}
