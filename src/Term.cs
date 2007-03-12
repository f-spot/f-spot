/*
 * Term.cs
 *
 * Author(s)
 *   Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

//FIXME: ths SqlStatement for OrTerm could be optimized if all the subterms are 
//TagTerm by using an 'id IN ()' ...

namespace FSpot {
	public abstract class Term
	{
		public abstract string SqlStatement {
			get;
		}

		public abstract bool IsEmpty {
			get;
		}

		//AND Operator
		public static Term operator & (Term left, Term right)
		{
			return AndTerm (left, right);
		}

		//OR Operator
		public static Term operator | (Term left, Term right)
		{
			return OrTerm (left, right);
		}

		//NOT Operator
		public static Term operator ! (Term term)
		{
			return NotTerm (term);
		}

		//XOR Operator
		public static Term operator ^ (Term left, Term right)
		{
			return (left & !right) | (!left & right);
		}

		//some static helpers
		public static Term AndTerm (Term left, Term right)
		{
			if (left == null)
				return right;
			if (right == null)
				return left;
			return new _AndTerm (left, right);
		}

		public static Term AndTerm (Term [] subterms)
		{
			if (subterms == null || subterms.Length == 0)
				return null;
			return new _AndTerm (subterms);
		}

		public static Term AndTerm (Tag [] tags)
		{
			if (tags == null || tags.Length == 0)
				return null;
			return new _AndTerm (tags);
		}

		public static Term OrTerm (Term left, Term right)
		{
			if (left == null)
				return right;
			if (right == null)
				return left;
			return new _OrTerm (left, right);
		}

		public static Term OrTerm (Term [] subterms)
		{
			if (subterms == null || subterms.Length == 0)
				return null;
			return new _OrTerm (subterms);
		}

		public static Term OrTerm (Tag [] tags)
		{
			if (tags == null || tags.Length == 0)
				return null;
			return new _OrTerm (tags);
		}

		public static Term NotTerm (Term term)
		{
			if (term == null)
				return null;
			if (term is NotTerm)
				return (term as NotTerm).Term;
			return new NotTerm (term);
		}

		public static Term TagTerm (Tag tag)
		{
			if (tag == null)
				return null;
			return new TagTerm (tag);
		}
	}

	public class TagTerm : Term
	{
		Tag t;
		public Tag Tag {
			get { return t; }
			set { t = value; }
		}

		public TagTerm (Tag t)
		{
			this.t = t;
		}

		//FIXME: should handle categories !
		public override string SqlStatement {
			get {
				System.Text.StringBuilder ids = new System.Text.StringBuilder (t.Id.ToString ());

				if (t is Category) {
					System.Collections.ArrayList tags = new System.Collections.ArrayList ();
					(t as Category).AddDescendentsTo (tags);
	
					for (int i = 0; i < tags.Count; i++)
						ids.Append (", " + (tags [i] as Tag).Id.ToString ());
				}

				return System.String.Format (" photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN ({0}))", ids);
			}
		}

		public override string ToString ()
		{
			return t.Name + "(" + t.Id + ")";
		}

		public override bool IsEmpty {
			get { return (t == null); }
		}
	}

	public abstract class OperatorTerm : Term
	{
		public abstract string OperatorName
		{
			get;
		}
	}

	public abstract class NAryOperatorTerm : OperatorTerm
	{
		protected System.Collections.ArrayList subterms;
		public Term [] SubTerms
		{
			get { return (Term []) subterms.ToArray (typeof (Term)); }
		}
	
		public NAryOperatorTerm (Term left, Term right) : this (new Term [] {left, right}) 
		{
		}

		public NAryOperatorTerm (Tag [] tags)
		{
			this.subterms = new System.Collections.ArrayList ();
			if (tags == null)
				return;

			foreach (Tag tag in tags)
				AddTerm (tag);
		}

		public NAryOperatorTerm (Term [] subterms)
		{
			this.subterms = new System.Collections.ArrayList ();
			if (subterms == null) {
				return;
			}
			foreach (Term term in subterms) {
				AddTerm (term);
			}
		}

		private void AddTerm (Term term)
		{
			if (term == null)
				return;
			if (this.GetType () == term.GetType ()) 
				foreach (Term t in (term as NAryOperatorTerm).SubTerms)
					AddTerm (t);
			else
				subterms.Add (term);
		}

		public override string SqlStatement {
			get {
				if (IsEmpty)
					return System.String.Empty;
	
				System.Text.StringBuilder sb = new System.Text.StringBuilder ("(");
				for (int i = 0; i < subterms.Count; i++) {
					sb.Append ((subterms [i] as Term).SqlStatement);
					if (i != subterms.Count - 1)
						sb.Append (OperatorName);
				}
				sb.Append (")");
				return sb.ToString ();
				
			}
		}

		public override string ToString ()
		{
			if (IsEmpty)
				return System.String.Empty;

			System.Text.StringBuilder sb = new System.Text.StringBuilder ("(");
			for (int i = 0; i < subterms.Count; i++) {
				sb.Append ((subterms [i] as Term).ToString ());
				if (i != subterms.Count - 1)
					sb.Append (OperatorName);
			}
			sb.Append (")");
			return sb.ToString ();
		}

		public override bool IsEmpty {
			get { return (subterms.Count == 0); }
		}
	}

	//FIXME: rename this AndTerm once the old AndTerm definition is removed
	public class _AndTerm : NAryOperatorTerm
	{
		public _AndTerm (Term left, Term right) : base (left, right)
		{	
		}

		public _AndTerm (Term [] subterms) : base (subterms)
		{
		}

		public _AndTerm (Tag [] tags) : base (tags)
		{
		}

		public override string OperatorName {
			get { return " AND ";}
		}
	}

	//FIXME: rename this OrTerm once the old OrTerm definition is removed
	public class _OrTerm : NAryOperatorTerm
	{
		public _OrTerm (Term left, Term right) : base (left, right)
		{	
		}

		public override string OperatorName {
			get { return " OR ";}
		}

		public _OrTerm (Term [] subterms) : base (subterms)
		{
		}

		public _OrTerm (Tag [] tags) : base (tags)
		{
		}
	}

	public class NotTerm : OperatorTerm
	{
		Term term;
		public Term Term {
			get { return term; }
		}

		public NotTerm (Term term)
		{
			this.term = term;
		}

		public override string OperatorName {
			get { return " NOT "; }
		}

		public override string SqlStatement {
			get {
				if (IsEmpty)
					return System.String.Empty;
				return OperatorName + "(" + term.SqlStatement + ")";
			}
		}

		public override string ToString ()
		{
			if (IsEmpty)
				return System.String.Empty;
			return OperatorName + "(" + term.ToString () + ")";
		}

		public override bool IsEmpty {
			get { return (term == null); }
		}
	}
}
