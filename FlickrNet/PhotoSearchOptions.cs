using System;

namespace FlickrNet
{
	/// <summary>
	/// Summary description for PhotoSearchOptions.
	/// </summary>
	public class PhotoSearchOptions
	{
		private string _userId;
		private string _tags;
		private TagMode _tagMode = TagMode.AllTags;
		private string _text;
		private DateTime _minUploadDate = DateTime.MinValue;
		private DateTime _maxUploadDate = DateTime.MinValue;
		private DateTime _minTakenDate = DateTime.MinValue;
		private DateTime _maxTakenDate = DateTime.MinValue;
		private int _license;
		private PhotoSearchExtras _extras = PhotoSearchExtras.All;
		private int _perPage = 100;
		private int _page = 1;
		private PhotoSearchSortOrder _sort = PhotoSearchSortOrder.None;

		/// <summary>
		/// The user Id of the user to search on. Defaults to null for no specific user.
		/// </summary>
		public string UserId
		{
			get { return _userId; }
			set { _userId = value; }
		}

		/// <summary>
		/// A comma delimited list of tags
		/// </summary>
		public string Tags
		{
			get { return _tags; }
			set { _tags = value; }
		}
	
		/// <summary>
		/// Tag mode can either be 'all', or 'any'. Defaults to <see cref="FlickrNet.TagMode.AllTags"/>
		/// </summary>
		public TagMode TagMode
		{
			get { return _tagMode; }
			set { _tagMode = value; }
		}
	
		/// <summary>
		/// Search for the given text in photo titles and descriptions.
		/// </summary>
		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}
	
		/// <summary>
		/// Minimum date uploaded. Defaults to <see cref="DateTime.MinValue"/> which
		/// signifies that the value is not to be used.
		/// </summary>
		public DateTime MinUploadDate
		{
			get { return _minUploadDate; }
			set { _minUploadDate = value; }
		}
	
		/// <summary>
		/// Maximum date uploaded. Defaults to <see cref="DateTime.MinValue"/> which
		/// signifies that the value is not to be used.
		/// </summary>
		public DateTime MaxUploadDate
		{
			get { return _maxUploadDate; }
			set { _maxUploadDate = value; }
		}
	
		/// <summary>
		/// Minimum date taken. Defaults to <see cref="DateTime.MinValue"/> which
		/// signifies that the value is not to be used.
		/// </summary>
		public DateTime MinTakenDate
		{
			get { return _minTakenDate; }
			set { _minTakenDate = value; }
		}
	
		/// <summary>
		/// Maximum date taken. Defaults to <see cref="DateTime.MinValue"/> which
		/// signifies that the value is not to be used.
		/// </summary>
		public DateTime MaxTakenDate
		{
			get { return _maxTakenDate; }
			set { _maxTakenDate = value; }
		}

		/// <summary>
		/// Only return licenses with the selected license number.
		/// See http://www.flickr.com/services/api/flickr.photos.licenses.getInfo.html
		/// for more details on the numbers to use.
		/// </summary>
		public int License
		{
			get { return _license; }
			set { _license = value; }
		}

		/// <summary>
		/// Optional extras to return, defaults to all. See <see cref="PhotoSearchExtras"/> for more details.
		/// </summary>
		public PhotoSearchExtras Extras
		{
			get { return _extras; }
			set { _extras = value; }
		}

		/// <summary>
		/// Number of photos to return per page. Defaults to 100.
		/// </summary>
		public int PerPage
		{
			get { return _perPage; }
			set { _perPage = value; }
		}

		/// <summary>
		/// The page to return. Defaults to page 1.
		/// </summary>
		public int Page
		{
			get { return _page; }
			set 
			{
				if( value < 1 ) throw new ArgumentOutOfRangeException("Page", value, "Must be greater than 0");
				_page = value; 
			}
		}

		/// <summary>
		/// The sort order of the returned list. Default is <see cref="PhotoSearchSortOrder.None"/>.
		/// </summary>
		public PhotoSearchSortOrder SortOrder
		{
			get { return _sort; }
			set { _sort = value; }
		}

		internal string SortOrderString
		{
			get
			{
				switch(_sort)
				{
					case PhotoSearchSortOrder.DatePostedAsc:
						return "date-posted-asc";
					case PhotoSearchSortOrder.DatePostedDesc:
						return "date-posted-desc";
					case PhotoSearchSortOrder.DateTakenAsc:
						return "date-taken-asc";
					case PhotoSearchSortOrder.DateTakenDesc:
						return "date-taken-desc";
					case PhotoSearchSortOrder.InterestingnessAsc:
						return "interestingness-asc";
					case PhotoSearchSortOrder.InterestingnessDesc:
						return "interestingness-desc";
					default:
						return null;
				}
			}
		}
	}

	/// <summary>
	/// The sort order for the PhotoSearch method.
	/// </summary>
	public enum PhotoSearchSortOrder
	{
		/// <summary>
		/// No sort order.
		/// </summary>
		None,
		/// <summary>
		/// Sort by date uploaded (posted).
		/// </summary>
		DatePostedAsc,
		/// <summary>
		/// Sort by date uploaded (posted) in descending order.
		/// </summary>
		DatePostedDesc,
		/// <summary>
		/// Sort by date taken.
		/// </summary>
		DateTakenAsc,
		/// <summary>
		/// Sort by date taken in descending order.
		/// </summary>
		DateTakenDesc,
		/// <summary>
		/// Sort by interestingness.
		/// </summary>
		InterestingnessAsc,
		/// <summary>
		/// Sort by interestingness in descending order.
		/// </summary>
		InterestingnessDesc
	}
}
