/*
 * FSpot.UI.Dialog.ProgressDialog.cs
 *
 * Author(s):
 * 	Ettore Perazzoli
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using GLib;
using Gtk;
using System;

using Mono.Unix;

namespace FSpot.UI.Dialog {
	public class ProgressDialog : Gtk.Dialog {
	
		private bool cancelled;
	
		private void HandleResponse (object me, ResponseArgs args)
		{
			cancelled = true;
		}
	
		public enum CancelButtonType {
			Cancel,
			Stop,
			None
		};
	
		private int total_count;
	
		private ProgressBar progress_bar;
		public ProgressBar Bar {
			get { return progress_bar; }
		}
	
		private Label message_label;
		public Label Message {
			get { return message_label; }
		}
	
		private Gtk.Button button;
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
				this.TransientFor = parent_window;
	
			HasSeparator = false;
			BorderWidth = 6;
			SetDefaultSize (300, -1);
	
			message_label = new Label (String.Empty);
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
	
			Response += new ResponseHandler (HandleResponse);
		}
	
		private int current_count;
	
		// Return true if the operation was cancelled by the user.
		public bool Update (string message)
		{
			current_count ++;
	
			message_label.Text = message;
			progress_bar.Text = String.Format (Catalog.GetString ("{0} of {1}"), current_count, total_count);
			progress_bar.Fraction = (double) current_count / total_count;
	
			ShowAll ();
	
			while (Application.EventsPending ())
				Application.RunIteration ();
	
			return cancelled;
		}
	}
}
