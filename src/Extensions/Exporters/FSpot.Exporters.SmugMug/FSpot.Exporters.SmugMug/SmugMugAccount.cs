//
// SmugMugAccount.cs
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
