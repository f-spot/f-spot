/*
 * SyncMetaData.cs
 *
 * Author(s)
 * 	Miguel Aguero  <miguelaguero@gmail.com>
 *
 * This is free software. See COPYING for details
 */

using System;
using System.Collections.Generic;

using Gtk;

using FSpot;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Jobs;

namespace SyncCatalogExtension {

	public class SyncCatalog : ICommand {
		public void Run (object o, EventArgs e) {
			if (ResponseType.Ok != HigMessageDialog.RunHigConfirmation (
				MainWindow.Toplevel.Window,
				DialogFlags.DestroyWithParent,
				MessageType.Warning,
				"Sync Catalog with photos",
				"Sync operation of the entire catalog with all photos could take hours, but hopefully it will be run in background. you can stop f-spot any time you want, the sync job will be restarted next time you start f-spot",
				"Do it now"))
				return;

			Photo [] photos = Core.Database.Photos.Query ((Tag [])null, null, null, null);

			foreach (Photo photo in photos) {
				SyncMetadataJob.Create (Core.Database.Jobs, photo);
			}


		}
	}

}
