//
// GalleryAccount.cs
//
// Author:
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Paul Lange
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

using Mono.Unix;

using FSpot.Settings;

using Hyena;

namespace FSpot.Exporters.Gallery
{
	public class GalleryAccount
	{
		public GalleryAccount (string name, string url, string username, string password) : this (name, url, username, password, GalleryVersion.VersionUnknown) {}
		public GalleryAccount (string name, string url, string username, string password, GalleryVersion version)
		{
			this.Name = name;
			this.username = username;
			this.password = password;
			this.Url = url;

			if (version != GalleryVersion.VersionUnknown)
				this.Version = version;
			else
				this.Version = Gallery.DetectGalleryVersion(Url);
		}

		public const string EXPORT_SERVICE = "gallery/";
		public const string LIGHTTPD_WORKAROUND_KEY = Preferences.APP_FSPOT_EXPORT + EXPORT_SERVICE + "lighttpd_workaround";

		public Gallery Connect ()
		{
			//System.Console.WriteLine ("GalleryAccount.Connect()");
			Gallery gal = null;

			if (Version == GalleryVersion.VersionUnknown)
				this.Version = Gallery.DetectGalleryVersion(Url);

			if (Version == GalleryVersion.Version1)
				gal = new Gallery1 (url, url);
			else if (Version == GalleryVersion.Version2)
				gal = new Gallery2 (url, url);
			else
				throw new GalleryException (Catalog.GetString("Cannot connect to a Gallery for which the version is unknown.\nPlease check that you have Remote plugin 1.0.8 or later"));

			Log.Debug ("Gallery created: " + gal);

			gal.Login (username, password);

			Gallery = gal;
			connected = true;

			Gallery.expect_continue = Preferences.Get<bool> (LIGHTTPD_WORKAROUND_KEY);

			return Gallery;
		}

		public GalleryVersion Version{ get; private set; }
		public Gallery Gallery { get; private set; }
		public string Name { get; set; }

		private bool connected;
		public bool Connected {
			get {
				bool retVal = false;
				if(Gallery != null) {
					retVal = Gallery.IsConnected ();
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
