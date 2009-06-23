/*
 * FSpot.Query.LogicalTerm
 *
 * Author(s):
 *	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections.Generic;
using FSpot.Utils;

namespace FSpot.Query
{
	public abstract class LogicalTerm : IQueryCondition
	{
		public abstract string SqlClause ();
	}

	public class TagTerm : LogicalTerm, IDisposable
	{
		Tag tag;
		public Tag Tag {
			get { return tag; }
		}

		public TagTerm (Tag tag)
		{
			this.tag = tag;
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
			if (tag != null)
				tag.Dispose ();
			System.GC.SuppressFinalize (this);
		}

		~TagTerm ()
		{
			Log.Debug ("Finalizer called on {0}. Should be Disposed", GetType ());
			if (tag != null)
				tag.Dispose ();
		}
	}

	public class TextTerm : LogicalTerm
	{
		string text;
		public string Text {
			get { return text; }
		}

		string field;
		public string Field {
			get { return field;  }
		}

		public TextTerm (string text, string field)
		{
			this.text = text;
			this.field = field;
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
			return String.Format (" {0} LIKE %{1}% ", field, text);
		}
	}

	public class NotTerm : LogicalTerm
	{
		LogicalTerm term;
		public LogicalTerm Term {
			get { return term; }
		}

		public NotTerm (LogicalTerm term)
		{
			this.term = term;
		}

		public override string SqlClause ()
		{
			return String.Format (" NOT ({0}) ", term.SqlClause ());
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
