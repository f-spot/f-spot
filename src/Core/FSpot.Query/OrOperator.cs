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
//  Permission is hereby granted, free of charge, to any person obtaining
//  a copy of this software and associated documentation files (the
//  "Software"), to deal in the Software without restriction, including
//  without limitation the rights to use, copy, modify, merge, publish,
//  distribute, sublicense, and/or sell copies of the Software, and to
//  permit persons to whom the Software is furnished to do so, subject to
//  the following conditions:
//
//  The above copyright notice and this permission notice shall be
//  included in all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
//  EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//  MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//  NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
//  LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
//  OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
//  WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//

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
