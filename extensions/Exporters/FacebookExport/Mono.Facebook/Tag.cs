//
// Mono.Facebook.Tag.cs:
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
