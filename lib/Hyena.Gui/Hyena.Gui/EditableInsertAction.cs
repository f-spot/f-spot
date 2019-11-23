//
// EditableInsertAction.cs
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
    internal class EditableInsertAction : IUndoAction
    {
        private Editable editable;
        private string text;
        private int index;
        private bool is_paste;

        public EditableInsertAction (Editable editable, int start, string text, int length)
        {
            this.editable = editable;
            this.text = text;
            this.index = start;
            this.is_paste = length > 1;
        }

        public void Undo ()
        {
            editable.DeleteText (index, index + text.Length);
            editable.Position = index;
        }

        public void Redo ()
        {
            int index_r = index;
            editable.InsertText (text, ref index_r);
            editable.Position = index_r;
        }

        public void Merge (IUndoAction action)
        {
            text += ((EditableInsertAction)action).text;
        }

        public bool CanMerge (IUndoAction action)
        {
            EditableInsertAction insert = action as EditableInsertAction;
            if (insert == null || String.IsNullOrEmpty (text)) {
                return false;
            }

            return !(
               is_paste || insert.is_paste ||                  // Don't group text pastes
               insert.index != index + text.Length ||          // Must meet eachother
               text[0] == '\n' ||                              // Don't group more than one line (inclusive)
               insert.text[0] == ' ' || insert.text[0] == '\t' // Don't group more than one word (exclusive)
            );
        }

        public override string ToString ()
        {
            return String.Format ("Inserted: [{0}] ({1})", text, index);
        }
    }
}
