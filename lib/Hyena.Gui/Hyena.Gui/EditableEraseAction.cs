//
// EditableEraseAction.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

namespace Hyena.Gui
{
	class EditableEraseAction : IUndoAction
	{
		Editable editable;
		string text;
		int start;
		int end;
		bool is_forward;
		bool is_cut;

		public EditableEraseAction (Editable editable, int start, int end)
		{
			this.editable = editable;
			text = editable.GetChars (start, end);
			this.start = start;
			this.end = end;
			is_cut = end - start > 1;
			is_forward = editable.Position < start;
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
			var erase = (EditableEraseAction)action;
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
			var erase = action as EditableEraseAction;
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
			return string.Format ("Erased: [{0}] ({1},{2})", text, start, end);
		}
	}
}
