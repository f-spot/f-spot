using System.Xml.Serialization;
using System.Xml.Schema;

namespace FlickrNet
{
	/// <summary>
	/// Obsolete.
	/// </summary>
	[System.Serializable]
	public class Online
	{
		/// <remarks/>
		[XmlElement("user", Form=XmlSchemaForm.Unqualified)]
		public OnlineUser[] UserCollection;
	}

	/// <summary>
	/// Obsolete.
	/// </summary>
	[System.Serializable]
	public class OnlineUser
	{
		/// <remarks/>
		[XmlAttribute("nsid", Form=XmlSchemaForm.Unqualified)]
		public string UserId;
    
		/// <remarks/>
		[XmlAttribute("username", Form=XmlSchemaForm.Unqualified)]
		public string UserName;
    
		/// <remarks/>
		[XmlAttribute("online", Form=XmlSchemaForm.Unqualified)]
		public OnlineStatus IsOnline;

		/// <remarks/>
		[XmlText()]
		public string AwayDescription;
	}

	/// <summary>
	/// Obsolete.
	/// </summary>
	[System.Serializable]
	public enum OnlineStatus
	{
		/// <summary>
		/// Obsolete.
		/// </summary>
		[XmlEnum("1")]
		Away = 1,
		/// <summary>
		/// Obsolete
		/// </summary>
		[XmlEnum("2")]
		Online = 2
	}

}