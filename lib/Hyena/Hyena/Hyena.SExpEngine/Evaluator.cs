//
// Evaluator.cs
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
    public class Evaluator : EvaluatorBase
    {
        public Evaluator() : base()
        {
            RegisterFunctions();
        }

        public Evaluator(TreeNode expression) : base(expression)
        {
            RegisterFunctions();
        }

        public Evaluator(string expression) : base(expression)
        {
            RegisterFunctions();
        }

        private void RegisterFunctions()
        {
            RegisterFunctionSet(new ControlFunctionSet());
            RegisterFunctionSet(new LogicFunctionSet());
            RegisterFunctionSet(new FunctionFunctionSet());
            RegisterFunctionSet(new CastFunctionSet());
            RegisterFunctionSet(new CompareFunctionSet());
            RegisterFunctionSet(new ArithmeticFunctionSet());
            RegisterFunctionSet(new StringFunctionSet());
            RegisterFunctionSet(new UtilityFunctionSet());
            RegisterFunctionSet(new ListFunctionSet());
        }
    }
}
