//
// Parser.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Text.RegularExpressions;
using System.Globalization;

namespace Hyena.SExpEngine
{
    public class ParserException : ApplicationException
    {
        public ParserException(string token, int line, int col, Exception inner) : base(String.Format(
            "Parser exception at token `{0}' [{1},{2}]: {3}",
            token, line, col, inner == null ? "Unknown error" : inner.Message), inner)
        {
        }
    }

    public class Parser
    {
        private static Regex number_regex = new Regex(@"^[-+]?(0x[\dA-Fa-f]+)?[\d]*\.?[\d]*([eE][-+]?[\d]+)?$");
        private static System.Globalization.CultureInfo culture_info = new System.Globalization.CultureInfo("en-US");

        private StreamReader reader;
        private bool debug;

        private StringBuilder current_token;
        private TreeNode root_node ;
        private TreeNode current_parent;
        private int scope;
        private int line;
        private int column;

        public Parser()
        {
        }

        public TreeNode Parse(string input)
        {
            return Parse(new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        public TreeNode Parse(Stream stream)
        {
            return Parse(new StreamReader(stream));
        }

        public TreeNode Parse(StreamReader reader)
        {
            this.reader = reader;

            current_token = new StringBuilder();
            root_node = new TreeNode();
            current_parent = root_node;
            scope = 0;
            line = 1;
            column = 0;

            try {
                Tokenize();

                if(scope != 0) {
                    throw new ApplicationException("Scope does pop back to zero");
                }
            } catch(Exception e) {
                throw new ParserException(current_token.ToString(), line, column, e);
            }

            return root_node;
        }

        private void Tokenize()
        {
            bool in_string = false;
            bool in_comment = false;

            while(true) {
                int ich = reader.Read();
                char ch = (char)ich;

                if(ich < 0) {
                    break;
                }

                if(ch == '\n') {
                    line++;
                    column = 0;
                } else {
                    column++;
                }

                if(in_comment) {
                    if(ch == '\r' || ch == '\n') {
                        in_comment = false;
                    }

                    continue;
                }

                switch(ch) {
                    case '(':
                    case ')':
                        if(!in_string) {
                            TokenPush(false);

                            if(ch == '(') {
                                ScopePush();
                            } else {
                                ScopePop();
                            }
                        } else {
                            current_token.Append(ch);
                        }
                        break;
                    case '"':
                        if(in_string) {
                            in_string = false;
                            TokenPush(true);
                        } else {
                            in_string = true;
                        }
                        break;
                    case '\\':
                        if((char)reader.Peek() == '"') {
                            current_token.Append((char)reader.Read());
                        }
                        break;
                    case ';':
                        if(in_string) {
                            current_token.Append(ch);
                        } else {
                            TokenPush(false);
                            in_comment = true;
                        }
                        break;
                    default:
                        if(Char.IsWhiteSpace(ch)) {
                            if(in_string) {
                                current_token.Append(ch);
                            } else {
                                TokenPush(false);
                            }
                        } else {
                            current_token.Append(ch);
                        }
                        break;
                }
            }

            TokenPush(false);

            reader.Close();
        }

        private void ScopePush()
        {
            current_parent = new TreeNode(current_parent);
            scope++;
        }

        private void ScopePop()
        {
            current_parent = current_parent.Parent;
            scope--;
        }

        private void TokenPush(bool as_string)
        {
            if(current_token.Length == 0 && !as_string) {
                return;
            }

            TreeNode node = null;
            string token = current_token.ToString();

            if(Debug) {
                Console.Write("{3}[{0}] TOKEN({4},{5}): [{2}{1}{2}]", scope, token,
                    as_string ? "\"" : String.Empty, String.Empty.PadLeft(scope - 1, ' '),
                    line, column - current_token.Length);
            }

            if(as_string) {
                node = new StringLiteral(token);
            } else if(token == "#t") {
                node = new BooleanLiteral(true);
            } else if(token == "#f") {
                node = new BooleanLiteral(false);
            } else if(token.Length > 0 && token != "." && token != "-" &&
                token != "+" && number_regex.IsMatch(token)) {
                try {
                    if(token.StartsWith("0x") || token.StartsWith("-0x")) {
                        int offset = token[0] == '-' ? 3 : 2;
                        int value = Int32.Parse(token.Substring(offset),
                            NumberStyles.HexNumber, culture_info.NumberFormat);
                        node = new IntLiteral(value * (offset == 3 ? -1 : 1));
                    } else if(token.Contains(".")) {
                        node = new DoubleLiteral(Double.Parse(token,
                            NumberStyles.Float, culture_info.NumberFormat));
                    } else {
                        node = new IntLiteral(Int32.Parse(token,
                            NumberStyles.Integer, culture_info.NumberFormat));
                    }
                } catch {
                    throw new FormatException("Invalid number format: " + token);
                }
            } else {
                node = new FunctionNode(token);
            }

            if(Debug) {
                Console.WriteLine(" => [{0}]", node);
            }

            node.Line = line;
            node.Column = column;
            current_parent.AddChild(node);

            current_token.Remove(0, current_token.Length);
        }

        public bool Debug {
            get { return debug; }
            set { debug = value; }
        }
    }
}
