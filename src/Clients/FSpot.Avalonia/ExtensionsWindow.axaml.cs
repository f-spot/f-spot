using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot;

namespace FSpot.Avalonia;

public partial class ExtensionsWindow : Window
{
	readonly IList<AddinStatus> addins;

	public ExtensionsWindow () : this (Array.Empty<AddinStatus> ())
	{
	}

	public ExtensionsWindow (IList<AddinStatus> addins)
	{
		this.addins = addins;
		InitializeComponent ();
		Addins.ItemsSource = addins;
	}

	void OnClose (object? sender, RoutedEventArgs e)
	{
		foreach (var addin in addins)
			AddinCatalog.SetEnabled (addins, addin.Id, addin.Enabled);
		Close ();
	}
}
