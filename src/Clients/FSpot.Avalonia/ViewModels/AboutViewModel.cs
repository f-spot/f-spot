//
// AboutViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Settings;

namespace FSpot.Avalonia.ViewModels;

public sealed class AboutViewModel : ObservableObject
{
	public string Version { get; } = $"Version {FSpotConfiguration.Version}";

	public string Description { get; } =
		"Personal photo management. The Avalonia client uses the same catalog as the GTK 2 client.";
}
