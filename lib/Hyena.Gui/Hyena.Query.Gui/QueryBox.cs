//
// QueryBox.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

using Mono.Unix;

using Gtk;
using Hyena;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    public class QueryBox : VBox
    {
        private QueryTermsBox terms_box;
        private bool complex_query = false;

        private HBox terms_entry_box;
        private Entry terms_entry;

        private QueryLimitBox limit_box;
        public QueryLimitBox LimitBox {
            get { return limit_box; }
        }

        private ComboBox terms_logic_combo;
        private CheckButton terms_enabled_checkbox;
        private Label terms_label;
        private QueryFieldSet field_set;
        private Frame matchesFrame;

        public QueryBox (QueryFieldSet fieldSet, QueryOrder [] orders, QueryLimit [] limits) : base ()
        {
            //this.sorted_fields = fieldSet.Fields;
            this.field_set = fieldSet;
            terms_box = new QueryTermsBox (field_set);
            limit_box = new QueryLimitBox (orders, limits);

            BuildInterface ();
        }

        private void BuildInterface ()
        {
            NoShowAll = true;

            Alignment matchesAlignment = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
            matchesAlignment.SetPadding (5, 5, 5, 5);
            matchesAlignment.Add (terms_box);

            matchesFrame = new Frame (null);
            matchesFrame.Add (matchesAlignment);
            matchesFrame.LabelWidget = BuildMatchHeader ();
            matchesFrame.ShowAll ();

            terms_entry_box = new HBox ();
            terms_entry_box.Spacing = 8;
            terms_entry_box.PackStart (new Label (Catalog.GetString ("Condition:")), false, false, 0);
            terms_entry = new Entry ();
            terms_entry_box.PackStart (terms_entry, true, true, 0);

            limit_box.ShowAll ();

            PackStart(matchesFrame, true, true, 0);
            PackStart(terms_entry_box, false, false, 0);
            PackStart(limit_box, false, false, 0);

            //ShowAll ();
        }

        private HBox BuildMatchHeader ()
        {
            HBox header = new HBox ();
            header.Show ();

            terms_enabled_checkbox = new CheckButton (Catalog.GetString ("_Match"));
            terms_enabled_checkbox.Show ();
            terms_enabled_checkbox.Active = true;
            terms_enabled_checkbox.Toggled += OnMatchCheckBoxToggled;
            header.PackStart (terms_enabled_checkbox, false, false, 0);

            terms_logic_combo = ComboBox.NewText ();
            terms_logic_combo.AppendText (Catalog.GetString ("all"));
            terms_logic_combo.AppendText (Catalog.GetString ("any"));
            terms_logic_combo.Show ();
            terms_logic_combo.Active = 0;
            header.PackStart (terms_logic_combo, false, false, 0);

            terms_label = new Label (Catalog.GetString ("of the following:"));
            terms_label.Show ();
            terms_label.Xalign = 0.0f;
            header.PackStart (terms_label, true, true, 0);

            header.Spacing = 5;

            return header;
        }

        private void OnMatchCheckBoxToggled   (object o, EventArgs args)
        {
            terms_box.Sensitive = terms_enabled_checkbox.Active;
            terms_logic_combo.Sensitive = terms_enabled_checkbox.Active;
            terms_label.Sensitive = terms_enabled_checkbox.Active;
        }

        public QueryNode QueryNode {
            get {
                if (!complex_query && !terms_enabled_checkbox.Active) {
                    return null;
                }

                if (complex_query) {
                    return UserQueryParser.Parse (terms_entry.Text, field_set);
                }

                QueryListNode node = new QueryListNode (terms_logic_combo.Active == 0 ? Keyword.And : Keyword.Or);
                foreach (QueryNode child in terms_box.QueryNodes) {
                    node.AddChild (child);
                }
                return node.Trim ();
            }

            set {
                if (value != null) {
                    terms_enabled_checkbox.Active = true;

                    try {
                        if (value is QueryListNode) {
                            terms_logic_combo.Active = ((value as QueryListNode).Keyword == Keyword.And) ? 0 : 1;
                            terms_box.QueryNodes = (value as QueryListNode).Children;
                        } else {
                            List<QueryNode> nodes = new List<QueryNode> ();
                            nodes.Add (value);
                            terms_box.QueryNodes = nodes;
                        }
                    } catch (ArgumentException) {
                        complex_query = true;
                        matchesFrame.HideAll ();
                        terms_entry.Text = value.ToUserQuery ();
                        terms_entry_box.ShowAll ();
                    }
                } else {
                    terms_enabled_checkbox.Active = false;
                }
            }
        }
    }
}
