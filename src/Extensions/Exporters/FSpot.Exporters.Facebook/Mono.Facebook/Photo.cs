//
// Mono.Facebook.Photo.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

using Mono.Facebook.Schemas;

namespace Mono.Facebook
{
    [System.Xml.Serialization.XmlRootAttribute("photos_upload_response", Namespace="http://api.facebook.com/1.0/")]
	public class Photo : photo, SessionWrapper
	{
        [XmlIgnore]
		public FacebookSession Session { get; set; }

        [XmlIgnore]
        public string PId { get { return pid; } }

		public Tag[] GetTags ()
		{
			PhotoTagsResponse rsp = Session.Util.GetResponse<PhotoTagsResponse> ("facebook.photos.getTags",
				FacebookParam.Create ("pids", pid),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			foreach (Tag t in rsp.Tags)
				t.Session = Session;

			return rsp.Tags;
		}

		public Album GetAlbum ()
		{
			var rsp = Session.Util.GetResponse<AlbumsResponse> ("facebook.photos.getAlbums",
				FacebookParam.Create ("aids", aid),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			if (rsp.album.Length < 1)
				return null;

			rsp.album[0].Session = Session;
			return rsp.album[0];
		}

		// does not work right now: cannot tag photo already visible on facebook
		public Tag AddTag (string tag_text, float x, float y)
		{
			Tag new_tag = Session.Util.GetResponse<Tag> ("facebook.photos.addTag",
				FacebookParam.Create ("pid", PId),
				FacebookParam.Create ("tag_text", tag_text),
				FacebookParam.Create ("x", x),
				FacebookParam.Create ("y", y),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			new_tag.Session = Session;

			return new_tag;
		}
	}
}
