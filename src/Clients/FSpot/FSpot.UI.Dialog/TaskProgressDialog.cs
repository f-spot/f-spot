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
using System.Threading.Tasks;
using Gtk;

using FSpot.Utils;
using Mono.Unix;

namespace FSpot.UI.Dialog
{
	public class TaskProgressDialog : Gtk.Dialog
	{
		readonly DelayedOperation delay;

		readonly ProgressBar progressBar;
		readonly Label messageLabel;
		readonly Button button;

		readonly Button retryButton;
		readonly Button skipButton;
		ResponseType errorResponse;
		AutoResetEvent errorResponseEvent;

		readonly object syncHandle = new object ();
		readonly Task task;

		public TaskProgressDialog (Task task, string name)
		{
			/*
			if (parent_window)
				this.TransientFor = parent_window;

			*/
			Title = name;
			this.task = task;

			HasSeparator = false;
			BorderWidth = 6;
			SetDefaultSize (300, -1);

			messageLabel = new Label (string.Empty);
			VBox.PackStart (messageLabel, true, true, 12);

			progressBar = new ProgressBar ();
			VBox.PackStart (progressBar, true, true, 6);

			retryButton = new Button (Catalog.GetString ("Retry"));
			retryButton.Clicked += HandleRetryClicked;
			skipButton = new Button (Catalog.GetString ("Skip"));
			skipButton.Clicked += HandleSkipClicked;

			ActionArea.Add (retryButton);
			ActionArea.Add (skipButton);

			buttonLabel = Stock.Cancel;
			button = (Button) AddButton (buttonLabel, (int)ResponseType.Cancel);

			delay = new DelayedOperation (HandleUpdate);

			Response += HandleResponse;
			Destroyed += HandleDestroy;
		}

		string progressText;
		public string ProgressText {
			get { return progressText; }
			set {
				lock (syncHandle) {
					progressText = value;
					delay.Start ();
				}
			}
		}

		string buttonLabel;
		public string ButtonLabel {
			get { return buttonLabel; }
			set {
				lock (syncHandle) {
					buttonLabel = value;
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
			get { return fraction; }
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

		internal void SetProperties (string progressTextProperty, string progressButtonLabel, string progressMessage, double progressFraction)
		{
			lock (syncHandle) {
				progressText = progressTextProperty;
				buttonLabel = progressButtonLabel;
				message = progressMessage;
				fraction = progressFraction;
				delay.Start ();
			}
		}

		bool retrySkip;
		bool RetrySkipVisible {
			set {
				retrySkip = value;
				delay.Start ();
			}
		}

		public bool PerformRetrySkip ()
		{
			errorResponse = ResponseType.None;
			RetrySkipVisible = true;

			errorResponseEvent = new AutoResetEvent (false);
			errorResponseEvent.WaitOne ();

			RetrySkipVisible = false;

			return (errorResponse == ResponseType.Yes);
		}

		void HandleResponse (object obj, ResponseArgs args)
		{
			Destroy ();
		}

		bool HandleUpdate ()
		{
			messageLabel.Text = message;
			progressBar.Text = progressText;
			progressBar.Fraction = Math.Min (1.0, Math.Max (0.0, fraction));
			button.Label = buttonLabel;
			retryButton.Visible = skipButton.Visible = retrySkip;

			if (widgets == null || widgets.Count <= 0)
				return false;

			foreach (var w in widgets)
				VBox.PackEnd (w);
			widgets.Clear ();

			return false;
		}

		void HandleDestroy (object sender, EventArgs args)
		{
			delay.Stop ();
		}

		void HandleRetryClicked (object obj, EventArgs args)
		{
			errorResponse = ResponseType.Yes;
			errorResponseEvent.Set ();
		}

		void HandleSkipClicked (object obj, EventArgs args)
		{
			errorResponse = ResponseType.No;
			errorResponseEvent.Set ();
		}

		public void Start ()
		{
			ShowAll ();
			RetrySkipVisible = false;
			task.Start ();
		}
	}
}
