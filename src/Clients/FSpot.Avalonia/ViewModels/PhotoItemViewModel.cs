//
// PhotoItemViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Core;
using FSpot.Utils;

namespace FSpot.Avalonia.ViewModels;

public partial class PhotoItemViewModel : ObservableObject
{
	[ObservableProperty]
	Bitmap? thumbnail;

	[ObservableProperty]
	bool selected;

	public PhotoItemViewModel (IPhoto photo)
	{
		Photo = photo;
		RefreshFromPhoto ();
	}

	public IPhoto Photo { get; }
	public string Caption { get; private set; } = string.Empty;
	public string Path { get; private set; } = string.Empty;
	public string DateCaption { get; private set; } = string.Empty;
	public string TagsCaption { get; private set; } = string.Empty;
	public string RatingCaption { get; private set; } = string.Empty;
	public string AutomationId { get; private set; } = "photo";

	public void RefreshCaptions ()
		=> RefreshFromPhoto ();

	public void RefreshFromPhoto ()
	{
		Caption = Photo.Name;
		Path = Photo.DefaultVersion.Uri?.AbsolutePath ?? string.Empty;
		AutomationId = PhotoAutomationId.For (Photo);
		DateCaption = PhotoDateCaption.Format (Photo, wide: false);
		TagsCaption = FormatTags (Photo);
		RatingCaption = Photo.Rating == 0 ? string.Empty : new string ('★', (int)Math.Min (Photo.Rating, 5u));
		OnPropertyChanged (nameof (Caption));
		OnPropertyChanged (nameof (Path));
		OnPropertyChanged (nameof (DateCaption));
		OnPropertyChanged (nameof (TagsCaption));
		OnPropertyChanged (nameof (RatingCaption));
		OnPropertyChanged (nameof (AutomationId));
	}

	static string FormatTags (IPhoto photo)
		=> photo.Tags is null
			? string.Empty
			: string.Join (", ", photo.Tags.Where (tag => tag is not null).Select (tag => tag.Name));
}
