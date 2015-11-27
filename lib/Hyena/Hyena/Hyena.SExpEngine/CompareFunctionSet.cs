//
// CompareFunctionSet.cs
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

namespace Hyena.SExpEngine
{
    public class CompareFunctionSet : FunctionSet
    {
        [Function("compare-to")]
        public virtual TreeNode OnCompareTo(TreeNode [] args)
        {
            if(args.Length != 2) {
                throw new ArgumentException("must have two arguments");
            }

            return Compare(Evaluator, args[0], args[1]);
        }

        public static TreeNode Compare(EvaluatorBase evaluator, TreeNode a, TreeNode b)
        {
            TreeNode arg_a = evaluator.Evaluate(a);
            TreeNode arg_b = evaluator.Evaluate(b);

            if(arg_a.GetType() != arg_b.GetType()) {
                throw new ArgumentException("arguments must be of the same type to compare");
            }

            int result = 0;

            if(arg_a is IntLiteral) {
                result = (arg_a as IntLiteral).Value.CompareTo(
                    (arg_b as IntLiteral).Value);
            } else if(arg_a is DoubleLiteral) {
                result = (arg_a as DoubleLiteral).Value.CompareTo(
                    (arg_b as DoubleLiteral).Value);
            } else if(arg_a is StringLiteral) {
                result = (arg_a as StringLiteral).Value.CompareTo(
                    (arg_b as StringLiteral).Value);
            } else if(arg_a is BooleanLiteral) {
                result = (arg_a as BooleanLiteral).Value.CompareTo(
                    (arg_b as BooleanLiteral).Value);
            } else {
                throw new ArgumentException("invalid type for comparison");
            }

            return new IntLiteral(result);
        }

        [Function("less-than", "<")]
        public virtual TreeNode OnCompareLessThan(TreeNode [] args)
        {
            IntLiteral result = (IntLiteral)OnCompareTo(args);
            return new BooleanLiteral(result.Value < 0);
        }

        [Function("greater-than", ">")]
        public virtual TreeNode OnCompareGreaterThan(TreeNode [] args)
        {
            IntLiteral result = (IntLiteral)OnCompareTo(args);
            return new BooleanLiteral(result.Value > 0);
        }

        [Function("equal", "=")]
        public virtual TreeNode OnCompareEqual(TreeNode [] args)
        {
            IntLiteral result = (IntLiteral)OnCompareTo(args);
            return new BooleanLiteral(result.Value == 0);
        }

        [Function("not-equal", "!=")]
        public virtual TreeNode OnCompareNotEqual(TreeNode [] args)
        {
            BooleanLiteral result = (BooleanLiteral)OnCompareEqual(args);
            return new BooleanLiteral(!result.Value);
        }

        [Function("less-than-or-equal", "<=")]
        public virtual TreeNode OnCompareLessThanOrEqual(TreeNode [] args)
        {
            BooleanLiteral a = (BooleanLiteral)OnCompareLessThan(args);
            BooleanLiteral b = (BooleanLiteral)OnCompareEqual(args);
            return new BooleanLiteral(a.Value || b.Value);
        }

        [Function("greater-than-or-equal", ">=")]
        public virtual TreeNode OnCompareGreaterThanOrEqual(TreeNode [] args)
        {
            BooleanLiteral a = (BooleanLiteral)OnCompareGreaterThan(args);
            BooleanLiteral b = (BooleanLiteral)OnCompareEqual(args);
            return new BooleanLiteral(a.Value || b.Value);
        }
    }
}
