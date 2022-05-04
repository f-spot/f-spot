//
// Mono.Facebook.Friend.cs:
//
// Authors:
//	George Talusan (george@convolve.ca)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace Mono.Facebook
{
	public class Friend : SessionWrapper
	{
		public FacebookSession Session { get; set; }
		public long UId { get; set; }

		public Friend (long UId, FacebookSession session)
		{
			this.UId = UId;
			this.Session = session;
		}

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
