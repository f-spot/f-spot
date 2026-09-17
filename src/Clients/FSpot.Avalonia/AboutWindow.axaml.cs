using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

public partial class AboutWindow : Window
{
	public AboutWindow ()
	{
		DataContext = new AboutViewModel ();
		InitializeComponent ();
	}

	void OnClose (object? sender, RoutedEventArgs e)
		=> Close ();
}
