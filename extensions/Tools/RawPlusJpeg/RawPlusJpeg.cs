/*
 * RawPlusJpeg.cs
 *
 * Author(s)
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.Collections.Generic;

using Gtk;

using FSpot;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Imaging;

using Hyena;
using Hyena.Widgets;

namespace RawPlusJpegExtension
{
	public class RawPlusJpeg : ICommand
	{
		public void Run (object o, EventArgs e)
		{
			Log.Debug ("EXECUTING RAW PLUS JPEG EXTENSION");

			if (ResponseType.Ok != HigMessageDialog.RunHigConfirmation (
				App.Instance.Organizer.Window,
				DialogFlags.DestroyWithParent,
				MessageType.Warning,
				"Merge Raw+Jpegs",
				"This operation will merge Raw and Jpegs versions of the same image as one unique image. The Raw image will be the Original version, the jpeg will be named 'Jpeg' and all subsequent versions will keep their original names (if possible).\n\nNote: only enabled for some formats right now.",
				"Do it now"))
				return;

			Photo [] photos = App.Instance.Database.Photos.Query ((Tag [])null, null, null, null);
			Array.Sort (photos, new IBrowsableItemComparer.CompareDirectory ());

			Photo raw = null;
			Photo jpeg = null;

			IList<MergeRequest> merge_requests = new List<MergeRequest> ();

			for (int i = 0; i < photos.Length; i++) {
				Photo p = photos [i];

				if (!ImageFile.IsRaw (p.DefaultVersion.Uri) && !ImageFile.IsJpeg (p.DefaultVersion.Uri))
					continue;

				if (ImageFile.IsJpeg (p.DefaultVersion.Uri))
					jpeg = p;
				if (ImageFile.IsRaw (p.DefaultVersion.Uri))
					raw = p;

				if (raw != null && jpeg != null && SamePlaceAndName (raw, jpeg))
					merge_requests.Add (new MergeRequest (raw, jpeg));
			}

			if (merge_requests.Count == 0)
				return;

			foreach (MergeRequest mr in merge_requests)
				mr.Merge ();

			App.Instance.Organizer.UpdateQuery ();
		}

		private static bool SamePlaceAndName (Photo p1, Photo p2)
		{
			return DirectoryPath (p1) == DirectoryPath (p2) &&
				System.IO.Path.GetFileNameWithoutExtension (p1.Name) == System.IO.Path.GetFileNameWithoutExtension (p2.Name);
		}


		private static string DirectoryPath (Photo p)
		{
			return p.VersionUri (Photo.OriginalVersionId).GetBaseUri ();
		}

		class MergeRequest
		{
			Photo raw;
			Photo jpeg;

			public MergeRequest (Photo raw, Photo jpeg)
			{
				this.raw = raw;
				this.jpeg = jpeg;
			}

			public void Merge ()
			{
				Log.DebugFormat ("Merging {0} and {1}", raw.VersionUri (Photo.OriginalVersionId), jpeg.VersionUri (Photo.OriginalVersionId));
				foreach (uint version_id in jpeg.VersionIds) {
					string name = jpeg.GetVersion (version_id).Name;
					try {
						raw.DefaultVersionId = raw.CreateReparentedVersion (jpeg.GetVersion (version_id) as PhotoVersion, version_id == Photo.OriginalVersionId);
						if (version_id == Photo.OriginalVersionId)
							raw.RenameVersion (raw.DefaultVersionId, "Jpeg");
						else
							raw.RenameVersion (raw.DefaultVersionId, name);
					} catch (Exception e) {
						Log.Exception (e);
					}
				}
				raw.AddTag (jpeg.Tags);
				uint [] version_ids = jpeg.VersionIds;
				Array.Reverse (version_ids);
				foreach (uint version_id in version_ids) {
					try {
						jpeg.DeleteVersion (version_id, true, true);
					} catch (Exception e) {
						Log.Exception (e);
					}
				}
				raw.Changes.DataChanged = true;
				App.Instance.Database.Photos.Commit (raw);
				App.Instance.Database.Photos.Remove (jpeg);
			}
		}
	}
}
