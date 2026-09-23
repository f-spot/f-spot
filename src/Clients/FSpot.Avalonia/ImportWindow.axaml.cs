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
		GtkMnemonic.ApplyHotKey (ImportButton, FSpot.Resources.Lang.Strings.Import2);
		Opened += (_, _) => ApplyUiScale ();
		ScalingChanged += (_, _) => ApplyUiScale ();
		Closing += (_, _) => viewModel.CancelInFlight ();
	}

	void ApplyUiScale ()
	{
		if (DataContext is ImportViewModel viewModel)
			viewModel.UiScale = RenderScaling;
	}

	void OnThumbnailContainerPrepared (object? sender, ContainerPreparedEventArgs e)
	{
		if (DataContext is not ImportViewModel viewModel || sender is not ListBox list)
			return;
		if ((uint)e.Index >= (uint)list.Items.Count || list.Items[e.Index] is not PhotoItemViewModel item)
			return;
		_ = viewModel.RealizeThumbnailAsync (e.Container, item);
	}

	void OnThumbnailContainerClearing (object? sender, ContainerClearingEventArgs e)
	{
		if (DataContext is ImportViewModel viewModel)
			viewModel.ReleaseThumbnail (e.Container);
	}
}
