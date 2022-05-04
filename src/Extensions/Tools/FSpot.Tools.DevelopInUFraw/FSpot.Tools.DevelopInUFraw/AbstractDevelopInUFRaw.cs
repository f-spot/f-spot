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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Settings;
using FSpot.Utils;

using Hyena;


namespace FSpot.Tools.DevelopInUFraw
{
	// Abstract version, contains shared functionality
	public abstract class AbstractDevelopInUFRaw : ICommand
	{
		// The executable used for developing RAWs
		readonly string executable;

		public const string DevelopInUfraw = Preferences.ExtensionKey + "DevelopInUfraw/";
		public const string UfrawJpegQualityKey = DevelopInUfraw + "JpegQuality";
		public const string UfrawArgumentsKey = DevelopInUfraw + "Arguments";
		public const string UfrawBatchArgumentsKey = DevelopInUfraw + "BatchArguments";

		int ufraw_jpeg_quality;
		string ufraw_args;
		string ufraw_batch_args;

		public AbstractDevelopInUFRaw (string executable)
		{
			this.executable = executable;
		}

		public abstract void Run (object o, EventArgs e);

		protected void DevelopPhoto (Photo p)
		{
			LoadPreference (UfrawJpegQualityKey);
			LoadPreference (UfrawArgumentsKey);
			LoadPreference (UfrawBatchArgumentsKey);

			var raw = p.GetVersion (Photo.OriginalVersionId) as PhotoVersion;
			if (!App.Instance.Container.Resolve<IImageFileFactory> ().IsRaw (raw.Uri)) {
				Logger.Log.Warning ("The original version of this image is not a (supported) RAW file");
				return;
			}

			string name = GetVersionName (p);
			System.Uri developed = GetUriForVersionName (p, name);
			string idfile = "";


			if (ufraw_jpeg_quality < 1 || ufraw_jpeg_quality > 100) {
				Logger.Log.Debug ("Invalid JPEG quality specified, defaulting to quality 98");
				ufraw_jpeg_quality = 98;
			}

			string args = "";
			switch (executable) {
			case "ufraw":
				args += ufraw_args;
				if (File.Exists (Path.ChangeExtension (raw.Uri.ToString (), ".ufraw"))) {
					// We found an ID file, use that instead of the raw file
					idfile = "--conf=" + GLib.Shell.Quote (Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
				}
				break;
			case "ufraw-batch":
				args += ufraw_batch_args;
				if (File.Exists (Path.Combine (FSpotConfiguration.BaseDirectory, "batch.ufraw"))) {
					// We found an ID file, use that instead of the raw file
					idfile = "--conf=" + GLib.Shell.Quote (Path.Combine (FSpotConfiguration.BaseDirectory, "batch.ufraw"));
				}
				break;
			}

			args += string.Format (" --overwrite --create-id=also --compression={0} --out-type=jpeg {1} --output={2} {3}",
				ufraw_jpeg_quality,
				idfile,
				GLib.Shell.Quote (developed.LocalPath),
				GLib.Shell.Quote (raw.Uri.LocalPath));
			Logger.Log.Debug (executable + " " + args);

			var ufraw = System.Diagnostics.Process.Start (executable, args);
			ufraw.WaitForExit ();
			if (!File.Exists (developed.ToString ())) {
				Logger.Log.Warning ("UFRaw quit with an error. Check that you have UFRaw 0.13 or newer. Or did you simply clicked on Cancel?");
				return;
			}

			if (File.Exists (Path.ChangeExtension (developed.ToString (), ".ufraw"))) {
				// We save our own copy of the last ufraw settings, as ufraw can overwrite it's own last used settings outside f-spot
				File.Delete (Path.Combine (FSpotConfiguration.BaseDirectory, "batch.ufraw"));
				File.Copy (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.Combine (FSpotConfiguration.BaseDirectory, "batch.ufraw"));

				// Rename the ufraw file to match the original RAW filename, instead of the (Developed In UFRaw) filename
				File.Delete (Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
				File.Move (Path.ChangeExtension (developed.LocalPath, ".ufraw"), Path.ChangeExtension (raw.Uri.LocalPath, ".ufraw"));
			}

			p.DefaultVersionId = p.AddVersion (new SafeUri (developed).GetBaseUri (), new SafeUri (developed).GetFilename (), name, true);
			p.Changes.DataChanged = true;
			App.Instance.Database.Photos.Commit (p);
		}

		static string GetVersionName (Photo p)
		{
			return GetVersionName (p, 1);
		}

		static string GetVersionName (Photo p, int i)
		{
			string name = i <= 1 ? Strings.DevelopedInUFRaw : Strings.DevelopedInUFRawX;
			name = string.Format (name, i);
			if (p.VersionNameExists (name))
				return GetVersionName (p, i + 1);
			return name;
		}

		System.Uri GetUriForVersionName (Photo p, string version_name)
		{
			string name_without_ext = Path.GetFileNameWithoutExtension (p.Name);
			return new System.Uri (Path.Combine (DirectoryPath (p), name_without_ext
						   + " (" + version_name + ")" + ".jpg"));
		}

		static string DirectoryPath (Photo p)
		{
			return p.VersionUri (Photo.OriginalVersionId).GetBaseUri ();
		}

		void LoadPreference (string key)
		{
			switch (key) {
			case UfrawJpegQualityKey:
				ufraw_jpeg_quality = Preferences.Get<int> (key);
				break;
			case UfrawArgumentsKey:
				ufraw_args = Preferences.Get<string> (key);
				break;
			case UfrawBatchArgumentsKey:
				ufraw_batch_args = Preferences.Get<string> (key);
				break;
			}
		}
	}
}
