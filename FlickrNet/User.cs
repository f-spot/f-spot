using System.Xml.Serialization;
using System.Xml.Schema;

namespace FlickrNet
{
	/// <summary>
	/// Contains details of a user
	/// </summary>
	[System.Serializable]
	public class User
	{
		private string _userId;
		private string _url;
		private string _username;

		/// <summary>
		/// The id of the user object.
		/// </summary>
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public string UserId
		{
			get { return _userId; }
			set { _userId = value; }
		}

		/// <summary>
		/// The url for the user. Only available for objects returned 
		/// by <see cref="Flickr.UrlsGetUserPhotos"/> and <see cref="Flickr.UrlsGetUserProfile"/>.
		/// </summary>
		[XmlAttribute("url", Form=XmlSchemaForm.Unqualified)]
		public string Url
		{
			get { return _url; }
			set { _url = value; }
		}

		/// <summary>
		/// The Username of the selected user.
		/// Not available when returned by <see cref="Flickr.UrlsGetUserPhotos"/> and <see cref="Flickr.UrlsGetUserProfile"/>.
		/// </summary>
		[XmlElement("username")]
		public string UserName
		{
			get { return _username; }
			set { _username = value; }
		}
	}
}