//
// Mono.Facebook.FriendInfo.cs:
//
// Authors:
//	Thomas Van Machelen (thomas.vanmachelen@gmail.com)
//

// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Serialization;

namespace Mono.Facebook
{
	public class FriendInfo
	{
		[XmlElement ("uid1")]
		public long UId1;

		[XmlElement ("uid2")]
		public long UId2;

		[XmlElement ("are_friends")]
		public bool AreFriends;
	}
}
