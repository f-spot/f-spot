using System;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace FlickrNet
{
	/// <summary>
	/// Detailed information returned by <see cref="Flickr.PhotosGetInfo"/> method.
	/// </summary>
	[System.Serializable]
	public class PhotoInfo
	{
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public string PhotoId;

		[XmlAttribute("secret", Form=XmlSchemaForm.Unqualified)]
		public string Secret;

		[XmlAttribute("server", Form=XmlSchemaForm.Unqualified)]
		public int Server;

		[XmlIgnore()]
		public DateTime DateUploaded
		{
			get { return Utils.UnixTimestampToDate(dateuploaded); }
		}

		[XmlAttribute("dateuploaded", Form=XmlSchemaForm.Unqualified)]
		public string dateuploaded;

		[XmlAttribute("isfavorite", Form=XmlSchemaForm.Unqualified)]
		public int IsFavourite;

		[XmlAttribute("license", Form=XmlSchemaForm.Unqualified)]
		public int License;

		[XmlElement("owner", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoOwner Owner;

		[XmlElement("title", Form=XmlSchemaForm.Unqualified)]
		public string Title;

		[XmlElement("description", Form=XmlSchemaForm.Unqualified)]
		public string Description;

		[XmlElement("visibility", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoVisibility Visibility;

		[XmlElement("permissions", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoPermissions Permissions;

		[XmlElement("editability", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoEditability Editability;

		[XmlElement("comments", Form=XmlSchemaForm.Unqualified)]
		public int CommentsCount;

		[XmlElement("notes", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoNotes Notes;

		[XmlElement("tags", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoTags Tags;
	
		[XmlElement("exif", Form=XmlSchemaForm.Unqualified)]
		public ExifTag[] ExifTagCollection;

		[XmlElement("dates", Form=XmlSchemaForm.Unqualified)]
		public PhotoDates Dates;

		[XmlIgnore()]
		public string WebUrl
		{
			get { return string.Format("http://www.flickr.com/photos/{0}/{1}/", Owner.UserId, PhotoId); }
		}

		private const string photoUrl = "http://photos{0}.flickr.com/{1}_{2}{3}.jpg";

		[XmlIgnore()]
		public string SquareThumbnailUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_s"); }
		}

		[XmlIgnore()]
		public string ThumbnailUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_t"); }
		}

		[XmlIgnore()]
		public string SmallUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_m"); }
		}

		[XmlIgnore()]
		public string MediumUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, ""); }
		}

		[XmlIgnore()]
		public string LargeUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_b"); }
		}
	}

	[System.Serializable]
	public class PhotoDates
	{
		[XmlIgnore]
		public DateTime PostedDate
		{
			get { return Utils.UnixTimestampToDate(posted); }
		}

		[XmlAttribute("posted", Form=XmlSchemaForm.Unqualified)]
		public int posted;

		[XmlIgnore]
		public DateTime TakenDate
		{
			get { return DateTime.Parse(taken); }
		}

		[XmlAttribute("taken", Form=XmlSchemaForm.Unqualified)]
		public string taken;

		[XmlAttribute("takengranularity", Form=XmlSchemaForm.Unqualified)]
		public int TakenGranularity;

	}

	[System.Serializable]
	public class WhoInfo
	{
		[XmlElement("tags", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoTags Tags;
	}

	[System.Serializable]
	public class PhotoInfoOwner
	{
		[XmlAttribute("nsid", Form=XmlSchemaForm.Unqualified)]
		public string UserId;

		[XmlAttribute("username", Form=XmlSchemaForm.Unqualified)]
		public string UserName;

		[XmlAttribute("realname", Form=XmlSchemaForm.Unqualified)]
		public string RealName;

		[XmlAttribute("location", Form=XmlSchemaForm.Unqualified)]
		public string Location;
	}

	[System.Serializable]
	public class PhotoInfoVisibility
	{
		[XmlAttribute("ispublic", Form=XmlSchemaForm.Unqualified)]
		public int IsPublic;

		[XmlAttribute("isfriend", Form=XmlSchemaForm.Unqualified)]
		public int IsFriend;

		[XmlAttribute("isfamily", Form=XmlSchemaForm.Unqualified)]
		public int IsFamily;
	}

	[System.Serializable]
	public class PhotoInfoPermissions
	{
		[XmlAttribute("permcomment", Form=XmlSchemaForm.Unqualified)]
		public PermissionComment PermissionComment;

		[XmlAttribute("permaddmeta", Form=XmlSchemaForm.Unqualified)]
		public PermissionAddMeta PermissionAddMeta;
	}

	public enum PermissionComment
	{
		[XmlEnum("0")]
		Nobody = 0,
		[XmlEnum("1")]
		FriendsAndFamily = 1,
		[XmlEnum("2")]
		ContactsOnly = 2,
		[XmlEnum("3")]
		Everybody = 3
	}

	public enum PermissionAddMeta
	{
		[XmlEnum("0")]
		Owner = 0,
		[XmlEnum("1")]
		FriendsAndFamily = 1,
		[XmlEnum("2")]
		Contacts = 2,
		[XmlEnum("3")]
		Everybody = 3
	}

	[System.Serializable]
	public class PhotoInfoEditability 
	{
		[XmlAttribute("cancomment", Form=XmlSchemaForm.Unqualified)]
		public string CanComment;

		[XmlAttribute("canaddmeta", Form=XmlSchemaForm.Unqualified)]
		public string CanAddMeta;
	}

	[System.Serializable]
	public class PhotoInfoNotes
	{
		[XmlElement("note", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoNote[] NoteCollection;
	}

	[System.Serializable]
	public class PhotoInfoNote
	{
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public int NoteId;

		[XmlAttribute("author", Form=XmlSchemaForm.Unqualified)]
		public string AuthorId;

		[XmlAttribute("authorname", Form=XmlSchemaForm.Unqualified)]
		public string AuthorName;

		[XmlAttribute("x", Form=XmlSchemaForm.Unqualified)]
		public int XPosition;

		[XmlAttribute("y", Form=XmlSchemaForm.Unqualified)]
		public int YPosition;

		[XmlAttribute("w", Form=XmlSchemaForm.Unqualified)]
		public int Width;

		[XmlAttribute("h", Form=XmlSchemaForm.Unqualified)]
		public int Height;

		[XmlText()]
		public string NoteText;
	}

	[System.Serializable]
	public class PhotoInfoTags
	{
		[XmlElement("tag", Form=XmlSchemaForm.Unqualified)]
		public PhotoInfoTag[] TagCollection;
	}

	[System.Serializable]
	public class PhotoInfoTag
	{
		/// <summary>
		/// The id of the tag.
		/// </summary>
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public long TagId;

		/// <summary>
		/// The author id of the tag.
		/// </summary>
		[XmlAttribute("author", Form=XmlSchemaForm.Unqualified)]
		public string AuthorId;

		/// <summary>
		/// Author of the tag - only available if using <see cref="Flickr.TagsGetListPhoto"/>.
		/// </summary>
		[XmlAttribute("authorname", Form=XmlSchemaForm.Unqualified)]
		public string AuthorName;

		/// <summary>
		/// Raw copy of the tag, as the user entered it.
		/// </summary>
		[XmlAttribute("raw", Form=XmlSchemaForm.Unqualified)]
		public string Raw;

		/// <summary>
		/// The actually tag.
		/// </summary>
		[XmlText()]
		public string TagText;
	}

}