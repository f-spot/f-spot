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

using FSpot.UI.Dialog;
using Gtk;
using Mono.Unix;

namespace FSpot.Database
{
	public class UpdaterUI : IUpdaterUI
	{
		ProgressDialog dialog;

		public void Show ()
		{
			dialog = new ProgressDialog (Catalog.GetString ("Updating F-Spot Database"), ProgressDialog.CancelButtonType.None, 0, null);
			dialog.Message.Text = Catalog.GetString ("Please wait while your F-Spot gallery's database is updated. This may take some time.");
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
