//
// TextViewEditable.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

		void OnBufferChanged (object o, EventArgs args)
		{
			Changed?.Invoke (this, EventArgs.Empty);
		}

		void OnBufferInsertText (object o, InsertTextArgs args)
		{
			TextInsertedHandler handler = TextInserted;
			if (handler != null) {
				var raise_args = new TextInsertedArgs ();
				raise_args.Args = new object[] {
					args.Text,
					args.Length,
					args.Pos.Offset
				};
				handler (this, raise_args);
			}
		}

		void OnBufferDeleteRange (object o, DeleteRangeArgs args)
		{
			TextDeletedHandler handler = TextDeleted;
			if (handler != null) {
				var raise_args = new TextDeletedArgs ();
				raise_args.Args = new object[] {
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
			start = 0;
			end = 0;

			if (Buffer.GetSelectionBounds (out var start_iter, out var end_iter)) {
				start = start_iter.Offset + 1;
				end = end_iter.Offset + 1;
				return true;
			}

			return true;
		}

		public void DeleteSelection ()
		{
			if (Buffer.GetSelectionBounds (out var start, out var end)) {
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
