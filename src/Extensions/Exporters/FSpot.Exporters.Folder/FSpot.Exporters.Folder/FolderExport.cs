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
//located on a GIO location.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;

using Hyena;

using Mono.Unix;

using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;

using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;

namespace FSpot.Exporters.Folder {
	public class FolderExport : FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		[Glade.Widget] Gtk.Dialog dialog;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Entry name_entry;
		[Glade.Widget] Gtk.Entry description_entry;

		//[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton export_tags_check;
		[Glade.Widget] Gtk.CheckButton export_tag_icons_check;
		[Glade.Widget] Gtk.CheckButton open_check;

		[Glade.Widget] Gtk.RadioButton static_radio;
		[Glade.Widget] Gtk.RadioButton original_radio;
		[Glade.Widget] Gtk.RadioButton plain_radio;

		[Glade.Widget] Gtk.SpinButton size_spin;

		[Glade.Widget] Gtk.HBox chooser_hbox;

		public const string EXPORT_SERVICE = "folder/";
		public const string SCALE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "scale";
		public const string SIZE_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "size";
		public const string OPEN_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "browser";
		public const string EXPORT_TAGS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "export_tags";
		public const string EXPORT_TAG_ICONS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "export_tag_icons";
		public const string METHOD_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "method";
		public const string URI_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "uri";
		public const string SHARPEN_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "sharpen";
		public const string INCLUDE_TARBALLS_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "include_tarballs";

		private Glade.XML xml;
		private string dialog_name = "folder_export_dialog";
		GLib.File dest;
		Gtk.FileChooserButton uri_chooser;

		bool open;
		bool scale;
		bool exportTags;
		bool exportTagIcons;
		int size;

		string description;
		string gallery_name = "Gallery";
		// FIXME this needs to be a real temp directory
		string gallery_path = Path.Combine (Path.GetTempPath (), "f-spot-original-" + System.DateTime.Now.Ticks.ToString ());

		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public FolderExport ()
		{}
		public void Run (IBrowsableCollection selection)
		{
			this.selection = selection;

			IconView view = (IconView) new IconView (selection);
			view.DisplayDates = false;
			view.DisplayTags = false;

			xml = new Glade.XML (null, "FolderExport.glade", dialog_name, "f-spot");
			xml.Autoconnect (this);
			Dialog.Modal = false;
			Dialog.TransientFor = null;

			thumb_scrolledwindow.Add (view);
			HandleSizeActive (null, null);
			name_entry.Text = gallery_name;

			string uri_path = System.IO.Path.Combine (FSpot.Core.Global.HomeDirectory, "Desktop");
			if (!System.IO.Directory.Exists (uri_path))
			        uri_path = FSpot.Core.Global.HomeDirectory;

			uri_chooser = new Gtk.FileChooserButton (Catalog.GetString ("Select Export Folder"),
								 Gtk.FileChooserAction.SelectFolder);

			uri_chooser.LocalOnly = false;

			if (!String.IsNullOrEmpty (Preferences.Get<string> (URI_KEY)))
				uri_chooser.SetCurrentFolderUri (Preferences.Get<string> (URI_KEY));
			else
				uri_chooser.SetFilename (uri_path);

			chooser_hbox.PackStart (uri_chooser);

			Dialog.ShowAll ();
			Dialog.Response += HandleResponse;

			LoadPreference (SCALE_KEY);
			LoadPreference (SIZE_KEY);
			LoadPreference (OPEN_KEY);
			LoadPreference (EXPORT_TAGS_KEY);
			LoadPreference (EXPORT_TAG_ICONS_KEY);
			LoadPreference (METHOD_KEY);
		}

		public void HandleSizeActive (object sender, System.EventArgs args)
		{
			size_spin.Sensitive = scale_check.Active;
		}

		public void HandleStandaloneActive (object sender, System.EventArgs args)
		{
			export_tags_check.Sensitive = static_radio.Active;
			HandleExportTagsActive (sender, args);
		}

		public void HandleExportTagsActive (object sender, System.EventArgs args)
		{
			export_tag_icons_check.Sensitive = export_tags_check.Active && static_radio.Active;
		}

