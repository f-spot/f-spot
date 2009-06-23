/*
 * FSpot.Query.FolderSet
 *
 * Author(s):
 *	Mike Gemuende  <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */


using System;
using System.Collections;


namespace FSpot.Query
{
	
	
	public class FolderSet : IQueryCondition
	{
		/* we use ArrayList, because UriList is located in the f-spot.exe
		 * assembly, which is (not yet) available here
		 */
		ArrayList uri_list;
		
		public FolderSet ()
		{
			uri_list = new ArrayList ();
		}
		
		private bool AddFolderInternal (Uri uri)
		{
			if (uri_list.Contains (uri))
				return false;
			
			uri_list.Add (uri);
			
			return true;
		}
		
		public ArrayList UriList {
			get { return uri_list; }
			set {
				uri_list = new ArrayList ();
				
				if (value == null)
					return;
				
				foreach (Uri uri in value)
					AddFolderInternal (uri);
			}
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
