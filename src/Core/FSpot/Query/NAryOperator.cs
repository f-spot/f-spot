//  NAryOperator.cs
//
//  Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace FSpot.Query
{
	public abstract class NAryOperator : LogicalTerm
	{
		protected List<LogicalTerm> terms { get; set; }

		public LogicalTerm[] Terms => terms.ToArray ();

		protected string[] ToStringArray ()
		{
			var ls = new List<string> (terms.Count);
			foreach (LogicalTerm term in terms)
				ls.Add (term.SqlClause ());
			return ls.ToArray ();
		}

		public static string SqlClause (string op, string[] items)
		{
			if (items.Length == 1)
				return items[0];

			return " (" + string.Join ($" {op} ", items) + ") ";
		}
	}
}
