/*
 * CDExport.cs
 *
 * Authors:
 *   Larry Ewing <lewing@novell.com>
 *   Lorenzo Milesi <maxxer@yetopen.it>
 *
 * Copyright (c) 2007-2009 Novell, Inc.
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Mono.Unix;

using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using Hyena;
using FSpot.UI.Dialog;

using GLib;
using Gtk;
using GtkBeans;

namespace FSpot.Exporters.CD {
	public class CDExport : FSpot.Extensions.IExporter {
		IBrowsableCollection selection;

		System.Uri dest = new System.Uri ("burn:///");

		int photo_index;
		bool clean;

		CDExportDialog dialog;
		ThreadProgressDialog progress_dialog;
		System.Threading.Thread command_thread;

		public CDExport ()
		{
		}

		public void Run (IBrowsableCollection selection)
		{
			this.selection = selection;
			dialog = new CDExportDialog (selection, dest);
			//LoadHistory ();

			if (dialog.Run () != (int)ResponseType.Ok) {
				dialog.Destroy ();
				return;
			}

			clean = dialog.Clean;

			command_thread = new System.Threading.Thread (new System.Threading.ThreadStart (Transfer));
			command_thread.Name = Catalog.GetString ("Transferring Pictures");

			progress_dialog = new ThreadProgressDialog (command_thread, selection.Count);
			progress_dialog.Start ();

			dialog.Destroy ();
		}

		[DllImport ("libc")]
		extern static int system (string program);

//		//FIXME: rewrite this as a Filter
	        public static GLib.File UniqueName (System.Uri path, string shortname)
	        {
	                int i = 1;
			GLib.File dest = FileFactory.NewForUri (new System.Uri (path, shortname));
	                while (dest.Exists) {
	                        string numbered_name = System.String.Format ("{0}-{1}{2}",
	                                                              System.IO.Path.GetFileNameWithoutExtension (shortname),
	                                                              i++,
	                                                              System.IO.Path.GetExtension (shortname));

				dest = FileFactory.NewForUri (new System.Uri (path, numbered_name));
	                }

	                return dest;
	        }

		void Clean (System.Uri path)
		{
			GLib.File source = FileFactory.NewForUri (path);
			foreach (GLib.FileInfo info in source.EnumerateChildren ("*", FileQueryInfoFlags.None, null)) {
				if (info.FileType == FileType.Directory)
					Clean (new System.Uri(path, info.Name + "/"));
				FileFactory.NewForUri (new System.Uri (path, info.Name)).Delete ();
			}
		}

		public void Transfer () {
			try {
				bool result = true;

				if (clean)
					Clean (dest);

				foreach (IBrowsableItem photo in selection.Items) {

				//FIXME need to implement the uniquename as a filter
					using (FilterRequest request = new FilterRequest (photo.DefaultVersion.Uri)) {
						GLib.File source = FileFactory.NewForUri (request.Current.ToString ());
						GLib.File target = UniqueName (dest, photo.Name);
						FileProgressCallback cb = Progress;

						progress_dialog.Message = System.String.Format (Catalog.GetString ("Transferring picture \"{0}\" To CD"), photo.Name);
						progress_dialog.Fraction = photo_index / (double) selection.Count;
						progress_dialog.ProgressText = System.String.Format (Catalog.GetString ("{0} of {1}"),
											     photo_index, selection.Count);

						result &= source.Copy (target,
									FileCopyFlags.None,
									null,
									cb);
					}
					photo_index++;
				}

				// FIXME the error dialog here is ugly and needs improvement when strings are not frozen.
				if (result) {
					progress_dialog.Message = Catalog.GetString ("Done Sending Photos");
					progress_dialog.Fraction = 1.0;
					progress_dialog.ProgressText = Catalog.GetString ("Transfer Complete");
					progress_dialog.ButtonLabel = Gtk.Stock.Ok;
					progress_dialog.Hide ();
					system ("brasero -n");
				} else {
					throw new System.Exception (System.String.Format ("{0}{3}{1}{3}{2}",
											  progress_dialog.Message,
											  Catalog.GetString ("Error While Transferring"),
											  result.ToString (),
											  System.Environment.NewLine));
				}

			} catch (System.Exception e) {
				Hyena.Log.DebugException (e);
				progress_dialog.Message = e.ToString ();
				progress_dialog.ProgressText = Catalog.GetString ("Error Transferring");
				return;
			}
			ThreadAssist.ProxyToMain (() => {
				progress_dialog.Destroy ();
			});
		}

		private void Progress (long current_num_bytes, long total_num_bytes)
		{
			progress_dialog.ProgressText = Catalog.GetString ("copying...");

			if (total_num_bytes > 0)
				progress_dialog.Fraction = current_num_bytes / (double)total_num_bytes;

		}

	}
}
