//
// GridNavigation.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

namespace FSpot.Avalonia.Input;

public enum GridKeyAction
{
	None,
	Move,
	Toggle,
	Open
}

/// <summary>
/// The outcome of one key press in the photo grid: where the focus cell goes and
/// which cells are selected afterwards.
/// </summary>
public readonly record struct GridKeyResult (GridKeyAction Action, int Focus, IReadOnlySet<int> Selection)
{
	public bool AllSelected { get; init; }

	public IReadOnlySet<int> Excluded { get; init; } = EmptyExcluded;

	static readonly IReadOnlySet<int> EmptyExcluded = new HashSet<int> ();
}

/// <summary>
/// GTK <c>SelectionCollectionGridView.OnKeyPressEvent</c>: h/j/k/l and the arrows,
/// Ctrl+Shift+Left/Right for the row start and end, PageUp/PageDown, Home/End,
/// r for a random cell, Space to toggle and Return to open. Shift extends the
/// selection from the old focus; Ctrl moves the focus without selecting.
/// </summary>
public static class GridNavigation
{
	public static bool Handles (Key key)
		=> key is Key.Down or Key.J or Key.Left or Key.H or Key.Right or Key.L or Key.Up or Key.K
			or Key.PageUp or Key.PageDown or Key.Home or Key.End or Key.R or Key.Space or Key.Enter;

	public static GridKeyResult Apply (Key key, KeyModifiers modifiers, int focus, int columns, int rows, int count,
		IReadOnlySet<int> selection, Func<int, int, int>? random = null)
		=> Apply (key, modifiers, focus, columns, rows, count, allSelected: false, picked: selection, random: random);

	public static GridKeyResult Band (IReadOnlySet<int> excluded, IReadOnlyList<int> cells, bool toggle, int focus)
	{
		var selected = new CompactSelection (true, excluded);
		if (cells != null) {
			foreach (int cell in cells) {
				if (!toggle)
					selected.Add (cell);
				else if (!selected.Remove (cell))
					selected.Add (cell);
			}
		}
		return selected.ToResult (GridKeyAction.Move, focus);
	}

	public static GridKeyResult Apply (Key key, KeyModifiers modifiers, int focus, int columns, int rows, int count,
		bool allSelected, IReadOnlySet<int> picked, Func<int, int, int>? random = null)
	{
		var unchanged = new GridKeyResult (GridKeyAction.None, focus, picked ?? Empty);
		if (count <= 0 || (modifiers & KeyModifiers.Alt) != 0 || !Handles (key))
			return unchanged;

		bool shift = (modifiers & KeyModifiers.Shift) != 0;
		bool control = (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
		columns = Math.Max (1, columns);
		rows = Math.Max (1, rows);
		int old = Clamp (focus, count);
		int next = old;
		var action = GridKeyAction.Move;
		var selected = new CompactSelection (allSelected, picked ?? Empty);

		switch (key) {
		case Key.Down:
		case Key.J:
			next = old + columns;
			break;
		case Key.Left:
		case Key.H:
			next = control && shift ? old - old % columns : old - 1;
			break;
		case Key.Right:
		case Key.L:
			next = control && shift ? old + columns - old % columns - 1 : old + 1;
			break;
		case Key.Up:
		case Key.K:
			next = old - columns;
			break;
		case Key.PageUp:
			next = old - columns * rows;
			break;
		case Key.PageDown:
			next = old + columns * rows;
			break;
		case Key.Home:
			next = 0;
			break;
		case Key.End:
			next = count - 1;
			break;
		case Key.R:
			// GTK: new Random ().Next (0, Count - 1).
			next = (random ?? Random.Shared.Next) (0, Math.Max (0, count - 1));
			break;
		case Key.Space:
			action = GridKeyAction.Toggle;
			if (!selected.Remove (old))
				selected.Add (old);
			break;
		case Key.Enter:
			action = GridKeyAction.Open;
			break;
		}

		next = Clamp (next, count);
		if (shift) {
			if (old != next && selected.Contains (old) && selected.Contains (next))
				selected.RemoveRangeExceptStart (next, old);
			else
				selected.AddRange (old, next);
		} else if (!control) {
			selected.ClearTo (next);
		}

		return selected.ToResult (action, next);
	}

	static readonly HashSet<int> Empty = new ();

	static int Clamp (int value, int count)
		=> Math.Min (Math.Max (value, 0), count - 1);

	sealed class CompactSelection
	{
		public bool All;
		public readonly HashSet<int> Ids = new ();

		public CompactSelection (bool all, IReadOnlySet<int> ids)
		{
			All = all;
			if (ids == null)
				return;
			foreach (int id in ids)
				Ids.Add (id);
		}

		public bool Contains (int index)
			=> All ? !Ids.Contains (index) : Ids.Contains (index);

		public void Add (int index)
		{
			if (All)
				Ids.Remove (index);
			else
				Ids.Add (index);
		}

		public bool Remove (int index)
		{
			if (!Contains (index))
				return false;
			if (All)
				Ids.Add (index);
			else
				Ids.Remove (index);
			return true;
		}

		public void ClearTo (int index)
		{
			All = false;
			Ids.Clear ();
			Ids.Add (index);
		}

		public void AddRange (int start, int end)
		{
			for (int i = Math.Min (start, end); i <= Math.Max (start, end); i++)
				Add (i);
		}

		// GTK SelectionCollection.Remove (start, end): the range without its start entry.
		public void RemoveRangeExceptStart (int start, int end)
		{
			int first = Math.Min (start + 1, end);
			int last = Math.Max (start - 1, end);
			for (int i = first; i <= last; i++)
				Remove (i);
		}

		public GridKeyResult ToResult (GridKeyAction action, int focus)
		{
			if (!All)
				return new GridKeyResult (action, focus, Ids);
			return new GridKeyResult (action, focus, Empty) { AllSelected = true, Excluded = Ids };
		}
	}
}
