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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
			return v == null ? string.Empty : v.Replace ("'", "''");
		}

		// LIKE treats % and _ as wildcards unless the clause names an escape character.
		static string EscapeLike (string value)
		{
			return EscapeQuotes (value).Replace ("\\", "\\\\").Replace ("%", "\\%").Replace ("_", "\\_");
		}

		public string SqlClause ()
		{
			var items = new string[uri_list.Count];

			if (items.Length == 0)
				return null;

			int i = 0;
			foreach (var uri in uri_list) {
				// Folder nodes carry a trailing slash. Photos store the directory
				// without one, and a bare prefix would also match a sibling folder.
				string plain = EscapeQuotes (uri.ToString ().TrimEnd ('/'));
				string folder = EscapeLike (uri.ToString ().TrimEnd ('/'));
				items[i] = string.Format (
					"id IN (SELECT id FROM photos WHERE base_uri = '{0}' OR base_uri LIKE '{1}/%' ESCAPE '\\')",
					plain, folder);
				i++;
			}

			return string.Join (" OR ", items);
		}
	}
}
