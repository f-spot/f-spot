//
// Mono.Google.Picasa.PicasaAlbum.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Stephane Delcroix (stephane@delcroix.org)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
// (C) Copyright 2007 S. Delcroix
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Mono.Google.Picasa {
	public class PicasaAlbum {
		GoogleConnection conn;
		string user;
		string title;
		string description;
		string id;
		string link;
		string authkey = null;
		AlbumAccess access = AlbumAccess.Public;
		int num_photos = -1;
		int num_photos_remaining = -1;
		long bytes_used = -1;

		private PicasaAlbum (GoogleConnection conn)
		{
			if (conn == null)
				throw new ArgumentNullException ("conn");

			this.conn = conn;
		}

		public PicasaAlbum (GoogleConnection conn, string aid) : this (conn)
		{
			if (conn.User == null)
				throw new ArgumentException ("Need authentication before being used.", "conn");

			if (aid == null || aid == String.Empty)
				throw new ArgumentNullException ("aid");

			this.user = conn.User;
			this.id = aid;

			string received = conn.DownloadString (GDataApi.GetAlbumEntryById (conn.User, aid));
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode entry = doc.SelectSingleNode ("atom:entry", nsmgr);
			ParseAlbum (entry, nsmgr);
		}

		public PicasaAlbum (GoogleConnection conn, string user, string aid, string authkey) : this (conn)
		{
			if (user == null || user == String.Empty)
				throw new ArgumentNullException ("user");

			if (aid == null || aid == String.Empty)
				throw new ArgumentNullException ("aid");

			this.user = user;
			this.id = aid;
			this.authkey = authkey;

			string download_link = GDataApi.GetAlbumEntryById (user, id);
			if (authkey != null && authkey != "")
				download_link += "&authkey=" + authkey;
			string received = conn.DownloadString (download_link);

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode entry = doc.SelectSingleNode ("atom:entry", nsmgr);
			ParseAlbum (entry, nsmgr);
		}

		internal PicasaAlbum (GoogleConnection conn, string user, XmlNode nodeitem, XmlNamespaceManager nsmgr) : this (conn)
		{
			this.user = user ?? conn.User;

			ParseAlbum (nodeitem, nsmgr);
		}


		private void ParseAlbum (XmlNode nodeitem, XmlNamespaceManager nsmgr)
		{

			title = nodeitem.SelectSingleNode ("atom:title", nsmgr).InnerText;
			description = nodeitem.SelectSingleNode ("media:group/media:description", nsmgr).InnerText;
			XmlNode node = nodeitem.SelectSingleNode ("gphoto:id", nsmgr);
			if (node != null)
				id = node.InnerText;

			foreach (XmlNode xlink in nodeitem.SelectNodes ("atom:link", nsmgr)) {
				if (xlink.Attributes.GetNamedItem ("rel").Value == "alternate") {
					link = xlink.Attributes.GetNamedItem ("href").Value;
					break;
				}
			}
			node = nodeitem.SelectSingleNode ("gphoto:access", nsmgr);
			if (node != null) {
				string acc = node.InnerText;
				access = (acc == "public") ? AlbumAccess.Public : AlbumAccess.Private;
			}
			node = nodeitem.SelectSingleNode ("gphoto:numphotos", nsmgr);
			if (node != null)
				num_photos = (int) UInt32.Parse (node.InnerText);

			node = nodeitem.SelectSingleNode ("gphoto:numphotosremaining", nsmgr);
			if (node != null)
				num_photos_remaining = (int) UInt32.Parse (node.InnerText);
			node = nodeitem.SelectSingleNode ("gphoto:bytesused", nsmgr);
			if (node != null)
				bytes_used = (long) UInt64.Parse (node.InnerText);
		}

		public PicasaPictureCollection GetPictures ()
		{

			string download_link = GDataApi.GetAlbumFeedById (user, id);
			if (authkey != null && authkey != "")
				download_link += "&authkey=" + authkey;
			string received = conn.DownloadString (download_link);
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode feed = doc.SelectSingleNode ("atom:feed", nsmgr);
			PicasaPictureCollection coll = new PicasaPictureCollection ();
			foreach (XmlNode item in feed.SelectNodes ("atom:entry", nsmgr)) {
				coll.Add (new PicasaPicture (conn, this, item, nsmgr));
			}
			coll.SetReadOnly ();
			return coll;
		}

		/* from http://code.google.com/apis/picasaweb/gdata.html#Add_Photo
		<entry xmlns='http://www.w3.org/2005/Atom'>
		  <title>darcy-beach.jpg</title>
		  <summary>Darcy on the beach</summary>
		  <category scheme="http://schemas.google.com/g/2005#kind"
		    term="http://schemas.google.com/photos/2007#photo"/>
		</entry>
		*/

		static string GetXmlForUpload (string title, string description)
		{
			XmlUtil xml = new XmlUtil ();
			xml.WriteElementString ("title", title);
			xml.WriteElementString ("summary", description);
			xml.WriteElementStringWithAttributes ("category", null,
					"scheme", "http://schemas.google.com/g/2005#kind",
					"term", "http://schemas.google.com/photos/2007#photo");
			return xml.GetDocumentString ();
		}

		public PicasaPicture UploadPicture (string title, Stream input)
		{
			return UploadPicture (title, null, input);
		}

		public PicasaPicture UploadPicture (string title, string description, Stream input)
		{
			return UploadPicture (title, description, "image/jpeg", input);
		}

		public PicasaPicture UploadPicture (string title, string description, string mime_type, Stream input)
		{
			if (title == null)
				throw new ArgumentNullException ("title");

			if (input == null)
				throw new ArgumentNullException ("input");

			if (!input.CanRead)
				throw new ArgumentException ("Cannot read from stream", "input");

			string url = GDataApi.GetURLForUpload (conn.User, id);
			if (url == null)
				throw new UnauthorizedAccessException ("You are not authorized to upload to this album.");

			MultipartRequest request = new MultipartRequest (conn, url);
			MemoryStream ms = null;
			if (UploadProgress != null) {
				// We do 'manual' buffering
				request.Request.AllowWriteStreamBuffering = false;
				ms = new MemoryStream ();
				request.OutputStream = ms;
			}

			request.BeginPart (true);
			request.AddHeader ("Content-Type: application/atom+xml; \r\n", true);
			string upload = GetXmlForUpload (title, description);
			request.WriteContent (upload);
			request.EndPart (false);
			request.BeginPart ();
			request.AddHeader ("Content-Type: " + mime_type + "\r\n", true);

			byte [] data = new byte [8192];
			int nread;
			while ((nread = input.Read (data, 0, data.Length)) > 0) {
				request.WritePartialContent (data, 0, nread);
			}
			request.EndPartialContent ();
			request.EndPart (true); // It won't call Close() on the MemoryStream

			if (UploadProgress != null) {
				int req_length = (int) ms.Length;
				request.Request.ContentLength = req_length;
				DoUploadProgress (title, 0, req_length);
				using (Stream req_stream = request.Request.GetRequestStream ()) {
					byte [] buffer = ms.GetBuffer ();
					int nwrite = 0;
					int offset;
					for (offset = 0; offset < req_length; offset += nwrite) {
						nwrite = System.Math.Min (16384, req_length - offset);
						req_stream.Write (buffer, offset, nwrite);
						// The progress uses the actual request size, not file size.
						DoUploadProgress (title, offset, req_length);
					}
					DoUploadProgress (title, offset, req_length);

				}
			}

			string received = request.GetResponseAsString ();
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode entry = doc.SelectSingleNode ("atom:entry", nsmgr);

			return new PicasaPicture (conn, this, entry, nsmgr);
		}

		public PicasaPicture UploadPicture (string filename)
		{
			return UploadPicture (filename, "");
		}

		public PicasaPicture UploadPicture (string filename, string description)
		{
			return UploadPicture (filename, Path.GetFileName (filename), description);
		}

		public PicasaPicture UploadPicture (string filename, string title, string description)
		{
			if (filename == null)
				throw new ArgumentNullException ("filename");

			if (title == null)
				throw new ArgumentNullException ("title");

			using (Stream stream = File.OpenRead (filename)) {
				return UploadPicture (title, description, stream);
			}
		}

		public string Title {
			get { return title; }
		}

		public string Description {
			get { return description; }
		}

		public string Link {
			get { return link; }
		}

		public string UniqueID {
			get { return id; }
		}

		public AlbumAccess Access {
			get { return access; }
		}

		public int PicturesCount {
			get { return num_photos; }
		}

		public int PicturesRemaining {
			get { return num_photos_remaining; }
		}

		public string User {
			get { return user; }
		}

		public long BytesUsed {
			get { return bytes_used; }
		}

		internal GoogleConnection Connection {
			get { return conn; }
		}

		void DoUploadProgress (string title, long sent, long total)
		{
			if (UploadProgress != null) {
				UploadProgress (this, new UploadProgressEventArgs (title, sent, total));
			}
		}

		public event UploadProgressEventHandler UploadProgress;
	}
}
