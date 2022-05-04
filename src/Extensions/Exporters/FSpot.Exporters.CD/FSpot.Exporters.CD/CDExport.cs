//
// CDExport.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (c) 2012 SUSE LINUX Products GmbH, Nuernberg, Germany.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;
using FSpot.Filters;
using FSpot.Resources.Lang;
using FSpot.UI.Dialog;

using Gtk;

using Hyena;

namespace FSpot.Exporters.CD
{
	public class CDExport : FSpot.Extensions.IExporter
	{
		IBrowsableCollection selection;
		IDiscBurner burner;

		Uri dest = new Uri ("burn:///");

		int photo_index;
		bool clean;

		CDExportDialog dialog;
		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public CDExport ()
		{
			burner = new Brasero ();
		}

		public void Run (IBrowsableCollection selection)
		{
			this.selection = selection;
			dialog = new CDExportDialog (selection, dest);

			if (dialog.Run () != (int)ResponseType.Ok) {
				dialog.Destroy ();
				return;
			}

			clean = dialog.RemovePreviousPhotos;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Transfer)) {
				Name = Strings.TransferringPictures
			};

			progress_dialog = new ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();

			dialog.Destroy ();
		}

		//FIXME: rewrite this as a Filter
		public static FileInfo UniqueName (Uri path, string shortname)
		{
			int i = 1;
			var dest = new FileInfo (new Uri (path, shortname).AbsolutePath);
			while (dest.Exists) {
				string numbered_name = $"{Path.GetFileNameWithoutExtension (shortname)}-{i++}{Path.GetExtension (shortname)}";

				dest = new FileInfo (new Uri (path, numbered_name).AbsolutePath);
			}

			return dest;
		}

		void Clean (Uri path)
			=> new DirectoryInfo (path.AbsolutePath).Delete (true);

		// FIXME, fix progress reporting
		public void Transfer ()
		{
			try {
				var result = true;

				if (clean)
					Clean (dest);

				foreach (IPhoto photo in selection.Items) {

					//FIXME need to implement the uniquename as a filter
					using (var request = new FilterRequest (photo.DefaultVersion.Uri)) {
						var source = new FileInfo (request.Current.AbsolutePath);
						var target = UniqueName (dest, photo.Name);

						progress_dialog.Message = string.Format (Strings.TransferringPictureXToCD, photo.Name);
						progress_dialog.Fraction = photo_index / (double)selection.Count;
						progress_dialog.ProgressText = string.Format (Strings.XOfY, photo_index, selection.Count);

						try {
							source.CopyTo (target.ToString ());
						} catch (Exception) {
							result &= false;
						}
					}
					photo_index++;
				}

				// FIXME the error dialog here is ugly and needs improvement when strings are not frozen.
				if (result) {
					progress_dialog.Message = Strings.DoneSendingPhotos;
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Strings.TransferComplete;
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
					progress_dialog.Hide ();
					burner.Run ();
				} else
					throw new Exception ($"{progress_dialog.Message}{Environment.NewLine}{Strings.ErrorWhileTransferring}{Environment.NewLine}{result}");

			} catch (Exception e) {
				Logger.Log.Debug (e, "");
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Strings.ErrorTransferring;
				return;
			}
			ThreadAssist.ProxyToMain (() => {
				progress_dialog.Destroy ();
			});
		}

		void Progress (long current_num_bytes, long total_num_bytes)
		{
			progress_dialog.ProgressText = Strings.Copying;

			if (total_num_bytes > 0)
				progress_dialog.Fraction = current_num_bytes / (double)total_num_bytes;
		}
	}
}
