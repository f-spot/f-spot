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
	public struct Category
	{
		public Category( string title, int id)
		{
			this.title = title;
			this.category_id = id;
		}

		private int category_id;
		public int CategoryID
		{
			get { return category_id; }
			set { category_id = value; }
		}

		private string title;
		public string Title
		{
			get { return title; }
			set { title = value; }
		}
	}
}
