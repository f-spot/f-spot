//
// Mono.Facebook.Tag.cs:
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
	public class Tag : photo_tag, SessionWrapper
	{
        [XmlIgnore]
		public FacebookSession Session { get; set; }

        [XmlIgnore]
        public long Subject { get { return subject; } }

		public Photo GetPhoto ()
		{
			PhotosResponse rsp = Session.Util.GetResponse<PhotosResponse> ("facebook.photos.get", FacebookParam.Create ("pids", pid),
					FacebookParam.Create ("session_key", Session.SessionKey),
					FacebookParam.Create ("call_id", System.DateTime.Now.Ticks));

			if (rsp.Photos.Length < 1)
				return null;

			rsp.Photos[0].Session = Session;
			return rsp.Photos[0];
		}
	}
}
