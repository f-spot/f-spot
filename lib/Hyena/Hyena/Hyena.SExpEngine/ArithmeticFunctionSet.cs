//
// ArithmeticFunctionSet.cs
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
    public class ArithmeticFunctionSet : FunctionSet
    {
        public enum ArithmeticOperation
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulo
        }

        public virtual TreeNode OnPerformArithmetic(TreeNode [] args, ArithmeticOperation operation)
        {
            double result = 0.0;
            bool as_int = true;

            for(int i = 0; i < args.Length; i++) {
                TreeNode arg = Evaluate(args[i]);

                if(arg is IntLiteral || arg is DoubleLiteral) {
                    double arg_value;

                    if(arg is DoubleLiteral) {
                        as_int = false;
                        arg_value = (arg as DoubleLiteral).Value;
                    } else {
                        arg_value = (int)(arg as IntLiteral).Value;
                    }

                    if(i == 0) {
                        result = arg_value;
                        continue;
                    }

                    switch(operation) {
                        case ArithmeticOperation.Add:
                            result += arg_value;
                            break;
                        case ArithmeticOperation.Subtract:
                            result -= arg_value;
                            break;
                        case ArithmeticOperation.Multiply:
                            result *= arg_value;
                            break;
                        case ArithmeticOperation.Divide:
                            result /= arg_value;
                            break;
                        case ArithmeticOperation.Modulo:
                            if(!(arg is IntLiteral)) {
                                throw new ArgumentException("Modulo requires int arguments");
                            }

                            result %= (int)arg_value;
                            break;
                    }
                } else {
                    throw new ArgumentException("arguments must be double or int");
                }
            }

            return as_int ?
                ((TreeNode)new IntLiteral((int)result)) :
                ((TreeNode)new DoubleLiteral(result));
        }

        [Function("add", "+")]
        public virtual TreeNode OnAdd(TreeNode [] args)
        {
            TreeNode first = Evaluate(args[0]);

            if(first is StringLiteral) {
                return StringFunctionSet.ConcatenateStrings(Evaluator, args);
            }

            return OnPerformArithmetic(args, ArithmeticOperation.Add);
        }

        [Function("sub", "-")]
        public virtual TreeNode OnSubtract(TreeNode [] args)
        {
            return OnPerformArithmetic(args, ArithmeticOperation.Subtract);
        }

        [Function("mul", "*")]
        public virtual TreeNode OnMultiply(TreeNode [] args)
        {
            return OnPerformArithmetic(args, ArithmeticOperation.Multiply);
        }

        [Function("div", "/")]
        public virtual TreeNode OnDivide(TreeNode [] args)
        {
            return OnPerformArithmetic(args, ArithmeticOperation.Divide);
        }

        [Function("mod", "%")]
        public virtual TreeNode OnModulo(TreeNode [] args)
        {
            return OnPerformArithmetic(args, ArithmeticOperation.Modulo);
        }

        [Function("++")]
        public virtual TreeNode OnIncrement(TreeNode [] args)
        {
            return IntegerUpdate(args, 1);
        }

        [Function("--")]
        public virtual TreeNode OnDecrement(TreeNode [] args)
        {
            return IntegerUpdate(args, -1);
        }

        private TreeNode IntegerUpdate(TreeNode [] args, int value)
        {
            TreeNode variable_node = (FunctionNode)args[0];
            TreeNode result = Evaluate(variable_node);
            TreeNode new_result = new IntLiteral(((IntLiteral)result).Value + value);

            FunctionFunctionSet.VariableSet(Evaluator, args[0], new_result);

            return new_result;
        }
    }
}
