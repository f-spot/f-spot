using System;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace FlickrNet
{
	/// <summary>
	/// Contains a list of active <see cref="Group"/> items.
	/// </summary>
	/// <remarks>
	/// This information is taken from FlickrLive.
	/// </remarks>
	[System.Serializable]
	public class ActiveGroups
	{
		/// <remarks/>
		[XmlElement("group", Form=XmlSchemaForm.Unqualified)]
		public Group[] GroupsCollection;
	}

	/// <summary>
	/// Provides details of a particular group.
	/// </summary>
	/// <remarks>Used by <see cref="Flickr.GroupsGetActiveList"/> and <see cref="Flickr.GroupsBrowse"/>.</remarks>
	[System.Serializable]
	public class Group
	{
		/// <summary>
		/// The id of the group.
		/// </summary>
		[XmlAttribute("nsid", Form=XmlSchemaForm.Unqualified)]
		public string GroupId;
    
		/// <summary>
		/// The name of the group
		/// </summary>
		[XmlAttribute("name", Form=XmlSchemaForm.Unqualified)]
		public string GroupName;

		/// <summary>
		/// The number of memebers of the group.
		/// </summary>
		[XmlAttribute("members", Form=XmlSchemaForm.Unqualified)]
		public long Members;
	
		/// <summary>
		/// The number of users online in FlickrLive for this group.
		/// </summary>
		[XmlAttribute("online", Form=XmlSchemaForm.Unqualified)]
		public int NumberOnline;
	
		/// <summary>
		/// The chat id of the chat room for this group.
		/// </summary>
		[XmlAttribute("chatnsid", Form=XmlSchemaForm.Unqualified)]
		public string ChatId;
	
		/// <summary>
		/// The number of users in the chat room in FlickrLive for this group.
		/// </summary>
		[XmlAttribute("inchat", Form=XmlSchemaForm.Unqualified)]
		public int NumberInChat;
	}

	/// <summary>
	/// Provides details of a particular group.
	/// </summary>
	/// <remarks>
	/// Used by the Url methods and <see cref="Flickr.GroupsGetInfo"/> method.
	/// The reason for a <see cref="Group"/> and <see cref="GroupInfo"/> are due to xml serialization
	/// incompatabilities.
	/// </remarks>
	[System.Serializable]
	public class GroupInfo
	{
		/// <remarks/>
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public string GroupId;
    
		/// <remarks/>
		[XmlAttribute("url", Form=XmlSchemaForm.Unqualified)]
		public string Url;

		/// <remarks/>
		[XmlElement("groupname", Form=XmlSchemaForm.Unqualified)]
		public string GroupName;

		/// <remarks/>
		[XmlElement("members", Form=XmlSchemaForm.Unqualified)]
		public long Members;
	
		/// <remarks/>
		[XmlElement("online", Form=XmlSchemaForm.Unqualified)]
		public int NumberOnline;
	
		/// <remarks/>
		[XmlElement("privacy", Form=XmlSchemaForm.Unqualified)]
		public PoolPrivacy Privacy;
	
		/// <remarks/>
		[XmlElement("chatnsid", Form=XmlSchemaForm.Unqualified)]
		public string ChatId;
	
		/// <remarks/>
		[XmlElement("chatcount", Form=XmlSchemaForm.Unqualified)]
		public int NumberInChat;

		/// <summary>
		/// Methods for automatically converting a <see cref="GroupInfo"/> object into
		/// and instance of a <see cref="Group"/> object.
		/// </summary>
		/// <param name="groupInfo">The incoming object.</param>
		/// <returns>The <see cref="Group"/> instance.</returns>
		public static implicit operator Group( GroupInfo groupInfo )	
		{
			Group g = new Group();
			g.GroupId = groupInfo.GroupId;
			g.GroupName = groupInfo.GroupName;
			g.Members = groupInfo.Members;
			g.NumberOnline = groupInfo.NumberOnline;
			g.ChatId = groupInfo.ChatId;
			g.NumberInChat = groupInfo.NumberInChat;

			return g;
		}

		/// <summary>
		/// Methods for allow converting a <see cref="GroupInfo"/> object into
		/// and instance of a <see cref="Group"/> object with casting. Information is lost in this cast.
		/// </summary>
		/// <example>
		/// The following code casts a <see cref="GroupInfo"/> instance to a <see cref="Group"/> instance.
		/// <code>GroupInfo groupInfo = new GroupInfo();
		/// Group group = (Group)groupInfo;</code>
		/// </example>
		/// <param name="group">The incoming object.</param>
		/// <returns>The <see cref="Group"/> instance.</returns>
		public static explicit operator GroupInfo( Group group )	
		{
			GroupInfo gi = new GroupInfo();
			gi.GroupId = group.GroupId;
			gi.GroupName = group.GroupName;
			gi.Members = group.Members;
			gi.NumberOnline = group.NumberOnline;
			gi.NumberInChat = group.NumberInChat;
			gi.ChatId = group.ChatId;

			return gi;
		}
	}

	/// <summary>
	/// A class which contains an array of <see cref="PoolInfo"/> objects.
	/// </summary>
	[System.Serializable]
	public class PoolGroups
	{
		/// <remarks/>
		[XmlElement("group", Form=XmlSchemaForm.Unqualified)]
		public PoolInfo[] GroupsCollection;
	}

	/// <summary>
	/// Information about a Flickr group.
	/// </summary>
	[System.Serializable]
	public class PoolInfo
	{
		/// <summary>The group id. Use <see cref="GroupId"/> to get the correct
		/// group id in all instances.</summary>
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public string gid;
    
		/// <summary>An alternative group id. Use <see cref="GroupId"/> to get the correct
		/// group id in all instances.</summary>
		[XmlAttribute("nsid", Form=XmlSchemaForm.Unqualified)]
		public string gid2;
    
		/// <summary>
		/// Property which returns the group id for the group.
		/// </summary>
		public string GroupId
		{
			get { return (gid==null?gid2:gid); }
		}

		/// <summary>The group name.</summary>
		[XmlAttribute("name", Form=XmlSchemaForm.Unqualified)]
		public string GroupName;

		/// <summary>
		/// Will contain 1 if you are the admin for the group, 0 if you are not.
		/// </summary>
		[XmlAttribute("admin", Form=XmlSchemaForm.Unqualified)]
		public int IsAdmin;
	
		/// <summary>
		/// The privacy settings for the group. See <see cref="PoolPrivacy"/> for more details.
		/// </summary>
		[XmlAttribute("privacy", Form=XmlSchemaForm.Unqualified)]
		public PoolPrivacy Privacy;
	
		/// <summary>
		/// Will contain 1 if the group is restricted to people who are 18 years old or over, 0 if it is not.
		/// </summary>
		[XmlAttribute("eighteenplus", Form=XmlSchemaForm.Unqualified)]
		public int EighteenPlus;
	
		/// <summary>
		/// The number of photos in the group.
		/// </summary>
		[XmlAttribute("photos", Form=XmlSchemaForm.Unqualified)]
		public long PhotoCount;
	}

	/// <summary>
	/// The various pricay settings for a group.
	/// </summary>
	[System.Serializable]
	public enum PoolPrivacy
	{
		/// <summary>
		/// The group is a private group. You cannot view pictures or posts until you are a 
		/// member. The group is also invite only.
		/// </summary>
		[XmlEnum("1")]
		Private = 1,
		/// <summary>
		/// A public group where you can see posts and photos in the group. The group is however invite only.
		/// </summary>
		[XmlEnum("2")]
		InviteOnlyPublic = 2,
		/// <summary>
		/// A public group.
		/// </summary>
		[XmlEnum("3")]
		OpenPublic = 3
	}

}
