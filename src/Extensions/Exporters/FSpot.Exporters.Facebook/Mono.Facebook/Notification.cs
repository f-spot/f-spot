//
// Mono.Facebook.Notification.cs:
//
// Authors:
//	George Talusan (george@convolve.ca)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

namespace Mono.Facebook
{
	public class Notification
	{
		[XmlElement ("unread")]
		public int Unread;

		[XmlElement ("most_recent")]
		public long MostRecent;
	}

	[XmlRoot ("notifications_get_response", Namespace = "http://api.facebook.com/1.0/", IsNullable = false)]
	public class Notifications
	{
		[XmlElement ("messages")]
		public Notification Messages;

		[XmlElement ("pokes")]
		public Notification Pokes;

		[XmlElement ("shares")]
		public Notification Shares;

		[XmlElement ("friend_requests")]
		public Friend[] FriendRequests;


	}
}
