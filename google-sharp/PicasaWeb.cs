//
// Mono.Google.Picasa.PicasaWeb.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// (C) Copyright 2006 Novell, Inc. (http://www.novell.com)
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Mono.Google.Picasa {
	public class PicasaWeb {
		GoogleConnection conn;
		PicasaV1 api;
		string title;
		string link;
		string user;
		string nickname;
		long quota_used;
		long quota_limit;

		public PicasaWeb (GoogleConnection conn)
		{
			if (conn == null)
				throw new ArgumentNullException ("conn");

			if (conn.User == null)
				throw new ArgumentException ("Need authentication before being used.", "conn");

			this.conn = conn;
			api = new PicasaV1 (conn);
		}

		public string Title {
			get { return title; }
		}

		public string Link {
			get { return link; }
		}

		public string User {
			get { return user; }
		}

		public string NickName {
			get { return nickname; }
		}

		public long QuotaUsed {
			get { return quota_used; }
		}

		public long QuotaLimit {
			get { return quota_limit; }
		}

		internal PicasaV1 API {
			get { return api; }
		}

		internal GoogleConnection Connection {
			get { return conn; }
		}

		public string CreateAlbum (string title)
		{
			return CreateAlbum (title, null);
		}

		public string CreateAlbum (string title, AlbumAccess access)
		{
			return CreateAlbum (title, null, access, DateTime.Now);
		}

		public string CreateAlbum (string title, string description)
		{
			return CreateAlbum (title, description, AlbumAccess.Public);
		}

		public string CreateAlbum (string title, string description, AlbumAccess access)
		{
			return CreateAlbum (title, description, access, DateTime.Now);
		}

		public string CreateAlbum (string title, string description, AlbumAccess access, DateTime pubDate)
		{
			if (title == null)
				throw new ArgumentNullException ("title");

			if (description == null)
				description = "";

			if (access != AlbumAccess.Public && access != AlbumAccess.Private)
				throw new ArgumentException ("Invalid value.", "access");

			// Check if pubDate can be in the past
			string url = api.GetPostURL ();
			string op_string = GetXmlForCreate (title, description, pubDate, access, conn.User);
			byte [] op_bytes = Encoding.UTF8.GetBytes (op_string);
			MultipartRequest request = new MultipartRequest (url);
			request.Request.CookieContainer = conn.Cookies;
			request.BeginPart ();
			request.AddHeader ("Content-Disposition: form-data; name=\"xml\"\r\n");
			request.AddHeader ("Content-Type: text/plain; charset=utf8\r\n", true);
			request.WriteContent (op_bytes);
			request.EndPart (true);
			string received = request.GetResponseAsString ();

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNode node = doc.SelectSingleNode ("/response/result");
			if (node == null)
				throw new CreateAlbumException ("Invalid response from server");

			if (node.InnerText != "success") {
				node = doc.SelectSingleNode ("/response/reason");
				if (node == null)
					throw new CreateAlbumException ("Unknown reason");
					
				throw new CreateAlbumException (node.InnerText);
			}
			return doc.SelectSingleNode ("/response/id").InnerText;
		}

		/*
			"<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
			"<rss version=\"2.0\" xmlns:gphoto=\"http://www.temp.com/\">\n" +
			" <channel>\n" +
			"  <title>{0}</title>\n" +
			"  <description/>\n" +
			"  <pubDate>{1:d' 'MMM' 'yyyy' 'H':'mm':'ss' 'zzz}</pubDate>\n" +
			"  <gphoto:access>{2}</gphoto:access>\n" +
			"  <gphoto:user>{3}</gphoto:user>\n" +
			"  <gphoto:location/>\n" +
			"  <gphoto:op>createAlbum</gphoto:op>\n" +
			" </channel>\n" +
			"</rss>";
		*/

		static string GetXmlForCreate (string title, string desc, DateTime date, AlbumAccess access, string username)
		{
			XmlUtil xml = new XmlUtil ();
			xml.WriteElementString ("title", title);
			xml.WriteElementString ("description", desc);
			CultureInfo info = CultureInfo.InvariantCulture;
			string pubdate = date.ToString ("d' 'MMM' 'yyyy' 'H':'mm':'ss' 'zzz", info);
			pubdate = pubdate.Substring (0, pubdate.Length - 3) + "00"; // Replaces ':00' with '00'
			xml.WriteElementString ("pubDate", pubdate);
			xml.WriteElementString ("access", access.ToString ().ToLower (CultureInfo.InvariantCulture), PicasaNamespaces.GPhoto);
			xml.WriteElementString ("user", username, PicasaNamespaces.GPhoto);
			xml.WriteElementString ("op", "createAlbum", PicasaNamespaces.GPhoto);
			// location?
			return xml.GetDocumentString ();
		}

		/*
			"<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n" +
			"<rss version=\"2.0\" xmlns:gphoto=\"http://www.temp.com/\">\n" +
			"  <channel>\n" +
			"    <gphoto:user>{0}</gphoto:user>\n" +
			"    <gphoto:id>{1}</gphoto:id>\n" +
			"    <gphoto:op>deleteAlbum</gphoto:op>\n" +
			"  </channel>\n" +
			"</rss>";
		*/

		static string GetXmlForDelete (string user, string aid)
		{
			XmlUtil xml = new XmlUtil ();
			xml.WriteElementString ("user", user, PicasaNamespaces.GPhoto);
			xml.WriteElementString ("id", aid, PicasaNamespaces.GPhoto);
			xml.WriteElementString ("op", "deleteAlbum", PicasaNamespaces.GPhoto);
			return xml.GetDocumentString ();
		}

		public void DeleteAlbum (PicasaAlbum album)
		{
			if (album == null)
				throw new ArgumentNullException ("album");

			DeleteAlbum (album.UniqueID);
		}

		public void DeleteAlbum (string unique_id)
		{
			if (unique_id == null)
				throw new ArgumentNullException ("unique_id");

			string url = api.GetPostURL ();
			string op_string = GetXmlForDelete (conn.User, unique_id);
			byte [] op_bytes = Encoding.UTF8.GetBytes (op_string);
			MultipartRequest request = new MultipartRequest (url);
			request.Request.CookieContainer = conn.Cookies;
			request.BeginPart ();
			request.AddHeader ("Content-Disposition: form-data; name=\"xml\"\r\n");
			request.AddHeader ("Content-Type: text/plain; charset=utf8\r\n", true);
			request.WriteContent (op_bytes);
			request.EndPart (true);
			string received = request.GetResponseAsString ();

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNode node = doc.SelectSingleNode ("/response/result");
			if (node == null)
				throw new DeleteAlbumException ("Invalid response from server");

			if (node.InnerText != "success") {
				node = doc.SelectSingleNode ("/response/reason");
				if (node == null)
					throw new DeleteAlbumException ("Unknown reason");
					
				throw new DeleteAlbumException (node.InnerText);
			}
		}

		public PicasaAlbumCollection GetAlbums ()
		{
			string gallery_link = api.GetGalleryLink (conn.User);
			string received = conn.DownloadString (gallery_link);

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNode channel = doc.SelectSingleNode ("/rss/channel");
			XmlNode node = channel.SelectSingleNode ("title");
			title = node.InnerText;
			node = channel.SelectSingleNode ("link");
			link = node.InnerText;
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			nsmgr.AddNamespace ("photo", "http://www.pheed.com/pheed/");
			nsmgr.AddNamespace ("media", "http://search.yahoo.com/mrss/");
			nsmgr.AddNamespace ("gphoto", "http://picasaweb.google.com/lh/picasaweb");
			node = channel.SelectSingleNode ("gphoto:user", nsmgr);
			user = node.InnerText;
			node = channel.SelectSingleNode ("gphoto:nickname", nsmgr);
			nickname = node.InnerText;
			node = channel.SelectSingleNode ("gphoto:quotacurrent", nsmgr);
			quota_used = (node != null) ? (long) UInt64.Parse (node.InnerText) : -1;
			node = channel.SelectSingleNode ("gphoto:quotalimit", nsmgr);
			quota_limit = (node != null) ? (long) UInt64.Parse (node.InnerText) : -1;
			PicasaAlbumCollection coll = new PicasaAlbumCollection ();
			foreach (XmlNode item in channel.SelectNodes ("item")) {
				coll.Add (PicasaAlbum.ParseAlbumInfo (this, item, nsmgr));
			}
			coll.SetReadOnly ();
			return coll;
		}
	}
}

