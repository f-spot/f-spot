// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FSpot.Avalonia;

public partial class FolderPickerWindow : Window
{
	readonly Func<string, Task<string?>> browse;

	public FolderPickerWindow ()
		: this (string.Empty, _ => Task.FromResult<string?> (null))
	{
	}

	public FolderPickerWindow (string title, Func<string, Task<string?>> browse)
	{
		this.browse = browse;
		InitializeComponent ();
		Title = title;
		PathBox.PlaceholderText = title;
	}

	public string? Accept ()
	{
		string? chosen = FolderPick.Chosen (PathBox.Text);
		Close (chosen);
		return chosen;
	}

	public async Task BrowseAsync ()
	{
		string? picked = await browse (Title ?? string.Empty);
		if (!string.IsNullOrWhiteSpace (picked))
			PathBox.Text = picked;
	}

	void OnOk (object? sender, RoutedEventArgs e)
		=> Accept ();

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close (null);

	void OnBrowse (object? sender, RoutedEventArgs e)
		=> _ = BrowseAsync ();
}
