//
// Mono.Facebook.Responses.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//	George Talusan (george@convolve.ca)
//
// (C) Copyright 2007 Novell, Inc. (http://www.novell.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

namespace Mono.Facebook
{
	[XmlRoot ("photos_getAlbums_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class AlbumsResponse
	{
        [XmlElement ("album")]
		public Album[] album;

		[XmlIgnore ()]
		public Album[] albums
		{
			get { return album ?? new Album[0]; }
		}

		public bool list;
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

	[XmlRoot ("friends_get_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class FriendsResponse
	{
		[XmlElement ("uid")]
		public long[] uids;

		[XmlIgnore ()]
		public long[] UIds
		{
			get { return uids ?? new long[0]; }
		}
	}

	[XmlRoot ("friends_areFriends_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class AreFriendsResponse
	{
		[XmlElement ("friend_info")]
		public FriendInfo[] friend_infos;
	}
}
