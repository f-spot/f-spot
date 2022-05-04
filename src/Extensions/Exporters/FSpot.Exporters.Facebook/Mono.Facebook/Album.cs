//
// Mono.Facebook.Album.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Xml.Serialization;
using System.Text;

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
