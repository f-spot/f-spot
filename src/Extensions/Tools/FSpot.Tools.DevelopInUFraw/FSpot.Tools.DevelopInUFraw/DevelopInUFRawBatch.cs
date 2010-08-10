using System;
using System.IO;
using Mono.Unix;
using Hyena;
using FSpot;
using FSpot.Core;
using FSpot.Utils;
using FSpot.Extensions;
using FSpot.Imaging;
using FSpot.UI.Dialog;

namespace FSpot.Tools.DevelopInUFraw
{
    // Batch Version
	public class DevelopInUFRawBatch : AbstractDevelopInUFRaw {
		public DevelopInUFRawBatch() : base("ufraw-batch")
		{
		}

		public override void Run (object o, EventArgs e)
		{
			ProgressDialog pdialog = new ProgressDialog(Catalog.GetString ("Developing photos"),
														ProgressDialog.CancelButtonType.Cancel,
														App.Instance.Organizer.SelectedPhotos ().Length,
														App.Instance.Organizer.Window);
			Log.Information ("Executing DevelopInUFRaw extension in batch mode");

			foreach (Photo p in App.Instance.Organizer.SelectedPhotos ()) {
				bool cancelled = pdialog.Update(String.Format(Catalog.GetString ("Developing {0}"), p.Name));
				if (cancelled) {
					break;
				}

				DevelopPhoto (p);
			}
			pdialog.Destroy();
		}
	}
}
