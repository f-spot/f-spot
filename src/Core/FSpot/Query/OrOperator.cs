//  OrTerm.cs
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

using System.Collections.Generic;

namespace FSpot.Query
{

	public class OrOperator : NAryOperator
	{
		public OrOperator (params LogicalTerm[] terms)
		{
			this.terms = new List<LogicalTerm> (terms.Length);
			foreach (LogicalTerm term in terms)
				Add (term);
		}

		void Add (LogicalTerm term)
		{
			var orTerm = term as OrOperator;
			if (orTerm != null) {
				foreach (LogicalTerm t in orTerm.terms)
					Add (t);
			}
			else
				terms.Add (term);
		}

		public override string SqlClause ()
		{
			var tagterms = new List<TagTerm> ();
			var otherterms = new List<string> ();

			foreach (LogicalTerm term in terms) {
				var tagTerm = term as TagTerm;
				if (tagTerm != null)
					tagterms.Add (tagTerm);
				else
					otherterms.Add (term.SqlClause ());
			}

			otherterms.Insert (0, TagTerm.SqlClause (tagterms.ToArray ()));
			return SqlClause ("OR", otherterms.ToArray ());
		}
	}
	
}
