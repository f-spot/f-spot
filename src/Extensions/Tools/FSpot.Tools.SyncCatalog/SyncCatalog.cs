//
// SyncCatalog.cs
//
// Author:
//   Lorenzo Milesi <maxxer@yetopen.it>
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
// Copyright (C) 2008 Lorenzo Milesi
// Copyright (C) 2007-2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Mono.Unix;

using Gtk;

using FSpot;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Jobs;

namespace SyncCatalogExtension {

	public class SyncCatalog : ICommand {
		public void Run (object o, EventArgs e) {
			SyncCatalogDialog dialog = new SyncCatalogDialog ();
			dialog.ShowDialog ();

		}
	}

	public class SyncCatalogDialog : Dialog
	{
		private RadioButton RadioSelectedPhotos;
		private RadioButton RadioEntireCatalog;

		public void ShowDialog ()
		{
			string message="";
			message = Catalog.GetString ("Sync operation of the entire catalog or a lot of selected photos with their files \n" +
					"could take hours, but hopefully it will be run in background.\n" +
					"You can stop F-Spot any time you want, the sync job will be restarted next time you start F-Fpot.\n" +
					"What do you want to do?");

			Gtk.Label label;
			label = new Gtk.Label (message);

			RadioSelectedPhotos = new RadioButton ("Synchronize selected photos");
			RadioEntireCatalog = new RadioButton (RadioSelectedPhotos, "Synchronize entire catalog");
			if (App.Instance.Organizer.SelectedPhotos ().Length > 0)
				RadioSelectedPhotos.Active = true;
			else
				RadioEntireCatalog.Active = true;

			VBox.PackStart (label, false, false, 5);
			VBox.PackStart (RadioSelectedPhotos, false, false, 1);
			VBox.PackStart (RadioEntireCatalog, false, false, 1);

			this.WindowPosition = WindowPosition.Center;

			this.AddButton ("_Run", ResponseType.Apply);
			this.AddButton ("_Cancel", ResponseType.Cancel);

			this.Response += HandleResponse;

			ShowAll ();

		}

		void HandleResponse (object obj, ResponseArgs args)
	        {
			switch(args.ResponseId)
			{
				case ResponseType.Cancel:
					this.Destroy ();
					break;
				case ResponseType.Apply:
					Photo [] photos = new Photo [0];
					if (RadioEntireCatalog.Active)
						photos = Core.Database.Photos.Query ();
					else if (RadioSelectedPhotos.Active)
						photos = App.Instance.Organizer.SelectedPhotos ();

					this.Hide ();
					foreach (Photo photo in photos) {
						SyncMetadataJob.Create (Core.Database.Jobs, photo);
					}
					this.Destroy();
					break;
			}
	        }

	}

}
