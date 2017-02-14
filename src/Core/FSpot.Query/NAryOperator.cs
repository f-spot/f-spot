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

using System;
using System.Collections.Generic;

namespace FSpot.Query
{
	public abstract class NAryOperator : LogicalTerm
	{
		protected List<LogicalTerm> terms;
		public LogicalTerm[] Terms {
			get { return terms.ToArray (); }
		}

		protected string [] ToStringArray ()
		{
			var ls = new List<string> (terms.Count);
			foreach (LogicalTerm term in terms)
				ls.Add (term.SqlClause ());
			return ls.ToArray ();
		}

		public static string SqlClause (string op, string[] items)
		{
			if (items.Length == 1)
				return items [0];
			return " (" + string.Join (string.Format (" {0} ", op), items) + ") ";
		}
	}
}
