//
// Term.cs
//
// Author:
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

using Hyena;

namespace FSpot.Query
{
	public abstract class Term
	{
		Term parent;
		protected bool isNegated { get; set; }
		protected Tag tag { get; set; }

		protected Term (Term parent, Literal after)
		{
			this.parent = parent;
			SubTerms = new List<Term> ();

			if (parent != null)
				if (after == null) {
					parent.Add (this);
				} else {
					parent.SubTerms.Insert (parent.SubTerms.IndexOf (after) + 1, this);
				}
		}

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
				return SubTerms.Count > 0 ? SubTerms[SubTerms.Count - 1] : null;
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
				if (parent == value)
					return;

				// If our parent was already set, remove ourself from it
				if (parent != null)
					parent.Remove (this);

				// Add ourself to our new parent
				parent = value;
				parent.Add (this);
			}
		}

		public virtual bool IsNegated {
			get { return isNegated; }
			set {
				if (isNegated != value)
					Invert (false);

				isNegated = value;
			}
		}

		public void Add (Term term)
		{
			SubTerms.Add (term);
		}

		public void Remove (Term term)
		{
			SubTerms.Remove (term);

			// Remove ourselves if we're now empty
			if (SubTerms.Count == 0 && Parent != null)
				Parent.Remove (this);
		}

		public void CopyAndInvertSubTermsFrom (Term term, bool recurse)
		{
			isNegated = true;
			var termsToMove = new List<Term> (term.SubTerms);

			foreach (Term subterm in termsToMove) {
				if (recurse)
					subterm.Invert (true).Parent = this;
				else
					subterm.Parent = this;
			}
		}

		public List<Term> FindByTag (Tag t)
		{
			return FindByTag (t, true);
		}

		public List<Term> FindByTag (Tag t, bool recursive)
		{
			var results = new List<Term> ();

			if (tag != null && tag == t)
				results.Add (this);

			if (recursive)
				foreach (Term term in SubTerms) {
					results.AddRange (term.FindByTag (t, true));
				}
			else
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

			return results;
		}

		public List<Term> LiteralParents ()
		{
			var results = new List<Term> ();

			bool meme = false;
			foreach (Term term in SubTerms) {
				if (term is Literal)
					meme = true;

				results.AddRange (term.LiteralParents ());
			}

			if (meme)
				results.Add (this);

			return results;
		}

		public bool TagIncluded (Tag t)
		{
			List<Term> parents = LiteralParents ();

			if (parents.Count == 0)
				return false;

			foreach (Term term in parents) {
				bool termHasTag = false;
				bool onlyTerm = true;

				foreach (Term literal in term.SubTerms) {
					if (literal.tag != null)
						if (literal.tag == t) {
							termHasTag = true;
						} else {
							onlyTerm = false;
						}
				}

				if (termHasTag && onlyTerm)
					return true;
			}

			return false;
		}

		public bool TagRequired (Tag t)
		{
			return TagRequired (t, out _, out _);
		}

		public bool TagRequired (Tag t, out int numTerms, out int groupedWith)
		{
			var parents = LiteralParents ();

			numTerms = 0;
			groupedWith = 100;
			int min_grouped_with = 100;

			if (parents.Count == 0)
				return false;

			foreach (Term term in parents) {
				bool termHasTag = false;

				// Don't count it as required if it's the only subterm..though it is..
				// it is more clearly identified as Included at that point.
				if (term.Count > 1)
					foreach (Term literal in term.SubTerms) {
						if (literal.tag != null) {
							if (literal.tag == t) {
								numTerms++;
								termHasTag = true;
								groupedWith = term.SubTerms.Count;
								break;
							}
						}
					}

				if (groupedWith < min_grouped_with)
					min_grouped_with = groupedWith;

				if (!termHasTag)
					return false;
			}

			groupedWith = min_grouped_with;

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
			var condition = new StringBuilder ("(");

			for (int i = 0; i < SubTerms.Count; i++) {
				Term term = SubTerms[i];
				condition.Append (term.SqlCondition ());

				if (i != SubTerms.Count - 1)
					condition.Append (SQLOperator ());
			}

			condition.Append (')');

			return condition.ToString ();
		}

		public virtual Gtk.Widget SeparatorWidget ()
		{
			return null;
		}

		public virtual string SQLOperator ()
		{
			return string.Empty;
		}

		public static Term TermFromOperator (string op, Term parent, Literal after)
		{
			//Console.WriteLine ("finding type for operator {0}", op);
			//op = op.Trim ();
			op = op.ToLower ();

			if (AndTerm.Operators.Contains (op))
				return new AndTerm (parent, after);

			if (OrTerm.Operators.Contains (op))
				return new OrTerm (parent, after);

			Log.Debug ($"Do not have Term for operator {op}");
			return null;
		}
	}
}
