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
	public class OriginalExport : GladeDialog {
		IPhotoCollection selection;
		[Glade.Widget] Gtk.ScrolledWindow thumb_scrolledwindow;
		[Glade.Widget] Gtk.Entry uri_entry;
		[Glade.Widget] Gtk.Entry name_entry;

		//[Glade.Widget] Gtk.CheckButton meta_check;
		[Glade.Widget] Gtk.CheckButton scale_check;
		[Glade.Widget] Gtk.CheckButton open_check;

		[Glade.Widget] Gtk.SpinButton size_spin;

		Gnome.Vfs.Uri dest;
		
		int photo_index;
		bool open;
		bool scale;

		int size;
		
		string gallery_name = "Web-Gallery";
		// FIME this needs to be a real temp directory
		string gallery_path = Path.Combine (Path.GetTempPath (), "f-spot-original-" + System.DateTime.Now.Ticks.ToString ());

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		
		public OriginalExport (IPhotoCollection selection) : base ("original_export_dialog")
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

				OriginalGallery gallery = new OriginalGallery(selection, gallery_path, gallery_name);

				if (scale)
					gallery.Size = size;

				gallery.StartProcessing ();

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
					Gnome.Url.Show (target.ToString ());

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

	class OriginalGallery
	{
		private IPhotoCollection selection;
		private string gallery_name;
		private string gallery_path;
		private bool setmtime = false;
		private bool scale = false;
		private int size;
		private int photo_index = 1; //used to name files

		FSpot.ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;
		
		public OriginalGallery (IPhotoCollection selection, string path, string gallery_name)
		{
			this.selection = selection;
			this.gallery_name = gallery_name;
			this.gallery_path = Path.Combine (path, gallery_name);
		}

		public void StartProcessing()
		{
			MakeDirs();
			CreateHtaccess();
			CreateInfo();
			StartAll ();
		}
		
		private void StartAll()
		{
			//FIXME: when used in a try statement, a System.NullReferenceException
			// is thrown after completion.
			try {
				foreach (Photo photo in selection.Photos) {
					CreateImage (photo.Path);
					CreateComments(photo.Path);

					//Set the directory's mtime sa the oldest photo's one.
					if (!setmtime)
					{
						try {
							Directory.SetLastWriteTimeUtc(gallery_path, photo.Time);
							setmtime = true;
						} catch { 
							setmtime = false; 
						}
					}
				
					photo_index++;


				}

				if (System.IO.Directory.Exists (SubdirPath ("mq")))
				    CreateZipFile("mq");

				if (System.IO.Directory.Exists (SubdirPath ("hq")))
				    CreateZipFile("hq");
			
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			} 
		}

		private void MakeDir (string path)
		{
			try {
				Directory.CreateDirectory (path);
			} catch {
				Console.WriteLine ("Error in creating directory " + path);
			}
		}

		private void MakeDirs()
		{
			//FIXME: Create this with 0700 mode in the case it is placed in /tmp
			MakeDir (gallery_path);
			MakeDir (SubdirPath ("thumbs"));
			MakeDir (SubdirPath ("comments"));
			MakeDir (SubdirPath ("zip"));
		}

		private void CreateImage (string photo_path)
		{
			string img_name = "img-" + photo_index + ".jpg";
			
			string [] keys = {"quality"};
			string [] values = {"75"};

			// scale the images to different sizes
			
			// High quality is the original image
			string path = SubdirPath ("hq", img_name);
			MakeDir (SubdirPath ("hq"));
			if (!scale) 
				File.Copy(photo_path, path, true);
			else 
				PixbufUtils.Resize (photo_path, path, size, true); 

			Gdk.Pixbuf source_image = PixbufUtils.LoadAtMaxSize (path, 800, 600);
			//Gdk.Pixbuf source_image = PhotoLoader.Load (path);
			Gdk.Pixbuf scaled;
			// Medium Quality
			if (!scale || size > 800) {
				MakeDir (SubdirPath ("mq"));
				path = SubdirPath ("mq", img_name);
				source_image.Savev(path, "jpeg", keys, values);
			}

			//low quality
			scaled = PixbufUtils.ScaleToMaxSize (source_image, 640, 480, false);
			MakeDir (SubdirPath ("lq"));
			path = SubdirPath ("lq", img_name);
			scaled.Savev(path, "jpeg", keys, values);
			source_image.Dispose ();
			source_image = scaled;

			// Thumbnail
			scaled = PixbufUtils.ScaleToMaxSize (source_image, 120, 90, false);
			path = SubdirPath ("thumbs", img_name);
			scaled.Savev(path, "jpeg", keys, values);
			source_image.Dispose ();
			scaled.Dispose ();
		}
		
		private string SubdirPath (string subdir)
		{
			return SubdirPath (subdir, null);
		}
		
		private string SubdirPath (string subdir, string file)
		{
			string path = Path.Combine (gallery_path, subdir);
			if (file != null)
				path = Path.Combine (path, file);

			return path;
		}

		private void CreateComments(string photo_path)
		{
			StreamWriter comment = File.CreateText(SubdirPath  ("comments", "img-" + photo_index + "txt"));
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
			info.WriteLine("date|" + selection.Photos[0].Time.Date.ToString ("dd.MM.yyyy"));
			info.Close();
		}
		
		public int Size {
			get {
				return size;
			}
			set {
				scale = true;
				size = value;
			}
		}

		// This is provided in order to pass the name as an argument through the
		// dialog window.
		
		public string GalleryPath {
			get {
				return gallery_path;
			}
		}
	}
}
