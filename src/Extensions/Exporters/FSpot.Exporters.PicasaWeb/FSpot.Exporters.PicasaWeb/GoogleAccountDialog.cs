using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Hyena;
using Hyena.Widgets;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Imaging;
using FSpot.UI.Dialog;
using Gnome.Keyring;
using Mono.Google;
using Mono.Google.Picasa;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAccountDialog {
		public GoogleAccountDialog (Gtk.Window parent) : this (parent, null, false, null) {
			Dialog.Response += HandleAddResponse;
			add_button.Sensitive = false;
		}

		public GoogleAccountDialog (Gtk.Window parent, GoogleAccount account, bool show_error, CaptchaException captcha_exception)
		{
			builder = new GtkBeans.Builder (null, "google_add_dialog.ui", null);
			builder.Autoconnect (this);
			Dialog.Modal = false;
			Dialog.TransientFor = parent;
			Dialog.DefaultResponse = Gtk.ResponseType.Ok;

			this.account = account;

			bool show_captcha = (captcha_exception != null);
			status_area.Visible = show_error;
			locked_area.Visible = show_captcha;
			captcha_label.Visible = show_captcha;
			captcha_entry.Visible = show_captcha;
			captcha_image.Visible = show_captcha;

			password_entry.ActivatesDefault = true;
			username_entry.ActivatesDefault = true;

			if (show_captcha) {
				try {
					using  (var img = ImageFile.Create(new SafeUri(captcha_exception.CaptchaUrl, true))) {
						captcha_image.Pixbuf = img.Load();
						token = captcha_exception.Token;
					}
				} catch (Exception) {}
			}

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

			add_button.Sensitive = !(password == String.Empty || username == String.Empty);
		}

		[GLib.ConnectBefore]
		protected void HandleAddResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				GoogleAccount account = new GoogleAccount (username, password);
				GoogleAccountManager.GetInstance ().AddAccount (account);
			}
			Dialog.Destroy ();
		}

		protected void HandleEditResponse (object sender, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok) {
				account.Username = username;
				account.Password = password;
				account.Token = token;
				account.UnlockCaptcha = captcha_entry.Text;
				GoogleAccountManager.GetInstance ().MarkChanged (true, account);
			} else if (args.ResponseId == Gtk.ResponseType.Reject) {
				// NOTE we are using Reject to signal the remove action.
				GoogleAccountManager.GetInstance ().RemoveAccount (account);
			}
			Dialog.Destroy ();
		}

		private Gtk.Dialog Dialog {
			get {
				if (dialog == null)
					dialog = new Gtk.Dialog (builder.GetRawObject ("google_add_dialog"));

				return dialog;
			}
		}

		private GoogleAccount account;
		private string password;
		private string username;
		private string token;

        GtkBeans.Builder builder;

		// widgets
		[GtkBeans.Builder.Object] Gtk.Dialog dialog;
		[GtkBeans.Builder.Object] Gtk.Entry password_entry;
		[GtkBeans.Builder.Object] Gtk.Entry username_entry;
		[GtkBeans.Builder.Object] Gtk.Entry captcha_entry;

		[GtkBeans.Builder.Object] Gtk.Button add_button;
		[GtkBeans.Builder.Object] Gtk.Button remove_button;

		[GtkBeans.Builder.Object] Gtk.HBox status_area;
		[GtkBeans.Builder.Object] Gtk.HBox locked_area;

		[GtkBeans.Builder.Object] Gtk.Image captcha_image;
		[GtkBeans.Builder.Object] Gtk.Label captcha_label;

	}
}
