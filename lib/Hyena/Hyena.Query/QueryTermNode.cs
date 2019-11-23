//
// QueryTermNode.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Xml;
using System.Text;
using System.Collections.Generic;

namespace Hyena.Query
{
    public class QueryTermNode : QueryNode
    {
        private QueryField field;
        private Operator op;
        private QueryValue qvalue;

        public static QueryTermNode ParseUserQuery (QueryFieldSet field_set, string token)
        {
            QueryTermNode term = new QueryTermNode ();

            // See if the query specifies a field, and if so, pull out the operator as well
            string field_alias = field_set.FindAlias (token);
            if (field_alias != null) {
                term.Field = field_set [field_alias];
                string token_without_field = token.Substring (field_alias.Length);

                foreach (QueryValue val in term.Field.CreateQueryValues ()) {
                    term.Value = val;

                    string op_alias = term.Value.OperatorSet.FindAlias (token_without_field);
                    if (op_alias != null) {
                        term.Operator = term.Value.OperatorSet [op_alias];
                        int field_separator = token.IndexOf (op_alias);
                        string temp = token.Substring (field_separator + op_alias.Length);

                        term.Value.ParseUserQuery (temp);

                        if (!term.Value.IsEmpty) {
                            break;
                        }
                    }

                    term.Operator = null;
                    term.Value = null;
                }
            }

            if (term.Value == null) {
                term.Field = null;
                term.Value = QueryValue.CreateFromUserQuery (token, term.Field);
                term.Operator = StringQueryValue.Contains;
            }

            return term;
        }

        public QueryTermNode () : base ()
        {
        }

        public override QueryNode Trim ()
        {
            if (Parent != null && (qvalue == null || qvalue.IsEmpty || (field != null && op == null))) {
                Parent.RemoveChild (this);
            }
            return this;
        }

        public override void AppendUserQuery (StringBuilder sb)
        {
            sb.Append (Field == null ? Value.ToUserQuery () : Field.ToTermString (Operator.PrimaryAlias, Value.ToUserQuery ()));
        }

        public override void AppendXml (XmlDocument doc, XmlNode parent, QueryFieldSet fieldSet)
        {
            XmlElement op_node = doc.CreateElement (op == null ? "contains" : op.Name);
            parent.AppendChild (op_node);

            QueryField field = Field;
            if (field != null) {
                XmlElement field_node = doc.CreateElement ("field");
                field_node.SetAttribute ("name", field.Name);
                op_node.AppendChild (field_node);
            }

            XmlElement val_node = doc.CreateElement (Value.XmlElementName);
            Value.AppendXml (val_node);
            op_node.AppendChild (val_node);
        }

        public override void AppendSql (StringBuilder sb, QueryFieldSet fieldSet)
        {
            if (Field == null) {
                sb.Append ("(");
                int emitted = 0;

                foreach (QueryField field in fieldSet.Fields) {
                    if (field.IsDefault)
                        if (EmitTermMatch (sb, field, emitted > 0))
                            emitted++;
                }

                sb.Append (")");
            } else {
                EmitTermMatch (sb, Field, false);
            }
        }

        private bool EmitTermMatch (StringBuilder sb, QueryField field, bool emit_or)
        {
            if (Value.IsEmpty) {
                return false;
            }

            if (emit_or) {
                sb.Append (" OR ");
            }

            sb.Append (field.ToSql (Operator, Value));
            return true;
        }

        public QueryField Field {
            get { return field; }
            set { field = value; }
        }

        public Operator Operator {
            get { return op; }
            set { op = value; }
        }

        public QueryValue Value {
            get { return qvalue; }
            set { qvalue = value; }
        }
    }
}
