//
// Mono.Google.Picasa.PicasaPicture.cs:
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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Mono.Google.Picasa {
	public class PicasaPicture {
		GoogleConnection conn;
		PicasaAlbum album;
		string title;
		string description;
		DateTime pub_date;
		string thumbnail_url;
		string image_url;
		int width;
		int height;
		int index;
		string id;
		string link;
		string tags;

		internal PicasaPicture (GoogleConnection conn, PicasaAlbum album, XmlNode nodeitem, XmlNamespaceManager nsmgr)
		{
			this.conn = conn;
			this.album = album;
			ParsePicture (nodeitem, nsmgr);
		}

		private void ParsePicture (XmlNode nodeitem, XmlNamespaceManager nsmgr)
		{
			title = nodeitem.SelectSingleNode ("atom:title", nsmgr).InnerText;
			foreach (XmlNode xlink in nodeitem.SelectNodes ("atom:link", nsmgr)) {
				if (xlink.Attributes.GetNamedItem ("rel").Value == "alternate") {
					link = xlink.Attributes.GetNamedItem ("href").Value;
					break;
				}
			}
			description = nodeitem.SelectSingleNode ("media:group/media:description", nsmgr).InnerText;
			CultureInfo info = CultureInfo.InvariantCulture;
			pub_date = DateTime.ParseExact (nodeitem.SelectSingleNode ("atom:published", nsmgr).InnerText, GDataApi.DateFormat, info);
			thumbnail_url = nodeitem.SelectSingleNode ("media:group/media:thumbnail", nsmgr).Attributes.GetNamedItem ("url").Value;
			image_url = nodeitem.SelectSingleNode ("media:group/media:content", nsmgr).Attributes.GetNamedItem ("url").Value;
			width = (int) UInt32.Parse (nodeitem.SelectSingleNode ("gphoto:width", nsmgr).InnerText);
			height = (int) UInt32.Parse (nodeitem.SelectSingleNode ("gphoto:height", nsmgr).InnerText);

			XmlNode node = nodeitem.SelectSingleNode ("gphoto:index", nsmgr);
			index = (node != null) ? (int) UInt32.Parse (node.InnerText) : -1;
			node = nodeitem.SelectSingleNode ("gphoto:id", nsmgr);
			id = (node != null) ? node.InnerText : "auto" + title.GetHashCode ().ToString ();
			tags = nodeitem.SelectSingleNode ("media:group/media:keywords", nsmgr).InnerText;
		}

		public void DownloadToStream (Stream stream)
		{
			conn.DownloadToStream (image_url, stream);
		}

		public void DownloadThumbnailToStream (Stream stream)
		{
			conn.DownloadToStream (thumbnail_url, stream);
		}

		public PicasaAlbum Album {
			get { return album; }
		}

		public string Title {
			get { return title; }
		}

		public string Description {
			get { return description; }
		}

		public DateTime Date {
			get { return pub_date; }
		}

		public string ThumbnailURL {
			get { return thumbnail_url; }
		}

		public string ImageURL {
			get { return image_url; }
		}

		public int Width {
			get { return width; }
		}

		public int Height {
			get { return height; }
		}

		public int Index {
			get { return index; }
		}

		public string UniqueID {
			get { return id; }
		}

		public string Link {
			get { return link; }
		}

		public string Tags {
			get { return tags; }
		}
	}
}

