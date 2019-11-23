//
// XmlQueryParser.cs
//
// Author:
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
using System.IO;
using System.Text;
using System.Xml;

namespace Hyena.Query
{
    public class XmlQueryParser : QueryParser
    {
        private string str;
        private QueryFieldSet field_set;

        public static QueryNode Parse (string input, QueryFieldSet fieldSet)
        {
            return new XmlQueryParser (input).BuildTree (fieldSet);
        }

        public XmlQueryParser () : base () {}

        public XmlQueryParser (string str)
        {
            this.str = str;
        }

        public override QueryNode BuildTree (QueryFieldSet fieldSet)
        {
            field_set = fieldSet;
            XmlDocument doc = new XmlDocument ();
            try {
                doc.LoadXml (str);
                XmlElement request = doc.FirstChild as XmlElement;
                if (request == null || request.Name != "request")
                    throw new Exception ("Invalid request");

                XmlElement query = request.FirstChild as XmlElement;
                if (query == null || query.Name != "query" || query.GetAttribute ("banshee-version") != "1")
                    throw new Exception ("Invalid query");

                QueryNode node = Parse (query.FirstChild as XmlElement, null);
                return (node != null) ? node.Trim () : null;
            } catch (Exception) {
            }
            return null;
        }

        private QueryNode Parse (XmlElement node, QueryListNode parent)
        {
            if (node == null)
                return null;

            QueryListNode list = null;
            //Console.WriteLine ("Parsing node: {0}", node.Name);
            switch (node.Name.ToLower ()) {
                case "and":
                    list = new QueryListNode (Keyword.And);
                    break;
                case "or":
                    list = new QueryListNode (Keyword.Or);
                    break;
                case "not":
                    list = new QueryListNode (Keyword.Not);
                    break;
                default:
                    QueryTermNode term = new QueryTermNode ();

                    // Get the field (if any) that this term applies to
                    if (node["field"] != null)
                        term.Field = field_set [node["field"].GetAttribute ("name")];

                    // Get the value
                    term.Value = QueryValue.CreateFromXml (node, term.Field);

                    // Get the operator from the term's name
                    term.Operator = term.Value.OperatorSet [node.Name];


                    if (parent != null) {
                        parent.AddChild (term);
                    }

                    return term;
            }

            if (list != null) {
                if (parent != null)
                    parent.AddChild (list);

                // Recursively parse the children of a QueryListNode
                foreach (XmlNode child in node.ChildNodes) {
                    Parse (child as XmlElement, list);
                }
            }

            return list;
        }

        public override void Reset ()
        {
        }
    }
}
