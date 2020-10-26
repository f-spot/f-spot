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
		HashSet<SafeUri> uriList;

		public FolderSet ()
		{
			uriList = new HashSet<SafeUri> ();
		}

		public IEnumerable<SafeUri> Folders {
			get => uriList;
			set { uriList = (value == null) ? new HashSet<SafeUri> () : new HashSet<SafeUri> (value); }
		}

		protected static string EscapeQuotes (string v)
		{
			return v == null ? string.Empty : v.Replace ("'", "''");
		}

		public string SqlClause ()
		{
			var items = new string[uriList.Count];

			if (items.Length == 0)
				return null;

			int i = 0;
			foreach (var uri in uriList) {
				items[i] = $"id IN (SELECT id FROM photos WHERE base_uri LIKE '{EscapeQuotes (uri.ToString ())}%')";
				i++;
			}

			return string.Join (" OR ", items);
		}
	}
}
