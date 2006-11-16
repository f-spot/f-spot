using System;
using System.Collections;
using System.Text.RegularExpressions;
using Gtk;
using Gdk;
using Mono.Unix;

using FSpot.Query;

namespace FSpot {
    public class FindBar : HBox {
        private Entry entry;
        private Label valid_label;
        private string last_entry_text = String.Empty;
        private int open_parens = 0, close_parens = 0;
        private PhotoQuery query;
        private LogicTerm root_term = null;

        /*
         * Properties
         */
        public bool Completing {
            get {
                return (entry.Completion as LogicEntryCompletion).Completing;
            }
        }

        public Gtk.Entry Entry {
            get { return entry; }
        }

        public LogicTerm RootTerm  {
            get { return root_term; }
        }

        /*
         * Constructor
         */
        public FindBar (PhotoQuery query, TreeModel model)
        {
            this.query = query;

            Spacing = 6;

            PackStart (new Label (Catalog.GetString ("Find:")), false, false, 0);

            entry = new Entry ();
            entry.Completion = new LogicEntryCompletion (entry, model);

            entry.TextInserted  += HandleEntryTextInserted;
            entry.TextDeleted   += HandleEntryTextDeleted;
            entry.KeyPressEvent += HandleEntryKeyPress;

            PackStart (entry, true, true, 0);

            valid_label = new Label (valid_str);
            valid_label.Xalign = 1.0f;
            PackStart (valid_label, false, false, 0);

			Button clear_button = new Gtk.Button ();
			clear_button.Add (new Gtk.Image ("gtk-close", Gtk.IconSize.Button));
			clear_button.Clicked += HandleCloseButtonClicked;
			clear_button.Relief = Gtk.ReliefStyle.None;
			PackStart (clear_button, false, false, 0);
        }

        /*
         * Event Handlers
         */

        private void HandleCloseButtonClicked (object sender, EventArgs args)
        {
            Clear ();
        }

        private void HandleEntryTextInserted (object sender, TextInsertedArgs args)
        {
            //Console.WriteLine ("inserting {0}, ( = {1}  ) = {2}", args.Text, open_parens, close_parens);

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
            //Console.WriteLine ("done w/ insert, {0}, ( = {1}  ) = {2}", args.Text, open_parens, close_parens);
            last_entry_text = entry.Text;

            Update ();
        }

        private void HandleEntryTextDeleted (object sender, TextDeletedArgs args)
        {
            int length = args.EndPos - args.StartPos;
            //Console.WriteLine ("start {0} end {1} len {2} last {3}", args.StartPos, args.EndPos, length, last_entry_text);
            string txt = last_entry_text.Substring (args.StartPos, length);

            for (int i = 0; i < txt.Length; i++) {
                if (txt [i] == '(')
                    open_parens--;
                else if (txt [i] == ')')
                    close_parens--;
            }

            last_entry_text = entry.Text;

            Update ();
        }

        private void HandleEntryKeyPress (object sender, KeyPressEventArgs args)
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

        /*
         * Helper methods.
         */

        private void Clear ()
        {
            entry.Text = String.Empty;
            Hide ();
        }

        private static string op_str = "(?'Ops' or | and |, )";

        private static string literal_str = "[^{0}{1}]+?";
        private static string not_term_str = "not\\s*\\((?'NotTag'[^{0}{1}]+)\\)";
        private static string simple_term_str = String.Format ("({0}|{1})", literal_str, not_term_str);

        private static string term_str = "(((?'Open'{0})(?'Pre'[^{0}{1}]*?))+((?'Close-Open'{1})(?'Post'[^{0}{1}]*?))+)*?(?(Open)(?!))";
        private static string comb_term_str = String.Format ("({0}|{1})", simple_term_str, term_str);

