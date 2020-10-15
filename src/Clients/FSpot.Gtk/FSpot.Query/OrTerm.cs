// OrTerm.cs
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
using System.Linq;

using Mono.Unix;

using Gtk;

using FSpot.Core;

namespace FSpot.Query
{
	public class OrTerm : Term
	{
		public static List<string> Operators { get; private set; }

		static OrTerm ()
		{
			Operators = new List<string> ();
			Operators.Add (Catalog.GetString (" or "));
		}

		public OrTerm (Term parent, Literal after) : base (parent, after)
		{
		}

		public static OrTerm FromTags (Tag [] fromTags)
		{
			if (fromTags == null || fromTags.Length == 0)
				return null;

			var or = new OrTerm (null, null);
			foreach (Literal l in fromTags.Select(t => new Literal (t)))
			{
			    l.Parent = or;
			}
			return or;
		}

		static readonly string OR = Catalog.GetString ("or");

		public override Term Invert (bool recurse)
		{
			var newme = new AndTerm (Parent, null);
			newme.CopyAndInvertSubTermsFrom (this, recurse);
			if (Parent != null)
				Parent.Remove (this);
			return newme;
		}

		public override Widget SeparatorWidget ()
		{
			Widget label = new Label (" " + OR + " ");
			label.Show ();
			return label;
		}

		public override string SQLOperator ()
		{
			return " OR ";
		}
	}
}
