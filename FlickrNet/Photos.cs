using System;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Collections;

namespace FlickrNet
{
	/// <summary>
	/// Which photo search extras to be included. Can be combined to include more than one
	/// value.
	/// </summary>
	/// <example>
	/// The following code sets options to return both the license and owner name along with
	/// the other search results.
	/// <code>
	/// PhotoSearchOptions options = new PhotoSearchOptions();
	/// options.Extras = PhotoSearchExtras.License &amp; PhotoSearchExtras.OwnerName
	/// </code>
	/// </example>
	[Flags]
	public enum PhotoSearchExtras
	{
		None = 0,
		License = 1,
		DateUploaded = 2,
		DateTaken = 4,
		OwnerName = 8,
		IconServer = 16,
		OriginalFormat = 32,
		All = License | DateUploaded | DateTaken | OwnerName | IconServer | OriginalFormat
	}

	/// <remarks/>
	[Serializable]
	public class Photos 
	{
    
		/// <remarks/>
		[XmlElement("photo", Form=XmlSchemaForm.Unqualified)]
		public PhotoCollection PhotoCollection = new PhotoCollection();
    
		/// <remarks/>
		[XmlAttribute("page", Form=XmlSchemaForm.Unqualified)]
		public long PageNumber;
    
		/// <remarks/>
		[XmlAttribute("pages", Form=XmlSchemaForm.Unqualified)]
		public long TotalPages;
    
		/// <remarks/>
		[XmlAttribute("perpage", Form=XmlSchemaForm.Unqualified)]
		public long PhotosPerPage;
    
		/// <remarks/>
		[XmlAttribute("total", Form=XmlSchemaForm.Unqualified)]
		public long TotalPhotos;
	}

	/// <remarks/>
	[System.Serializable]
	public class Photo 
	{
    
		/// <remarks/>
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public string PhotoId;
    
		/// <remarks/>
		[XmlAttribute("owner", Form=XmlSchemaForm.Unqualified)]
		public string UserId;
    
		/// <remarks/>
		[XmlAttribute("secret", Form=XmlSchemaForm.Unqualified)]
		public string Secret;
    
		/// <remarks/>
		[XmlAttribute("server", Form=XmlSchemaForm.Unqualified)]
		public string Server;
    
		/// <remarks/>
		[XmlAttribute("title", Form=XmlSchemaForm.Unqualified)]
		public string Title;
    
		/// <remarks/>
		[XmlAttribute("ispublic", Form=XmlSchemaForm.Unqualified)]
		public int IsPublic;
    
		/// <remarks/>
		[XmlAttribute("isfriend", Form=XmlSchemaForm.Unqualified)]
		public int IsFriend;
    
		/// <remarks/>
		[XmlAttribute("isfamily", Form=XmlSchemaForm.Unqualified)]
		public int IsFamily;

		/// <remarks/>
		[XmlAttribute("isprimary", Form=XmlSchemaForm.Unqualified)]
		public int IsPrimary;

		/// <remarks/>
		[XmlAttribute("license", Form=XmlSchemaForm.Unqualified)]
		public string License;

		/// <remarks/>
		[XmlAttribute("dateupload", Form=XmlSchemaForm.Unqualified)]
		public string dateupload_raw;

		/// <summary>
		/// Converts the raw dateupload field to a <see cref="DateTime"/>.
		/// </summary>
		[XmlIgnore]
		public DateTime DateUploaded
		{
			get { return Utils.UnixTimestampToDate(dateupload_raw); }
		}

		/// <remarks/>
		[XmlAttribute("datetaken", Form=XmlSchemaForm.Unqualified)]
		public string datetaken_raw;

		/// <summary>
		/// Converts the raw datetaken field to a <see cref="DateTime"/>.
		/// </summary>
		[XmlIgnore]
		public DateTime DateTaken
		{
			get { return Utils.UnixTimestampToDate(datetaken_raw); }
		}

		/// <remarks/>
		[XmlAttribute("ownername", Form=XmlSchemaForm.Unqualified)]
		public string OwnerName;

		/// <remarks/>
		[XmlAttribute("iconserver", Form=XmlSchemaForm.Unqualified)]
		public string IconServer;

		/// <summary>
		/// Optional extra field containing the original format (jpg, png etc) of the 
		/// photo.
		/// </summary>
		[XmlAttribute("originalformat", Form=XmlSchemaForm.Unqualified)]
		public string OriginalFormat;

		private const string photoUrl = "http://static.flickr.com/{0}/{1}_{2}{3}.{4}";

		/// <summary>
		/// The url to the web page for this photo. Uses the users userId, not their web alias, but
		/// will still work.
		/// </summary>
		[XmlIgnore()]
		public string WebUrl
		{
			get { return string.Format("http://www.flickr.com/photos/{0}/{1}/", UserId, PhotoId); }
		}

