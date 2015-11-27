//
// SelectionProxy.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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

namespace Hyena.Collections
{
    public class SelectionProxy
    {
        private Selection selection;

        public event EventHandler Changed;
        public event EventHandler SelectionChanged;
        public event EventHandler FocusChanged;

        public Selection Selection {
            get { return selection; }
            set {
                if (selection == value)
                    return;

                if (selection != null) {
                    selection.Changed -= HandleSelectionChanged;
                    selection.FocusChanged -= HandleSelectionFocusChanged;
                }

                selection = value;

                if (selection != null) {
                    selection.Changed += HandleSelectionChanged;
                    selection.FocusChanged += HandleSelectionFocusChanged;
                }

                OnSelectionChanged ();
            }
        }

        protected virtual void OnChanged ()
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (selection, EventArgs.Empty);
            }
        }

        protected virtual void OnFocusChanged ()
        {
            EventHandler handler = FocusChanged;
            if (handler != null) {
                handler (selection, EventArgs.Empty);
            }
        }

        protected virtual void OnSelectionChanged ()
        {
            EventHandler handler = SelectionChanged;
            if (handler != null) {
                handler (selection, EventArgs.Empty);
            }
        }

        private void HandleSelectionChanged (object o, EventArgs args)
        {
            OnChanged ();
        }

        private void HandleSelectionFocusChanged (object o, EventArgs args)
        {
            OnFocusChanged ();
        }
    }
}