		public void Upload ()
		{
			// FIXME use mkstemp

			try {
				ThreadAssist.ProxyToMain (Dialog.Hide);

				GLib.File source = GLib.FileFactory.NewForPath (Path.Combine (gallery_path, gallery_name));
				GLib.File target = GLib.FileFactory.NewForPath (Path.Combine (dest.Path, source.Basename));

				if (dest.IsNative)
					gallery_path = dest.Path;

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
					Log.DebugFormat ("Resize Photos to {0}.", size);
					gallery.SetScale (size);
				} else {
					Log.Debug ("Exporting full size.");
				}

				if (exportTags)
					gallery.SetExportTags ();

				if (exportTagIcons)
					gallery.SetExportTagIcons ();

				gallery.Description = description;
				gallery.GenerateLayout ();
				
				FilterSet filter_set = new FilterSet ();
				if (scale)
					filter_set.Add (new ResizeFilter ((uint) size));
				filter_set.Add (new ChmodFilter ());
				filter_set.Add (new UniqueNameFilter (new SafeUri (gallery_path)));

				for (int photo_index = 0; photo_index < selection.Count; photo_index++)
				{
					try {
						progress_dialog.Message = System.String.Format (Catalog.GetString ("Exporting \"{0}\"..."), selection[photo_index].Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						gallery.ProcessImage (photo_index, filter_set);
						progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"), (photo_index + 1), selection.Count);
					}
					catch (Exception e) {
						Log.Error (e.ToString ());
						progress_dialog.Message = String.Format (Catalog.GetString ("Error Copying \"{0}\" to Gallery:{2}{1}"),
							selection[photo_index].Name, e.Message, Environment.NewLine);
						progress_dialog.ProgressText = Catalog.GetString ("Error");

						if (progress_dialog.PerformRetrySkip ())
							photo_index--;
					}
				}

				// create the zip tarballs for original
				if (gallery is OriginalGallery) {
					bool include_tarballs;
					try {
						include_tarballs = Preferences.Get<bool> (INCLUDE_TARBALLS_KEY);
					} catch (NullReferenceException){
						include_tarballs = true;
						Preferences.Set (INCLUDE_TARBALLS_KEY, true);
					}
					if (include_tarballs)
						(gallery as OriginalGallery).CreateZip ();
				}

				// we've created the structure, now if the destination was local (native) we are done
				// otherwise we xfer
				if (!dest.IsNative) {
					Log.DebugFormat ("Transferring \"{0}\" to \"{1}\"", source.Path, target.Path);
					progress_dialog.Message = String.Format (Catalog.GetString ("Transferring to \"{0}\""), target.Path);
					progress_dialog.ProgressText = Catalog.GetString ("Transferring...");
					source.CopyRecursive (target, GLib.FileCopyFlags.Overwrite, new GLib.Cancellable (), Progress);
				}
				
				// No need to check result here as if result is not true, an Exception will be thrown before
				progress_dialog.Message = Catalog.GetString ("Export Complete.");
				progress_dialog.Fraction = 1.0;
				progress_dialog.ProgressText = Catalog.GetString ("Exporting Photos Completed.");
				progress_dialog.ButtonLabel = Gtk.Stock.Ok;

				if (open) {
					Log.DebugFormat (String.Format ("Open URI \"{0}\"", target.Uri.ToString ()));
					ThreadAssist.ProxyToMain (() => { GtkBeans.Global.ShowUri (Dialog.Screen, target.Uri.ToString () ); });
				}

				// Save these settings for next time
				Preferences.Set (SCALE_KEY, scale);
				Preferences.Set (SIZE_KEY, size);
				Preferences.Set (OPEN_KEY, open);
				Preferences.Set (EXPORT_TAGS_KEY, exportTags);
				Preferences.Set (EXPORT_TAG_ICONS_KEY, exportTagIcons);
				Preferences.Set (METHOD_KEY, static_radio.Active ? "static" : original_radio.Active ? "original" : "folder" );
				Preferences.Set (URI_KEY, uri_chooser.Uri);
			} catch (System.Exception e) {
				Log.Error (e.ToString ());
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Catalog.GetString ("Error Transferring");
			} finally {
				// if the destination isn't local then we want to remove the temp directory we
				// created.
				if (!dest.IsNative)
					System.IO.Directory.Delete (gallery_path, true);

				ThreadAssist.ProxyToMain (() => { Dialog.Destroy(); });
			}
		}

