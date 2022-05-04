//
// Mono.Facebook.Event.cs:
// // Authors:
//	George Talusan (george@convolve.ca)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;
using System.Xml.Serialization;

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
