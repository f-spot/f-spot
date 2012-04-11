//
// Mono.Facebook.Album.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Net;
using Mono.Facebook.Schemas;

namespace Mono.Facebook
{
    [System.Xml.Serialization.XmlRootAttribute("photos_createAlbum_response", Namespace="http://api.facebook.com/1.0/")]
	public class Album : album, SessionWrapper
	{
        [XmlIgnore]
		public FacebookSession Session { get; set; }

		public Photo[] GetPhotos ()
		{
			PhotosResponse rsp = Session.Util.GetResponse<PhotosResponse> ("facebook.photos.get",
				FacebookParam.Create ("aid", aid),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", DateTime.Now.Ticks));

			foreach (Photo p in rsp.Photos)
				p.Session = Session;

			return rsp.Photos;
		}

		public Tag[] GetTags ()
		{
			StringBuilder pids = new StringBuilder ();

			foreach (Photo p in GetPhotos ()) {
				if (pids.Length > 0)
					pids.Append (",");

				pids.Append (p.PId);
			}

			PhotoTagsResponse rsp = Session.Util.GetResponse<PhotoTagsResponse> ("facebook.photos.getTags",
				FacebookParam.Create ("pids", pids),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			foreach (Tag t in rsp.Tags)
				t.Session = Session;

			return rsp.Tags;
		}


		public Photo Upload (string caption, string path)
		{
			Photo uploaded = Session.Util.Upload (aid, caption, path, Session.SessionKey);
			uploaded.Session = this.Session;

			return uploaded;
		}
	}
}
