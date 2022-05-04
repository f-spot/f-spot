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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Gtk;

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
				Title = Strings.CreateNewVersion;
				prompt_label.Text = Strings.NameColon;
				break;

			case RequestType.Rename:
				Title = Strings.RenameVersion;
				prompt_label.Text = Strings.NewNameColon;
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
			var response = (ResponseType)Run ();

			name = version_name_entry.Text;
			if (request_type == RequestType.Rename && name == photo.GetVersion (photo.DefaultVersionId).Name)
				response = ResponseType.Cancel;

			Destroy ();

			return response;
		}
	}
}
