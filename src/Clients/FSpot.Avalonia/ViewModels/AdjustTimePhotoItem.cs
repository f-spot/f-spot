// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

namespace FSpot.Avalonia.ViewModels;

/// <summary>
/// One row of the Adjust Time dialog's photo tray: the GTK dialog's tray_frame
/// (a single-column BrowseablePointerGridView with DisplayDates enabled). It shows the photo's
/// own recorded time, never a projected new time — only the reference photo previews that.
/// </summary>
public partial class AdjustTimePhotoItem : ObservableObject
{
	[ObservableProperty]
	Bitmap? thumbnail;

	public AdjustTimePhotoItem (string name, DateTime time, string path)
	{
		Name = name;
		Time = time;
		Path = path;
	}

	public string Name { get; }
	public DateTime Time { get; }
	public string Path { get; }
	public string TimeCaption => Time.ToString ("g", CultureInfo.CurrentCulture);
}
