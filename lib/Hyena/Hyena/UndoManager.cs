//
// UndoManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Hyena
{
	public class UndoManager
	{
		Stack<IUndoAction> undo_stack = new Stack<IUndoAction> ();
		Stack<IUndoAction> redo_stack = new Stack<IUndoAction> ();
		int frozen_count;
		bool try_merge;

		public event EventHandler UndoChanged;

		public void Undo ()
		{
			lock (this) {
				UndoRedo (undo_stack, redo_stack, true);
			}
		}

		public void Redo ()
		{
			lock (this) {
				UndoRedo (redo_stack, undo_stack, false);
			}
		}

		public void Clear ()
		{
			lock (this) {
				frozen_count = 0;
				try_merge = false;
				undo_stack.Clear ();
				redo_stack.Clear ();
				OnUndoChanged ();
			}
		}

		public void AddUndoAction (IUndoAction action)
		{
			lock (this) {
				if (frozen_count != 0) {
					return;
				}

				if (try_merge && undo_stack.Count > 0) {
					IUndoAction top = undo_stack.Peek ();
					if (top.CanMerge (action)) {
						top.Merge (action);
						return;
					}
				}

				undo_stack.Push (action);
				redo_stack.Clear ();

				try_merge = true;

				OnUndoChanged ();
			}
		}

		protected virtual void OnUndoChanged ()
		{
			UndoChanged?.Invoke (this, EventArgs.Empty);
		}

		void UndoRedo (Stack<IUndoAction> pop_from, Stack<IUndoAction> push_to, bool is_undo)
		{
			if (pop_from.Count == 0) {
				return;
			}

			IUndoAction action = pop_from.Pop ();

			frozen_count++;
			if (is_undo) {
				action.Undo ();
			} else {
				action.Redo ();
			}
			frozen_count--;

			push_to.Push (action);

			try_merge = true;

			OnUndoChanged ();
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
