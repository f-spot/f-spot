//
// PhotoInfoViewModel.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Core;
using FSpot.Utils;

namespace FSpot.Avalonia.ViewModels;

public partial class PhotoInfoViewModel : ObservableObject
{
	bool syncing;

	[ObservableProperty]
	string name = string.Empty;

	[ObservableProperty]
	string date = string.Empty;

	[ObservableProperty]
	string size = string.Empty;

	[ObservableProperty]
	string exposure = string.Empty;

	[ObservableProperty]
	string focalLength = string.Empty;

	[ObservableProperty]
	string camera = string.Empty;

	[ObservableProperty]
	string creator = string.Empty;

	[ObservableProperty]
	string rating = string.Empty;

	[ObservableProperty]
	uint ratingValue;

	[ObservableProperty]
	IReadOnlyList<string> versions = [];

	[ObservableProperty]
	string? selectedVersion;

	[ObservableProperty]
	string tags = string.Empty;

	public IReadOnlyList<uint> RatingChoices { get; } = [0, 1, 2, 3, 4, 5];

	public event Action<string>? VersionChosen;
	public event Action<uint>? RatingChosen;

	public void Clear ()
	{
		syncing = true;
		try {
			Name = Date = Size = Exposure = FocalLength = Camera = Creator = Rating = Tags = string.Empty;
			RatingValue = 0;
			Versions = [];
			SelectedVersion = null;
		} finally {
			syncing = false;
		}
	}

	public void Update (IPhoto photo)
	{
		TagLib.Image.File? metadata = null;
		try {
			metadata = MetadataUtils.Parse (photo.DefaultVersion.Uri);
		} catch (Exception ex) {
			Logger.Log.Debug (ex, "Failed to parse metadata for {Name}", photo.Name);
		}

		syncing = true;
		try {
			Name = PhotoInfoFormatter.Name (photo);
			Date = PhotoInfoFormatter.Date (photo);
			Size = PhotoInfoFormatter.Size (metadata);
			Exposure = PhotoInfoFormatter.Exposure (metadata);
			FocalLength = PhotoInfoFormatter.FocalLength (metadata);
			Camera = PhotoInfoFormatter.Camera (metadata);
			Creator = PhotoInfoFormatter.Creator (metadata);
			RatingValue = photo.Rating;
			Rating = photo.Rating.ToString ();
			Versions = photo.Versions.Select (v => v.Name).ToList ();
			SelectedVersion = photo.DefaultVersion.Name;
			Tags = photo.Tags is null
				? string.Empty
				: string.Join (", ", photo.Tags.Select (t => t.Name));
		} finally {
			syncing = false;
		}
	}

	partial void OnSelectedVersionChanged (string? value)
	{
		if (!syncing && value is not null)
			VersionChosen?.Invoke (value);
	}

	partial void OnRatingValueChanged (uint value)
	{
		if (syncing)
			return;
		Rating = value.ToString ();
		RatingChosen?.Invoke (value);
	}
}
