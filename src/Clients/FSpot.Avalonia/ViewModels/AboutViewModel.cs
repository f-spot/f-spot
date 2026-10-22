//
// AboutViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.InteropServices;

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot.Avalonia.ViewModels;

public sealed class AboutViewModel : ObservableObject
{
	public string Version { get; } = string.Format (Strings.VersionX, FSpotConfiguration.Version);

	public string Licence { get; } = "MIT License";

	public string Runtime { get; } = DescribeRuntime ();

	public string Telemetry => FSpotSentry.ReportingDescription ();

	static string DescribeRuntime ()
	{
		var avalonia = typeof (global::Avalonia.Application).Assembly.GetName ().Version;
		string avaloniaText = avalonia is null ? "Avalonia" : $"Avalonia {avalonia.ToString (3)}";
		return avaloniaText + " · " + RuntimeInformation.FrameworkDescription;
	}
}
