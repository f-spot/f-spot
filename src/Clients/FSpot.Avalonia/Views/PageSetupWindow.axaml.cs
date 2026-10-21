// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia.Services;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia;

// Gtk.Print.RunPageSetupDialog: paper size and orientation.
public partial class PageSetupWindow : Window
{
	public PageSetupWindow ()
		: this (PrintPageSetup.Letter)
	{
	}

	public PageSetupWindow (PrintPageSetup current)
	{
		InitializeComponent ();
		Paper.ItemsSource = PrintPageSetup.Papers;
		Paper.SelectedItem = current.Paper ?? PrintPageSetup.Papers[0];
		Portrait.IsChecked = !current.Landscape;
		Landscape.IsChecked = current.Landscape;
		Refresh ();
	}

	public PrintPageSetup? Result { get; private set; }

	internal PrintPageSetup Current
		=> PrintPageSetup.FromPaper (Paper.SelectedItem as PaperSize ?? PrintPageSetup.Papers[0], Landscape.IsChecked == true);

	void OnChanged (object? sender, RoutedEventArgs e)
		=> Refresh ();

	void Refresh ()
	{
		if (SizeCaption is null || Paper is null)
			return;
		var setup = Current;
		SizeCaption.Text = string.Format (Strings.PaperSizeXxYmm,
			Math.Round (setup.PageWidthMm, 1), Math.Round (setup.PageHeightMm, 1));
	}

	void OnOk (object? sender, RoutedEventArgs e)
	{
		Result = Current;
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
