//
// FunctionSet.cs
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
using System.Reflection;

namespace Hyena.SExpEngine
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : Attribute
    {
        private string [] names;
        private bool evaluate_variables;

        public FunctionAttribute(params string [] names)
        {
            this.evaluate_variables = true;
            this.names = names;
        }

        public FunctionAttribute(bool evaluateVariables, params string [] names)
        {
            this.evaluate_variables = evaluateVariables;
            this.names = names;
        }

        public string [] Names {
            get { return names; }
        }

        public bool EvaluateVariables {
            get { return evaluate_variables; }
        }
    }

    public abstract class FunctionSet
    {
        private EvaluatorBase evaluator;

        public void Load(EvaluatorBase evaluator)
        {
            this.evaluator = evaluator;

            foreach(MethodInfo method in GetType().GetMethods()) {
                string [] names = null;
                bool evaluate_variables = true;

                foreach(Attribute attr in method.GetCustomAttributes(false)) {
                    if(attr is FunctionAttribute) {
                        names = (attr as FunctionAttribute).Names;
                        evaluate_variables = (attr as FunctionAttribute).EvaluateVariables;
                        break;
                    }
                }

                if(names == null || names.Length == 0) {
                    continue;
                }

                evaluator.RegisterFunction(this, method, names, evaluate_variables);
            }
        }

        public TreeNode Evaluate(TreeNode node)
        {
            return evaluator.Evaluate(node);
        }

        protected EvaluatorBase Evaluator {
            get { return evaluator; }
        }
    }
}
