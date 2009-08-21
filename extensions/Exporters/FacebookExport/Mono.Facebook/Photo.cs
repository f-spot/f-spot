//
// Mono.Facebook.Photo.cs:
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
using System.Xml.Serialization;

namespace Mono.Facebook
{
	[XmlRoot ("photos_upload_response", Namespace="http://api.facebook.com/1.0/")]
	public class Photo : SessionWrapper
	{
		[XmlElement ("pid")]
		public long PId;

		[XmlElement ("aid")]
		public long AId;

		[XmlElement ("owner")]
		public long Owner;

		[XmlElement ("src")]
		public string Source;

		[XmlElement ("src_big")]
		public string SourceBig;

		[XmlElement ("src_small")]
		public string SourceSmall;

		[XmlElement ("link")]
		public string Link;

		[XmlElement ("caption")]
		public string Caption;

		[XmlElement ("created")]
		public long Created;

		public Tag[] GetTags ()
		{
			PhotoTagsResponse rsp = Session.Util.GetResponse<PhotoTagsResponse> ("facebook.photos.getTags",
				FacebookParam.Create ("pids", PId),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			foreach (Tag t in rsp.Tags)
				t.Session = Session;

			return rsp.Tags;
		}

		public Album GetAlbum ()
		{
			AlbumsResponse rsp = Session.Util.GetResponse<AlbumsResponse> ("facebook.photos.getAlbums",
				FacebookParam.Create ("aids", AId),
				FacebookParam.Create ("session_key", Session.SessionKey),
				FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			if (rsp.Albums.Length < 1)
				return null;

			rsp.Albums[0].Session = Session;
			return rsp.Albums[0];
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
