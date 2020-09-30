//  TextLiteral.cs
//
//  Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2007-2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// This has to do with Finding photos based on tags
// http://mail.gnome.org/archives/f-spot-list/2005-November/msg00053.html
// http://bugzilla-attachments.gnome.org/attachment.cgi?id=54566
namespace FSpot.Query
{
	public class TextLiteral : AbstractLiteral
	{
		readonly string text;

		public TextLiteral (Term parent, string text) : base (parent, null)
		{
			this.text = text;
		}

		public override string SqlCondition ()
		{
			return $"id {(IsNegated ? "NOT " : "")}IN (SELECT id FROM photos WHERE base_uri LIKE '%{EscapeQuotes (text)}%' OR filename LIKE '%{EscapeQuotes (text)}%' OR description LIKE '%{EscapeQuotes (text)}%')";
		}

		protected static string EscapeQuotes (string v)
		{
			return v == null ? string.Empty : v.Replace ("'", "''");
		}
	}
}
