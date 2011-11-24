//
// Term.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Stephane Delcroix <stephane@delcroix.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2007-2009 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2007-2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// This has to do with Finding photos based on tags
// http://mail.gnome.org/archives/f-spot-list/2005-November/msg00053.html
// http://bugzilla-attachments.gnome.org/attachment.cgi?id=54566
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mono.Unix;
using Gtk;
using Gdk;
using Hyena;
using FSpot.Core;

namespace FSpot
{
	public abstract class Term
	{
		private Term parent = null;
		protected bool is_negated = false;
		protected Tag tag = null;

		public Term (Term parent, Literal after)
		{
			this.parent = parent;
			SubTerms = new List<Term> ();

			if (parent != null) {
				if (after == null) {
					parent.Add (this);
				} else {
					parent.SubTerms.Insert (parent.SubTerms.IndexOf (after) + 1, this);
				}
			}
		}

		#region Properties
		public bool HasMultiple {
			get {
				return (SubTerms.Count > 1);
			}
		}

		public List<Term> SubTerms { get; private set; }

		/// <summary>
		/// Returns the last Literal in term
		/// </summary>
		/// <value>
		/// last Literal in term, else null
		/// </value>
		public Term Last {
			get {
				if (SubTerms.Count > 0) {
					return SubTerms [SubTerms.Count - 1];
				} else {
					return null;
				}
			}
		}

		public int Count {
			get {
				return SubTerms.Count;
			}
		}

		public Term Parent {
			get { return parent; }
			set {
				if (parent == value) {
					return;
				}

				// If our parent was already set, remove ourself from it
				if (parent != null) {
					parent.Remove (this);
				}

				// Add ourself to our new parent
				parent = value;
				parent.Add (this);
			}
		}

		public virtual bool IsNegated {
			get { return is_negated; }
			set {
				if (is_negated != value) {
					Invert (false);
				}

				is_negated = value;
			}
		}
		#endregion

		#region Methods
		public void Add (Term term)
		{
			SubTerms.Add (term);
		}

		public void Remove (Term term)
		{
			SubTerms.Remove (term);

			// Remove ourselves if we're now empty
			if (SubTerms.Count == 0 && Parent != null) {
				Parent.Remove (this);
			}
		}

		public void CopyAndInvertSubTermsFrom (Term term, bool recurse)
		{
			is_negated = true;
			List<Term> termsToMove = new List<Term> (term.SubTerms);

			foreach (Term subterm in termsToMove) {
				if (recurse) {
					subterm.Invert (true).Parent = this;
				} else {
					subterm.Parent = this;
				}
			}
		}

		public List<Term> FindByTag (Tag t)
		{
			return FindByTag (t, true);
		}

		public List<Term> FindByTag (Tag t, bool recursive)
		{
			List<Term> results = new List<Term> ();

			if (tag != null && tag == t) {
				results.Add (this);
			}

			if (recursive) {
				foreach (Term term in SubTerms) {
					results.AddRange (term.FindByTag (t, true));
				}
			} else {
				foreach (Term term in SubTerms) {
					foreach (Term literal in SubTerms) {
						if (literal.tag != null && literal.tag == t) {
							results.Add (literal);
						}
					}

					if (term.tag != null && term.tag == t) {
						results.Add (term);
					}
				}
			}

			return results;
		}

		public List<Term> LiteralParents ()
		{
			List<Term> results = new List<Term> ();

			bool meme = false;
			foreach (Term term in SubTerms) {
				if (term is Literal) {
					meme = true;
				}

				results.AddRange (term.LiteralParents ());
			}

			if (meme) {
				results.Add (this);
			}

			return results;
		}

		public bool TagIncluded (Tag t)
		{
			List<Term> parents = LiteralParents ();

			if (parents.Count == 0) {
				return false;
			}

			foreach (Term term in parents) {
				bool termHasTag = false;
				bool onlyTerm = true;

				foreach (Term literal in term.SubTerms) {
					if (literal.tag != null) {
						if (literal.tag == t) {
							termHasTag = true;
						} else {
							onlyTerm = false;
						}
					}
				}

				if (termHasTag && onlyTerm) {
					return true;
				}
			}

			return false;
		}

		public bool TagRequired (Tag t)
		{
			int count, grouped_with;
			return TagRequired (t, out count, out grouped_with);
		}

