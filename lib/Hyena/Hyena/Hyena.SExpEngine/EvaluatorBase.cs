//
// EvaluatorBase.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Hyena.SExpEngine
{
    public delegate TreeNode SExpFunctionHandler(EvaluatorBase evaluator, TreeNode [] args);
    public delegate TreeNode SExpVariableResolutionHandler(TreeNode node);

    public class EvaluationException : ApplicationException
    {
        public EvaluationException(TreeNode node, string token, Exception inner) : base(String.Format(
            "Evaluation exception at token `{0} ({1})' [{2},{3}]",
            node.GetType(), token, node.Line, node.Column), inner)
        {
        }
    }

    public class UnknownVariableException : ApplicationException
    {
        public UnknownVariableException(string var) : base(var)
        {
        }
    }

    public class EvaluatorBase
    {
        private class MethodInfoContainer
        {
            public object Object;
            public MethodInfo MethodInfo;
            public bool EvaluateVariables;
        }

        private TreeNode expression;
        private TreeNode function_table_expression = new TreeNode();

        private string input;
        private Dictionary<string, object> functions = new Dictionary<string, object>();
        private List<Exception> exceptions = new List<Exception>();

        public EvaluatorBase()
        {
            expression = function_table_expression;
        }

        public EvaluatorBase(TreeNode expression)
        {
            this.expression = expression;
        }

        public EvaluatorBase(string input)
        {
            expression = function_table_expression;
            this.input = input;
        }

        public void RegisterVariable(string name, string value)
        {
            expression.RegisterFunction(name, value);
        }

        public void RegisterVariable(string name, bool value)
        {
            expression.RegisterFunction(name, value);
        }

        public void RegisterVariable(string name, int value)
        {
            expression.RegisterFunction(name, value);
        }

        public void RegisterVariable(string name, double value)
        {
            expression.RegisterFunction(name, value);
        }

        public void RegisterVariable(string name, SExpVariableResolutionHandler value)
        {
            expression.RegisterFunction(name, value);
        }

        public void RegisterVariable(string name, TreeNode value)
        {
            expression.RegisterFunction(name, value);
        }

        public void RegisterFunction(SExpFunctionHandler handler, params string [] names)
        {
            foreach(string name in names) {
                if(functions.ContainsKey(name)) {
                    functions.Remove(name);
                }

                functions.Add(name, handler);
            }
        }

        public void RegisterFunction(object o, MethodInfo method, string [] names)
        {
            RegisterFunction(o, method, names, true);
        }

        public void RegisterFunction(object o, MethodInfo method, string [] names, bool evaluateVariables)
        {
            MethodInfoContainer container = new MethodInfoContainer();
            container.MethodInfo = method;
            container.Object = o;
            container.EvaluateVariables = evaluateVariables;

            foreach(string name in names) {
                if(functions.ContainsKey(name)) {
                    functions.Remove(name);
                }

                functions.Add(name, container);
            }
        }

        public void RegisterFunctionSet(FunctionSet functionSet)
        {
            functionSet.Load(this);
        }

        public TreeNode EvaluateTree(TreeNode expression)
        {
            this.expression = expression;
            this.input = null;
            return Evaluate();
        }

        public TreeNode EvaluateString(string input)
        {
            this.expression = null;
            this.input = input;
            return Evaluate();
        }

        public TreeNode Evaluate()
        {
            exceptions.Clear();

            try {
                if(expression == null) {
                    Parser parser = new Parser();
                    expression = parser.Parse(input);
                    expression.CopyFunctionsFrom(function_table_expression);
                }

                return Evaluate(expression);
            } catch(Exception e) {
                Exception next = e;

                do {
                    if(next != null) {
                        exceptions.Add(next);
                        next = next.InnerException;
                    }
                } while(next != null && next.InnerException != null);

                if(next != null) {
                    exceptions.Add(next);
                }
            }

            return null;
        }

        public TreeNode Evaluate(TreeNode node)
        {
            if(!node.HasChildren || node is FunctionNode) {
                return EvaluateNode(node);
            }

            TreeNode final_result_node = new TreeNode();

            foreach(TreeNode child_node in node.Children) {
                TreeNode result_node = EvaluateNode(child_node);

                if(result_node != null) {
                    final_result_node.AddChild(result_node);
                }

                if(child_node is FunctionNode) {
                    if(functions.ContainsKey((child_node as FunctionNode).Function)) {
                        break;
                    }

                    FunctionNode impl = ResolveFunction(child_node as FunctionNode);
                    if(impl != null && impl.RequiresArguments) {
                        break;
                    }
                }
            }

            return final_result_node.ChildCount == 1 ? final_result_node.Children[0] : final_result_node;
        }

        private TreeNode EvaluateNode(TreeNode node)
        {
            TreeNode result_node = null;

            if(node is FunctionNode) {
                try {
                    result_node = EvaluateFunction(node as FunctionNode);
                } catch(Exception e) {
                    Exception ee = e;
                    if(e is TargetInvocationException) {
                        ee = e.InnerException;
                    }

                    throw new EvaluationException(node, (node as FunctionNode).Function, ee);
                }
            } else if(node is LiteralNodeBase) {
                result_node = node;
            } else {
                result_node = Evaluate(node);
            }

            return result_node;
        }

        private TreeNode EvaluateFunction(FunctionNode node)
        {
            object handler = null;

            TreeNode parent = node.Parent;
            TreeNode [] args = null;

            if(!functions.ContainsKey(node.Function)) {
                handler = ResolveFunction(node);
                if(handler == null) {
                    throw new InvalidFunctionException(node.Function);
                }
            } else {
                handler = functions[node.Function];
            }

            if(parent.Children[0] == node) {
                args = new TreeNode[parent.ChildCount - 1];

                for(int i = 0; i < args.Length; i++) {
                    args[i] = parent.Children[i + 1];

                    if(handler is MethodInfoContainer && !(handler as MethodInfoContainer).EvaluateVariables) {
                        continue;
                    }
                }
            }

            if(handler is FunctionNode) {
                return (handler as FunctionNode).Evaluate(this, args);
            } else if(handler is SExpFunctionHandler) {
                return ((SExpFunctionHandler)handler)(this, args);
            } else if(handler is MethodInfoContainer) {
                MethodInfoContainer container = (MethodInfoContainer)handler;
                return (TreeNode)container.MethodInfo.Invoke(container.Object, new object [] { args });
            } else {
                throw new InvalidFunctionException(String.Format(
                    "Unknown runtime method handler type {1}", handler.GetType()));
            }
        }

        internal FunctionNode ResolveFunction(FunctionNode node)
        {
            TreeNode shift_node = node;

            do {
                if(shift_node.Functions.ContainsKey(node.Function)) {
                    return shift_node.Functions[node.Function];
                }

                shift_node = shift_node.Parent;
            } while(shift_node != null);

            return null;
        }

        public bool Success {
            get { return exceptions.Count == 0; }
        }

        public TreeNode ExpressionTree {
            get { return expression; }
        }

        public ReadOnlyCollection<Exception> Exceptions {
            get { return new ReadOnlyCollection<Exception>(exceptions); }
        }

        public string ErrorMessage {
            get {
                if(exceptions.Count == 0) {
                    return null;
                } else if(exceptions.Count >= 2) {
                    return String.Format("{0}: {1}", exceptions[exceptions.Count - 2].Message,
                        exceptions[exceptions.Count - 1].Message);
                }

                return exceptions[0].Message;
            }
        }
    }
}
