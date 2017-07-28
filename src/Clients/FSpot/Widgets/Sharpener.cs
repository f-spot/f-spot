//
// Sharpener.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
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
using System.Threading.Tasks;
using Gtk;

using Mono.Unix;

using FSpot.Core;
using FSpot.UI.Dialog;

using Hyena.Widgets;

namespace FSpot.Widgets
{
	public class Sharpener : Loupe
	{
		SpinButton amountSpin = new SpinButton (0.5, 100.0, .01);
		SpinButton radiusSpin = new SpinButton (5.0, 50.0, .01);
		SpinButton thresholdSpin = new SpinButton (0.0, 50.0, .01);
		Dialog dialog;
		TaskProgressDialog progressDialog;
		bool okClicked;

		public Sharpener (PhotoImageView view) : base (view)
		{
		}

		protected override void UpdateSample ()
		{
			if (okClicked)
				return;

			base.UpdateSample ();

			overlay?.Dispose ();

			overlay = null;
			if (source != null)
				overlay = PixbufUtils.UnsharpMask (source, radiusSpin.Value, amountSpin.Value, thresholdSpin.Value, null);
		}

		void HandleSettingsChanged (object sender, EventArgs args)
		{
			UpdateSample ();
		}

		public void DoSharpening (IProgress<double> progress)
		{
			progress.Report (0.0);
			progressDialog.Message = Catalog.GetString ("Photo is being sharpened");

			okClicked = true;
			Photo photo = view.Item.Current as Photo;

			if (photo == null)
				return;

			try {
				Gdk.Pixbuf orig = view.Pixbuf;
				Gdk.Pixbuf final = PixbufUtils.UnsharpMask (orig, radiusSpin.Value, amountSpin.Value, thresholdSpin.Value, progress);

				var createVersion = photo.DefaultVersion.IsProtected;

				photo.SaveVersion (final, createVersion);
				photo.Changes.DataChanged = true;
				App.Instance.Database.Photos.Commit (photo);
			} catch (Exception e) {
				var msg = Catalog.GetString ("Error saving sharpened photo");
				var desc = string.Format (Catalog.GetString ($"Received exception \"{e.Message}\". Unable to save photo {photo.Name}"));

				var md = new HigMessageDialog (this, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, msg, desc);
				md.Run ();
				md.Destroy ();
			}

			progress.Report (1.0);
			progressDialog.Message = Catalog.GetString ("Sharpening complete!");
			progressDialog.ButtonLabel = Stock.Ok;

			Destroy ();
		}

		void HandleOkClicked (object sender, EventArgs args)
		{
			Hide ();
			dialog.Hide ();

			var progress = new Progress<double> ();
			var task = new Task (() => { DoSharpening (progress); });

			progressDialog = new TaskProgressDialog (task, "Sharpening");
			progress.ProgressChanged += (o, p) => {
				progressDialog.Fraction = p;
			};

			progressDialog.Start ();
		}

		public void HandleCancelClicked (object sender, EventArgs args)
		{
			Destroy ();
		}

		public void HandleLoupeDestroyed (object sender, EventArgs args)
		{
			dialog.Destroy ();
		}

		protected override void BuildUI ()
		{
			base.BuildUI ();

			var title = Catalog.GetString ("Sharpen");
			dialog = new Dialog (title, this, DialogFlags.DestroyWithParent) {
				BorderWidth = 12
			};
			dialog.VBox.Spacing = 6;

			Table table = new Table (3, 2, false) {
				ColumnSpacing = 6,
				RowSpacing = 6
			};

			table.Attach (SetFancyStyle (new Label (Catalog.GetString ("Amount:"))), 0, 1, 0, 1);
			table.Attach (SetFancyStyle (new Label (Catalog.GetString ("Radius:"))), 0, 1, 1, 2);
			table.Attach (SetFancyStyle (new Label (Catalog.GetString ("Threshold:"))), 0, 1, 2, 3);

			SetFancyStyle (amountSpin = new SpinButton (0.00, 100.0, .01));
			SetFancyStyle (radiusSpin = new SpinButton (1.0, 50.0, .01));
			SetFancyStyle (thresholdSpin = new SpinButton (0.0, 50.0, .01));
			amountSpin.Value = .5;
			radiusSpin.Value = 5;
			thresholdSpin.Value = 0.0;

			amountSpin.ValueChanged += HandleSettingsChanged;
			radiusSpin.ValueChanged += HandleSettingsChanged;
			thresholdSpin.ValueChanged += HandleSettingsChanged;

			table.Attach (amountSpin, 1, 2, 0, 1);
			table.Attach (radiusSpin, 1, 2, 1, 2);
			table.Attach (thresholdSpin, 1, 2, 2, 3);

			Button cancelButton = new Button (Stock.Cancel);
			cancelButton.Clicked += HandleCancelClicked;
			dialog.AddActionWidget (cancelButton, ResponseType.Cancel);

			Button okButton = new Button (Stock.Ok);
			okButton.Clicked += HandleOkClicked;
			dialog.AddActionWidget (okButton, ResponseType.Cancel);

			dialog.DeleteEvent += HandleCancelClicked;

			Destroyed += HandleLoupeDestroyed;

			table.ShowAll ();
			dialog.VBox.PackStart (table);
			dialog.ShowAll ();
		}
	}
}
