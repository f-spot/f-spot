//
// FolderSet.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2009-2010 Novell, Inc.
// Copyright (C) 2009 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Hyena;


namespace FSpot.Query
{
	public class FolderSet : IQueryCondition
	{
		HashSet<SafeUri> uri_list;
		
		public FolderSet ()
		{
			uri_list = new HashSet<SafeUri> ();
		}
		
		public IEnumerable<SafeUri> Folders {
			get { return uri_list; }
			set { uri_list = (value == null) ? new HashSet<SafeUri> () : new HashSet<SafeUri> (value); }
		}
		
		protected static string EscapeQuotes (string v)
		{
			return v == null ? string.Empty : v.Replace("'", "''");
		}
		
		public string SqlClause ()
		{
			var items = new string [uri_list.Count];
			
			if (items.Length == 0)
				return null;
			
			int i = 0;
			foreach (var uri in uri_list) {
				items[i] =
					string.Format ("id IN (SELECT id FROM photos WHERE base_uri LIKE '{0}%')",
					               EscapeQuotes (uri.ToString ()));
				i++;
			}
			
			return string.Join (" OR ", items);
		}
	}
}
