//
// AbstractDevelopInUFRaw.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2008-2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

using Mono.Unix;

using Hyena;

using FSpot;
using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.Settings;
using FSpot.Utils;

namespace FSpot.Tools.DevelopInUFraw
{
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
			if (!App.Instance.Container.Resolve<IImageFileFactory> ().IsRaw (raw.Uri)) {
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
					if (GLib.FileFactory.NewForUri (Path.Combine (Global.BaseDirectory, "batch.ufraw")).Exists) {
						// We found an ID file, use that instead of the raw file
						idfile = "--conf=" + GLib.Shell.Quote (Path.Combine (Global.BaseDirectory, "batch.ufraw"));
					}
					break;
			}

			args += string.Format(" --overwrite --create-id=also --compression={0} --out-type=jpeg {1} --output={2} {3}",
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
				File.Delete (Path.Combine (Global.BaseDirectory, "batch.ufraw"));
				File.Copy (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.Combine (Global.BaseDirectory, "batch.ufraw"));

				// Rename the ufraw file to match the original RAW filename, instead of the (Developed In UFRaw) filename
				File.Delete (Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
				File.Move (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
			}

			p.DefaultVersionId = p.AddVersion (new SafeUri (developed).GetBaseUri (),new SafeUri (developed).GetFilename (), name, true);
			p.Changes.DataChanged = true;
			App.Instance.Database.Photos.Commit (p);
		}

		private static string GetVersionName (Photo p)
		{
			return GetVersionName (p, 1);
		}

		private static string GetVersionName (Photo p, int i)
		{
			string name = Catalog.GetPluralString ("Developed in UFRaw", "Developed in UFRaw ({0})", i);
			name = string.Format (name, i);
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
			return p.VersionUri (Photo.OriginalVersionId).GetBaseUri ();
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
