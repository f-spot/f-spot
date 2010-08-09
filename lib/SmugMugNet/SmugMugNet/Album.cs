using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Specialized;
using Hyena;

namespace SmugMugNet
{
	public struct Album
	{
		public Album(string title, int id)
		{
			this.album_id = id;
			this.title = title;
		}

		private int album_id;
		public int AlbumID
		{
			get { return album_id; }
			set { album_id = value; }
		}

		private string title;
		public string Title
		{
			get { return title; }
			set { title = value; }
		}
	}
}
