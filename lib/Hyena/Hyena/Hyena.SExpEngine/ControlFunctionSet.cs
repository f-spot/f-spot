//
// ControlFunctionSet.cs
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
    public class ControlFunctionSet : FunctionSet
    {
        [Function("if")]
        public virtual TreeNode OnIf(TreeNode [] args)
        {
            if(args == null || args.Length < 2 || args.Length > 3) {
                throw new ArgumentException("if accepts 2 or 3 arguments");
            }

            TreeNode arg = Evaluate(args[0]);
            if(!(arg is BooleanLiteral)) {
                throw new ArgumentException("first if argument must be boolean");
            }

            BooleanLiteral conditional = (BooleanLiteral)arg;

            if(conditional.Value) {
                return Evaluate(args[1]);
            } else if(args.Length == 3) {
                return Evaluate(args[2]);
            }

            return new VoidLiteral();
        }

        [Function("while")]
        public virtual TreeNode OnWhile(TreeNode [] args)
        {
            if(args == null || args.Length < 1 || args.Length > 2) {
                throw new ArgumentException("while accepts a condition and an expression or just an expression");
            }

            while(true) {
                if(args.Length == 2) {
                    TreeNode result = Evaluate(args[0]);
                    if(!(result is BooleanLiteral)) {
                        throw new ArgumentException("condition is not boolean");
                    }

                    if(!(result as BooleanLiteral).Value) {
                        break;
                    }
                }

                try {
                    Evaluate(args[args.Length - 1]);
                } catch(Exception e) {
                    if(BreakHandler(e)) {
                        break;
                    }
                }
            }

            return new VoidLiteral();
        }

        [Function("break")]
        public virtual TreeNode OnBreak(TreeNode [] args)
        {
            throw new BreakException();
        }

        private class BreakException : Exception
        {
            public BreakException()
            {
            }
        }

        public static bool BreakHandler(Exception e)
        {
            Exception parent_e = e;

            while(parent_e != null) {
                if(parent_e is BreakException) {
                    return true;
                }
                parent_e = parent_e.InnerException;
            }

            throw e;
        }
    }
}
