//
// AboutViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot.Avalonia.ViewModels;

public sealed class AboutViewModel : ObservableObject
{
	public string Version { get; } = string.Format (Strings.VersionX, FSpotConfiguration.Version);

	public string Telemetry { get; } = Strings.CrashReportsStayOffUntilEnabled;
}
