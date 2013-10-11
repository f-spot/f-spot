//  OrTerm.cs
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
