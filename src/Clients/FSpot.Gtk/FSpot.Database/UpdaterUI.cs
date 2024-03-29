//
// UpdaterUI.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2006-2010 Novell, Inc.
// Copyright (C) 2009-2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2006, 2009 Gabriel Burt
// Copyright (C) 2007-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;
using FSpot.UI.Dialog;

using Gtk;

namespace FSpot.Database
{
	public class UpdaterUI : IUpdaterUI
	{
		ProgressDialog dialog;

		public void Show ()
		{
			dialog = new ProgressDialog (Strings.UpdatingFSpotDatabase, ProgressDialog.CancelButtonType.None, 0, null);
			dialog.Message.Text = Strings.PleaseWaitFSpotGalleryDBUpdating;
			dialog.Bar.Fraction = 0.0;
			dialog.Modal = false;
			dialog.SkipTaskbarHint = true;
			dialog.WindowPosition = WindowPosition.Center;
			dialog.ShowAll ();
			dialog.Present ();
			dialog.QueueDraw ();
		}

		public void Pulse ()
		{
			if (dialog != null) {
				dialog.Bar.Pulse ();
				dialog.ShowAll ();
			}
		}

		public void Destroy ()
		{
			dialog?.Destroy ();
		}
	}
}
