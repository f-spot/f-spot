//
// ProgressDialog.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

using Gtk;

using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class ProgressDialog : Gtk.Dialog
	{
		bool cancelled;
		readonly int total_count;
		int current_count;

		public ProgressBar Bar { get; private set; }
		public Label Message { get; private set; }
		public Button Button { get; private set; }

		public ProgressDialog (string title, CancelButtonType cancelButtonType, int totalCount, Window parentWindow)
		{
			Title = title;
			total_count = totalCount;

			if (parentWindow != null)
				TransientFor = parentWindow;

			BorderWidth = 6;
			SetDefaultSize (300, -1);

			// GTK3: PackStart
			Message = new Label (String.Empty);
//			VBox.PackStart (Message, true, true, 12);

			Bar = new ProgressBar ();
//			VBox.PackStart (Bar, true, true, 6);

			switch (cancelButtonType) {
			case CancelButtonType.Cancel:
				Button = (Button)AddButton (Stock.Cancel, (int) ResponseType.Cancel);
				break;
			case CancelButtonType.Stop:
				Button = (Button)AddButton (Stock.Stop, (int) ResponseType.Cancel);
				break;
			}

			Response += HandleResponse;
		}

		void HandleResponse (object me, ResponseArgs args)
		{
			cancelled = true;
		}

		public enum CancelButtonType {
			Cancel,
			Stop,
			None
		};

		// Return true if the operation was cancelled by the user.
		public bool Update (string message)
		{
			current_count ++;

			Message.Text = message;
			Bar.Text = String.Format (Catalog.GetString ("{0} of {1}"), current_count, total_count);
			Bar.Fraction = (double) current_count / total_count;

			ShowAll ();

			while (Application.EventsPending ())
				Application.RunIteration ();

			return cancelled;
		}
	}
}
