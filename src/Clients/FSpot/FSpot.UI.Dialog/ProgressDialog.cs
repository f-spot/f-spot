//
// ProgressDialog.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
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

using Gtk;

using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class ProgressDialog : Gtk.Dialog
	{
		bool cancelled;

		void HandleResponse (object me, ResponseArgs args)
		{
			cancelled = true;
		}

		public enum CancelButtonType {
			Cancel,
			Stop,
			None
		};

		int total_count;

		ProgressBar progress_bar;
		public ProgressBar Bar {
			get { return progress_bar; }
		}

		Label message_label;
		public Label Message {
			get { return message_label; }
		}

		Gtk.Button button;
		public Gtk.Button Button {
			get {
				return button;
			}
		}

		public ProgressDialog (string title, CancelButtonType cancel_button_type, int total_count, Gtk.Window parent_window)
		{
			Title = title;
			this.total_count = total_count;

			if (parent_window != null)
				TransientFor = parent_window;

			HasSeparator = false;
			BorderWidth = 6;
			SetDefaultSize (300, -1);

			message_label = new Label (string.Empty);
			VBox.PackStart (message_label, true, true, 12);

			progress_bar = new ProgressBar ();
			VBox.PackStart (progress_bar, true, true, 6);

			switch (cancel_button_type) {
			case CancelButtonType.Cancel:
				button = (Gtk.Button)AddButton (Gtk.Stock.Cancel, (int) ResponseType.Cancel);
				break;
			case CancelButtonType.Stop:
				button = (Gtk.Button)AddButton (Gtk.Stock.Stop, (int) ResponseType.Cancel);
				break;
			}

			Response += HandleResponse;
		}

		int current_count;

		// Return true if the operation was cancelled by the user.
		public bool Update (string message)
		{
			current_count ++;

			message_label.Text = message;
			progress_bar.Text = string.Format (Catalog.GetString ("{0} of {1}"), current_count, total_count);
			progress_bar.Fraction = (double) current_count / total_count;

			ShowAll ();

			while (Application.EventsPending ())
				Application.RunIteration ();

			return cancelled;
		}
	}
}
