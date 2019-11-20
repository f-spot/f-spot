//  TextTerm.cs
//
//  Author:
//		 Stephen Shaw <sshaw@decriptor.com>
//       Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (c) 2013 Stephen Shaw
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

	public class TextTerm : LogicalTerm
	{
		public string Text { get; private set; }

		public string Field { get; private set; }

		public TextTerm (string text, string field)
		{
			Text = text;
			Field = field;
		}

		public static OrOperator SearchMultiple (string text, params string[] fields)
		{
			var terms = new List<TextTerm> (fields.Length);
			foreach (string field in fields)
				terms.Add (new TextTerm (text, field));
			return new OrOperator (terms.ToArray ());
		}

		public override string SqlClause ()
		{
			return string.Format (" {0} LIKE %{1}% ", Field, Text);
		}
	}
}
