//
// Mono.Facebook.Event.cs:
// // Authors:
//	George Talusan (george@convolve.ca)
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
using Mono.Facebook.Schemas;

namespace Mono.Facebook
{
	public class EventMemberList
	{
		public Friend[] Attending;
		public Friend[] Unsure;
		public Friend[] Declined;
		public Friend[] NotReplied;
	}

	public class Event : @event, SessionWrapper
	{
        [XmlIgnore]
		public FacebookSession Session { get; set; }

		[XmlIgnore ()]
		public Uri PicUri
		{
			get { return new Uri (pic); }
		}

		[XmlIgnore ()]
		public Uri PicBigUri
		{
			get { return new Uri (pic_big); }
		}

		[XmlIgnore ()]
		public Uri PicSmallUri
		{
			get { return new Uri (pic_small); }
		}

		[XmlIgnore ()]
		public EventMemberList MemberList {
			get {
				var rsp = Session.Util.GetResponse<event_members> ("facebook.events.getMembers",
					FacebookParam.Create ("session_key", Session.SessionKey),
					FacebookParam.Create ("call_id", DateTime.Now.Ticks),
					FacebookParam.Create ("eid", eid));

				EventMemberList list = new EventMemberList ();

				list.Attending = new Friend [rsp.attending.uid.Length];
				for (int i = 0; i < list.Attending.Length; i++)
					list.Attending[i] = new Friend (rsp.attending.uid [i], this.Session);

				list.Unsure = new Friend [rsp.unsure.uid.Length];
				for (int i = 0; i < list.Unsure.Length; i++)
					list.Unsure [i] = new Friend (rsp.unsure.uid [i], this.Session);

				list.Declined = new Friend [rsp.declined.uid.Length];
				for (int i = 0; i < list.Declined.Length; i ++)
					list.Declined [i] = new Friend (rsp.declined.uid [i], this.Session);

				list.NotReplied = new Friend [rsp.not_replied.uid.Length];

				return list;
			}
		}
	}
}
