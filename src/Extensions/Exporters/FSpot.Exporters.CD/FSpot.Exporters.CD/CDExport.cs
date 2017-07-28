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
// Copyright (c) 2017 Stephen Shaw
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
using System.Threading.Tasks;
using Mono.Unix;

using FSpot.Core;
using FSpot.Filters;
using FSpot.UI.Dialog;

using Hyena;

using GLib;
using Gtk;

namespace FSpot.Exporters.CD
{
	public class CDExport : FSpot.Extensions.IExporter
	{
		IBrowsableCollection selection;
		readonly IDiscBurner burner;

		readonly Uri dest = new Uri ("burn:///");

		int photoIndex;
		bool clean;

		CDExportDialog dialog;
		TaskProgressDialog progressDialog;
		Task task;

		public CDExport ()
		{
			burner = new Brasero();
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

			task = new Task (Transfer);

			progressDialog = new TaskProgressDialog (task, Catalog.GetString ("Transferring Pictures"));
			progressDialog.Start ();

			dialog.Destroy ();
		}

		//FIXME: rewrite this as a Filter
		public static GLib.File UniqueName (Uri path, string shortname)
		{
				int i = 1;
				GLib.File dest = FileFactory.NewForUri (new Uri (path, shortname));
				while (dest.Exists) {
						var numberedName = $"{Path.GetFileNameWithoutExtension (shortname)}-{i++}{Path.GetExtension (shortname)}";

					dest = FileFactory.NewForUri (new Uri (path, numberedName));
				}

				return dest;
		}

		void Clean (Uri path)
		{
			GLib.File source = FileFactory.NewForUri (path);
			using (var children = source.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				foreach (GLib.FileInfo info in children) {
					if (info.FileType == FileType.Directory)
						Clean (new Uri (path, info.Name + "/"));
					FileFactory.NewForUri (new Uri (path, info.Name)).Delete ();
				}
			}
		}

		public void Transfer ()
		{
			try {
				bool result = true;

				if (clean)
					Clean (dest);

				foreach (IPhoto photo in selection.Items) {

					//FIXME need to implement the uniquename as a filter
					using (FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri)) {
						GLib.File source = FileFactory.NewForUri (request.Current.ToString ());
						GLib.File target = UniqueName (dest, photo.Name);
						FileProgressCallback cb = Progress;

						progressDialog.Message = string.Format (Catalog.GetString ("Transferring picture \"{0}\" To CD"), photo.Name);
						progressDialog.Fraction = photoIndex / (double)selection.Count;
						progressDialog.ProgressText = string.Format (Catalog.GetString ("{0} of {1}"), photoIndex, selection.Count);

						result &= source.Copy (target, FileCopyFlags.None, null, cb);
					}
					photoIndex++;
				}

				// FIXME the error dialog here is ugly and needs improvement when strings are not frozen.
				if (result) {
					progressDialog.Message = Catalog.GetString ("Done Sending Photos");
					progressDialog.Fraction = 1.0;
					progressDialog.ProgressText = Catalog.GetString ("Transfer Complete");
					progressDialog.ButtonLabel = Stock.Ok;
					progressDialog.Hide ();
					burner.Run ();
				} else
					throw new Exception ($"{progressDialog.Message}{Environment.NewLine}{Catalog.GetString ("Error While Transferring")}{Environment.NewLine}{false}");

			} catch (Exception e) {
				Hyena.Log.DebugException (e);
				progressDialog.Message = e.ToString ();
				progressDialog.ProgressText = Catalog.GetString ("Error Transferring");
				return;
			}
			ThreadAssist.ProxyToMain (() => {
				progressDialog.Destroy ();
			});
		}

		private void Progress (long currentNumBytes, long totalNumBytes)
		{
			progressDialog.ProgressText = Catalog.GetString ("copying...");

			if (totalNumBytes > 0)
				progressDialog.Fraction = currentNumBytes / (double)totalNumBytes;

		}

	}
}
