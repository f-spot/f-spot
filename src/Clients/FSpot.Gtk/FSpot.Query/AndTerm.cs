//  AndTerm.cs
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
using System.Collections.Generic;
using System.Text;

using FSpot.Core;
using FSpot.Models;
using FSpot.Resources.Lang;

using Gtk;

namespace FSpot.Query
{
	public class AndTerm : Term
	{
		public static List<string> Operators { get; private set; }

		static AndTerm ()
		{
			Operators = new List<string> {
				Strings.LiteralAnd,
				Strings.CommaSpace
			};
		}

		public AndTerm (Term parent, Literal after) : base (parent, after)
		{
		}

		public override Term Invert (bool recurse)
		{
			var newme = new OrTerm (Parent, null);
			newme.CopyAndInvertSubTermsFrom (this, recurse);
			if (Parent != null)
				Parent.Remove (this);
			return newme;
		}

		public override Widget SeparatorWidget ()
		{
			Widget separator = new Label (string.Empty);
			separator.SetSizeRequest (3, 1);
			separator.Show ();
			return separator;
		}

		public override string SqlCondition ()
		{
			var condition = new StringBuilder ("(");

			condition.Append (base.SqlCondition ());

			Tag hidden = App.Instance.Database.Tags.Hidden;
			if (hidden != null)
				if (FindByTag (hidden, true).Count == 0) {
					condition.Append ($" AND id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {hidden.Id})");
				}

			condition.Append (')');

			return condition.ToString ();
		}

		public override string SQLOperator ()
		{
			return " AND ";
		}
	}
}
