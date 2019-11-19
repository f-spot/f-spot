//
// FindBar.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephane Delcroix <sdelcroix@src.gnome.org>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2007 Gabriel Burt
// Copyright (C) 2010 Daniel Köb
// Copyright (C) 2007-2008 Stephane Delcroix
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
using System.Text.RegularExpressions;

using Gtk;

using Mono.Unix;

using FSpot.Core;
using FSpot.Query;

using Hyena;

namespace FSpot.Widgets
{
	public class FindBar : HighlightedBox
	{
		Entry entry;
		string last_entry_text = string.Empty;
		int open_parens;
		int close_parens;
		PhotoQuery query;
		Term root_term;
		HBox box;
		readonly object lockObject = new object ();

		#region Properties
		public bool Completing {
			get {
				return (entry.Completion as LogicEntryCompletion).Completing;
			}
		}

		public Entry Entry {
			get { return entry; }
		}

		public Term RootTerm  {
			get { return root_term; }
		}

		public FindBar (PhotoQuery query, TreeModel model) : base(new HBox())
		{
			this.query = query;
			box = Child as HBox;

			box.Spacing = 6;
			box.BorderWidth = 2;

			box.PackStart (new Label (Catalog.GetString ("Find:")), false, false, 0);

			entry = new Entry ();
			entry.Completion = new LogicEntryCompletion (entry, model);

			entry.TextInserted  += HandleEntryTextInserted;
			entry.TextDeleted   += HandleEntryTextDeleted;
			entry.KeyPressEvent += HandleEntryKeyPress;

			box.PackStart (entry, true, true, 0);

			var clear_button = new Button ();
			clear_button.Add (new Image ("gtk-close", IconSize.Button));
			clear_button.Clicked += HandleCloseButtonClicked;
			clear_button.Relief = ReliefStyle.None;
			box.PackStart (clear_button, false, false, 0);
		}
		#endregion

		#region Event Handlers

		void HandleCloseButtonClicked (object sender, EventArgs args)
		{
			Clear ();
		}

		void HandleEntryTextInserted (object sender, TextInsertedArgs args)
		{
			//Log.DebugFormat ("inserting {0}, ( = {1}  ) = {2}", args.Text, open_parens, close_parens);

			//int start = args.Position - args.Length;

			for (int i = 0; i < args.Text.Length; i++) {
				char c = args.Text [i];
				if (c == '(')
					open_parens++;
				else if (c == ')')
					close_parens++;
			}

			int pos = entry.Position + 1;
			int close_parens_needed = open_parens - close_parens;
			for (int i = 0; i < close_parens_needed; i++) {
				entry.TextInserted -= HandleEntryTextInserted;
				entry.InsertText (")", ref pos);
				close_parens++;
				entry.TextInserted += HandleEntryTextInserted;
				pos++;
			}
			//Log.DebugFormat ("done w/ insert, {0}, ( = {1}  ) = {2}", args.Text, open_parens, close_parens);
			last_entry_text = entry.Text;

			QueueUpdate ();
		}

		void HandleEntryTextDeleted (object sender, TextDeletedArgs args)
		{
			int length = args.EndPos - args.StartPos;
			//Log.DebugFormat ("start {0} end {1} len {2} last {3}", args.StartPos, args.EndPos, length, last_entry_text);
			string txt = length < 0 ? last_entry_text : last_entry_text.Substring (args.StartPos, length);

			for (int i = 0; i < txt.Length; i++) {
				if (txt [i] == '(')
					open_parens--;
				else if (txt [i] == ')')
					close_parens--;
			}

			last_entry_text = entry.Text;

			QueueUpdate ();
		}

		void HandleEntryKeyPress (object sender, KeyPressEventArgs args)
		{
			//bool shift = ModifierType.ShiftMask == (args.Event.State & ModifierType.ShiftMask);

			switch (args.Event.Key) {
			case (Gdk.Key.Escape):
				Clear ();
				args.RetVal = true;
				break;

			case (Gdk.Key.Tab):
				// If we are at the end of the entry box, let the normal Tab handler do its job
				if (entry.Position == entry.Text.Length) {
					args.RetVal = false;
					return;
				}

				// Go until the current character is an open paren
				while (entry.Position < entry.Text.Length && entry.Text [entry.Position] != '(')
					entry.Position++;

				// Put the cursor right after the open paren
				entry.Position++;

				args.RetVal = true;
				break;

			default:
				args.RetVal = false;
				break;
			}
		}
		#endregion

		/*
		 * Helper methods.
		 */

