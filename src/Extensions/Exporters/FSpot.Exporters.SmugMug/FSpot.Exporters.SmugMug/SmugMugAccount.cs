using System;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using Gtk;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using Hyena;
using FSpot.UI.Dialog;
using Gnome.Keyring;
using SmugMugNet;

namespace FSpot.Exporters.SmugMug
{
	public class SmugMugAccount {
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
			Log.Debug ("SmugMug.Connect() " + username);
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
