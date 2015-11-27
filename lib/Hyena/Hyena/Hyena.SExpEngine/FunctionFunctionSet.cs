//
// FunctionFunctionSet.cs
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
    public class FunctionFunctionSet : FunctionSet
    {
        [Function(false, "set")]
        public virtual TreeNode OnSet(TreeNode [] args)
        {
            return VariableSet(Evaluator, args, true);
        }

        public static TreeNode VariableSet(EvaluatorBase evaluator, TreeNode var, TreeNode value)
        {
            return VariableSet(evaluator, new TreeNode[] { var, value }, true);
        }

        public static TreeNode VariableSet(EvaluatorBase evaluator, TreeNode [] args, bool update)
        {
            if(args.Length != 2) {
                throw new ArgumentException("must have two arguments");
            }

            if(!(args[0] is FunctionNode)) {
                throw new ArgumentException("first argument must be a variable");
            }

            FunctionNode variable_node = evaluator.ResolveFunction(args[0] as FunctionNode);
            if(variable_node != null) {
                variable_node.Body = evaluator.Evaluate(args[1]);
            } else {
                TreeNode parent = args[0].Parent;
                parent = parent.Parent ?? parent;

                parent.RegisterFunction((args[0] as FunctionNode).Function, evaluator.Evaluate(args[1]));
            }

            return new VoidLiteral();
        }

        [Function(false, "define")]
        public virtual TreeNode OnDefine(TreeNode [] args)
        {
            if(args.Length < 2 || args.Length > 3) {
                throw new ArgumentException("define must have two or three arguments");
            }

            if(!(args[0] is FunctionNode)) {
                throw new ArgumentException("first define argument must be a variable");
            }

            FunctionNode function = new FunctionNode((args[0] as FunctionNode).Function, args[args.Length - 1]);

            if(args.Length == 3 && args[1].HasChildren) {
                foreach(TreeNode function_arg in args[1].Children) {
                    if(!(function_arg is FunctionNode)) {
                        throw new ArgumentException("define function arguments must be variable tokens");
                    }

                    function.RegisterFunction((function_arg as FunctionNode).Function, new VoidLiteral());
                }
            }

            TreeNode parent = args[0].Parent;
            parent = parent.Parent ?? parent;

            parent.RegisterFunction(function.Function, function);

            return new VoidLiteral();
        }
    }
}