		void Clear ()
		{
			entry.Text = string.Empty;
			Hide ();
		}

		// OPS The operators we support, case insensitive
		//private static string op_str = "(?'Ops' or | and |, | \\s+ )";
		static string op_str = "(?'Ops' "+ Catalog.GetString ("or") + " | "+ Catalog.GetString ("and")  + " |, )";

		// Match literals, eg tags or other text to search on
		static string literal_str = "[^{0}{1}]+?";
		//private static string not_literal_str = "not\\s*\\((?'NotTag'[^{0}{1}]+)\\)";

		// Match a group surrounded by parenthesis and one or more terms separated by operators
		static string term_str = "(((?'Open'{0})(?'Pre'[^{0}{1}]*?))+((?'Close-Open'{1})(?'Post'[^{0}{1}]*?))+)*?(?(Open)(?!))";

		// Match a group surrounded by parenthesis and one or more terms separated by operators, surrounded by not()
		//private static string not_term_str = string.Format("not\\s*(?'NotTerm'{0})", term_str);

		// Match a simple term or a group term or a not(group term)
		//private static string comb_term_str = string.Format ("(?'Term'{0}|{2}|{1})", simple_term_str, term_str, not_term_str);
		static string comb_term_str = string.Format ("(?'Term'{0}|{1})|not\\s*\\((?'NotTerm'{0})\\)|not\\s*(?'NotTerm'{1})", literal_str, term_str);

		// Match a single term or a set of terms separated by operators
		static string regex_str = string.Format ("^((?'Terms'{0}){1})*(?'Terms'{0})$", comb_term_str, op_str);

