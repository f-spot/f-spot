//
// SmugMugAccountDialog.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Exporters.SmugMug
{
	public class SmugMugAccountDialog
	{
		public SmugMugAccountDialog (Gtk.Window parent) : this (parent, null) {
			Dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}

		public SmugMugAccountDialog (Gtk.Window parent, SmugMugAccount account)
		{
			builder = new GtkBeans.Builder (null, "smugmug_add_dialog.ui", null);
			builder.Autoconnect (this);

			Dialog.Modal = false;
			Dialog.TransientFor = parent;
			Dialog.DefaultResponse = Gtk.ResponseType.Ok;

			this.account = account;

			password_entry.ActivatesDefault = true;
			username_entry.ActivatesDefault = true;

			if (account != null) {
				password_entry.Text = account.Password;
				username_entry.Text = account.Username;
				add_button.Label = Gtk.Stock.Ok;
				Dialog.Response += HandleEditResponse;
			}

			if (remove_button != null)
				remove_button.Visible = account != null;

			this.Dialog.Show ();

			password_entry.Changed += HandleChanged;
			username_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		private void HandleChanged (object sender, System.EventArgs args)
		{
			password = password_entry.Text;
			username = username_entry.Text;

			add_button.Sensitive = !(password == string.Empty || username == string.Empty);
		}

		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				SmugMugAccount account = new SmugMugAccount (username, password);
				SmugMugAccountManager.GetInstance ().AddAccount (account);
			}
			Dialog.Destroy ();
		}

		protected void HandleEditResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				account.Username = username;
				account.Password = password;
				SmugMugAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				SmugMugAccountManager.GetInstance ().RemoveAccount (account);
			}
			Dialog.Destroy ();
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject (dialog_name));

				return dialog;
			}
		}

		private SmugMugAccount account;
		private string password;
		private string username;
		private string dialog_name = "smugmug_add_dialog";
		private GtkBeans.Builder builder;

#pragma warning disable 649
		// widgets
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		[GtkBeans.Builder.Object] Gtk.Entry password_entry;
		[GtkBeans.Builder.Object] Gtk.Entry username_entry;

		[GtkBeans.Builder.Object] Gtk.Button add_button;
		[GtkBeans.Builder.Object] Gtk.Button remove_button;
#pragma warning restore 649
	}
}
