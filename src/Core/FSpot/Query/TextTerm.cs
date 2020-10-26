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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Query
{

	public class TextTerm : LogicalTerm
	{
		public string Text { get; }

		public string Field { get; }

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
			return $" {Field} LIKE %{Text}% ";
		}
	}
}
