//
// SelectionRatioWindow.axaml.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia.ViewModels;
using FSpot.Editors;

namespace FSpot.Avalonia;

// GTK SelectionRatioDialog: edit the CustomCropRatios list; Close saves it.
public partial class SelectionRatioWindow : Window
{
	readonly SelectionRatioEditor editor;

	public SelectionRatioWindow () : this (Array.Empty<CropConstraint> ())
	{
	}

	public SelectionRatioWindow (IEnumerable<CropConstraint> custom)
	{
		editor = new SelectionRatioEditor (custom);
		DataContext = editor;
		InitializeComponent ();
		Closing += (_, _) => Result = editor.ToConstraints ();
	}

	public IReadOnlyList<CropConstraint>? Result { get; private set; }

	void OnRatioLostFocus (object? sender, RoutedEventArgs e)
	{
		if (sender is Control { DataContext: SelectionRatioRow row })
			row.CommitRatioText ();
	}

	void OnClose (object? sender, RoutedEventArgs e)
		=> Close ();
}
