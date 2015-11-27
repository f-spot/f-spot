//
// StringFunctionSet.cs
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
using System.Text;
using System.Text.RegularExpressions;

namespace Hyena.SExpEngine
{
    public class StringFunctionSet : FunctionSet
    {
        [Function("string-concat")]
        public virtual TreeNode OnConcatenateStrings(TreeNode [] args)
        {
            return ConcatenateStrings(Evaluator, args);
        }

        public static TreeNode ConcatenateStrings(EvaluatorBase evaluator, TreeNode [] args)
        {
            StringBuilder result = new StringBuilder();

            foreach(TreeNode arg in args) {
                TreeNode eval_arg = evaluator.Evaluate(arg);
                if(!(eval_arg is VoidLiteral)) {
                    result.Append(eval_arg);
                }
            }

            return new StringLiteral(result.ToString());
        }

        private void CheckArgumentCount(TreeNode [] args, int expected)
        {
            CheckArgumentCount(args, expected, expected);
        }

        private void CheckArgumentCount(TreeNode [] args, int expected_min, int expected_max)
        {
            if(args.Length < expected_min || args.Length > expected_max) {
                throw new ArgumentException("expects " + expected_min + " <= args <= "
                    + expected_max + " arguments");
            }
        }

        private string GetArgumentString(TreeNode [] args, int index)
        {
            TreeNode node = Evaluate(args[index]);
            if(!(node is StringLiteral)) {
                throw new ArgumentException("argument " + index + " must be a string");
            }

            return (node as StringLiteral).Value;
        }

        private int GetArgumentInteger(TreeNode [] args, int index)
        {
            TreeNode node = Evaluate(args[index]);
            if(!(node is IntLiteral)) {
                throw new ArgumentException("argument " + index + " must be an integer");
            }

            return (node as IntLiteral).Value;
        }

        [Function("length")]
        public virtual TreeNode OnLength(TreeNode [] args)
        {
            CheckArgumentCount(args, 1);

            TreeNode node = Evaluate(args[0]);

            if(node is StringLiteral) {
                return new IntLiteral((node as StringLiteral).Value.Length);
            } else if(!(node is LiteralNodeBase) && !(node is FunctionNode)) {
                return new IntLiteral(node.ChildCount);
            }

            return new IntLiteral(0);
        }

        [Function("contains")]
        public virtual TreeNode OnContains(TreeNode [] args)
        {
            CheckArgumentCount(args, 2);
            return new BooleanLiteral(GetArgumentString(args, 0).Contains(GetArgumentString(args, 1)));
        }

        [Function("index-of")]
        public virtual TreeNode OnIndexOf(TreeNode [] args)
        {
            CheckArgumentCount(args, 2);
            return new IntLiteral(GetArgumentString(args, 0).IndexOf(GetArgumentString(args, 1)));
        }

        [Function("last-index-of")]
        public virtual TreeNode OnLastIndexOf(TreeNode [] args)
        {
            CheckArgumentCount(args, 2);
            return new IntLiteral(GetArgumentString(args, 0).LastIndexOf(GetArgumentString(args, 1)));
        }

        [Function("starts-with")]
        public virtual TreeNode OnStartsWith(TreeNode [] args)
        {
            CheckArgumentCount(args, 2);
            return new BooleanLiteral(GetArgumentString(args, 0).StartsWith(GetArgumentString(args, 1)));
        }

        [Function("ends-with")]
        public virtual TreeNode OnEndsWith(TreeNode [] args)
        {
            CheckArgumentCount(args, 2);
            return new BooleanLiteral(GetArgumentString(args, 0).EndsWith(GetArgumentString(args, 1)));
        }

        [Function("substring")]
        public virtual TreeNode OnSubstring(TreeNode [] args)
        {
            CheckArgumentCount(args, 2, 3);
            if(args.Length == 2) {
                return new StringLiteral(GetArgumentString(args, 0).Substring(GetArgumentInteger(args, 1)));
            } else {
                return new StringLiteral(GetArgumentString(args, 0).Substring(
                    GetArgumentInteger(args, 1), GetArgumentInteger(args, 2)));
            }
        }

        [Function("split")]
        public virtual TreeNode OnSplit(TreeNode [] args)
        {
            CheckArgumentCount(args, 2);

            TreeNode result = new TreeNode();
            string str = GetArgumentString(args, 0);
            string pattern = GetArgumentString(args, 1);

            foreach(string item in Regex.Split(str, pattern)) {
                result.AddChild(new StringLiteral(item));
            }

            return result;
        }

        [Function("trim")]
        public virtual TreeNode OnTrim(TreeNode [] args)
        {
            CheckArgumentCount(args, 1);
            return new StringLiteral(GetArgumentString(args, 0).Trim());
        }
    }
}
