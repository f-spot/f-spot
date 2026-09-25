//
// PhotoPopup.axaml.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;

using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Controls;

/// <summary>
/// GTK's /FSpot/Menus/PhotoPopup: the context menu of a photo in the browser, the filmstrip
/// and the photo view (including fullscreen), with the rows extensions contribute after it.
/// </summary>
public partial class PhotoPopup : ContextMenu
{
	readonly List<Control> contributed = [];

	public PhotoPopup ()
	{
		InitializeComponent ();
		Opening += OnOpening;
	}

	protected override Type StyleKeyOverride => typeof (ContextMenu);

	void OnOpening (object? sender, CancelEventArgs e)
	{
		if (PlacementTarget is not null && TopLevel.GetTopLevel (PlacementTarget)?.DataContext is MainWindowViewModel viewModel)
			DataContext = viewModel;
		if (DataContext is not MainWindowViewModel current)
			return;
		current.RefreshPhotoPopup ();
		foreach (var dynamic in Items.OfType<MenuItem> ().Where (item => Input.DynamicMenu.GetSource (item) is not null))
			Input.DynamicMenu.Fill (dynamic);
		foreach (var item in contributed)
			Items.Remove (item);
		contributed.Clear ();
		var contributions = current.PhotoPopupContributions ();
		if (contributions.Count == 0)
			return;
		contributed.Add (new Separator ());
		foreach (var contribution in contributions) {
			contributed.Add (new MenuItem {
				Header = contribution.Label,
				Command = current.RunMenuContributionCommand,
				CommandParameter = contribution
			});
		}
		foreach (var item in contributed)
			Items.Add (item);
	}

	void OnStarClick (object? sender, RoutedEventArgs e)
		=> Close ();
}
