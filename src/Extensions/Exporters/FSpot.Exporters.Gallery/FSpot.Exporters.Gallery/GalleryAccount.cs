//
// GalleryAccount.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;
using FSpot.Settings;


namespace FSpot.Exporters.Gallery
{
	public class GalleryAccount
	{
		public GalleryAccount (string name, string url, string username, string password) : this (name, url, username, password, GalleryVersion.VersionUnknown) { }
		public GalleryAccount (string name, string url, string username, string password, GalleryVersion version)
		{
			Name = name;
			this.username = username;
			this.password = password;
			Url = url;

			if (version != GalleryVersion.VersionUnknown)
				Version = version;
			else
				Version = Gallery.DetectGalleryVersion (Url);
		}

		public const string EXPORT_SERVICE = "gallery/";
		public const string LIGHTTPD_WORKAROUND_KEY = Preferences.ExportKey + EXPORT_SERVICE + "lighttpd_workaround";

		public Gallery Connect ()
		{
			//System.Console.WriteLine ("GalleryAccount.Connect()");
			Gallery gal = null;

			if (Version == GalleryVersion.VersionUnknown)
				Version = Gallery.DetectGalleryVersion (Url);

			if (Version == GalleryVersion.Version1)
				gal = new Gallery1 (url, url);
			else if (Version == GalleryVersion.Version2)
				gal = new Gallery2 (url, url);
			else
				throw new GalleryException (Strings.CannotConnectToGalleryUnknownVersionCheckRemotePlugin);

			Logger.Log.Debug ("Gallery created: " + gal);

			gal.Login (username, password);

			Gallery = gal;
			connected = true;

			Gallery.expect_continue = Preferences.Get<bool> (LIGHTTPD_WORKAROUND_KEY);

			return Gallery;
		}

		public GalleryVersion Version { get; private set; }
		public Gallery Gallery { get; private set; }
		public string Name { get; set; }

		bool connected;
		public bool Connected {
			get {
				bool retVal = false;
				if (Gallery != null) {
					retVal = Gallery.IsConnected ();
				}
				if (connected != retVal) {
					Logger.Log.Warning ("Connected and retVal for IsConnected() don't agree");
				}
				return retVal;
			}
		}

		public void MarkChanged ()
		{
			connected = false;
			Gallery = null;
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
