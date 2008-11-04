/*
 * FSpot.UI.Dialog.ThreadProgressDialog.cs
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Threading;

namespace FSpot.UI.Dialog {
	public class ThreadProgressDialog : Gtk.Dialog {
		FSpot.Delay delay;

		private Gtk.ProgressBar progress_bar;
		private Gtk.Label message_label;
		private Gtk.Button button;

		private Gtk.Button retry_button;
		private Gtk.Button skip_button;
		private Gtk.ResponseType error_response;
		private AutoResetEvent error_response_event;
		private AutoResetEvent error_response_ack_event;

		private Thread thread;

		public ThreadProgressDialog (Thread thread, int total) {
			/*
			if (parent_window)
				this.TransientFor = parent_window;

			*/
			this.Title = thread.Name;
			this.thread = thread;
			
			HasSeparator = false;
			BorderWidth = 6;
			SetDefaultSize (300, -1);
			
			message_label = new Gtk.Label (String.Empty);
			VBox.PackStart (message_label, true, true, 12);
			
			progress_bar = new Gtk.ProgressBar ();
			VBox.PackStart (progress_bar, true, true, 6);

			retry_button = new Gtk.Button (Mono.Unix.Catalog.GetString ("Retry"));
			retry_button.Clicked += new EventHandler (HandleRetryClicked);
			skip_button = new Gtk.Button (Mono.Unix.Catalog.GetString ("Skip"));
			skip_button.Clicked += new EventHandler (HandleSkipClicked);

			ActionArea.Add (retry_button);
			ActionArea.Add (skip_button);

			button_label = Gtk.Stock.Cancel;
			button = (Gtk.Button) AddButton (button_label, (int)Gtk.ResponseType.Cancel);
			
			delay = new Delay (new GLib.IdleHandler (HandleUpdate));

			Response += HandleResponse;
			Destroyed += HandleDestroy;
		}

		private string progress_text;
		public string ProgressText {
			get {
				return progress_text;
			}
			set {
				lock (this) {
					progress_text = value;
					delay.Start ();
				}
			}
		}

		private string button_label;
		public string ButtonLabel {
			get {
				return button_label;
			}			
			set {
				lock (this) {
					button_label = value;
					delay.Start ();
				}
			}
		}
		
		private string message;
		public string Message {
			get {
				return message;
			}
			set {
				lock (this) {
					message = value;
					delay.Start ();
				}
			}
		}
		
		private double fraction;
		public double Fraction {
			get {
				return Fraction;
			}
			set {
				lock (this) {
					fraction = value;
					delay.Start ();
				}
			}
		}

		private bool retry_skip;
		private bool RetrySkipVisible {
			set { 
				retry_skip = value;
				delay.Start ();
			} 
		}

		public bool PerformRetrySkip ()
		{
			error_response = Gtk.ResponseType.None;
			RetrySkipVisible = true;

			error_response_event = new AutoResetEvent (false);
			error_response_ack_event = new AutoResetEvent (false);
			error_response_event.WaitOne ();

			RetrySkipVisible = false;

			return (error_response == Gtk.ResponseType.Yes);
		}

		private void HandleResponse (object obj, Gtk.ResponseArgs args) {
			this.Destroy ();
		}

		private bool HandleUpdate ()
		{
			message_label.Text = message;
			progress_bar.Text = progress_text;
			progress_bar.Fraction = System.Math.Min (1.0, System.Math.Max (0.0, fraction));
			button.Label = button_label;
			retry_button.Visible = skip_button.Visible = retry_skip;

			return false;
		}

		private void HandleDestroy (object sender, EventArgs args)
		{
			delay.Stop ();
			if (thread.IsAlive) {
				thread.Abort ();
			}
		}

		private void HandleRetryClicked (object obj, EventArgs args)
		{
			error_response = Gtk.ResponseType.Yes;
			error_response_event.Set ();
		}

		private void HandleSkipClicked (object obj, EventArgs args)
		{
			error_response = Gtk.ResponseType.No;
			error_response_event.Set ();
		}

		public void Start () {
			ShowAll ();
			RetrySkipVisible = false;
			thread.Start ();
		}
	}
}
