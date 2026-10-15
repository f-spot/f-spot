//
// TagTreeInput.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

/// <summary>
/// GTK <c>MainWindow.HandleTagSelectionKeyPress</c> (connected before the tree's own
/// handlers): Delete removes the tag, Space and Return add the highlighted tags to the
/// query, and F2 starts renaming.
/// </summary>
public static class TagTreeInput
{
	public static readonly AttachedProperty<bool> EnableProperty =
		AvaloniaProperty.RegisterAttached<TreeView, bool> ("Enable", typeof (TagTreeInput));

	static TagTreeInput ()
		=> EnableProperty.Changed.AddClassHandler<TreeView> ((tree, args) => {
			if (args.GetNewValue<bool> ())
				tree.AddHandler (InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
		});

	public static void SetEnable (TreeView element, bool value) => element.SetValue (EnableProperty, value);
	public static bool GetEnable (TreeView element) => element.GetValue (EnableProperty);

	public enum TagKey
	{
		None,
		Delete,
		Include,
		Rename
	}

	public static TagKey Map (Key key, KeyModifiers modifiers)
	{
		if ((modifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
			return TagKey.None;
		return key switch {
			Key.Delete => TagKey.Delete,
			Key.Space or Key.Enter => TagKey.Include,
			Key.F2 => TagKey.Rename,
			_ => TagKey.None
		};
	}

	static void OnKeyDown (object? sender, KeyEventArgs e)
	{
		if (sender is not TreeView tree || WindowCommand.Model (tree) is not MainWindowViewModel viewModel)
			return;
		var focused = TopLevel.GetTopLevel (tree)?.FocusManager?.GetFocusedElement ();
		if (!ShortcutGuards.AllowsUnadornedKey (focused, e.Key, tree))
			return;
		var action = Map (e.Key, e.KeyModifiers);
		if (action == TagKey.None)
			return;

		var highlighted = Highlighted (tree, focused, viewModel);
		if (highlighted.Count == 0)
			return;
		switch (action) {
		case TagKey.Delete:
			_ = viewModel.DeleteHighlightedTagsAsync (highlighted);
			break;
		case TagKey.Include:
			viewModel.IncludeTagsInQuery (highlighted);
			break;
		case TagKey.Rename:
			viewModel.BeginRenameTagCommand.Execute (highlighted[0]);
			Dispatcher.UIThread.Post (() => FocusRenameBox (tree, highlighted[0]));
			break;
		}
		e.Handled = true;
	}

	static List<TagNode> Highlighted (TreeView tree, IInputElement? focused, MainWindowViewModel viewModel)
	{
		var nodes = tree.SelectedItems?.OfType<TagNode> ().ToList () ?? [];
		if (nodes.Count > 0)
			return nodes;
		for (var visual = focused as Visual; visual is not null && !ReferenceEquals (visual, tree); visual = visual.GetVisualParent ()) {
			if (visual is TreeViewItem { DataContext: TagNode node })
				return [node];
		}
		return viewModel.SelectedTag is TagNode selected ? [selected] : [];
	}

	static void FocusRenameBox (TreeView tree, TagNode node)
	{
		var box = tree.GetVisualDescendants ().OfType<TextBox> ()
			.FirstOrDefault (candidate => ReferenceEquals (candidate.DataContext, node) && candidate.IsVisible);
		if (box is null)
			return;
		box.Focus ();
		box.SelectAll ();
	}
}
