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

using FSpot;
using FSpot.Extensions;
using Mono.Unix;

namespace DevelopInUFRawExtension
{
	// GUI Version
	public class DevelopInUFRaw : AbstractDevelopInUFRaw {
		public DevelopInUFRaw() : base("ufraw")
		{
		}

		public override void Run (object o, EventArgs e)
		{
			Console.WriteLine ("EXECUTING DEVELOP IN UFRAW EXTENSION");
			
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
			Console.WriteLine ("EXECUTING DEVELOP IN UFRAW EXTENSION");
			
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

		public AbstractDevelopInUFRaw(string executable) 
		{
			this.executable = executable;
		}

		public abstract void Run (object o, EventArgs e);

		protected void DevelopPhoto (Photo p)
		{
			PhotoVersion raw = p.GetVersion (Photo.OriginalVersionId) as PhotoVersion;
			if (!ImageFile.IsRaw (raw.Uri.AbsolutePath)) {
				Console.WriteLine ("The Original version of this image is not a (supported) RAW file");
				return;
			}

			string name = GetVersionName (p);
			System.Uri developed = GetUriForVersionName (p, name);
			string idfile = "";

			if (new Gnome.Vfs.Uri (Path.ChangeExtension (raw.Uri.ToString (), ".ufraw")).Exists) {
				// We found an ID file, use that instead of the raw file
				idfile = "--conf=" + Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw");
			}

			string args = String.Format("--overwrite --create-id=also --compression=98 --out-type=jpeg {0} --output={1} {2}", 
				idfile,
				CheapEscape (developed.LocalPath),
				CheapEscape (raw.Uri.ToString ()));
			Console.WriteLine (executable+" " + args);

			System.Diagnostics.Process ufraw = System.Diagnostics.Process.Start (executable, args); 
			ufraw.WaitForExit ();
			if (!(new Gnome.Vfs.Uri (developed.ToString ())).Exists) {
				Console.WriteLine ("UFraw didn't ended well. Check that you have UFRaw 0.13 (or CVS newer than 2007-09-06). Or did you simply clicked on Cancel ?");
				return;
			}

			if (new Gnome.Vfs.Uri (Path.ChangeExtension (developed.ToString (), ".ufraw")).Exists) {
				if (new Gnome.Vfs.Uri (Path.ChangeExtension (raw.Uri.ToString (), ".ufraw")).Exists) {
					File.Delete (Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
				}
				File.Move (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
			}

			p.DefaultVersionId = p.AddVersion (developed, name, true);
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

		private static string CheapEscape (string input)
		{
			string escaped = input;
			escaped = escaped.Replace (" ", "\\ ");
			escaped = escaped.Replace ("(", "\\(");
			escaped = escaped.Replace (")", "\\)");
			return escaped;
		}
		
		private static string DirectoryPath (Photo p)
		{
			System.Uri uri = p.VersionUri (Photo.OriginalVersionId);
			return uri.Scheme + "://" + uri.Host + System.IO.Path.GetDirectoryName (uri.AbsolutePath);
		}
	}
}
