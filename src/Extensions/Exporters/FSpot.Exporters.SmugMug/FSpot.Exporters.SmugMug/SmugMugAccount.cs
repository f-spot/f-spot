//
// SmugMugAccount.cs
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

using SmugMugNet;

namespace FSpot.Exporters.SmugMug
{
	public class SmugMugAccount
	{
		private string username;
		private string password;
		private SmugMugApi smugmug_proxy;

		public SmugMugAccount (string username, string password)
		{
			this.username = username;
			this.password = password;
		}

		public SmugMugApi Connect ()
		{
			Logger.Log.Debug ("SmugMug.Connect() " + username);
			SmugMugApi proxy = new SmugMugApi (username, password);
			ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy ();
			proxy.Login ();

			this.smugmug_proxy = proxy;
			return proxy;
		}

		private void MarkChanged()
		{
			smugmug_proxy = null;
		}

		public bool Connected {
			get {
				return (smugmug_proxy != null && smugmug_proxy.Connected);
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

		public SmugMugApi SmugMug {
			get {
				return smugmug_proxy;
			}
		}
	}
}