        private static string regex_str = String.Format ("^((?'Terms'{0}){1})*(?'Terms'{0})$", comb_term_str, op_str);
        private static Regex term_regex = new Regex (
                String.Format (regex_str, "\\(", "\\)"),
                RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static void PrintGroup (GroupCollection groups, string name)
        {
            Group group = groups [name];
            Console.WriteLine ("Name: {2} (success = {1}) group: {0}", group, group.Success, name);
            foreach (Capture capture in group.Captures) {
                Console.WriteLine ("  Have capture: {0}", capture);
            }
        }

        private bool ConstructQuery (LogicTerm parent, int depth, string txt)
        {
            if (txt == null || txt.Length == 0)
                return true;

            //string indent = String.Format ("{0," + depth*2 + "}", " ");

            //Console.WriteLine (indent + "Have text: {0}", txt);
            Match match = term_regex.Match (txt);

            if (!match.Success) {
                //Console.WriteLine (indent + "Failed to match.");
                return false;
            }

            bool op_valid = true;
            string op = String.Empty;
            foreach (Capture capture in match.Groups ["Ops"].Captures) {
                if (op == String.Empty)
                    op = capture.Value;
                else if (op != capture.Value) {
                    op_valid = false;
                    break;
                }
            }

            if (!op_valid) {
                //Console.WriteLine (indent + "Ambiguous operator sequence.  Use parenthesis to explicitly define evaluation order.");
                return false;
            }

            if (match.Groups ["Terms"].Captures.Count == 1) {
                //Console.WriteLine (indent + "Unbreakable term: {0}", match.Groups ["Terms"].Captures [0]);
                string tag_name;
                bool is_negated = false;
                Tag tag = null;

                if (match.Groups ["NotTag"].Captures.Count == 1) {
                    tag_name = match.Groups ["NotTag"].Captures [0].Value;
                    is_negated = true;
                } else {
                    tag_name = match.Groups ["Terms"].Captures [0].Value;
                }

                tag = MainWindow.Toplevel.Database.Tags.GetTagByName (tag_name);

                if (tag != null) {
                    Literal us = new Literal (parent, tag, null);

                    if (is_negated)
                        us.IsNegated = true;

                    if (depth == 0)
                        root_term = us;
                    return true;
                } else {
                    return false;
                }
            } else {
                LogicTerm us = LogicTerm.TermFromOperator (op, parent, null);
                if (depth == 0)
                    root_term = us;

                foreach (Capture capture in match.Groups ["Terms"].Captures) {
                    string subterm = capture.Value.Trim ();

                    if (subterm == null || subterm.Length == 0)
                        continue;

                    // Strip leading/trailing parens
                    if (subterm [0] == '(' && subterm [subterm.Length - 1] == ')') {
                        subterm = subterm.Remove (subterm.Length - 1, 1);
                        subterm = subterm.Remove (0, 1);
                    }

                    //Console.WriteLine (indent + "Breaking subterm apart: {0}", subterm);

                    if (!ConstructQuery (us, depth + 1, subterm))
                        return false;
                }

                return true;
            }
        }

        static string valid_str = Catalog.GetString ("Valid");
        static string invalid_str = Catalog.GetString ("Invalid");

        private void Update ()
        {
            // Clear the last root term
            root_term = null;

            if (ParensValid () && ConstructQuery (null, 0, entry.Text)) {
                if (RootTerm != null) {
                    //Console.WriteLine ("condition = {0}", RootTerm.ConditionString ());
                    query.ExtraCondition = RootTerm.ConditionString ();
                } else {
                    query.ExtraCondition = null;
                    //Console.WriteLine ("root term is null");
                }

                valid_label.Text = valid_str;
            } else {
                valid_label.Text = invalid_str;
            }
        }

        private bool ParensValid ()
        {
            for (int i = 0; i < entry.Text.Length; i++) {
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
        private static int ParenPairPosition (string txt, int pos)
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
                    else
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

    public class LogicEntryCompletion : EntryCompletion {
        Entry entry;

        private bool completing = false;
        public bool Completing {
            get { return completing; }
        }

        public LogicEntryCompletion (Entry entry, TreeModel model)
        {
            this.entry = entry;
            Model = model;
            InlineCompletion = false;
            MinimumKeyLength = 1;
            TextColumn = 1;
            PopupSetWidth = false;
            MatchFunc = LogicEntryCompletionMatchFunc;
            MatchSelected += HandleMatchSelected;

            // Insert these when appropriate..
            //InsertActionText (0, "or");
            //InsertActionText (1, "and");
            // HandleAction...
        }

        [GLib.ConnectBefore]
        private void HandleMatchSelected (object sender, MatchSelectedArgs args)
        {
            // Delete what the user had entered in case their casing wasn't the same
            entry.DeleteText (entry.Position - transformed_key.Length, entry.Position);

            string name = args.Model.GetValue (args.Iter, TextColumn) as string;
            //Console.WriteLine ("match selected..{0}", name);
            int pos = entry.Position;

            completing = true;
            entry.InsertText (name, ref pos);
            completing = false;

            entry.Position += name.Length;

            args.RetVal = true;
            //Console.WriteLine ("done w/ match selected");
        }

        string last_key = String.Empty;
        string transformed_key = String.Empty;
        public bool LogicEntryCompletionMatchFunc (EntryCompletion completion, string key, TreeIter iter)
        {
            if (Completing)
                return false;

            // If this is the fist comparison for this key, convert the key (which is the entire search string)
            // into just the part that is relevant to completing this tag name.
            if (key != last_key) {
                last_key = key;

                int pos = entry.Position - 1;
                if (key == null || key.Length == 0 || pos < 0 || pos > key.Length - 1)
                    transformed_key = String.Empty;
                else if (key [pos] == '(' || key [pos] == ')' || key [pos] == ',')
                    transformed_key = String.Empty;
                else {
                    int start = 0;
                    for (int i = entry.Position - 1; i >= 0; i--) {
                        if (key [i] == ' ' || key [i] == ')' || key [i] == '(') {
                            //Console.WriteLine ("have start break char at {0}", i);
                            start = i + 1;
                            break;
                        }
                    }

                    int end = key.Length - 1;
                    for (int j = entry.Position - 1; j < key.Length; j++) {
                        if (key [j] == ' ' || key [j] == ')' || key [j] == '(') {
                            end = j - 1;
                            break;
                        }
                    }

                    //Console.WriteLine ("start = {0} end = {1}", start, end);

                    int len = end - start + 1;
                    if (len > 0 && start < last_key.Length)
                        transformed_key = last_key.Substring (start, end - start + 1);
                    else
                        transformed_key = String.Empty;
                }
                //Console.WriteLine ("transformed key {0} into {1}", key, transformed_key);
            }

            if (transformed_key == String.Empty)
                return false;

            string name = completion.Model.GetValue (iter, completion.TextColumn) as string;

            // Ignore null or names that are too short
            if (name == null || name.Length <= transformed_key.Length)
                return false;

            //Console.WriteLine ("entered = {0} compared to {1}", transformed_key, name);
            return (String.Compare (transformed_key, name.Substring(0, transformed_key.Length), true) == 0);
        }
    }
}
