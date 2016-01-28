//
// QueryNode.cs
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
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Text;

namespace Hyena.Query
{
    public enum QueryNodeSearchMethod
    {
        DepthFirst,
        BreadthFirst
    }

    public abstract class QueryNode
    {
        private QueryListNode parent;
        private int source_column;
        private int source_line;

        public QueryNode()
        {
        }

        public QueryNode(QueryListNode parent)
        {
            Parent = parent;
            Parent.AddChild(this);
        }

        protected void PrintIndent(int depth)
        {
            Console.Write(String.Empty.PadLeft(depth * 2, ' '));
        }

        public void Dump()
        {
            Dump(0);
        }

        internal virtual void Dump(int depth)
        {
            PrintIndent(depth);
            Console.WriteLine(this);
        }

        public abstract QueryNode Trim ();

        public string ToUserQuery ()
        {
            StringBuilder sb = new StringBuilder ();
            AppendUserQuery (sb);
            return sb.ToString ();
        }

        public abstract void AppendUserQuery (StringBuilder sb);

        public string ToXml (QueryFieldSet fieldSet)
        {
            return ToXml (fieldSet, false);
        }

        public virtual string ToXml (QueryFieldSet fieldSet, bool pretty)
        {
            XmlDocument doc = new XmlDocument ();

            XmlElement request = doc.CreateElement ("request");
            doc.AppendChild (request);

            XmlElement query = doc.CreateElement ("query");
            query.SetAttribute ("banshee-version", "1");
            request.AppendChild (query);

            AppendXml (doc, query, fieldSet);

            if (!pretty) {
                return doc.OuterXml;
            }

            using (StringWriter sw = new StringWriter ()) {
                using (XmlTextWriter xtw = new XmlTextWriter (sw)) {
                    xtw.Formatting = System.Xml.Formatting.Indented;
                    xtw.Indentation = 2;
                    doc.WriteTo (xtw);
                    return sw.ToString ();
                }
            }
        }

        public IEnumerable<T> SearchForValues<T> () where T : QueryValue
        {
            return SearchForValues<T> (QueryNodeSearchMethod.DepthFirst);
        }

        public IEnumerable<T> SearchForValues<T> (QueryNodeSearchMethod method) where T : QueryValue
        {
            if (method == QueryNodeSearchMethod.DepthFirst) {
                return SearchForValuesByDepth<T> (this);
            } else {
                return SearchForValuesByBreadth<T> ();
            }
        }

        private static IEnumerable<T> SearchForValuesByDepth<T> (QueryNode node) where T : QueryValue
        {
            QueryListNode list = node as QueryListNode;
            if (list != null) {
                foreach (QueryNode child in list.Children) {
                    foreach (T item in SearchForValuesByDepth<T> (child)) {
                        yield return item;
                    }
                }
            } else {
                QueryTermNode term = node as QueryTermNode;
                if (term != null) {
                    T value = term.Value as T;
                    if (value != null) {
                        yield return value;
                    }
                }
            }
        }

        private IEnumerable<T> SearchForValuesByBreadth<T> () where T : QueryValue
        {
            Queue<QueryNode> queue = new Queue<QueryNode> ();
            queue.Enqueue (this);
            do {
                QueryNode node = queue.Dequeue ();
                QueryListNode list = node as QueryListNode;
                if (list != null) {
                    foreach (QueryNode child in list.Children) {
                        queue.Enqueue (child);
                    }
                } else {
                    QueryTermNode term = node as QueryTermNode;
                    if (term != null) {
                        T value = term.Value as T;
                        if (value != null) {
                            yield return value;
                        }
                    }
                }
            } while (queue.Count > 0);
        }

        public IEnumerable<QueryField> GetFields ()
        {
            foreach (QueryTermNode term in GetTerms ())
                yield return term.Field;
        }

        public IEnumerable<QueryTermNode> GetTerms ()
        {
            Queue<QueryNode> queue = new Queue<QueryNode> ();
            queue.Enqueue (this);
            do {
                QueryNode node = queue.Dequeue ();
                QueryListNode list = node as QueryListNode;
                if (list != null) {
                    foreach (QueryNode child in list.Children) {
                        queue.Enqueue (child);
                    }
                } else {
                    QueryTermNode term = node as QueryTermNode;
                    if (term != null) {
                        yield return term;
                    }
                }
            } while (queue.Count > 0);
        }

        public override string ToString ()
        {
            return ToUserQuery ();
        }

        public abstract void AppendXml (XmlDocument doc, XmlNode parent, QueryFieldSet fieldSet);

        public virtual string ToSql (QueryFieldSet fieldSet)
        {
            StringBuilder sb = new StringBuilder ();
            AppendSql (sb, fieldSet);
            return sb.ToString ();
        }

        public abstract void AppendSql (StringBuilder sb, QueryFieldSet fieldSet);

        public QueryListNode Parent {
            get { return parent; }
            set { parent = value; }
        }

        public int SourceColumn {
            get { return source_column; }
            set { source_column = value; }
        }

        public int SourceLine {
            get { return source_line; }
            set { source_line = value; }
        }
    }
}
