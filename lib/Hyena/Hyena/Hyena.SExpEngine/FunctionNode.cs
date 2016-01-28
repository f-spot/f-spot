//
// FunctionNode.cs
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
using System.Collections.Generic;

namespace Hyena.SExpEngine
{
    public class InvalidFunctionException : Exception
    {
        public InvalidFunctionException(string message) : base(message)
        {
        }
    }

    public class FunctionNode : TreeNode
    {
        private string function;
        private object body;

        public FunctionNode(string function)
        {
            this.function = function;
        }

        public FunctionNode(string function, object body)
        {
            this.function = function;
            this.body = body;
        }

        public TreeNode Evaluate(EvaluatorBase evaluator, TreeNode [] args)
        {
            if(args != null && args.Length != FunctionCount && RequiresArguments) {
                throw new ArgumentException("Function " + function + " takes "
                    + FunctionCount + " arguments, not " + args.Length);
            }

            if(args != null && RequiresArguments) {
                int i = 0;
                string [] names = new string[args.Length];

                foreach(KeyValuePair<string, FunctionNode> var in Functions) {
                    names[i++] = var.Key;
                }

                for(i = 0; i < args.Length; i++) {
                    (body as TreeNode).RegisterFunction(names[i], evaluator.Evaluate(args[i]));
                }
            }

            return evaluator.Evaluate(ResolveBody(evaluator));
        }

        private TreeNode ResolveBody(EvaluatorBase evaluator)
        {
            if(body == null) {
                throw new UnknownVariableException(Function);
            }

            if(body is string) {
                return new StringLiteral((string)body);
            } else if(body is double) {
                return new DoubleLiteral((double)body);
            } else if(body is int) {
                return new IntLiteral((int)body);
            } else if(body is bool) {
                return new BooleanLiteral((bool)body);
            } else if(body is SExpVariableResolutionHandler) {
                return ((SExpVariableResolutionHandler)body)(this);
            } else if(body is TreeNode) {
                return evaluator.Evaluate((TreeNode)body);
            }

            throw new UnknownVariableException(String.Format(
                "Unknown function type `{0}' for function `{1}'",
                body.GetType(), Function));
        }

        public override string ToString()
        {
            return Function;
        }

        internal object Body {
            get { return body; }
            set { body = value; }
        }

        internal bool RequiresArguments {
            get { return FunctionCount > 0 && body is TreeNode; }
        }

        public string Function {
            get { return function; }
        }
    }
}
