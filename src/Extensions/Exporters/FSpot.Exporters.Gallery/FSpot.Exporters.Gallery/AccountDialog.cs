//
// AccountDialog.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Resources.Lang;

using Hyena.Widgets;


namespace FSpot.Exporters.Gallery
{
	public class AccountDialog
	{
		public AccountDialog (Gtk.Window parent) : this (parent, null, false)
		{
			add_dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}

		public AccountDialog (Gtk.Window parent, GalleryAccount account, bool show_error)
		{
			var builder = new GtkBeans.Builder (null, "gallery_add_dialog.ui", null);
			builder.Autoconnect (this);
			add_dialog = new Gtk.Dialog (builder.GetRawObject ("gallery_add_dialog"));
			add_dialog.Modal = false;
			add_dialog.TransientFor = parent;
			add_dialog.DefaultResponse = Gtk.ResponseType.Ok;

			this.account = account;

			status_area.Visible = show_error;

			if (account != null) {
				gallery_entry.Text = account.Name;
				url_entry.Text = account.Url;
				password_entry.Text = account.Password;
				username_entry.Text = account.Username;
				add_button.Label = Gtk.Stock.Ok;
				add_dialog.Response += HandleEditResponse;
			}

			if (remove_button != null)
				remove_button.Visible = account != null;

			add_dialog.Show ();

			gallery_entry.Changed += HandleChanged;
			url_entry.Changed += HandleChanged;
			password_entry.Changed += HandleChanged;
			username_entry.Changed += HandleChanged;
			HandleChanged (null, null);
		}

		void HandleChanged (object sender, System.EventArgs args)
		{
			name = gallery_entry.Text;
			url = url_entry.Text;
			password = password_entry.Text;
			username = username_entry.Text;

			if (name == string.Empty || url == string.Empty || password == string.Empty || username == string.Empty)
				add_button.Sensitive = false;
			else
				add_button.Sensitive = true;

		}

		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				try {
					var uri = new Uri (url);
					if (uri.Scheme != Uri.UriSchemeHttp &&
						uri.Scheme != Uri.UriSchemeHttps)
						throw new System.UriFormatException ();

					//Check for name uniqueness
					foreach (GalleryAccount acc in GalleryAccountManager.GetInstance ().GetAccounts ())
						if (acc.Name == name)
							throw new ArgumentException ("name");
					var created = new GalleryAccount (name,
											 url,
											 username,
											 password);

					created.Connect ();
					GalleryAccountManager.GetInstance ().AddAccount (created);
					account = created;
				} catch (System.UriFormatException) {
					var md =
						new HigMessageDialog (add_dialog,
									  Gtk.DialogFlags.Modal |
									  Gtk.DialogFlags.DestroyWithParent,
									  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
									  Strings.InvalidUrl,
									  Strings.TheGalleryUrlEntryDoesNotAppearToBeValid);
					md.Run ();
					md.Destroy ();
					return;
				} catch (GalleryException e) {
					var md =
						new HigMessageDialog (add_dialog,
									  Gtk.DialogFlags.Modal |
									  Gtk.DialogFlags.DestroyWithParent,
									  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
									  Strings.ErrorWhileConnectingToGallery,
									  string.Format (Strings.FollowingErrorWasEncounteredAttemptingToLogInX, e.Message));
					if (e.ResponseText != null) {
						Logger.Log.Debug (e.Message);
						Logger.Log.Debug (e.ResponseText);
					}
					md.Run ();
					md.Destroy ();
					return;
				} catch (ArgumentException ae) {
					var md =
						new HigMessageDialog (add_dialog,
									  Gtk.DialogFlags.Modal |
									  Gtk.DialogFlags.DestroyWithParent,
									  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
									  Strings.GalleryWithThisNameAlreadyExists,
									  string.Format (Strings.ThereAlreadyGallerySameNameInYourRegisteredGalleriesPleaseChooseUniqueName));
					Logger.Log.Error (ae, "");
					md.Run ();
					md.Destroy ();
					return;
				} catch (System.Net.WebException we) {
					var md =
						new HigMessageDialog (add_dialog,
									  Gtk.DialogFlags.Modal |
									  Gtk.DialogFlags.DestroyWithParent,
									  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
									  Strings.ErrorWhileConnectingToGallery,
									  string.Format (Strings.FollowingErrorWasEncounteredAttemptingToLogInX, we.Message));
					md.Run ();
					md.Destroy ();
					return;
				} catch (Exception se) {
					var md =
						new HigMessageDialog (add_dialog,
									  Gtk.DialogFlags.Modal |
									  Gtk.DialogFlags.DestroyWithParent,
									  Gtk.MessageType.Error, Gtk.ButtonsType.Ok,
									  Strings.ErrorWhileConnectingToGallery,
									  string.Format (Strings.FollowingErrorWasEncounteredAttemptingToLogInX, se.Message));
					Logger.Log.Error (se, "");
					md.Run ();
					md.Destroy ();
					return;
				}
			}
			add_dialog.Destroy ();
		}

		protected void HandleEditResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				account.Name = name;
				account.Url = url;
				account.Username = username;
				account.Password = password;
				GalleryAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				GalleryAccountManager.GetInstance ().RemoveAccount (account);
			}
			add_dialog.Destroy ();
		}

		GalleryAccount account;
		string name;
		string url;
		string password;
		string username;

#pragma warning disable 649
		// widgets
		[GtkBeans.Builder.Object] Gtk.Dialog add_dialog;

		[GtkBeans.Builder.Object] Gtk.Entry url_entry;
		[GtkBeans.Builder.Object] Gtk.Entry password_entry;
		[GtkBeans.Builder.Object] Gtk.Entry gallery_entry;
		[GtkBeans.Builder.Object] Gtk.Entry username_entry;

		[GtkBeans.Builder.Object] Gtk.Button add_button;
		[GtkBeans.Builder.Object] Gtk.Button remove_button;

		[GtkBeans.Builder.Object] Gtk.HBox status_area;
#pragma warning restore 649
	}
}
