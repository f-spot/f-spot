//  AndTerm.cs
//
//  Author:
//	 Stephen Shaw <sshaw@decriptor.com>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Query
{
	public class AndOperator : NAryOperator
	{
		public AndOperator (params LogicalTerm[] terms)
		{
			this.terms = new List<LogicalTerm> (terms.Length);
			foreach (LogicalTerm term in terms)
				Add (term);
		}

		void Add (LogicalTerm term)
		{
			var andTerm = term as AndOperator;
			if (andTerm != null) {
				foreach (LogicalTerm t in andTerm.Terms)
					Add (t);
			} else
				terms.Add (term);
		}

		public override string SqlClause ()
		{
			return SqlClause ("AND", ToStringArray ());
		}
	}
}