		public bool TagRequired (Tag t, out int num_terms, out int grouped_with)
		{
			List<Term> parents = LiteralParents ();

			num_terms = 0;
			grouped_with = 100;
			int min_grouped_with = 100;

			if (parents.Count == 0) {
				return false;
			}

			foreach (Term term in parents) {
				bool termHasTag = false;

				// Don't count it as required if it's the only subterm..though it is..
				// it is more clearly identified as Included at that point.
				if (term.Count > 1) {
					foreach (Term literal in term.SubTerms) {
						if (literal.tag != null) {
							if (literal.tag == t) {
								num_terms++;
								termHasTag = true;
								grouped_with = term.SubTerms.Count;
								break;
							}
						}
					}
				}

				if (grouped_with < min_grouped_with) {
					min_grouped_with = grouped_with;
				}

				if (!termHasTag) {
					return false;
				}
			}

			grouped_with = min_grouped_with;

			return true;
		}

		public abstract Term Invert (bool recurse);

		/// <summary>
		/// Recursively generate the SQL condition clause that this term represents.
		/// </summary>
		/// <returns>
		/// The condition string
		/// </returns>
		public virtual string SqlCondition ()
		{
			StringBuilder condition = new StringBuilder ("(");

			for (int i = 0; i < SubTerms.Count; i++) {
				Term term = SubTerms [i] as Term;
				condition.Append (term.SqlCondition ());

				if (i != SubTerms.Count - 1) {
					condition.Append (SQLOperator ());
				}
			}

			condition.Append (")");

			return condition.ToString ();
		}

		public virtual Gtk.Widget SeparatorWidget ()
		{
			return null;
		}

		public virtual string SQLOperator ()
		{
			return String.Empty;
		}

		protected static Hashtable op_term_lookup = new Hashtable ();

		public static Term TermFromOperator (string op, Term parent, Literal after)
		{
			//Console.WriteLine ("finding type for operator {0}", op);
			//op = op.Trim ();
			op = op.ToLower ();

			if (AndTerm.Operators.Contains (op)) {
				//Console.WriteLine ("AND!");
				return new AndTerm (parent, after);
			} else if (OrTerm.Operators.Contains (op)) {
				//Console.WriteLine ("OR!");
				return new OrTerm (parent, after);
			}

			Log.DebugFormat ("Do not have Term for operator {0}", op);
			return null;
		}
		#endregion
	}

	public class AndTerm : Term
	{
		public static List<string> Operators { get; private set; }

		static AndTerm ()
		{
			Operators = new List<string> ();
			Operators.Add (Catalog.GetString (" and "));
			//Operators.Add (Catalog.GetString (" && "));
			Operators.Add (Catalog.GetString (", "));
		}

		public AndTerm (Term parent, Literal after) : base (parent, after)
		{
		}

		public override Term Invert (bool recurse)
		{
			OrTerm newme = new OrTerm (Parent, null);
			newme.CopyAndInvertSubTermsFrom (this, recurse);
			if (Parent != null) {
				Parent.Remove (this);
			}
			return newme;
		}

		public override Widget SeparatorWidget ()
		{
			Widget separator = new Label (String.Empty);
			separator.SetSizeRequest (3, 1);
			separator.Show ();
			return separator;
		}

		public override string SqlCondition ()
		{
			StringBuilder condition = new StringBuilder ("(");

			condition.Append (base.SqlCondition ());

			Tag hidden = App.Instance.Database.Tags.Hidden;
			if (hidden != null) {
				if (FindByTag (hidden, true).Count == 0) {
					condition.Append (String.Format (
								" AND id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})", hidden.Id
								));
				}
			}

			condition.Append (")");

			return condition.ToString ();
		}

		public override string SQLOperator ()
		{
			return " AND ";
		}
	}

	public class OrTerm : Term
	{
		public static List<string> Operators { get; private set; }

		static OrTerm ()
		{
			Operators = new List<string> ();
			Operators.Add (Catalog.GetString (" or "));
			//Operators.Add (Catalog.GetString (" || "));
		}

		public OrTerm (Term parent, Literal after) : base (parent, after)
		{
		}

		public static OrTerm FromTags (Tag [] from_tags)
		{
			if (from_tags == null || from_tags.Length == 0) {
				return null;
			}

			OrTerm or = new OrTerm (null, null);
			foreach (Tag t in from_tags) {
				Literal l = new Literal (t);
				l.Parent = or;
			}
			return or;
		}

		private static string OR = Catalog.GetString ("or");

		public override Term Invert (bool recurse)
		{
			AndTerm newme = new AndTerm (Parent, null);
			newme.CopyAndInvertSubTermsFrom (this, recurse);
			if (Parent != null) {
				Parent.Remove (this);
			}
			return newme;
		}

		public override Gtk.Widget SeparatorWidget ()
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
