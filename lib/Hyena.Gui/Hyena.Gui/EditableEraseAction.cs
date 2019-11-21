//
// EditableEraseAction.cs
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
using Gtk;

using Hyena;

namespace Hyena.Gui
{
    internal class EditableEraseAction : IUndoAction
    {
        private Editable editable;
        private string text;
        private int start;
        private int end;
        private bool is_forward;
        private bool is_cut;

        public EditableEraseAction (Editable editable, int start, int end)
        {
            this.editable = editable;
            this.text = editable.GetChars (start, end);
            this.start = start;
            this.end = end;
            this.is_cut = end - start > 1;
            this.is_forward = editable.Position < start;
        }

        public void Undo ()
        {
            int start_r = start;
            editable.InsertText (text, ref start_r);
            editable.Position = is_forward ? start_r : end;
        }

        public void Redo ()
        {
            editable.DeleteText (start, end);
            editable.Position = start;
        }

        public void Merge (IUndoAction action)
        {
            EditableEraseAction erase = (EditableEraseAction)action;
            if (start == erase.start) {
                text += erase.text;
                end += erase.end - erase.start;
            } else {
                text = erase.text + text;
                start = erase.start;
            }
        }

        public bool CanMerge (IUndoAction action)
        {
            EditableEraseAction erase = action as EditableEraseAction;
            if (erase == null) {
                return false;
            }

            return !(
                is_cut || erase.is_cut ||                          // don't group separate text cuts
                start != (is_forward ? erase.start : erase.end) || // must meet eachother
                is_forward != erase.is_forward ||                  // don't group deletes with backspaces
                text[0] == '\n' ||                                 // don't group more than one line (inclusive)
                erase.text[0] == ' ' || erase.text[0] == '\t'      // don't group more than one word (exclusive)
            );
        }

        public override string ToString ()
        {
            return String.Format ("Erased: [{0}] ({1},{2})", text, start, end);
        }
    }
}
