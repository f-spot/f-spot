//
// UndoManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

namespace Hyena
{
    public class UndoManager
    {
        private Stack<IUndoAction> undo_stack = new Stack<IUndoAction>();
        private Stack<IUndoAction> redo_stack = new Stack<IUndoAction>();
        private int frozen_count;
        private bool try_merge;

        public event EventHandler UndoChanged;

        public void Undo()
        {
            lock(this) {
                UndoRedo(undo_stack, redo_stack, true);
            }
        }

        public void Redo()
        {
            lock(this) {
                UndoRedo(redo_stack, undo_stack, false);
            }
        }

        public void Clear()
        {
            lock(this) {
                frozen_count = 0;
                try_merge = false;
                undo_stack.Clear();
                redo_stack.Clear();
                OnUndoChanged();
            }
        }

        public void AddUndoAction(IUndoAction action)
        {
            lock(this) {
                if(frozen_count != 0) {
                    return;
                }

                if(try_merge && undo_stack.Count > 0) {
                    IUndoAction top = undo_stack.Peek();
                    if(top.CanMerge(action)) {
                        top.Merge(action);
                        return;
                    }
                }

                undo_stack.Push(action);
                redo_stack.Clear();

                try_merge = true;

                OnUndoChanged();
            }
        }

        protected virtual void OnUndoChanged()
        {
            EventHandler handler = UndoChanged;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        private void UndoRedo(Stack<IUndoAction> pop_from, Stack<IUndoAction> push_to, bool is_undo)
        {
            if(pop_from.Count == 0) {
                return;
            }

            IUndoAction action = pop_from.Pop();

            frozen_count++;
            if(is_undo) {
                action.Undo();
            } else {
                action.Redo();
            }
            frozen_count--;

            push_to.Push(action);

            try_merge = true;

            OnUndoChanged();
        }

        public bool CanUndo {
            get { return undo_stack.Count > 0; }
        }

        public bool CanRedo {
            get { return redo_stack.Count > 0; }
        }

        public IUndoAction UndoAction {
            get {
                lock (this) {
                    return CanUndo ? undo_stack.Peek () : null;
                }
            }
        }

        public IUndoAction RedoAction {
            get {
                lock (this) {
                    return CanRedo ? redo_stack.Peek () : null;
                }
            }
        }
    }
}
