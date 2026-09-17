using Avalonia.Controls;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia;

public partial class ImportWindow : Window
{
	public ImportWindow ()
	{
		InitializeComponent ();
	}

	public ImportWindow (ImportViewModel viewModel)
	{
		DataContext = viewModel;
		InitializeComponent ();
	}
}
