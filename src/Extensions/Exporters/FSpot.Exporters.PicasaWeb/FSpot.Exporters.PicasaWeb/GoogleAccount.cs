//
// GoogleAccount.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;

using Hyena;

using Mono.Google;
using Mono.Google.Picasa;

namespace FSpot.Exporters.PicasaWeb
{
	public class GoogleAccount
	{
		private string username;
		private string password;
		private string token;
		private string unlock_captcha;
		private GoogleConnection connection;
		private Mono.Google.Picasa.PicasaWeb picasa;

		public GoogleAccount (string username, string password)
		{
			this.username = username;
			this.password = password;
		}

		public GoogleAccount (string username, string password, string token, string unlock_captcha)
		{
			this.username = username;
			this.password = password;
			this.token = token;
			this.unlock_captcha = unlock_captcha;
		}

		public Mono.Google.Picasa.PicasaWeb Connect ()
		{
			Log.Debug ("GoogleAccount.Connect()");
			GoogleConnection conn = new GoogleConnection (GoogleService.Picasa);
			ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy ();
			if (unlock_captcha == null || token == null)
				conn.Authenticate(username, password);
			else {
				conn.Authenticate(username, password, token, unlock_captcha);
				token = null;
				unlock_captcha = null;
			}
			connection = conn;
			var picasa = new Mono.Google.Picasa.PicasaWeb(conn);
			this.picasa = picasa;
			return picasa;
		}

		private void MarkChanged()
		{
			connection = null;
		}

		public bool Connected {
			get {
				return (connection != null);
			}
		}

		public string Username {
			get {
				return username;
			}
			set {
				if (username != value) {
					username = value;
					MarkChanged ();
				}
			}
		}

		public string Password {
			get {
				return password;
			}
			set {
				if (password != value) {
					password = value;
					MarkChanged ();
				}
			}
		}

		public string Token {
			get {
				return token;
			}
			set {
				token = value;
			}
		}

		public string UnlockCaptcha {
			get {
				return unlock_captcha;
			}
			set {
				unlock_captcha = value;
			}
		}

		public Mono.Google.Picasa.PicasaWeb Picasa {
			get {
				return picasa;
			}
		}
	}
}
