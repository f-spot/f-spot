//
// BaseListModel.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena.Collections;

namespace Hyena.Data
{
	public abstract class BaseListModel<T> : IListModel<T>
	{
		Selection selection;

		public event EventHandler Cleared;
		public event EventHandler Reloaded;

		public BaseListModel () : base ()
		{
		}

		protected virtual void OnCleared ()
		{
			Selection.MaxIndex = Count - 1;

			Cleared?.Invoke (this, EventArgs.Empty);
		}

		protected virtual void OnReloaded ()
		{
			Selection.MaxIndex = Count - 1;

			Reloaded?.Invoke (this, EventArgs.Empty);
		}

		public void RaiseReloaded ()
		{
			OnReloaded ();
		}

		public abstract void Clear ();

		public abstract void Reload ();

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
			get { return Selection.FocusedIndex == -1 ? default : this[Selection.FocusedIndex]; }
		}

		bool can_reorder = false;
		public bool CanReorder {
			get { return can_reorder; }
			set { can_reorder = value; }
		}
	}
}
