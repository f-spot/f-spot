/*
 * DevelopInUFraw.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.IO;

using Mono.Unix;

using FSpot;
using FSpot.Utils;
using FSpot.Extensions;
using FSpot.UI.Dialog;

namespace DevelopInUFRawExtension
{
	// GUI Version
	public class DevelopInUFRaw : AbstractDevelopInUFRaw {
		public DevelopInUFRaw() : base("ufraw")
		{
		}

		public override void Run (object o, EventArgs e)
		{
			Log.Information ("Executing DevelopInUFRaw extension");

			foreach (Photo p in MainWindow.Toplevel.SelectedPhotos ()) {
				DevelopPhoto (p);
			}
		}
	}

	// Batch Version
	public class DevelopInUFRawBatch : AbstractDevelopInUFRaw {
		public DevelopInUFRawBatch() : base("ufraw-batch")
		{
		}

		public override void Run (object o, EventArgs e)
		{
			ProgressDialog pdialog = new ProgressDialog(Catalog.GetString ("Developing photos"),
														ProgressDialog.CancelButtonType.Cancel,
														MainWindow.Toplevel.SelectedPhotos ().Length,
														MainWindow.Toplevel.Window);
			Log.Information ("Executing DevelopInUFRaw extension in batch mode");

			foreach (Photo p in MainWindow.Toplevel.SelectedPhotos ()) {
				bool cancelled = pdialog.Update(String.Format(Catalog.GetString ("Developing {0}"), p.Name));
				if (cancelled) {
					break;
				}

				DevelopPhoto (p);
			}
			pdialog.Destroy();
		}
	}

	// Abstract version, contains shared functionality
	public abstract class AbstractDevelopInUFRaw : ICommand
	{
		// The executable used for developing RAWs
		private string executable;

		public const string APP_FSPOT_EXTENSION = Preferences.APP_FSPOT + "extension/";
		public const string EXTENSION_DEVELOPINUFRAW = "developinufraw/";
		public const string UFRAW_JPEG_QUALITY_KEY = APP_FSPOT_EXTENSION + EXTENSION_DEVELOPINUFRAW + "ufraw_jpeg_quality";
		public const string UFRAW_ARGUMENTS_KEY = APP_FSPOT_EXTENSION + EXTENSION_DEVELOPINUFRAW + "ufraw_arguments";
		public const string UFRAW_BATCH_ARGUMENTS_KEY = APP_FSPOT_EXTENSION + EXTENSION_DEVELOPINUFRAW + "ufraw_batch_arguments";

		int ufraw_jpeg_quality;
		string ufraw_args;
		string ufraw_batch_args;

		public AbstractDevelopInUFRaw(string executable)
		{
			this.executable = executable;
		}

		public abstract void Run (object o, EventArgs e);

		protected void DevelopPhoto (Photo p)
		{
			LoadPreference (UFRAW_JPEG_QUALITY_KEY);
			LoadPreference (UFRAW_ARGUMENTS_KEY);
			LoadPreference (UFRAW_BATCH_ARGUMENTS_KEY);

			PhotoVersion raw = p.GetVersion (Photo.OriginalVersionId) as PhotoVersion;
			if (!ImageFile.IsRaw (raw.Uri.AbsolutePath)) {
				Log.Warning ("The original version of this image is not a (supported) RAW file");
				return;
			}

			string name = GetVersionName (p);
			System.Uri developed = GetUriForVersionName (p, name);
			string idfile = "";


			if (ufraw_jpeg_quality < 1 || ufraw_jpeg_quality > 100) {
				Log.Debug ("Invalid JPEG quality specified, defaulting to quality 98");
				ufraw_jpeg_quality = 98;
			}

			string args = "";
			switch (executable) {
				case "ufraw":
					args += ufraw_args;
					if (GLib.FileFactory.NewForUri (Path.ChangeExtension (raw.Uri.ToString (), ".ufraw")).Exists) {
						// We found an ID file, use that instead of the raw file
						idfile = "--conf=" + GLib.Shell.Quote (Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
					}
					break;
				case "ufraw-batch":
					args += ufraw_batch_args;
					if (GLib.FileFactory.NewForUri (Path.Combine (FSpot.Global.BaseDirectory, "batch.ufraw")).Exists) {
						// We found an ID file, use that instead of the raw file
						idfile = "--conf=" + GLib.Shell.Quote (Path.Combine (FSpot.Global.BaseDirectory, "batch.ufraw"));
					}
					break;
			}

			args += String.Format(" --overwrite --create-id=also --compression={0} --out-type=jpeg {1} --output={2} {3}",
				ufraw_jpeg_quality,
				idfile,
				GLib.Shell.Quote (developed.LocalPath),
				GLib.Shell.Quote (raw.Uri.LocalPath));
			Log.Debug (executable + " " + args);

			System.Diagnostics.Process ufraw = System.Diagnostics.Process.Start (executable, args);
			ufraw.WaitForExit ();
			if (!(GLib.FileFactory.NewForUri (developed.ToString ())).Exists) {
				Log.Warning ("UFRaw quit with an error. Check that you have UFRaw 0.13 or newer. Or did you simply clicked on Cancel?");
				return;
			}

			if (GLib.FileFactory.NewForUri (Path.ChangeExtension (developed.ToString (), ".ufraw")).Exists) {
				// We save our own copy of the last ufraw settings, as ufraw can overwrite it's own last used settings outside f-spot
				File.Delete (Path.Combine (FSpot.Global.BaseDirectory, "batch.ufraw"));
				File.Copy (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.Combine (FSpot.Global.BaseDirectory, "batch.ufraw"));

				// Rename the ufraw file to match the original RAW filename, instead of the (Developed In UFRaw) filename
				File.Delete (Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
				File.Move (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
			}

			p.DefaultVersionId = p.AddVersion (developed, name, true);
			p.Changes.DataChanged = true;
			Core.Database.Photos.Commit (p);
		}

		private static string GetVersionName (Photo p)
		{
			return GetVersionName (p, 1);
		}

		private static string GetVersionName (Photo p, int i)
		{
			string name = Catalog.GetPluralString ("Developed in UFRaw", "Developed in UFRaw ({0})", i);
			name = String.Format (name, i);
			if (p.VersionNameExists (name))
				return GetVersionName (p, i + 1);
			return name;
		}

		private System.Uri GetUriForVersionName (Photo p, string version_name)
		{
			string name_without_ext = System.IO.Path.GetFileNameWithoutExtension (p.Name);
			return new System.Uri (System.IO.Path.Combine (DirectoryPath (p),  name_without_ext
					       + " (" + version_name + ")" + ".jpg"));
		}

		private static string DirectoryPath (Photo p)
		{
			System.Uri uri = p.VersionUri (Photo.OriginalVersionId);
			return uri.Scheme + "://" + uri.Host + System.IO.Path.GetDirectoryName (uri.AbsolutePath);
		}

		void LoadPreference (string key)
		{
			switch (key) {
				case UFRAW_JPEG_QUALITY_KEY:
					ufraw_jpeg_quality = Preferences.Get<int> (key);
					break;
				case UFRAW_ARGUMENTS_KEY:
					ufraw_args = Preferences.Get<string> (key);
					break;
				case UFRAW_BATCH_ARGUMENTS_KEY:
					ufraw_batch_args = Preferences.Get<string> (key);
					break;
			}
		}

	}
}
