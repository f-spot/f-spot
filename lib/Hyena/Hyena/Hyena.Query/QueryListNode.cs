//
// QueryListNode.cs
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
    public enum Keyword {
        Not,
        Or,
        And
    }

    public class QueryListNode : QueryNode
    {
        private List<QueryNode> children = new List<QueryNode>();
        private Keyword keyword;

        public QueryListNode(Keyword keyword) : base()
        {
            this.keyword = keyword;
        }

        public QueryListNode(Keyword keyword, QueryListNode parent) : base(parent)
        {
            this.keyword = keyword;
        }

        public void AddChild(QueryNode child)
        {
            child.Parent = this;
            children.Add(child);
        }

        public void RemoveChild(QueryNode child)
        {
            child.Parent = null;
            children.Remove(child);
        }

        public void TakeChildren (QueryListNode from)
        {
            foreach (QueryNode child in from.Children) {
                AddChild (child);
            }
            from.Children.Clear ();
        }

        public void ReplaceChild(QueryNode old_child, QueryNode new_child)
        {
            int index = children.IndexOf(old_child);
            if(index < 0) {
                throw new ApplicationException("old_child does not exist");
            }

            children.RemoveAt(index);
            children.Insert(index, new_child);
        }

        public void InsertChild(int index, QueryNode child)
        {
            child.Parent = this;
            children.Insert(index, child);
        }

        public int IndexOfChild(QueryNode child)
        {
            return children.IndexOf(child);
        }

        internal override void Dump(int depth)
        {
            PrintIndent(depth);
            Console.WriteLine("<{0}>", Keyword);

            foreach(QueryNode child in children) {
                child.Dump(depth + 1);
            }

            PrintIndent(depth);
            Console.WriteLine("</{0}>", Keyword);
        }

        public QueryNode GetLeftSibling(QueryNode node)
        {
            int index = IndexOfChild(node);
            if(index >= 1) {
                return Children[index - 1];
            }

            return null;
        }

        public QueryNode GetRightSibling(QueryNode node)
        {
            int index = IndexOfChild(node);
            if(index < 0 || index > ChildCount - 2) {
                return null;
            }
            return Children[index + 1];
        }

        public override QueryNode Trim ()
        {
            // Trim depth first
            List<QueryNode> copy = new List<QueryNode> (Children);
            foreach (QueryNode child in copy)
                child.Trim ();

            if (Keyword == Keyword.Not) {
                if (ChildCount != 1) {
                    if (Parent != null) {
                        Parent.RemoveChild (this);
                    } else {
                        return null;
                    }
                }
            } else {
                if (ChildCount <= 1) {
                    if (Parent != null) {
                        QueryListNode p = Parent;
                        p.RemoveChild (this);
                        p.TakeChildren (this);
                    } else if (ChildCount == 1) {
                        Children[0].Parent = null;
                        return Children[0];
                    }
                }
            }

            return this;
        }

        public override void AppendXml (XmlDocument doc, XmlNode parent, QueryFieldSet fieldSet)
        {
            if (ChildCount == 0)
                return;

            XmlElement node = doc.CreateElement (Keyword.ToString ().ToLower ());
            parent.AppendChild (node);
            foreach (QueryNode child in Children)
                child.AppendXml (doc, node, fieldSet);
        }

        public override void AppendUserQuery (StringBuilder sb)
        {
            if (ChildCount == 0)
                return;

            if (Keyword != Keyword.Not) {
                if (ChildCount > 1 && Parent != null)
                    sb.Append ("(");
                bool first = true;
                foreach (QueryNode child in Children) {
                    if (!first) {
                        if (Keyword == Keyword.Or) {
                            // FIXME translate
                            sb.Append (" or ");
                        } else {
                            sb.Append (" ");
                        }
                    } else {
                        first = false;
                    }
                    child.AppendUserQuery (sb);
                }
                if (ChildCount > 1 && Parent != null)
                    sb.Append (")");
            } else {
                sb.Append ("-");
                Children [0].AppendUserQuery (sb);
            }
        }

        public override void AppendSql (StringBuilder sb, QueryFieldSet fieldSet)
        {
            if (ChildCount == 0)
                return;

            if (Keyword != Keyword.Not) {
                sb.Append ("(");
                bool first = true;
                foreach (QueryNode child in Children) {
                    if (!first) {
                        sb.AppendFormat (" {0} ", Keyword);
                    } else {
                        first = false;
                    }
                    child.AppendSql (sb, fieldSet);
                }
                sb.Append (")");
            } else {
                sb.Append ("NOT(");
                Children [0].AppendSql (sb, fieldSet);
                sb.Append (")");
            }
        }

        public bool IsEmpty {
            get { return ChildCount == 0; }
        }

        public List<QueryNode> Children {
            get { return children; }
        }

        public QueryNode LastChild {
            get { return ChildCount > 0 ? children[ChildCount - 1] : null; }
        }

        public int ChildCount {
            get { return children.Count; }
        }

        public Keyword Keyword {
            get { return keyword; }
            set { keyword = value; }
        }
    }
}
