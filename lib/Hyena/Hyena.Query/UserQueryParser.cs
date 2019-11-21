//
// UserQueryParser.cs
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
using System.IO;
using System.Text;

namespace Hyena.Query
{
    public class UserQueryParser : QueryParser
    {
        private QueryListNode current_parent;

        private char peek;
        private int current_column;
        private int current_line;
        private int token_start_column;
        private int token_start_line;
        private bool eos_consumed;

        private QueryListNode root;
        private QueryFieldSet field_set;

        public static QueryNode Parse (string input, QueryFieldSet fieldSet)
        {
            return new UserQueryParser (input).BuildTree (fieldSet);
        }

        public UserQueryParser () : base () {}
        public UserQueryParser (string inputQuery) : base (inputQuery) {}
        public UserQueryParser (Stream stream) : base (stream) {}
        public UserQueryParser (StreamReader reader) : base (reader) {}

        public override QueryNode BuildTree (QueryFieldSet fieldSet)
        {
            field_set = fieldSet;
            root = current_parent = new QueryListNode (Keyword.And);
            bool last_was_term = false;

            while (true) {
                QueryToken token = Scan ();

                if (token.ID == TokenID.Unknown) {
                    break;
                }

                token.Column = token_start_column;
                token.Line = token_start_line;

                // If we have two terms in a row, put an AND between them
                if (last_was_term && token.ID == TokenID.Term)
                    ParseToken (new QueryToken (TokenID.And));

                ParseToken (token);

                last_was_term = token.ID == TokenID.Term;
            }

            return root.Trim ();
        }

        private void DepthPush ()
        {
            current_parent = new QueryListNode (Keyword.And, current_parent);
        }

        private void DepthPop ()
        {
            // Avoid trying to pop more than is possible
            if (current_parent.Parent != null)
                current_parent = current_parent.Parent;
        }

        private void NodePush (QueryNode node)
        {
            if (current_parent == null && node is QueryListNode) {
                root = current_parent = node as QueryListNode;
                return;
            }

            if (current_parent.Keyword == Keyword.Not && current_parent.ChildCount == 1)
                DepthPop ();

            current_parent.AddChild (node);

            // If the node is a list, it's our new parent
            QueryListNode list = node as QueryListNode;
            if (list != null) {
                current_parent = list;
            }
        }

        private void ParseToken (QueryToken token)
        {
            switch (token.ID) {
                case TokenID.OpenParen:
                    DepthPush ();
                    break;

                case TokenID.CloseParen:
                    DepthPop ();
                    break;

                case TokenID.Not:
                    NodePush (new QueryListNode (Keyword.Not));
                    break;

                case TokenID.Or:
                case TokenID.And:
                    // Only push a node if the current_parent is not the same as this token
                    if (current_parent.Keyword == Keyword.Not ||
                            current_parent.Keyword == (token.ID == TokenID.Or ? Keyword.And : Keyword.Or)) {

                        QueryListNode list = new QueryListNode (token.ID == TokenID.Or ? Keyword.Or : Keyword.And);
                        QueryListNode p = current_parent.Parent;

                        if (p != null) {
                            current_parent.Parent.RemoveChild (current_parent);
                        }

                        if (current_parent.Keyword == Keyword.Not || current_parent.ChildCount > 1) {
                            list.AddChild (current_parent);
                        } else {
                            list.TakeChildren (current_parent);
                        }

                        current_parent = p;
                        NodePush (list);
                    }
                    break;

                case TokenID.Term:
                    NodePush (QueryTermNode.ParseUserQuery (field_set, token.Term));
                    break;
            }
        }

        private QueryToken Scan ()
        {
            if (reader.EndOfStream) {
                if (eos_consumed)
                    return new QueryToken (TokenID.Unknown);
                else
                    eos_consumed = true;
            }

            for (; ; ReadChar ()) {
                if (Char.IsWhiteSpace (peek) && peek != '\n') {
                    continue;
                } else if (peek == '\n') {
                    current_line++;
                    current_column = 0;
                } else {
                    break;
                }
            }

            token_start_column = current_column;
            token_start_line = current_line;

            if (peek == '(') {
                ReadChar ();
                return new QueryToken (TokenID.OpenParen);
            } else if (peek == ')') {
                ReadChar ();
                return new QueryToken (TokenID.CloseParen);
            } else if (peek == '-') {
                ReadChar ();
                return new QueryToken (TokenID.Not);
            } else if (peek == '|' || peek == ',') {
                ReadChar ();
                return new QueryToken (TokenID.Or);
            } else {
                string token = ScanString ();

                if (reader.EndOfStream)
                    eos_consumed = true;

                switch (token) {
                    case "or":
                    case "OR":
                        return new QueryToken (TokenID.Or);
                    case "NOT":
                        return new QueryToken (TokenID.Not);
                    default:
                        return new QueryToken (token);
                }
            }
        }

        // TODO: Allow white space before/after term operators

        private bool IsStringTerminationChar (char ch)
        {
            return Char.IsWhiteSpace (ch) || ch == '(' || ch == ')' || ch == '|' || ch == ',';
        }

        private string ScanString ()
        {
            StringBuilder buffer = new StringBuilder ();
            bool in_string = false;

            while (true) {
                if (!in_string && IsStringTerminationChar (peek)) {
                    break;
                } else if (peek == '"') {
                    in_string = !in_string;
                } else {
                    buffer.Append (peek);
                    if (reader.EndOfStream) {
                        break;
                    }
                }

                ReadChar ();

                if (reader.EndOfStream) {
                    if (!IsStringTerminationChar (peek) && peek != '"') {
                        buffer.Append (peek);
                    }

                    break;
                }
            }

            return buffer.ToString ();
        }

        public override void Reset ()
        {
            peek = ' ';
            current_column = 0;
            current_line = 0;
            token_start_column = 0;
            token_start_line = 0;
        }

        private void ReadChar ()
        {
            if (peek == Char.MinValue) {
                return;
            }

            peek = (char)reader.Read ();
            current_column++;
        }
    }
}
