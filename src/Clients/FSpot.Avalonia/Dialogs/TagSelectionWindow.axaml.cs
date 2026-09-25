// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia.Models;
using FSpot.Core;

namespace FSpot.Avalonia.Dialogs;

/// <summary>
/// "Select a Tag...": the tag tree in a dialog; OK returns every highlighted tag.
/// </summary>
public partial class TagSelectionWindow : Window
{
	public TagSelectionWindow ()
		: this (new Category (null, 0, string.Empty))
	{
	}

	public TagSelectionWindow (Category root)
	{
		InitializeComponent ();
		Nodes = root.Children.Select (tag => new TagNode (tag)).ToList ();
		TagTree.ItemsSource = Nodes;
	}

	internal IReadOnlyList<TagNode> Nodes { get; }

	public IReadOnlyList<Tag>? Result { get; private set; }

	internal void Select (params TagNode[] nodes)
	{
		TagTree.SelectedItems.Clear ();
		foreach (var node in nodes)
			TagTree.SelectedItems.Add (node);
	}

	internal void Accept ()
	{
		Result = TagTree.SelectedItems.OfType<TagNode> ().Select (node => node.Tag).ToList ();
		Close ();
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
