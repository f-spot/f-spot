//
// BaseListModel.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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

using Hyena.Collections;

namespace Hyena.Data
{
    public abstract class BaseListModel<T> : IListModel<T>
    {
        private Selection selection;

        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        public BaseListModel () : base ()
        {
        }

        protected virtual void OnCleared ()
        {
            Selection.MaxIndex = Count - 1;

            EventHandler handler = Cleared;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnReloaded ()
        {
            Selection.MaxIndex = Count - 1;

            EventHandler handler = Reloaded;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        public void RaiseReloaded ()
        {
            OnReloaded ();
        }

        public abstract void Clear();

        public abstract void Reload();

        public abstract T this[int index] { get; }

        public abstract int Count { get; }

        public virtual object GetItem (int index)
        {
            return this[index];
        }

        public virtual Selection Selection {
            get { return selection; }
            protected set { selection = value; }
        }

        protected ModelSelection<T> model_selection;
        public virtual ModelSelection<T> SelectedItems {
            get {
                return model_selection ?? (model_selection = new ModelSelection<T> (this, Selection));
            }
        }

        public T FocusedItem {
            get { return Selection.FocusedIndex == -1 ? default(T) : this[Selection.FocusedIndex]; }
        }

        private bool can_reorder = false;
        public bool CanReorder {
            get { return can_reorder; }
            set { can_reorder = value; }
        }
    }
}
