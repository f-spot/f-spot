/*
 * Widgets/FindBar.cs
 *
 * Author(s)
 * 	Gabriel Burt  <gabriel.burt@gmail.com>
 * 	Stephane Delcroix  <stephane@delcroix.org>
 * 
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections;
using System.Text.RegularExpressions;
using Gtk;
using Gdk;
using Mono.Unix;

using FSpot.Query;

namespace FSpot.Widgets {
	public class FindBar : HBox {
		private Entry entry;
		private Label valid_label;
		private PhotoQuery query;
		private TagStore tag_store;
		FSpot.Delay update_delay;

		public string Text {
			get { return entry.Text; }
			set { entry.Text = value; }
		}

		public FindBar (PhotoQuery query, TagStore tag_store)
		{
			this.query = query;
			this.tag_store = tag_store;

			Spacing = 6;

			PackStart (new Label (Catalog.GetString ("Find:")), false, false, 0);

			entry = new Entry ();
			entry.KeyPressEvent += HandleKeyPressEvent;
			entry.TextInserted += HandleTextInserted;
			entry.TextDeleted += HandleTextDeleted;
			PackStart (entry, true, true, 0);

			valid_label = new Label (valid_str);
			valid_label.Xalign = 1.0f;
			PackStart (valid_label, false, false, 0);

			Button clear_button = new Gtk.Button ();
			clear_button.Add (new Gtk.Image ("gtk-close", Gtk.IconSize.Button));
			clear_button.Clicked += HandleCloseButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			PackStart (clear_button, false, false, 0);

			CanFocus = true;

			update_delay = new FSpot.Delay (750, new GLib.IdleHandler (Update));
			update_delay.Start ();
		}

		private void HandleCloseButtonClicked (object sender, EventArgs args)
		{
			Clear ();
		}

		protected override void OnFocusGrabbed ()
		{
			entry.GrabFocus ();
		}

		[GLib.ConnectBefore]
		private void HandleKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			args.RetVal = false;
			switch (args.Event.Key) {
			case (Gdk.Key.Escape):
				Clear ();
				args.RetVal = true;
				break;
			case (Gdk.Key.Return):
				if (completion_index != -1) {
					CompletionValidate ();
					args.RetVal = true;
				}
				break;
			case (Gdk.Key.Tab):
				DoCompletion ();
				args.RetVal = true;
				break;
			default:
				ClearCompletion ();
				break;
			}
			QueueUpdate ();
		}

        	static string valid_str = Catalog.GetString ("Valid");
	        static string invalid_str = Catalog.GetString ("Invalid");
		string old_text; //Required for TextDeleted.

		private void QueueUpdate ()
		{
			old_text = Text;
			update_delay.Start ();	
		}

		private bool Update ()
		{
			if (Text.Length == 0) {
				query.Terms = null;
				return false;
			}

			if (!ParensValid (Text)) {
				valid_label.Text = invalid_str;
				return false;
			}

			query.ExtraCondition = null;
			Term term = ParseQueryString (Text);
			if (term == null) {
				valid_label.Text = invalid_str;
				return false;
			}

			query.Terms = term;
			valid_label.Text = valid_str;

			return false;
		}

		static char [] separators = {' ', ',', '(', ')', '!', '|', '&'};
		int completion_index = -1;
		string typed_so_far, left_part, right_part, completion;
		Tag [] tag_completions;

		private void DoCompletion ()
		{
			int position = entry.Position;

			if (completion_index != -1)
				completion_index = (completion_index + 1) % tag_completions.Length;
			else {
				if (position == 0)
					return;

				int last_separator = Text.LastIndexOfAny (separators, position - 1, position) + 1;
				if (position != entry.Text.Length) {
					int next_separator =  Text.IndexOfAny (separators, last_separator) > 0 ? Text.IndexOfAny (separators, last_separator) : Text.Length ; 
					if (next_separator != position)
						return;
				}
				typed_so_far = Text.Substring (last_separator, position - last_separator); 
				if (typed_so_far == null || typed_so_far == String.Empty)
					return;

				tag_completions = tag_store.GetTagsByNameStart (typed_so_far);
				if (tag_completions == null)
					return;

				completion_index = 0;

				left_part = Text.Substring (0, position);
				right_part = Text.Substring (position);
			}

			completion = tag_completions [completion_index].Name.Substring (typed_so_far.Length);
			entry.Text = left_part + completion + " " + right_part;
			entry.SelectRegion (left_part.Length, left_part.Length + completion.Length);
		}

		private void ClearCompletion ()
		{
			typed_so_far = null;
			completion_index = -1;
			tag_completions = null;
		}

		private void CompletionValidate ()
		{
			entry.Position = left_part.Length + completion.Length + 1;
			ClearCompletion ();
		}

		int open_parens = 0, close_parens = 0;

		private void HandleTextInserted (object sender, TextInsertedArgs args)
		{
			for (int i = 0; i < args.Text.Length; i++) {
				switch (args.Text [i]) {
				case '(': open_parens ++; break;
				case ')': close_parens++; break;
				}
			}

			int pos = entry.Position + 1;
			int close_parens_needed = open_parens - close_parens;
			for (int i = 0; i < close_parens_needed; i++) {
				entry.TextInserted -= HandleTextInserted;
				entry.InsertText (")", ref pos);
				close_parens++;
				entry.TextInserted += HandleTextInserted;
				pos++;
			}

			QueueUpdate ();
		}

		private void HandleTextDeleted (object sender, TextDeletedArgs args)
		{
			int length = args.EndPos - args.StartPos;
			string txt = old_text.Substring (args.StartPos, length);

			for (int i = 0; i < txt.Length; i++) {
				if (txt [i] == '(')
					open_parens--;
				else if (txt [i] == ')')
					close_parens--;
			}

			QueueUpdate ();
		}

		private void Clear ()
		{
			ClearCompletion ();
			query.Terms = null;
			entry.Text = String.Empty;
			Hide ();
		}

		private static bool ParensValid (string text)
		{
			int open_parens = 0;
			for (int i = 0; i < text.Length; i++) {
				if (text [i] == '(')
					open_parens ++;
				if (text [i] == ')') {
					open_parens --;
					if (open_parens < 0)
						return false;
				}
			}
			return (open_parens == 0);
		}
	
		// OPS The operators we support, case insensitive
		private static string op_str = "(?'Ops' or | and |, )";

		// Match literals, eg tags or other text to search on
		private static string literal_str = "[^{0}{1}]+?";

		// Match a group surrounded by parenthesis and one or more terms separated by operators
		private static string term_str = "(((?'Open'{0})(?'Pre'[^{0}{1}]*?))+((?'Close-Open'{1})(?'Post'[^{0}{1}]*?))+)*?(?(Open)(?!))";

		// Match a simple term or a group term or a not(group term)
		//private static string comb_term_str = String.Format ("(?'Term'{0}|{2}|{1})", simple_term_str, term_str, not_term_str);
		private static string comb_term_str = String.Format ("(?'Term'{0}|{1})|not\\s*\\((?'NotTerm'{0})\\)|not\\s*(?'NotTerm'{1})", literal_str, term_str);

		// Match a single term or a set of terms separated by operators
		private static string regex_str = String.Format ("^((?'Terms'{0}){1})*(?'Terms'{0})$", comb_term_str, op_str);

		private static Regex term_regex = new Regex (String.Format (regex_str, "\\(", "\\)"),
							     RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private Term ParseQueryString (string txt)
		{
			if (txt == null)
				return null;

			txt.Trim ();
			// Strip leading/trailing parens
			if (txt.Length > 1 && txt [0] == '(' && txt [txt.Length - 1] == ')') {
				txt = txt.Remove (txt.Length - 1, 1);
				txt = txt.Remove (0, 1);
			}

			if (txt.Length == 0)
				return null;

			Match match = term_regex.Match (txt);

			if (!match.Success)
				return null;

			//Check for ambiguosities like: A and B or C
			string op = String.Empty;
			foreach (Capture capture in match.Groups ["Ops"].Captures) {
				if (op == String.Empty)
					op = capture.Value;
				else if (op != capture.Value)
					return null;
			}

			if (match.Groups ["Terms"].Captures.Count == 1 
			    && match.Groups ["NotTerm"].Captures.Count != 1) { //Single Term, should be a tag !

				string tag_name = match.Groups ["Terms"].Captures [0].Value;

				Tag tag = tag_store.GetTagByName (tag_name.Trim ());

				if (tag_store.Hidden != null && tag == tag_store.Hidden)
					query.ExtraCondition = String.Empty;

				return Term.TagTerm (tag);
			} else { //Complex Term
				System.Collections.ArrayList subterms = new System.Collections.ArrayList ();
				foreach (Capture capture in match.Groups ["Term"].Captures) {
					string subterm = capture.Value.Trim ();
					if (subterm == null || subterm.Length == 0)
						continue;

					subterms.Add ((Term)ParseQueryString (subterm));
				}
				
				switch (op.ToLower().Trim ()) {
					case "or":
						return Term.OrTerm ((Term [])subterms.ToArray (typeof (Term)));
					case "and":
						return Term.AndTerm ((Term [])subterms.ToArray (typeof (Term)));
				}


				if (match.Groups ["NotTerm"].Captures.Count == 1)
					return Term.NotTerm (ParseQueryString (match.Groups ["NotTerm"].Captures [0].Value.Trim ()));
			}
			return null;
		}
	}
}
