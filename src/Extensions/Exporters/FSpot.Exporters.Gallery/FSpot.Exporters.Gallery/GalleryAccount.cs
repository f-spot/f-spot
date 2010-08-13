using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Web;
using Mono.Unix;
using FSpot;
using FSpot.Core;
using FSpot.Filters;
using FSpot.Widgets;
using FSpot.Utils;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using Hyena;
using Hyena.Widgets;

namespace FSpot.Exporters.Gallery
{
	public class GalleryAccount {
		public GalleryAccount (string name, string url, string username, string password) : this (name, url, username, password, GalleryVersion.VersionUnknown) {}
		public GalleryAccount (string name, string url, string username, string password, GalleryVersion version)
		{
			this.name = name;
			this.username = username;
			this.password = password;
			this.Url = url;

			if (version != GalleryVersion.VersionUnknown) {
				this.version = version;
			} else {
				this.version = Gallery.DetectGalleryVersion(Url);
			}
		}

		public const string EXPORT_SERVICE = "gallery/";
		public const string LIGHTTPD_WORKAROUND_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "lighttpd_workaround";

		public Gallery Connect ()
		{
			//System.Console.WriteLine ("GalleryAccount.Connect()");
			Gallery gal = null;

			if (version == GalleryVersion.VersionUnknown)
				this.version = Gallery.DetectGalleryVersion(Url);

			if (version == GalleryVersion.Version1) {
				gal = new Gallery1 (url, url);
			} else if (version == GalleryVersion.Version2) {
				gal = new Gallery2 (url, url);
			} else {
				throw new GalleryException (Catalog.GetString("Cannot connect to a Gallery for which the version is unknown.\nPlease check that you have Remote plugin 1.0.8 or later"));
			}

			Log.Debug ("Gallery created: " + gal);

			gal.Login (username, password);

			gallery = gal;
			connected = true;

			gallery.expect_continue = Preferences.Get<bool> (LIGHTTPD_WORKAROUND_KEY);

			return gallery;
		}

		GalleryVersion version;
		public GalleryVersion Version{
			get {
				return version;
			}
		}

		private bool connected;
		public bool Connected {
			get {
				bool retVal = false;
				if(gallery != null) {
					retVal = gallery.IsConnected ();
				}
				if (connected != retVal) {
					Log.Warning ("Connected and retVal for IsConnected() don't agree");
				}
				return retVal;
			}
		}

		public void MarkChanged ()
		{
			connected = false;
			gallery = null;
		}

		Gallery gallery;
		public Gallery Gallery {
			get {
				return gallery;
			}
		}

		string name;
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		string url;
		public string Url {
			get {
				return url;
			}
			set {
				if (url != value) {
					url = value;
					MarkChanged ();
				}
			}
		}

		string username;
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

		string password;
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
	}
}
