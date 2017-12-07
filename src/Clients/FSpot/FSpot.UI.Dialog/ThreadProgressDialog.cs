//
// ThreadProgressDialog.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Larry Ewing <lewing@src.gnome.org>
//   Thomas Van Machelen <thomas.vanmachelen@gmail.com>
//
// Copyright (C) 2004-2009 Novell, Inc.
// Copyright (C) 2007-2009 Stephane Delcroix
// Copyright (C) 2004-2006 Larry Ewing
// Copyright (C) 2007 Thomas Van Machelen
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
using System.Threading;
using System.Collections.Generic;

using Gtk;

using FSpot.Utils;

namespace FSpot.UI.Dialog
{
	public class ThreadProgressDialog : Gtk.Dialog
	{
		DelayedOperation delay;

		Gtk.ProgressBar progress_bar;
		Gtk.Label message_label;
		Gtk.Button button;

		Gtk.Button retry_button;
		Gtk.Button skip_button;
		Gtk.ResponseType error_response;
		AutoResetEvent error_response_event;

		object syncHandle = new object ();
		readonly Thread thread;

		// FIXME: The total parameter makes sense, but doesn't seem to ever be used?
		public ThreadProgressDialog (Thread thread, int total)
		{
			/*
			if (parent_window)
				this.TransientFor = parent_window;

			*/
			Title = thread.Name;
			this.thread = thread;

			HasSeparator = false;
			BorderWidth = 6;
			SetDefaultSize (300, -1);

			message_label = new Gtk.Label (string.Empty);
			VBox.PackStart (message_label, true, true, 12);

			progress_bar = new Gtk.ProgressBar ();
			VBox.PackStart (progress_bar, true, true, 6);

			retry_button = new Gtk.Button (Mono.Unix.Catalog.GetString ("Retry"));
			retry_button.Clicked += HandleRetryClicked;
			skip_button = new Gtk.Button (Mono.Unix.Catalog.GetString ("Skip"));
			skip_button.Clicked += HandleSkipClicked;

			ActionArea.Add (retry_button);
			ActionArea.Add (skip_button);

			button_label = Gtk.Stock.Cancel;
			button = (Gtk.Button) AddButton (button_label, (int)Gtk.ResponseType.Cancel);

			delay = new DelayedOperation (new GLib.IdleHandler (HandleUpdate));

			Response += HandleResponse;
			Destroyed += HandleDestroy;
		}

		string progress_text;
		public string ProgressText {
			get { return progress_text; }
			set {
				lock (syncHandle) {
					progress_text = value;
					delay.Start ();
				}
			}
		}

		string button_label;
		public string ButtonLabel {
			get { return button_label; }
			set {
				lock (syncHandle) {
					button_label = value;
					delay.Start ();
				}
			}
		}

		string message;
		public string Message {
			get { return message; }
			set {
				lock (syncHandle) {
					message = value;
					delay.Start ();
				}
			}
		}

		double fraction;
		public double Fraction {
			get { return Fraction; }
			set {
				lock (syncHandle) {
					fraction = value;
					delay.Start ();
				}
			}
		}

		List<Widget> widgets;
		public void VBoxPackEnd (Widget w)
		{
			if (widgets == null)
				widgets = new List<Widget> ();
			lock (syncHandle) {
				widgets.Add (w);
				delay.Start ();
			}
		}

		internal void SetProperties (string progress_text, string button_label, string message, double fraction)
		{
			lock (syncHandle) {
				this.progress_text = progress_text;
				this.button_label = button_label;
				this.message = message;
				this.fraction = fraction;
				delay.Start ();
			}
		}

		bool retry_skip;
		bool RetrySkipVisible {
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
			error_response_event.WaitOne ();

			RetrySkipVisible = false;

			return (error_response == Gtk.ResponseType.Yes);
		}

		void HandleResponse (object obj, Gtk.ResponseArgs args) {
			Destroy ();
		}

		bool HandleUpdate ()
		{
			message_label.Text = message;
			progress_bar.Text = progress_text;
			progress_bar.Fraction = Math.Min (1.0, Math.Max (0.0, fraction));
			button.Label = button_label;
			retry_button.Visible = skip_button.Visible = retry_skip;

			if (widgets != null && widgets.Count > 0) {
				foreach (var w in widgets)
					VBox.PackEnd (w);
				widgets.Clear ();
			}

			return false;
		}

		void HandleDestroy (object sender, EventArgs args)
		{
			delay.Stop ();
			if (thread.IsAlive) {
				thread.Abort ();
			}
		}

		void HandleRetryClicked (object obj, EventArgs args)
		{
			error_response = Gtk.ResponseType.Yes;
			error_response_event.Set ();
		}

		void HandleSkipClicked (object obj, EventArgs args)
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