		private void Progress (long current_num_bytes, long total_num_bytes)
		{
			if (total_num_bytes > 0) {
				progress_dialog.Fraction = current_num_bytes / (double)total_num_bytes;
			}
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

			dest = GLib.FileFactory.NewForUri (uri_chooser.Uri);
			open = open_check.Active;
			scale = scale_check.Active;
			exportTags = export_tags_check.Active;
			exportTagIcons = export_tag_icons_check.Active;

			gallery_name = name_entry.Text;

			if (description_entry != null)
				description = description_entry.Text;

			if (scale)
				size = size_spin.ValueAsInt;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Upload));
			command_thread.Name = Catalog.GetString ("Exporting Photos");

			progress_dialog = new ThreadProgressDialog (command_thread, 1);
			progress_dialog.Start ();
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case SCALE_KEY:
				if (scale_check.Active != Preferences.Get<bool> (key))
					scale_check.Active = Preferences.Get<bool> (key);
				break;

			case SIZE_KEY:
				int size;
				if (Preferences.TryGet<int> (key, out size))
					size_spin.Value = (double) size;
				else
					size_spin.Value = 400;
				break;

			case OPEN_KEY:
				if (open_check.Active != Preferences.Get<bool> (key))
					open_check.Active = Preferences.Get<bool> (key);
				break;

			case EXPORT_TAGS_KEY:
				if (export_tags_check.Active != Preferences.Get<bool> (key))
					export_tags_check.Active = Preferences.Get<bool> (key);
				break;

			case EXPORT_TAG_ICONS_KEY:
				if (export_tag_icons_check.Active != Preferences.Get<bool> (key))
					export_tag_icons_check.Active = Preferences.Get<bool> (key);
				break;

			case METHOD_KEY:
				static_radio.Active = (Preferences.Get<string> (key) == "static");
				original_radio.Active = (Preferences.Get<string> (key) == "original");
				plain_radio.Active = (Preferences.Get<string> (key) == "folder");
				break;
			}
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = (Gtk.Dialog) xml.GetWidget (dialog_name);

				return dialog;
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
		protected bool exportTags;
		protected bool exportTagIcons;
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
            var uri = collection [image_num].DefaultVersion.Uri;
            var dest_uri = new SafeUri (gallery_path);

            // Find an unused name
            int i = 1;
            var dest = dest_uri.Append (uri.GetFilename ());
            var file = GLib.FileFactory.NewForUri (dest);
            while (file.Exists) {
                var filename = uri.GetFilenameWithoutExtension ();
                var extension = uri.GetExtension ();
                dest = dest_uri.Append (String.Format ("{0}-{1}{2}", filename, i++, extension));
                file = GLib.FileFactory.NewForUri (dest);
            }

            return dest.GetFilename ();
		}

		public void ProcessImage (int image_num, FilterSet filter_set)
		{
			IBrowsableItem photo = collection [image_num];
			string path;
			ScaleRequest req;

			req = requests [0];

			MakeDir (SubdirPath (req.Name));
			path = SubdirPath (req.Name, ImageName (image_num));

			using (FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri)) {
				filter_set.Convert (request);
				if (request.Current.LocalPath == path)
					request.Preserve(request.Current);
				else
					System.IO.File.Copy (request.Current.LocalPath, path, true);

				if (photo != null && photo is Photo && App.Instance.Database != null) {
					App.Instance.Database.Exports.Create ((photo as Photo).Id, (photo as Photo).DefaultVersionId,
								      ExportStore.FolderExportType,
								      // FIXME this is wrong, the final path is the one
								      // after the Xfer.
								      new SafeUri (path).ToString ());
				}

				for (int i = 1; i < requests.Length; i++) {

					req = requests [i];
					if (scale && req.AvoidScale (size))
						continue;

					FilterSet req_set = new FilterSet ();
					req_set.Add (new ResizeFilter ((uint)Math.Max (req.Width, req.Height)));

					bool sharpen;
					try {
						sharpen = Preferences.Get<bool> (FolderExport.SHARPEN_KEY);
					} catch (NullReferenceException) {
						sharpen = true;
						Preferences.Set (FolderExport.SHARPEN_KEY, true);
					}

					if (sharpen) {
						if (req.Name == "lq")
							req_set.Add (new SharpFilter (0.1, 2, 4));
						if (req.Name == "thumbs")
							req_set.Add (new SharpFilter (0.1, 2, 5));
					}
					using (FilterRequest tmp_req = new FilterRequest (photo.DefaultVersion.Uri)) {
						req_set.Convert (tmp_req);
						MakeDir (SubdirPath (req.Name));
						path = SubdirPath (req.Name, ImageName (image_num));
						System.IO.File.Copy (tmp_req.Current.LocalPath, path, true);
					}
				}
			}
		}

		protected string MakeDir (string path)
		{
			try {
				Directory.CreateDirectory (path);
			} catch {
				Log.ErrorFormat ("Error in creating directory \"{0}\"", path);
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

		public void SetExportTags () {
			this.exportTags = true;
		}

		public void SetExportTagIcons () {
			this.exportTagIcons = true;
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

		private void SetTime ()
		{
			try {
				for (int i = 0; i < collection.Count; i++)
					CreateComments (collection [i].DefaultVersion.Uri.LocalPath, i);

				Directory.SetLastWriteTimeUtc(gallery_path, collection [0].Time);
			} catch (System.Exception e) {
				Log.Error (e.ToString ());
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
				Log.Error (e.ToString ());
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
		int perpage = 16;
		string stylesheet = "f-spot-simple.css";
		string altstylesheet = "f-spot-simple-white.css";
		string javascript = "f-spot.js";

		//Note for translators: light as clear, opposite as dark
		static string light = Catalog.GetString("Light");
		static string dark = Catalog.GetString("Dark");

		ArrayList allTagNames = new ArrayList ();
		Hashtable allTags = new Hashtable ();
		Hashtable tagSets = new Hashtable ();

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

			if (exportTags) {
				// identify tags present in these photos
				i = 0;
				foreach (IBrowsableItem photo in photos) {
					foreach (var tag in photo.Tags) {
						if (!tagSets.ContainsKey (tag.Name)) {
							tagSets.Add (tag.Name, new ArrayList ());
							allTags.Add (tag.Name, tag);
						}
						((ArrayList) tagSets [tag.Name]).Add (i);
					}
					i++;
				}
				allTagNames = new ArrayList (tagSets.Keys);
				allTagNames.Sort ();

				// create tag pages
				SaveTagsPage ();
				foreach (string tag in allTagNames) {
					for (i = 0; i < TagPageCount (tag); i++)
						SaveTagIndex (tag, i);
				}
			}

			if (exportTags && exportTagIcons) {
				SaveTagIcons ();
			}

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

		public int TagPageCount (string tag)
		{
			return (int) System.Math.Ceiling (((ArrayList) tagSets [tag]).Count / (double)perpage);
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

			if (exportTags)
				WritePageNav (writer, "tagpage", TagsIndexPath (), Catalog.GetString ("Tags"));

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
			writer.AddAttribute ("class", "picture");
			writer.RenderBeginTag ("img");
			writer.RenderEndTag (); //img
			writer.RenderEndTag (); //a

			writer.AddAttribute ("id", "description");
			writer.RenderBeginTag ("div");
			writer.Write (collection [i].Description);
			writer.RenderEndTag (); //div#description

			writer.RenderEndTag (); //div.photo

			WriteTagsLinks (writer, collection [i].Tags);

			WriteStyleSelectionBox (writer);

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

		public static string TagsIndexPath ()
		{
			return "tags.html";
		}

		public static string TagIndexPath (string tag, int page_num)
		{
			string name = "tag_"+tag;
			name = name.Replace ("/", "_").Replace (" ","_");
			if (page_num == 0)
				return name + ".html";
			else
				return name + String.Format ("_{0}.html", page_num);
		}

		static string IndexTitle (int page)
		{
			return String.Format ("{0}", page + 1);
		}

		public void WriteHeader (System.Web.UI.HtmlTextWriter writer)
		{
			WriteHeader (writer, "");
		}

		public void WriteHeader (System.Web.UI.HtmlTextWriter writer, string titleExtension)
		{
			writer.RenderBeginTag ("head");
			/* It seems HtmlTextWriter always uses UTF-8, unless told otherwise */
			writer.Write ("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />");
			writer.WriteLine ();
			writer.RenderBeginTag ("title");
			writer.Write (gallery_name + titleExtension);
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

			writer.AddAttribute ("href", "http://f-spot.org");
			writer.RenderBeginTag ("a");
			writer.Write (String.Format ("{0} {1}", FSpot.Core.Defines.PACKAGE, FSpot.Core.Defines.VERSION));
			writer.RenderEndTag ();

			writer.RenderEndTag ();
		}

		public static void WriteStyleSelectionBox (System.Web.UI.HtmlTextWriter writer)
		{
			//Style Selection Box
			writer.AddAttribute ("id", "styleboxcontainer");
			writer.RenderBeginTag ("div");
			writer.AddAttribute ("id", "stylebox");
			writer.AddAttribute ("style", "display: none;");
			writer.RenderBeginTag ("div");
			writer.RenderBeginTag ("ul");
			writer.RenderBeginTag ("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", dark);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + dark + "')");
			writer.RenderBeginTag ("a");
			writer.Write (dark);
			writer.RenderEndTag (); //a
			writer.RenderEndTag (); //li
			writer.RenderBeginTag ("li");
			writer.AddAttribute ("href", "#");
			writer.AddAttribute ("title", light);
			writer.AddAttribute ("onclick", "setActiveStyleSheet('" + light + "')");
			writer.RenderBeginTag ("a");
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
		}

		public void WriteTagsLinks (System.Web.UI.HtmlTextWriter writer, Tag[] tags)
		{
			ArrayList tagsList = new ArrayList (tags.Length);
			foreach (var tag in tags) {
				tagsList.Add (tag);
			}
			WriteTagsLinks (writer, tagsList);
		}

		public void WriteTagsLinks (System.Web.UI.HtmlTextWriter writer, System.Collections.ICollection tags)
		{

			// check if we should write tags
			if (!exportTags && tags.Count>0)
				return;

			writer.AddAttribute ("id", "tagbox");
			writer.RenderBeginTag ("div");
			writer.RenderBeginTag ("h1");
			writer.Write (Catalog.GetString ("Tags"));
			writer.RenderEndTag (); //h1
			writer.AddAttribute ("id", "innertagbox");
			writer.RenderBeginTag ("ul");
			foreach (Tag tag in tags) {
				writer.AddAttribute ("class", "tag");
				writer.RenderBeginTag ("li");
				writer.AddAttribute ("href", TagIndexPath (tag.Name, 0));
				writer.RenderBeginTag ("a");
				if (exportTagIcons) {
					writer.AddAttribute ("alt", tag.Name);
					writer.AddAttribute ("longdesc", Catalog.GetString ("Tags: ")+tag.Name);
					writer.AddAttribute ("title", Catalog.GetString ("Tags: ")+tag.Name);
					writer.AddAttribute ("src", TagPath (tag));
					writer.RenderBeginTag ("img");
					writer.RenderEndTag ();
				}
				writer.Write(" ");
				if (exportTagIcons)
					writer.AddAttribute ("class", "tagtext-icon");
				else
					writer.AddAttribute ("class", "tagtext-noicon");
				writer.RenderBeginTag ("span");
				writer.Write (tag.Name);
				writer.RenderEndTag (); //span.tagtext
				writer.RenderEndTag (); //a href
				writer.RenderEndTag (); //div.tag
			}
			writer.RenderEndTag (); //div#tagbox
		}

		public void SaveTagsPage ()
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (TagsIndexPath ()));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			string titleExtension = " " + Catalog.GetString ("Tags");
			WriteHeader (writer, titleExtension);

			writer.AddAttribute ("onload", "checkForTheme()");
			writer.AddAttribute ("id", "tagpage");
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (gallery_name + titleExtension);
			writer.RenderEndTag (); //title div

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "navipage");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", IndexPath (0));
			writer.RenderBeginTag ("a");
			writer.Write (Catalog.GetString ("Index"));
			writer.RenderEndTag (); //a

			writer.RenderEndTag (); //navipage
			writer.RenderEndTag (); //navi
			writer.RenderEndTag (); //header

			WriteTagsLinks (writer, allTags.Values);

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html

			writer.Close ();
			stream.Close ();
		}

		public void SaveTagIndex (string tag, int page_num)
		{
			System.IO.StreamWriter stream = System.IO.File.CreateText (SubdirPath (TagIndexPath (tag, page_num)));
			System.Web.UI.HtmlTextWriter writer = new System.Web.UI.HtmlTextWriter (stream);

			writer.WriteLine ("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">");
			writer.AddAttribute ("xmlns", "http://www.w3.org/1999/xhtml");
			writer.AddAttribute ("xml:lang", this.Language);
			writer.RenderBeginTag ("html");
			string titleExtension = ": " + tag;
			WriteHeader (writer, titleExtension);

			writer.AddAttribute ("onload", "checkForTheme()");
			writer.RenderBeginTag ("body");

			writer.AddAttribute ("class", "container1");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("class", "header");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("id", "title");
			writer.RenderBeginTag ("div");
			writer.Write (gallery_name + titleExtension);
			writer.RenderEndTag (); //title div

			writer.AddAttribute ("class", "navi");
			writer.RenderBeginTag ("div");

			// link to all photos
			writer.AddAttribute ("class", "navipage");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", IndexPath (0));
			writer.RenderBeginTag ("a");
			writer.Write ("Index");
			writer.RenderEndTag (); //a

			writer.RenderEndTag (); //navipage
			// end link to all photos

			// link to all tags
			writer.AddAttribute ("class", "navipage");
			writer.RenderBeginTag ("div");

			writer.AddAttribute ("href", TagsIndexPath ());
			writer.RenderBeginTag ("a");
			writer.Write ("Tags");
			writer.RenderEndTag (); //a

			writer.RenderEndTag (); //navipage
			// end link to all tags

			writer.AddAttribute ("class", "navilabel");
			writer.RenderBeginTag ("div");
			writer.Write (Catalog.GetString ("Page:"));
			writer.RenderEndTag (); //pages div

			int i;
			for (i = 0; i < TagPageCount (tag); i++) {
				writer.AddAttribute ("class", i == page_num ? "navipage-current" : "navipage");
				writer.RenderBeginTag ("div");

				writer.AddAttribute ("href", TagIndexPath (tag, i));
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
			ArrayList tagSet = (ArrayList) tagSets [tag];
			int end = Math.Min (start + perpage, tagSet.Count);
			for (i = start; i < end; i++) {
				writer.AddAttribute ("href", PhotoIndexPath ((int) tagSet [i]));
				writer.RenderBeginTag ("a");

				writer.AddAttribute  ("src", PhotoThumbPath ((int) tagSet [i]));
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

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html

			writer.Close ();
			stream.Close ();
		}

		public void SaveTagIcons ()
		{
			MakeDir (SubdirPath ("tags"));
			foreach (Tag tag in allTags.Values)
				SaveTagIcon (tag);
		}

		public void SaveTagIcon (Tag tag) {
			Gdk.Pixbuf icon = tag.Icon;
			Gdk.Pixbuf scaled = null;
			if (icon.Height != 52 || icon.Width != 52) {
				scaled=icon.ScaleSimple(52,52,Gdk.InterpType.Bilinear);
			} else
				scaled=icon.Copy ();
			scaled.Save (SubdirPath("tags",TagName(tag)), "png");
			scaled.Dispose ();
		}

		public string TagPath (Tag tag)
		{
			return System.IO.Path.Combine("tags",TagName(tag));
		}

		public string TagName (Tag tag)
		{
			return "tag_"+ ((DbItem)tag).Id+".png";
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

			if (exportTags) {
				// link to all tags
				writer.AddAttribute ("class", "navipage");
				writer.RenderBeginTag ("div");

				writer.AddAttribute ("href", TagsIndexPath ());
				writer.RenderBeginTag ("a");
				writer.Write ("Tags");
				writer.RenderEndTag (); //a

				writer.RenderEndTag (); //navipage
				// end link to all tags
			}

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

			WriteStyleSelectionBox (writer);

			writer.RenderEndTag (); //container1

			WriteFooter (writer);

			writer.RenderEndTag (); //body
			writer.RenderEndTag (); //html

			writer.Close ();
			stream.Close ();
		}

	}
}
