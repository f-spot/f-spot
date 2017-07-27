//
// VersionNameDialog.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Anton Keks <anton@azib.net>
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2003-2010 Novell, Inc.
// Copyright (C) 2009-2010 Anton Keks
// Copyright (C) 2003 Ettore Perazzoli
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
	public class VersionNameDialog : BuilderDialog
	{
		readonly Photo photo;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Button ok_button;
		[GtkBeans.Builder.Object] Entry version_name_entry;
		[GtkBeans.Builder.Object] Label prompt_label;
		[GtkBeans.Builder.Object] Label already_in_use_label;
#pragma warning restore 649

		public enum RequestType
		{
			Create,
			Rename
		}

		RequestType request_type;

		void Update ()
		{
			string new_name = version_name_entry.Text;

			if (photo.VersionNameExists (new_name)
				&& !(request_type == RequestType.Rename
				  && new_name == photo.GetVersion (photo.DefaultVersionId).Name)) {
				already_in_use_label.Markup = "<small>This name is already in use</small>";
				ok_button.Sensitive = false;
				return;
			}

			already_in_use_label.Text = string.Empty;

			if (new_name.Length == 0)
				ok_button.Sensitive = false;
			else
				ok_button.Sensitive = true;
		}

		void HandleVersionNameEntryChanged (object obj, EventArgs args)
		{
			Update ();
		}

		public VersionNameDialog (RequestType request_type, Photo photo, Window parent_window) : base ("VersionNameDialog.ui", "version_name_dialog")
		{
			this.request_type = request_type;
			this.photo = photo;

			switch (request_type) {
			case RequestType.Create:
				Title = Catalog.GetString ("Create New Version");
				prompt_label.Text = Catalog.GetString ("Name:");
				break;

			case RequestType.Rename:
				Title = Catalog.GetString ("Rename Version");
				prompt_label.Text = Catalog.GetString ("New name:");
				version_name_entry.Text = photo.GetVersion (photo.DefaultVersionId).Name;
				version_name_entry.SelectRegion (0, -1);
				break;
			}

			version_name_entry.Changed += HandleVersionNameEntryChanged;
			version_name_entry.ActivatesDefault = true;

			TransientFor = parent_window;
			DefaultResponse = ResponseType.Ok;

			Update ();
		}

		public ResponseType Run (out string name)
		{
			ResponseType response = (ResponseType)Run ();

			name = version_name_entry.Text;
			if (request_type == RequestType.Rename && name == photo.GetVersion (photo.DefaultVersionId).Name)
				response = ResponseType.Cancel;

			Destroy ();

			return response;
		}
	}
}