		/// <summary>
		/// The URL for the square thumbnail of a photo.
		/// </summary>
		[XmlIgnore()]
		public string SquareThumbnailUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_s", "jpg"); }
		}

		/// <summary>
		/// The URL for the thumbnail of a photo.
		/// </summary>
		[XmlIgnore()]
		public string ThumbnailUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_t", "jpg"); }
		}

		/// <summary>
		/// The URL for the small copy of a photo.
		/// </summary>
		[XmlIgnore()]
		public string SmallUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_m", "jpg"); }
		}

		/// <summary>
		/// The URL for the medium copy of a photo.
		/// </summary>
		/// <remarks>There is a chance that extremely small images will not have a medium copy.
		/// Use <see cref="Flickr.PhotosGetSizes"/> to get the available URLs for a photo.</remarks>
		[XmlIgnore()]
		public string MediumUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "", "jpg"); }
		}

		/// <summary>
		/// The URL for the large copy of a photo.
		/// </summary>
		/// <remarks>There is a chance that small images will not have a large copy.
		/// Use <see cref="Flickr.PhotosGetSizes"/> to get the available URLs for a photo.</remarks>
		[XmlIgnore()]
		public string LargeUrl
		{
			get { return string.Format(photoUrl, Server, PhotoId, Secret, "_b", "jpg"); }
		}

		/// <summary>
		/// If <see cref="OriginalFormat"/> was returned then this will contain the url of the original file.
		/// </summary>
		[XmlIgnore()]
		public string OriginalUrl
		{
			get 
			{ 
				if( OriginalFormat == null || OriginalFormat.Length == 0 )
					throw new ArgumentNullException("OriginalUrl", "No original format information available.");

				return string.Format(photoUrl, Server, PhotoId, Secret, "_o", OriginalFormat);
			}
		}
	}

	[System.Serializable]
	public class PhotoPermissions
	{
		/// <remarks/>
		[XmlAttribute("id", Form=XmlSchemaForm.Unqualified)]
		public string PhotoId;

		/// <remarks/>
		[XmlAttribute("ispublic", Form=XmlSchemaForm.Unqualified)]
		public int IsPublic;
    
		/// <remarks/>
		[XmlAttribute("isfriend", Form=XmlSchemaForm.Unqualified)]
		public int IsFriend;
    
		/// <remarks/>
		[XmlAttribute("isfamily", Form=XmlSchemaForm.Unqualified)]
		public int IsFamily;

		/// <remarks/>
		[XmlAttribute("permcomment", Form=XmlSchemaForm.Unqualified)]
		public PermissionComment PermissionComment;

		/// <remarks/>
		[XmlAttribute("permaddmeta", Form=XmlSchemaForm.Unqualified)]
		public PermissionAddMeta PermissionAddMeta;
	}

	[System.Serializable]
	public class PhotoCollection : CollectionBase
	{
	
		public PhotoCollection()
		{
		}

		public PhotoCollection(Photo[] photos)
		{
			for (int i=0; i<photos.Length; i++)
			{
				List.Add(photos[i]);
			}
		}

		public int Length
		{
			get { return List.Count; }
		}

		#region ICollection Members

		public bool IsSynchronized
		{
			get
			{
				return List.IsSynchronized;
			}
		}

		public void CopyTo(Array array, int index)
		{
			List.CopyTo(array, index);
		}

		public object SyncRoot
		{
			get
			{
				return List.SyncRoot;
			}
		}

		#endregion
	
		#region IList Members

		public bool IsReadOnly
		{
			get
			{
				return List.IsReadOnly;
			}
		}

		public Photo this[int index]
		{
			get
			{
				return (Photo)List[index];
			}
			set
			{
				List[index] = value;
			}
		}


		public void Insert(int index, Photo value)
		{
			List.Insert(index, value);
		}

		public void Remove(Photo value)
		{
			List.Remove(value);
		}

		public bool Contains(Photo value)
		{
			return List.Contains(value);
		}


		public int IndexOf(object value)
		{
			return List.IndexOf(value);
		}

		public int Add(Photo value)
		{
			return List.Add(value);
		}

		public void AddRange(Photo[] array)
		{
			foreach(Photo photo in array)
				List.Add(photo);
		}

		public void AddRange(PhotoCollection collection)
		{
			foreach(Photo photo in collection)
				List.Add(photo);
		}

		public bool IsFixedSize
		{
			get
			{
				return List.IsFixedSize;
			}
		}

		#endregion

		public static implicit operator Photo[](PhotoCollection collection)
		{
			Photo[] photos = new Photo[collection.Count];
			collection.CopyTo(photos, 0);
			return photos;
		}

		public static implicit operator PhotoCollection(Photo[] photos)
		{
			return new PhotoCollection(photos);
		}
	}

}