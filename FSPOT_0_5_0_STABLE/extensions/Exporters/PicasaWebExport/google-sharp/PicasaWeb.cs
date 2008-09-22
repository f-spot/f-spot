//
// Mono.Google.Picasa.PicasaWeb.cs:
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Stephane Delcroix  (stephane@delcroix.org)
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
// Check Picasa Web Albums Data Api at http://code.google.com/apis/picasaweb/gdata.html
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
		string title;
		string user;
		string nickname;
		long quota_used;
		long quota_limit;

		public PicasaWeb (GoogleConnection conn) : this (conn, null)
		{
		}

		public PicasaWeb (GoogleConnection conn, string username)
		{
			if (conn == null)
				throw new ArgumentNullException ("conn");

			if (conn.User == null && username == null)
				throw new ArgumentException ("The connection should be authenticated OR you should call this constructor with a non-null username argument");

			this.conn = conn;
			this.user = username ?? conn.User;

			string received = conn.DownloadString (GDataApi.GetGalleryEntry (user));
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode entry = doc.SelectSingleNode ("atom:entry", nsmgr);
			ParseGallery (entry, nsmgr);
		}

		public string Title {
			get { return title; }
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

		internal GoogleConnection Connection {
			get { return conn; }
		}

		public PicasaAlbum CreateAlbum (string title)
		{
			return CreateAlbum (title, null);
		}

		public PicasaAlbum CreateAlbum (string title, AlbumAccess access)
		{
			return CreateAlbum (title, null, access, DateTime.Now);
		}

		public PicasaAlbum CreateAlbum (string title, string description)
		{
			return CreateAlbum (title, description, AlbumAccess.Public);
		}

		public PicasaAlbum CreateAlbum (string title, string description, AlbumAccess access)
		{
			return CreateAlbum (title, description, access, DateTime.Now);
		}

		public PicasaAlbum CreateAlbum (string title, string description, AlbumAccess access, DateTime pubDate)
		{
			if (title == null)
				throw new ArgumentNullException ("title");

			if (description == null)
				description = "";

			if (access != AlbumAccess.Public && access != AlbumAccess.Private)
				throw new ArgumentException ("Invalid value.", "access");

			// Check if pubDate can be in the past
			string url = GDataApi.GetPostURL (conn.User);
			if (url == null)
				throw new UnauthorizedAccessException ("You are not authorized to create albums.");
			string op_string = GetXmlForCreate (title, description, pubDate, access);
			byte [] op_bytes = Encoding.UTF8.GetBytes (op_string);
			HttpWebRequest request = conn.AuthenticatedRequest (url);
			request.ContentType = "application/atom+xml; charset=UTF-8";
			request.Method = "POST";
			Stream output_stream = request.GetRequestStream ();
			output_stream.Write (op_bytes, 0, op_bytes.Length);
			output_stream.Close ();

			HttpWebResponse response = (HttpWebResponse) request.GetResponse ();
			string received = "";
			using (Stream stream = response.GetResponseStream ()) {
				StreamReader sr = new StreamReader (stream, Encoding.UTF8);
				received = sr.ReadToEnd ();
			}
			response.Close ();

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode entry = doc.SelectSingleNode ("atom:entry", nsmgr);
			return new PicasaAlbum (conn, null, entry, nsmgr);
		}

		/* (from gdata documentation...)
		<entry xmlns='http://www.w3.org/2005/Atom'
		    xmlns:media='http://search.yahoo.com/mrss/'
		    xmlns:gphoto='http://schemas.google.com/photos/2007'>
		  <title type='text'>Trip To Italy</title>
		  <summary type='text'>This was the recent trip I took to Italy.</summary>
		  <gphoto:location>Italy</gphoto:location>
		  <gphoto:access>public</gphoto:access>
		  <gphoto:commentingEnabled>true</gphoto:commentingEnabled>
		  <gphoto:timestamp>1152255600000</gphoto:timestamp>
		  <media:group>
		    <media:keywords>italy, vacation</media:keywords>
		  </media:group>
		  <category scheme='http://schemas.google.com/g/2005#kind'
		    term='http://schemas.google.com/photos/2007#album'></category>
		</entry>
		*/

		private static string GetXmlForCreate (string title, string desc, DateTime date, AlbumAccess access)
		{
			XmlUtil xml = new XmlUtil ();
			xml.WriteElementStringWithAttributes ("title", title, "type", "text");
			xml.WriteElementStringWithAttributes ("summary", desc, "type", "text");
			// location ?
			xml.WriteElementString ("access", access.ToString ().ToLower (CultureInfo.InvariantCulture), PicasaNamespaces.GPhoto);
			// commentingEnabled ?
			xml.WriteElementString ("timestamp", ((long)(date - new DateTime (1970, 1, 1)).TotalSeconds * 1000).ToString (), PicasaNamespaces.GPhoto);
			//keywords ?
			xml.WriteElementStringWithAttributes ("category", null,
					"scheme", "http://schemas.google.com/g/2005#kind",
					"term", "http://schemas.google.com/photos/2007#album");

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

//		static string GetXmlForDelete (string user, string aid)
//		{
//			XmlUtil xml = new XmlUtil ();
//			xml.WriteElementString ("user", user, PicasaNamespaces.GPhoto);
//			xml.WriteElementString ("id", aid, PicasaNamespaces.GPhoto);
//			xml.WriteElementString ("op", "deleteAlbum", PicasaNamespaces.GPhoto);
//			return xml.GetDocumentString ();
//		}

		public void DeleteAlbum (PicasaAlbum album)
		{
			if (album == null)
				throw new ArgumentNullException ("album");

			DeleteAlbum (album.UniqueID);
		}

		public void DeleteAlbum (string unique_id)
		{
	//FIXME: implement this
			throw new System.NotImplementedException ("but I'll implemented if you say please...");
//			if (unique_id == null)
//				throw new ArgumentNullException ("unique_id");
//
//			string url = GDataApi.GetPostURL (conn.User);
//			if (url == null)
//				throw new UnauthorizedAccessException ("You are not authorized to delete this album.");
//			string op_string = GetXmlForDelete (conn.User, unique_id);
//			byte [] op_bytes = Encoding.UTF8.GetBytes (op_string);
//			MultipartRequest request = new MultipartRequest (conn, url);
////			request.Request.CookieContainer = conn.Cookies;
//			request.BeginPart ();
//			request.AddHeader ("Content-Disposition: form-data; name=\"xml\"\r\n");
//			request.AddHeader ("Content-Type: text/plain; charset=utf8\r\n", true);
//			request.WriteContent (op_bytes);
//			request.EndPart (true);
//			string received = request.GetResponseAsString ();
//
//			XmlDocument doc = new XmlDocument ();
//			doc.LoadXml (received);
//			XmlNode node = doc.SelectSingleNode ("/response/result");
//			if (node == null)
//				throw new DeleteAlbumException ("Invalid response from server");
//
//			if (node.InnerText != "success") {
//				node = doc.SelectSingleNode ("/response/reason");
//				if (node == null)
//					throw new DeleteAlbumException ("Unknown reason");
//
//				throw new DeleteAlbumException (node.InnerText);
//			}
		}

		public PicasaAlbumCollection GetAlbums ()
		{
			string gallery_link = GDataApi.GetGalleryFeed (user);
			string received = conn.DownloadString (gallery_link);

			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (received);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager (doc.NameTable);
			XmlUtil.AddDefaultNamespaces (nsmgr);
			XmlNode feed = doc.SelectSingleNode ("atom:feed", nsmgr);
			PicasaAlbumCollection coll = new PicasaAlbumCollection ();
			foreach (XmlNode item in feed.SelectNodes ("atom:entry", nsmgr)) {
				coll.Add (new PicasaAlbum (conn, user, item, nsmgr));
			}
			coll.SetReadOnly ();
			return coll;
		}

		private void ParseGallery (XmlNode nodeitem, XmlNamespaceManager nsmgr)
		{
			XmlNode node = nodeitem.SelectSingleNode ("atom:title", nsmgr);
			title = node.InnerText;
			node = nodeitem.SelectSingleNode ("gphoto:user", nsmgr);
			user = node.InnerText;
			node = nodeitem.SelectSingleNode ("gphoto:nickname", nsmgr);
			nickname = node.InnerText;
			node = nodeitem.SelectSingleNode ("gphoto:quotacurrent", nsmgr);
			quota_used = (node != null) ? (long) UInt64.Parse (node.InnerText) : -1;
			node = nodeitem.SelectSingleNode ("gphoto:quotalimit", nsmgr);
			quota_limit = (node != null) ? (long) UInt64.Parse (node.InnerText) : -1;
		}
	}
}
