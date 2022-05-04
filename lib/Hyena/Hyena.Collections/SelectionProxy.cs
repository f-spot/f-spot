//
// SelectionProxy.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace Hyena.Collections
{
	public class SelectionProxy
	{
		Selection selection;

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
			Changed?.Invoke (selection, EventArgs.Empty);
		}

		protected virtual void OnFocusChanged ()
		{
			FocusChanged?.Invoke (selection, EventArgs.Empty);
		}

		protected virtual void OnSelectionChanged ()
		{
			SelectionChanged?.Invoke (selection, EventArgs.Empty);
		}

		void HandleSelectionChanged (object o, EventArgs args)
		{
			OnChanged ();
		}

		void HandleSelectionFocusChanged (object o, EventArgs args)
		{
			OnFocusChanged ();
		}
	}
}
