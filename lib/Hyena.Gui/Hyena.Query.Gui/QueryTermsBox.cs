//
// QueryTermsBox.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
// Copyright (C) 2009 Alexander Kojevnikov
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
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Gtk;
using Hyena;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    public class QueryTermsBox : Table
    {
        private QueryField [] sorted_fields;
        private List<QueryTermBox> terms = new List<QueryTermBox> ();

        public QueryTermBox FirstRow {
            get { return terms.Count > 0 ? terms[0] : null; }
        }

        public QueryTermsBox (QueryFieldSet fieldSet) : base (1, 4, false)
        {
            // Sort the fields alphabetically by their label
            sorted_fields = fieldSet.OrderBy (f => f.Label).ToArray ();

            ColumnSpacing = 5;
            RowSpacing = 5;

            CreateRow (false);
        }

        public List<QueryNode> QueryNodes {
            get {
                return terms.Select<QueryTermBox, QueryNode> (t => t.QueryNode).ToList ();
            }
            set {
                ClearRows ();
                first_add_node = true;
                foreach (QueryNode child in value) {
                    AddNode (child);
                }
            }
        }

        private bool first_add_node;
        protected void AddNode (QueryNode node)
        {
            if (node is QueryTermNode) {
                QueryTermBox box = first_add_node ? FirstRow : CreateRow (true);
                box.QueryNode = node as QueryTermNode;
                first_add_node = false;
            } else {
                throw new ArgumentException ("Query is too complex for GUI query editor", "node");
            }
        }

        protected QueryTermBox CreateRow (bool canDelete)
        {
            QueryTermBox row = new QueryTermBox (sorted_fields);

            row.ValueEntry.HeightRequest = 31;
            row.Buttons.HeightRequest = 31;

            Resize ((uint) terms.Count + 1, NColumns);
            Attach (row.FieldChooser, 0, 1, NRows - 1, NRows);
            Attach (row.OpChooser, 1, 2, NRows - 1, NRows);
            Attach (row.ValueEntry, 2, 3, NRows - 1, NRows);
            Attach (row.Buttons, 3, 4, NRows - 1, NRows);

            if (terms.Count > 0) {
                row.FieldChooser.Active = terms[terms.Count - 1].FieldChooser.Active;
                row.OpChooser.Active = terms[terms.Count - 1].OpChooser.Active;
            }

            row.Show ();

            row.CanDelete = canDelete;
            row.AddRequest += OnRowAddRequest;
            row.RemoveRequest += OnRowRemoveRequest;

            if (terms.Count == 0) {
                //row.FieldBox.GrabFocus ();
            }

            terms.Add (row);

            return row;
        }

        protected void OnRowAddRequest (object o, EventArgs args)
        {
            CreateRow (true);
            UpdateCanDelete ();
        }

        protected void OnRowRemoveRequest (object o, EventArgs args)
        {
            RemoveRow (terms.IndexOf (o as QueryTermBox));
        }

        private void ClearRows ()
        {
            for (int index = terms.Count - 1; index > 0; index--) {
                RemoveRow (index);
            }
        }

        private void RemoveRow (int index)
        {
            FreezeChildNotify ();

            QueryTermBox row = terms [index];
            Remove (row.FieldChooser);
            Remove (row.OpChooser);
            Remove (row.ValueEntry);
            Remove (row.Buttons);

            for (int i = index + 1; i < terms.Count; i++) {
                Remove (terms[i].FieldChooser);
                Remove (terms[i].OpChooser);
                Remove (terms[i].ValueEntry);
                Remove (terms[i].Buttons);

                Attach (terms[i].FieldChooser, 0, 1, (uint) i - 1, (uint) i);
                Attach (terms[i].OpChooser, 1, 2, (uint) i - 1, (uint) i);
                Attach (terms[i].ValueEntry, 2, 3, (uint) i - 1, (uint) i);
                Attach (terms[i].Buttons, 3, 4, (uint) i - 1, (uint) i);
            }

            ThawChildNotify ();
            terms.Remove (row);
            UpdateCanDelete ();
        }

        protected void UpdateCanDelete ()
        {
            if (FirstRow != null) {
                FirstRow.CanDelete = terms.Count > 1;
            }
        }

        private new void Attach (Widget widget, uint left_attach, uint right_attach, uint top_attach, uint bottom_attach)
        {
            Attach (widget, left_attach, right_attach, top_attach, bottom_attach,
                AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
        }
    }
}
