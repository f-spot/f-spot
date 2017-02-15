//
// DevelopInUFRawBatch.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
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

using Mono.Unix;

using Hyena;

using FSpot;
using FSpot.UI.Dialog;

namespace FSpot.Tools.DevelopInUFraw
{
    // Batch Version
	public class DevelopInUFRawBatch : AbstractDevelopInUFRaw
	{
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
				bool cancelled = pdialog.Update(string.Format(Catalog.GetString ("Developing {0}"), p.Name));
				if (cancelled) {
					break;
				}

				DevelopPhoto (p);
			}
			pdialog.Destroy();
		}
	}
}
