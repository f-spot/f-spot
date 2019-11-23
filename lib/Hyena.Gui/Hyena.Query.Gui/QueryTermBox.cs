//
// QueryTermBox.cs
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
using System.Text;
using System.Collections.Generic;

using Gtk;
using Hyena;
using Hyena.Query;

namespace Hyena.Query.Gui
{
    public class QueryTermBox
    {
        private Button add_button;
        private Button remove_button;

        public event EventHandler AddRequest;
        public event EventHandler RemoveRequest;

        private QueryField field;
        private List<QueryValueEntry> value_entries = new List<QueryValueEntry> ();
        private List<Operator> operators = new List<Operator> ();
        private Dictionary<Operator, QueryValueEntry> operator_entries = new Dictionary<Operator, QueryValueEntry> ();
        private QueryValueEntry current_value_entry;
        private Operator op;

        private QueryField [] sorted_fields;

        private ComboBox field_chooser;
        public ComboBox FieldChooser {
            get { return field_chooser; }
        }

        private ComboBox op_chooser;
        public ComboBox OpChooser {
            get { return op_chooser; }
        }

        private HBox value_box;
        public HBox ValueEntry {
            get { return value_box; }
        }

        private HBox button_box;
        public HBox Buttons {
            get { return button_box; }
        }

        public QueryTermBox (QueryField [] sorted_fields) : base ()
        {
            this.sorted_fields = sorted_fields;
            BuildInterface ();
        }

        private void BuildInterface ()
        {
            field_chooser = ComboBox.NewText ();
            field_chooser.Changed += HandleFieldChanged;

            op_chooser = ComboBox.NewText ();
            op_chooser.RowSeparatorFunc = IsRowSeparator;
            op_chooser.Changed += HandleOperatorChanged;

            value_box = new HBox ();

            remove_button = new Button (new Image ("gtk-remove", IconSize.Button));
            remove_button.Relief = ReliefStyle.None;
            remove_button.Clicked += OnButtonRemoveClicked;

            add_button = new Button (new Image ("gtk-add", IconSize.Button));
            add_button.Relief = ReliefStyle.None;
            add_button.Clicked += OnButtonAddClicked;

            button_box = new HBox ();
            button_box.PackStart (remove_button, false, false, 0);
            button_box.PackStart (add_button, false, false, 0);

            foreach (QueryField field in sorted_fields) {
                field_chooser.AppendText (field.Label);
            }

            Show ();
            field_chooser.Active = 0;
        }

        private bool IsRowSeparator (TreeModel model, TreeIter iter)
        {
            return String.IsNullOrEmpty (model.GetValue (iter, 0) as string);
        }

        public void Show ()
        {
            field_chooser.ShowAll ();
            op_chooser.ShowAll ();
            value_box.ShowAll ();
            button_box.ShowAll ();
        }

        private bool first = true;
        private void SetValueEntry (QueryValueEntry entry)
        {
            if (first) {
                first = false;
            } else {
                value_box.Remove (value_box.Children [0]);
            }

            current_value_entry = entry;
            value_box.PackStart (current_value_entry, false, true, 0);
            current_value_entry.ShowAll ();
        }

        private void HandleFieldChanged (object o, EventArgs args)
        {
            if (field_chooser.Active < 0 || field_chooser.Active >= sorted_fields.Length)
                return;

            QueryField field = sorted_fields [field_chooser.Active];

            // Leave everything as is unless the new field is a different type
            if (this.field != null && (field.ValueTypes.Length == 1 && this.field.ValueTypes.Length == 1 && field.ValueTypes[0] == this.field.ValueTypes[0])) {
                this.field = field;
                return;
            }

            op_chooser.Changed -= HandleOperatorChanged;

            this.field = field;

            // Remove old type's operators
            while (op_chooser.Model.IterNChildren () > 0) {
                op_chooser.RemoveText (0);
            }

            // Add new field's operators
            int val_count = 0;
            value_entries.Clear ();
            operators.Clear ();
            operator_entries.Clear ();
            foreach (QueryValue val in this.field.CreateQueryValues ()) {
                QueryValueEntry entry = QueryValueEntry.Create (val);
                value_entries.Add (entry);

                if (val_count++ > 0) {
                    op_chooser.AppendText (String.Empty);
                    operators.Add (null);
                }

                foreach (Operator op in val.OperatorSet) {
                    op_chooser.AppendText (op.Label);
                    operators.Add (op);
                    operator_entries [op] = entry;
                }
            }

            SetValueEntry (value_entries[0]);

            // TODO: If we have the same operator that was previously selected, select it
            op_chooser.Changed += HandleOperatorChanged;
            op_chooser.Active = 0;
        }

        private void HandleOperatorChanged (object o, EventArgs args)
        {
            if (op_chooser.Active < 0 || op_chooser.Active >= operators.Count) {
                return;
            }

            this.op = operators [op_chooser.Active];
            if (operator_entries [this.op] != current_value_entry) {
                SetValueEntry (operator_entries [this.op]);
            }

            //value_entry = new QueryValueEntry <field.ValueType> ();
        }

        private void OnButtonAddClicked (object o, EventArgs args)
        {
            EventHandler handler = AddRequest;
            if (handler != null)
                handler (this, new EventArgs ());
        }

        private void OnButtonRemoveClicked (object o, EventArgs args)
        {
            EventHandler handler = RemoveRequest;
            if (handler != null)
                handler (this, new EventArgs ());
        }

        public bool CanDelete {
            get { return remove_button.Sensitive; }
            set { remove_button.Sensitive = value; }
        }

        public QueryTermNode QueryNode {
            get {
                QueryTermNode node = new QueryTermNode ();
                node.Field = field;
                node.Operator = op;
                node.Value = current_value_entry.QueryValue;
                return node;
            }

            set {
                QueryTermNode node = value;
                if (node == null) {
                    return;
                }

                field_chooser.Active = Array.IndexOf (sorted_fields, node.Field);

                op_chooser.Active = operators.IndexOf (node.Operator);

                current_value_entry.QueryValue = node.Value;
                /*foreach (QueryValueEntry entry in value_entries) {
                    if (QueryValueEntry.GetValueType (entry) == node.Value.GetType ()) {
                        Console.WriteLine ("In QueryTermBox, setting QueryNode, got matching value types, value is {0}, empty? {1}", node.Value.ToString (), node.Value.IsEmpty);
                        entry.QueryValue = node.Value;
                        SetValueEntry (entry);
                        break;
                    }
                }*/

            }
        }
    }
}
