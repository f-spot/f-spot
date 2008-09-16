//
// Mono.Facebook.Friend.cs:
//
// Authors:
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
	public class Friend : SessionWrapper
	{
		[XmlElement ("uid")]
		public long UId;

		public Friend (long UId, FacebookSession session)
		{
			this.UId = UId;
			this.Session = session;
		}

		public Friend ()
		{ }

		public User GetUserInfo ()
		{
			User[] users = Session.GetUserInfo (new long[] { UId }, User.FIELDS);

			return users[0];
		}

		public bool IsFriendsWith (Friend friend)
		{
			FriendInfo[] info = IsFriendsWith (new Friend[] { friend });
			return info[0].AreFriends;
		}

		public Group[] Groups
		{
			get { return Session.GetGroups (UId, null); }
		}

		public Event[] Events
		{
			get { return Session.GetEvents (UId, null, 0, 0, null); }
		}

		public FriendInfo[] IsFriendsWith (Friend[] friends)
		{
			long[] me = new long[friends.Length];
			for (int i = 0; i < me.Length; i++)
				me[i] = UId;

			long[] them = new long[friends.Length];
			for (int i = 0; i < friends.Length; i++)
			{
				them[i] = friends[i].UId;
			}

			return Session.AreFriends (me, them);
		}
	}

	public class Me : Friend
	{
		public Me (long uid, FacebookSession session)
			: base (uid, session)
		{ }

		public Notifications Notifications
		{
			get { return Session.GetNotifications (); }
		}
	}
}
