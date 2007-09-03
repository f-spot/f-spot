/*
 * RawPlusJpeg.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;

using Gtk;

using FSpot;
using FSpot.Extensions;

namespace RawPlusJpegExtension
{
	public class RawPlusJpeg : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			Console.WriteLine ("EXECUTING RAW PLUS JPEG EXTENSION");

			if (ResponseType.Ok != HigMessageDialog.RunHigConfirmation (
				MainWindow.Toplevel.Window,
				DialogFlags.DestroyWithParent,
				MessageType.Warning,
				"Merge Raw+Jpegs",
				"This operation will merge Raw and Jpegs versions of the same image as one unique image. The Raw image will be the Original version, the jpeg will be named 'Jpeg' and all subsequent versions will keep their original names (if possible).\n\nNote: only enabled for .nef and .cr2 right now.",
				"Do it now"))
				return;

			Photo [] photos = Core.Database.Photos.Query ((Tag [])null, null, null, null);
			Array.Sort (photos, new Photo.CompareDirectory ());
			Photo previous = null;
			foreach (Photo p in photos) {
				if (previous != null && 
					p.DirectoryPath == previous.DirectoryPath && 
					System.IO.Path.GetFileNameWithoutExtension (p.Name) == System.IO.Path.GetFileNameWithoutExtension (previous.Name))
					Merge (previous, p);
				previous = p;
			}
		}

		private void Merge (Photo first, Photo second)
		{
			Photo raw;
			Photo jpeg;
			if (System.IO.Path.GetExtension (first.Name).ToLower () == ".jpg" || System.IO.Path.GetExtension (first.Name).ToLower () == ".jpeg") {
				jpeg = 	first;
				raw = second;
			} else {
				jpeg = second;
				raw = first;
			}
			if (System.IO.Path.GetExtension (jpeg.Name).ToLower () != ".jpg" && System.IO.Path.GetExtension (jpeg.Name).ToLower () != ".jpeg") 
				return;
			if (System.IO.Path.GetExtension (raw.Name).ToLower () != ".nef" && System.IO.Path.GetExtension (jpeg.Name).ToLower () != ".cr2") 
				return;
			Console.WriteLine ("Merging {0} and {1}", raw.VersionUri (Photo.OriginalVersionId), jpeg.VersionUri (Photo.OriginalVersionId));
			foreach (uint version_id in jpeg.VersionIds) {
				string name = jpeg.GetVersion (version_id).Name;
				try {
					raw.DefaultVersionId = raw.CreateReparentedVersion (jpeg.GetVersion (version_id) as PhotoVersion);
					if (version_id == Photo.OriginalVersionId)
						raw.RenameVersion (raw.DefaultVersionId, "Jpeg");
					else
						raw.RenameVersion (raw.DefaultVersionId, name);
				} catch (Exception e) {
					Console.WriteLine (e);
				}
			}
			uint [] version_ids = jpeg.VersionIds;
			Array.Reverse (version_ids);
			foreach (uint version_id in version_ids) {
				try {
					jpeg.DeleteVersion (version_id, true, true);
				} catch (Exception e) {
					Console.WriteLine (e);
				}
			}
			Core.Database.Photos.Commit (raw);
			Core.Database.Photos.Remove (jpeg);
		}
	}
}
