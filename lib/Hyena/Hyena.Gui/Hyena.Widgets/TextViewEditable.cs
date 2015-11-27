//
// TextViewEditable.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

namespace Hyena.Widgets
{
    public class TextViewEditable : TextView, Editable
    {
        public TextViewEditable ()
        {
            Buffer.Changed += OnBufferChanged;
            Buffer.InsertText += OnBufferInsertText;
            Buffer.DeleteRange += OnBufferDeleteRange;
        }

        public event EventHandler Changed;
        public event TextDeletedHandler TextDeleted;
        public event TextInsertedHandler TextInserted;

        private void OnBufferChanged (object o, EventArgs args)
        {
            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnBufferInsertText (object o, InsertTextArgs args)
        {
            TextInsertedHandler handler = TextInserted;
            if (handler != null) {
                TextInsertedArgs raise_args = new TextInsertedArgs ();
                raise_args.Args = new object [] {
                    args.Text,
                    args.Length,
                    args.Pos.Offset
                };
                handler (this, raise_args);
            }
        }

        private void OnBufferDeleteRange (object o, DeleteRangeArgs args)
        {
            TextDeletedHandler handler = TextDeleted;
            if (handler != null) {
                TextDeletedArgs raise_args = new TextDeletedArgs ();
                raise_args.Args = new object [] {
                    args.Start.Offset,
                    args.End.Offset
                };
                handler (this, raise_args);
            }
        }

        void Editable.PasteClipboard ()
        {
        }

        void Editable.CutClipboard ()
        {
        }

        void Editable.CopyClipboard ()
        {
        }

        public void DeleteText (int start_pos, int end_pos)
        {
            start_pos--;
            end_pos--;

            TextIter start_iter = Buffer.GetIterAtOffset (start_pos);
            TextIter end_iter = Buffer.GetIterAtOffset (start_pos + (end_pos - start_pos));
            Buffer.Delete (ref start_iter, ref end_iter);
        }

        public void InsertText (string new_text, ref int position)
        {
            TextIter iter = Buffer.GetIterAtOffset (position - 1);
            Buffer.Insert (ref iter, new_text);
            position = iter.Offset + 1;
        }

        public string GetChars (int start_pos, int end_pos)
        {
            start_pos--;
            end_pos--;

            TextIter start_iter = Buffer.GetIterAtOffset (start_pos);
            TextIter end_iter = Buffer.GetIterAtOffset (start_pos + (end_pos - start_pos));
            return Buffer.GetText (start_iter, end_iter, true);
        }

        public void SelectRegion (int start, int end)
        {
            Buffer.SelectRange (Buffer.GetIterAtOffset (start - 1), Buffer.GetIterAtOffset (end - 1));
        }

        public bool GetSelectionBounds (out int start, out int end)
        {
            TextIter start_iter, end_iter;
            start = 0;
            end = 0;

            if (Buffer.GetSelectionBounds (out start_iter, out end_iter)) {
                start = start_iter.Offset + 1;
                end = end_iter.Offset + 1;
                return true;
            }

            return true;
        }

        public void DeleteSelection ()
        {
            TextIter start, end;
            if (Buffer.GetSelectionBounds (out start, out end)) {
                Buffer.Delete (ref start, ref end);
            }
        }

        public int Position {
            get { return Buffer.CursorPosition; }
            set { Buffer.PlaceCursor (Buffer.GetIterAtOffset (Position)); }
        }

        public bool IsEditable {
            get { return Editable; }
            set { Editable = value; }
        }
    }
}
