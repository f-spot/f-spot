//
// Mono.Google.Picasa.PicasaPicture.cs:
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
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

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

		internal PicasaPicture (GoogleConnection conn, PicasaAlbum album)
		{
			this.conn = conn;
			this.album = album;
		}

		internal static PicasaPicture ParsePictureInfo (GoogleConnection conn, PicasaAlbum album, XmlNode nodeitem, XmlNamespaceManager nsmgr)
		{
			PicasaPicture picture = new PicasaPicture (conn, album);
			picture.title = nodeitem.SelectSingleNode ("title").InnerText;
			picture.description = nodeitem.SelectSingleNode ("description").InnerText;
			picture.pub_date = DateTime.ParseExact (nodeitem.SelectSingleNode ("pubDate").InnerText, "d' 'MMM' 'yyyy' 'H':'mm':'ss' 'zzz", null);
			picture.thumbnail_url = nodeitem.SelectSingleNode ("photo:thumbnail", nsmgr).InnerText;
			picture.image_url = nodeitem.SelectSingleNode ("photo:imgsrc", nsmgr).InnerText;
			picture.width = (int) UInt32.Parse (nodeitem.SelectSingleNode ("gphoto:width", nsmgr).InnerText);
			picture.height = (int) UInt32.Parse (nodeitem.SelectSingleNode ("gphoto:height", nsmgr).InnerText);
			XmlNode node = nodeitem.SelectSingleNode ("gphoto:index", nsmgr);
			picture.index = (node != null) ? (int) UInt32.Parse (node.InnerText) : -1;
			node = nodeitem.SelectSingleNode ("gphoto:id", nsmgr);
			picture.id = (node != null) ? node.InnerText : "auto" + picture.title.GetHashCode ().ToString ();
			return picture;
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
	}
}

