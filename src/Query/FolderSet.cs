/*
 * FSpot.Query.FolderSet
 *
 * Author(s):
 *	Mike Gemuende  <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */


using System;
using System.Collections.Generic;


namespace FSpot.Query
{
	public class FolderSet : IQueryCondition
	{
		HashSet<Uri> uri_list;
		
		public FolderSet ()
		{
			uri_list = new HashSet<Uri> ();
		}
		
		public IEnumerable<Uri> Folders {
			get { return uri_list; }
			set { uri_list = (value == null) ? new HashSet<Uri> () : new HashSet<Uri> (value); }
		}
		
		protected static string EscapeQuotes (string v)
		{
			return v == null ? String.Empty : v.Replace("'", "''");
		}
		
		public string SqlClause ()
		{
			string[] items = new string [uri_list.Count];
			
			if (items.Length == 0)
				return null;
			
			int i = 0;
			foreach (Uri uri in uri_list) {
				items[i] =
					String.Format ("id IN (SELECT id FROM photos WHERE base_uri LIKE '{0}%')",
					               EscapeQuotes (uri.ToString ()));
				i++;
			}
			
			return String.Join (" OR ", items);
		}
	}
}
