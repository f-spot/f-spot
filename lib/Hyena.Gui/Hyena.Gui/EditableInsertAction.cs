//
// EditableInsertAction.cs
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
	class EditableInsertAction : IUndoAction
	{
		Editable editable;
		string text;
		int index;
		bool is_paste;

		public EditableInsertAction (Editable editable, int start, string text, int length)
		{
			this.editable = editable;
			this.text = text;
			index = start;
			is_paste = length > 1;
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
			var insert = action as EditableInsertAction;
			if (insert == null || string.IsNullOrEmpty (text)) {
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
			return string.Format ("Inserted: [{0}] ({1})", text, index);
		}
	}
}
