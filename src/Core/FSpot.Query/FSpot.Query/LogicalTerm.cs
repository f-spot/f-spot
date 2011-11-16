//
// LogicalTerm.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
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

using System;
using System.Collections.Generic;
using Hyena;
using FSpot.Core;

namespace FSpot.Query
{
	public abstract class LogicalTerm : IQueryCondition
	{
		public abstract string SqlClause ();
	}

	public class TagTerm : LogicalTerm, IDisposable
	{
		public Tag Tag { get; private set; }

		public TagTerm (Tag tag)
		{
			Tag = tag;
		}

		public override string SqlClause ()
		{
			return SqlClause (this);
		}

		internal static string SqlClause (params TagTerm [] tags)
		{
			List<string> list = new List<string> (tags.Length);
			foreach (TagTerm tag in tags)
				list.Add (tag.Tag.Id.ToString ());
			return SqlClause (list.ToArray ());
		}

		private static string SqlClause (string [] tagids)
		{
			if (tagids.Length == 0)
				return null;
			if (tagids.Length == 1)
				return String.Format (" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0})) ", tagids[0]);
			else
				return String.Format (" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({0}))) ", String.Join (", ", tagids));
		}

		public void Dispose ()
		{
			if (Tag != null)
				Tag.Dispose ();
			System.GC.SuppressFinalize (this);
		}

		~TagTerm ()
		{
			Log.DebugFormat ("Finalizer called on {0}. Should be Disposed", GetType ());
			if (Tag != null)
				Tag.Dispose ();
		}
	}

	public class TextTerm : LogicalTerm
	{
		public string Text { get; private set; }

		public string Field { get; private set; }

		public TextTerm (string text, string field)
		{
			Text = text;
			Field = field;
		}

		public static OrTerm SearchMultiple (string text, params string[] fields)
		{
			List<TextTerm> terms = new List<TextTerm> (fields.Length);
			foreach (string field in fields)
				terms.Add (new TextTerm (text, field));
			return new OrTerm (terms.ToArray ());
		}

		public override string SqlClause ()
		{
			return String.Format (" {0} LIKE %{1}% ", Field, Text);
		}
	}

	public class NotTerm : LogicalTerm
	{
		public LogicalTerm Term { get; private set; }

		public NotTerm (LogicalTerm term)
		{
			Term = term;
		}

		public override string SqlClause ()
		{
			return String.Format (" NOT ({0}) ", Term.SqlClause ());
		}
	}

	public abstract class NAryOperator : LogicalTerm
	{
		protected List<LogicalTerm> terms;
		public LogicalTerm[] Terms {
			get { return terms.ToArray (); }
		}

		protected string [] ToStringArray ()
		{
			List<string> ls = new List<string> (terms.Count);
			foreach (LogicalTerm term in terms)
				ls.Add (term.SqlClause ());
			return ls.ToArray ();
		}

		public static string SqlClause (string op, string[] items)
		{
			if (items.Length == 1)
				return items [0];
			else
				return " (" + String.Join (String.Format (" {0} ", op), items) + ") ";
		}
		
	}

	public class OrTerm : NAryOperator
	{
		public OrTerm (params LogicalTerm[] terms)
		{
			this.terms = new List<LogicalTerm> (terms.Length);
			foreach (LogicalTerm term in terms)
				Add (term);
		}

		private void Add (LogicalTerm term)
		{
			if (term is OrTerm)
				foreach (LogicalTerm t in (term as OrTerm).terms)
					Add (t);
			else
				terms.Add (term);
		}

		public override string SqlClause ()
		{
			List<TagTerm> tagterms = new List<TagTerm> ();
			List<string> otherterms = new List<string> ();
			foreach (LogicalTerm term in terms)
				if (term is TagTerm)
					tagterms.Add (term as TagTerm);
				else
					otherterms.Add (term.SqlClause ());
			otherterms.Insert (0, TagTerm.SqlClause (tagterms.ToArray ()));
			return SqlClause ("OR", otherterms.ToArray ());
		}
	}

	public class AndTerm : NAryOperator
	{
		public AndTerm (params LogicalTerm[] terms)
		{
			this.terms = new List<LogicalTerm> (terms.Length);
			foreach (LogicalTerm term in terms)
				Add (term);
		}

		private void Add (LogicalTerm term)
		{
			if (term is AndTerm)
				foreach (LogicalTerm t in (term as AndTerm).terms)
					Add (t);
			else
				terms.Add (term);
		}

		public override string SqlClause ()
		{
			return SqlClause ("AND", ToStringArray ());
		}
	}
}
