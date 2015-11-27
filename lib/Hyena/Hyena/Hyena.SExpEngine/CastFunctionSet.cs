//
// CastFunctionSet.cs
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
    public class CastFunctionSet : FunctionSet
    {
        [Function("cast-double")]
        public virtual TreeNode OnCastDouble(TreeNode [] args)
        {
            if(args.Length != 1) {
                throw new ArgumentException("cast must have only one argument");
            }

            TreeNode arg = Evaluate(args[0]);

            if(arg is DoubleLiteral) {
                return arg;
            } else if(arg is IntLiteral) {
                return new DoubleLiteral((int)(arg as IntLiteral).Value);
            } else if(arg is StringLiteral) {
                return new DoubleLiteral(Convert.ToDouble((arg as StringLiteral).Value));
            }

            throw new ArgumentException("can only cast double, int, or string literals");
        }

        [Function("cast-int")]
        public virtual TreeNode OnCastInt(TreeNode [] args)
        {
            DoubleLiteral result = (DoubleLiteral)OnCastDouble(args);
            return new IntLiteral((int)result.Value);
        }

        [Function("cast-bool")]
        public virtual TreeNode OnCastBool(TreeNode [] args)
        {
            DoubleLiteral result = (DoubleLiteral)OnCastDouble(args);
            return new BooleanLiteral((int)result.Value != 0);
        }

        [Function("cast-string")]
        public virtual TreeNode OnCastString(TreeNode [] args)
        {
            if(args.Length != 1) {
                throw new ArgumentException("cast must have only one argument");
            }

            TreeNode arg = Evaluate(args[0]);

            if(arg is DoubleLiteral) {
                return new StringLiteral(Convert.ToString((arg as DoubleLiteral).Value));
            } else if(arg is IntLiteral) {
                return new StringLiteral(Convert.ToString((arg as IntLiteral).Value));
            } else if(arg is StringLiteral) {
                return arg;
            }

            throw new ArgumentException("can only cast double, int, or string literals");
        }
    }
}
