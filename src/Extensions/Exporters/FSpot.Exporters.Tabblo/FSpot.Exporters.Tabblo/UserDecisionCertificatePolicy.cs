//
// FSpotTabbloExport.UserDecisionCertificatePolicy
//
// Authors:
//	Wojciech Dzierzanowski (wojciech.dzierzanowski@gmail.com)
//
// (C) Copyright 2008 Wojciech Dzierzanowski
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

using Gtk;

using Hyena;

namespace FSpot.Exporters.Tabblo
{
	class UserDecisionCertificatePolicy : ApplicationCentricCertificatePolicy
	{
		const string DialogName = "trust_error_dialog";
		[Builder.Object] Dialog dialog;
		[Builder.Object] Label url_label;
		[Builder.Object] RadioButton abort_radiobutton;
		[Builder.Object] RadioButton once_radiobutton;
		[Builder.Object] RadioButton always_radiobutton;

		WebRequest request;
		Decision decision;

		Object decision_lock = new Object ();
		ManualResetEvent decision_event;


		protected override Decision GetDecision (
				X509Certificate certificate,
				WebRequest request)
		{
			this.request = request;

			lock (decision_lock) {
				GLib.Idle.Add (this.DoGetDecision);
				decision_event = new ManualResetEvent (false);
				decision_event.WaitOne ();
			}

			return decision;
		}

		private bool DoGetDecision ()
		{
			Builder builder = new Builder (
					Assembly.GetExecutingAssembly (),
					"TrustError.ui", null);
			builder.Autoconnect (this);
			dialog = (Gtk.Dialog) builder.GetObject (DialogName);

			url_label.Markup = String.Format (
					url_label.Text, String.Format (
							"<b>{0}</b>",
							request.RequestUri));

			Gtk.ResponseType response =
					(Gtk.ResponseType) dialog.Run ();
			Log.Debug ("Decision dialog response: " + response);

			dialog.Destroy ();

			decision = Decision.DontTrust;
			if (0 == response) {
				if (abort_radiobutton.Active) {
					decision = Decision.DontTrust;
				} else if (once_radiobutton.Active) {
					decision = Decision.TrustOnce;
				} else if (always_radiobutton.Active) {
					decision = Decision.TrustAlways;
				} else {
					Debug.Assert (false,
							"Unhandled decision");
				}
			}

			decision_event.Set ();
			return false;
		}
	}
}
