//
// DevelopInUFRawBatch.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Models;
using FSpot.Resources.Lang;
using FSpot.UI.Dialog;

namespace FSpot.Tools.DevelopInUFraw
{
	// Batch Version
	public class DevelopInUFRawBatch : AbstractDevelopInUFRaw
	{
		public DevelopInUFRawBatch () : base ("ufraw-batch")
		{
		}

		public override void Run (object o, EventArgs e)
		{
			var pdialog = new ProgressDialog (Strings.DevelopingPhotos,
														ProgressDialog.CancelButtonType.Cancel,
														App.Instance.Organizer.SelectedPhotos ().Length,
														App.Instance.Organizer.Window);
			Logger.Log.Information ("Executing DevelopInUFRaw extension in batch mode");

			foreach (Photo p in App.Instance.Organizer.SelectedPhotos ()) {
				bool cancelled = pdialog.Update (string.Format (Strings.DevelopingX, p.Name));
				if (cancelled) {
					break;
				}

				DevelopPhoto (p);
			}
			pdialog.Destroy ();
		}
	}
}
