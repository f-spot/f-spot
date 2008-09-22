//
// Mono.Facebook.Responses.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolve.ca)
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
using System;
using System.Xml.Serialization;

namespace Mono.Facebook
{
	[XmlRoot ("auth_getSession_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class SessionInfo
	{
		[XmlElement ("session_key")]
		public string SessionKey;

		[XmlElement ("uid")]
		public long UId;

		[XmlElement ("secret")]
		public string Secret;

		[XmlElement ("expires")]
		public long Expires;

		[XmlIgnore ()]
		public bool IsInfinite
		{
			get { return Expires == 0; }
		}

		public SessionInfo ()
		{}

		// use this if you want to create a session based on infinite session
		// credentials
		public SessionInfo (string session_key, long uid, string secret)
		{
			this.SessionKey = session_key;
			this.UId = uid;
			this.Secret = secret;
			this.Expires = 0;
		}
	}

	[XmlRoot ("photos_getAlbums_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class AlbumsResponse
	{
		[XmlElement ("album")]
		public Album[] album_array;

		[XmlIgnore ()]
		public Album[] Albums
		{
			get { return album_array ?? new Album[0]; }
		}

		[XmlAttribute ("list")]
		public bool List;
	}

	[XmlRoot ("photos_get_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class PhotosResponse
	{
		[XmlElement ("photo")]
		public Photo[] photo_array;

		[XmlIgnore ()]
		public Photo[] Photos
		{
			get { return photo_array ?? new Photo[0]; }
		}

		[XmlAttribute ("list")]
		public bool List;
	}

	[XmlRoot ("photos_getTags_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class PhotoTagsResponse
	{
		[XmlElement ("photo_tag")]
		public Tag[] tag_array;

		public Tag[] Tags
		{
			get { return tag_array ?? new Tag[0]; }
		}

		[XmlAttribute ("list")]
		public bool List;
	}
	[XmlRoot ("groups_get_response", Namespace = "http://api.facebook.com/1.0/")]
	public class GroupsResponse
	{
		[XmlElement ("group")]
		public Group[] group_array;

		public Group[] Groups
		{
			get { return group_array ?? new Group[0]; }
		}

		[XmlAttribute ("list")]
		public bool List;
	}

	[XmlRoot ("groups_getMembers_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class GroupMembersResponse
	{
		[XmlElement ("members")]
		public PeopleList Members;

		[XmlElement ("admins")]
		public PeopleList Admins;

		[XmlElement ("officers")]
		public PeopleList Officers;

		[XmlElement ("not_replied")]
		public PeopleList NotReplied;
	}

	[XmlRoot ("users_getInfo_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class UserInfoResponse
	{
		[XmlElement ("user")]
		public User[] user_array;

		public User[] Users
		{
			get { return user_array ?? new User[0]; }
		}

		[XmlAttribute ("list")]
		public bool List;
	}

	[XmlRoot ("events_get_response", Namespace="http://api.facebook.com/1.0/", IsNullable=false)]
	public class EventsResponse
	{
		[XmlElement ("event")]
		public Event[] event_array;

		public Event[] Events
		{
			get { return event_array ?? new Event[0]; }
		}

		[XmlAttribute ("list")]
		public bool List;
	}

	[XmlRoot ("events_getMembers_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class EventMembersResponse
	{
		[XmlElement ("attending")]
		public PeopleList Attending;

		[XmlElement ("unsure")]
		public PeopleList Unsure;

		[XmlElement ("declined")]
		public PeopleList Declined;

		[XmlElement ("not_replied")]
		public PeopleList NotReplied;
	}

	[XmlRoot ("friends_get_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class FriendsResponse
	{
		[XmlElement ("uid")]
		public int[] uids;

		[XmlIgnore ()]
		public int[] UIds
		{
			get { return uids ?? new int[0]; }
		}
	}

	[XmlRoot ("friends_areFriends_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class AreFriendsResponse
	{
		[XmlElement ("friend_info")]
		public FriendInfo[] friend_infos;
	}
}