		static Regex term_regex = new Regex (
						  string.Format (regex_str, "\\(", "\\)"),
						  RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Breaking the query the user typed into something useful involves running
		// it through the above regular expression recursively until it is broken down
		// into literals and operators that we can use to generate SQL queries.
		bool ConstructQuery (Term parent, int depth, string txt)
		{
			return ConstructQuery(parent, depth, txt, false);
		}

		bool ConstructQuery (Term parent, int depth, string txt, bool negated)
		{
			if (string.IsNullOrEmpty(txt))
				return true;

			string indent = string.Format ("{0," + depth*2 + "}", " ");

			//Log.DebugFormat (indent + "Have text: {0}", txt);

			// Match the query the user typed against our regular expression
			Match match = term_regex.Match (txt);

			if (!match.Success) {
				//Log.Debug (indent + "Failed to match.");
				return false;
			}

			bool op_valid = true;
			string op = string.Empty;

			// For the moment at least we don't support operator precedence, so we require
			// that only a single operator is used for any given term unless it is made unambiguous
			// by using parenthesis.
			foreach (Capture capture in match.Groups ["Ops"].Captures) {
				if (op == string.Empty)
					op = capture.Value;
				else if (op != capture.Value) {
					op_valid = false;
					break;
				}
			}

			if (!op_valid) {
				Log.Information (indent + "Ambiguous operator sequence.  Use parenthesis to explicitly define evaluation order.");
				return false;
			}

			if (match.Groups ["Terms"].Captures.Count == 1 && match.Groups["NotTerm"].Captures.Count != 1) {
				//Log.DebugFormat (indent + "Unbreakable term: {0}", match.Groups ["Terms"].Captures [0]);
				string literal;
				bool is_negated = false;
				Tag tag = null;


				if (match.Groups ["NotTag"].Captures.Count == 1) {
					literal = match.Groups ["NotTag"].Captures [0].Value;
					is_negated = true;
				} else {
					literal = match.Groups ["Terms"].Captures [0].Value;
				}

				is_negated = is_negated || negated;

				tag = App.Instance.Database.Tags.GetTagByName (literal);

				// New OR term so we can match against both tag and text search
				parent = new OrTerm(parent, null);

				// If the literal is the name of a tag, include it in the OR
				//AbstractLiteral term = null;
				if (tag != null) {
					new Literal (parent, tag, null);
				}

				// Always include the literal text in the search (path, comment, etc)
				new TextLiteral (parent, literal);

				// If the term was negated, negate the OR parent term
				if (is_negated) {
					parent = parent.Invert(true);
				}

				if (RootTerm == null)
					root_term = parent;

				return true;
			} else {
				Term us = null;
				if (op != null && op != string.Empty) {
					us = Term.TermFromOperator (op, parent, null);
					if (RootTerm == null)
						root_term = us;
				}

				foreach (Capture capture in match.Groups ["Term"].Captures) {
					string subterm = capture.Value.Trim ();

					if (string.IsNullOrEmpty (subterm))
						continue;

					// Strip leading/trailing parens
					if (subterm [0] == '(' && subterm [subterm.Length - 1] == ')') {
						subterm = subterm.Remove (subterm.Length - 1, 1);
						subterm = subterm.Remove (0, 1);
					}

					//Log.DebugFormat (indent + "Breaking subterm apart: {0}", subterm);

					if (!ConstructQuery (us, depth + 1, subterm, negated))
						return false;
				}

				foreach (Capture capture in match.Groups ["NotTerm"].Captures) {
					string subterm = capture.Value.Trim ();

					if (string.IsNullOrEmpty (subterm))
						continue;

					// Strip leading/trailing parens
					if (subterm [0] == '(' && subterm [subterm.Length - 1] == ')') {
						subterm = subterm.Remove (subterm.Length - 1, 1);
						subterm = subterm.Remove (0, 1);
					}

					//Log.DebugFormat (indent + "Breaking not subterm apart: {0}", subterm);

					if (!ConstructQuery (us, depth + 1, subterm, true))
						return false;
				}

				if (negated && us != null) {
					if (us == RootTerm)
						root_term = us.Invert(false);
					else
						us.Invert(false);
				}

				return true;
			}
		}

		bool updating;
		uint update_timeout_id = 0;
		void QueueUpdate ()
		{
			if (updating || update_timeout_id != 0) {
				lock(lockObject) {
					// If there is a timer set and we are not yet handling its timeout, then remove the timer
					// so we delay its trigger for another 500ms.
					if (!updating && update_timeout_id != 0)
						GLib.Source.Remove (update_timeout_id);

					// Assuming we're not currently handling a timeout, add a new timer
					if (!updating)
						update_timeout_id = GLib.Timeout.Add(500, OnUpdateTimer);
				}
			} else {
				// If we are not updating and there isn't a timer already set, then there is
				// no risk of race condition with the  timeout handler.
				update_timeout_id = GLib.Timeout.Add(500, OnUpdateTimer);
			}
		}

		bool OnUpdateTimer ()
		{
			lock(lockObject) {
				updating = true;
			}

			Update();

			lock(lockObject) {
				updating = false;
				update_timeout_id = 0;
			}

			return false;
		}

		void Update ()
		{
			// Clear the last root term
			root_term = null;

			if (ParensValid () && ConstructQuery (null, 0, entry.Text)) {
				if (RootTerm != null) {
					//Log.DebugFormat("rootTerm = {0}", RootTerm);
					if (!(RootTerm is AndTerm)) {
						// A little hacky, here to make sure the root term is a AndTerm which will
						// ensure we handle the Hidden tag properly
						var root_parent = new AndTerm(null, null);
						RootTerm.Parent = root_parent;
						root_term = root_parent;
					}

					//Log.DebugFormat("rootTerm = {0}", RootTerm);
					if (!(RootTerm is AndTerm)) {
						// A little hacky, here to make sure the root term is a AndTerm which will
						// ensure we handle the Hidden tag properly
						var root_parent = new AndTerm(null, null);
						RootTerm.Parent = root_parent;
						root_term = root_parent;
					}
					//Log.DebugFormat ("condition = {0}", RootTerm.SqlCondition ());
					query.TagTerm = new ConditionWrapper (RootTerm.SqlCondition ());
				} else {
					query.TagTerm = null;
					//Log.Debug ("root term is null");
				}
			}
		}

		bool ParensValid ()
		{
			for (int i = 0; i < entry.Text.Length; i++)
			{
				if (entry.Text [i] == '(' || entry.Text [i] == ')') {
					int pair_pos = ParenPairPosition (entry.Text, i);

					if (pair_pos == -1)
						return false;
				}
			}

			return true;
		}

		/*
		 * Static Utility Methods
		 */
		static int ParenPairPosition (string txt, int pos)
		{
			char one = txt [pos];
			bool open = (one == '(');
			char two = (open) ? ')' : '(';

			//int level = 0;
			int num = (open) ? txt.Length - pos - 1: pos;

			int sames = 0;
			for (int i = 0; i < num; i++) {
				if (open)
					pos++;
				else
					pos--;

				if (pos < 0 || pos > txt.Length - 1)
					return -1;

				if (txt [pos] == one)
					sames++;
				else if (txt [pos] == two) {
					if (sames == 0)
						return pos;
					sames--;
				}
			}

			return -1;
		}

		/*private static string ReverseString (string txt)
		{
		    char [] txt_a = txt.ToCharArray ();
		    System.Reverse (txt_a);
		    return new String (txt_a);
		}*/
	}
}
