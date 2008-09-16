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

namespace Mono.Facebook
{
	public class EventMemberList
	{
		public Friend[] Attending;

		public Friend[] Unsure;

		public Friend[] Declined;

		public Friend[] NotReplied;
	}

	public class Event : SessionWrapper
	{
		[XmlElement ("eid")]
		public long EId;

		[XmlElement ("name")]
		public string Name;

		[XmlElement ("tagline")]
		public string Tagline;

		[XmlElement ("nid")]
		public long NId;

		[XmlElement ("pic")]
		public string Pic;

		[XmlIgnore ()]
		public Uri PicUri
		{
			get { return new Uri (Pic); }
		}

		[XmlElement ("pic_big")]
		public string PicBig;

		[XmlIgnore ()]
		public Uri PicBigUri
		{
			get { return new Uri (PicBig); }
		}

		[XmlElement ("pic_small")]
		public string PicSmall;

		[XmlIgnore ()]
		public Uri PicSmallUri
		{
			get { return new Uri (PicSmall); }
		}

		[XmlElement ("host")]
		public string Host;

		[XmlElement ("description")]
		public string Description;

		[XmlElement ("event_type")]
		public string EventType;

		[XmlElement ("event_subtype")]
		public string EventSubType;

		[XmlElement ("start_time")]
		public long StartTime;

		[XmlElement ("end_time")]
		public long EndTime;

		[XmlElement ("creator")]
		public long? Creator;

		[XmlElement ("update_time")]
		public long UpdateTime;

		[XmlElement ("location")]
		public string Location;

		[XmlElement ("venue")]
		public Location Venue;

		[XmlIgnore ()]
		public EventMemberList MemberList {
			get {
				EventMembersResponse rsp = Session.Util.GetResponse<EventMembersResponse> ("facebook.events.getMembers",
					FacebookParam.Create ("session_key", Session.SessionKey),
					FacebookParam.Create ("call_id", DateTime.Now.Ticks),
					FacebookParam.Create ("eid", EId));

				EventMemberList list = new EventMemberList ();

				list.Attending = new Friend [rsp.Attending.UIds.Length];
				for (int i = 0; i < list.Attending.Length; i++)
					list.Attending[i] = new Friend (rsp.Attending.UIds [i], this.Session);

				list.Unsure = new Friend [rsp.Unsure.UIds.Length];
				for (int i = 0; i < list.Unsure.Length; i++)
					list.Unsure [i] = new Friend (rsp.Unsure.UIds [i], this.Session);

				list.Declined = new Friend [rsp.Declined.UIds.Length];
				for (int i = 0; i < list.Declined.Length; i ++)
					list.Declined [i] = new Friend (rsp.Declined.UIds [i], this.Session);

				list.NotReplied = new Friend [rsp.NotReplied.UIds.Length];

				return list;
			}

		}
	}
}
