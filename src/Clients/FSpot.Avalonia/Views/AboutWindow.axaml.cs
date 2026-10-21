using Avalonia.Controls;
using Avalonia.Input;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

public partial class AboutWindow : Window
{
	public AboutWindow ()
	{
		DataContext = new AboutViewModel ();
		InitializeComponent ();
	}

	void OnAboutKeyDown (object? sender, KeyEventArgs e)
	{
		if (e.Key != Key.Escape)
			return;
		Close ();
		e.Handled = true;
	}
}
