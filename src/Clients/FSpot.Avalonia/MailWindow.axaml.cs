// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Mail;
using FSpot.Filters;
using FSpot.Resources.Lang;
using FSpot.Settings;

using Hyena;

namespace FSpot.Avalonia;

// SendEmail (mail_dialog.ui): choose the attachment size, see the photos and the
// original and estimated sizes, then "Create Mail".
public partial class MailWindow : Window
{
	const int TrayThumbnailWidth = 96;

	readonly IReadOnlyList<string> paths;
	readonly bool forceOriginal;
	readonly long originalTotal;
	double[] scales = MailSizing.ReferenceScales.ToArray ();

	public MailWindow ()
		: this ([], loadTray: false)
	{
	}

	public MailWindow (IReadOnlyList<string> paths, bool loadTray = true)
	{
		this.paths = paths;
		InitializeComponent ();
		SummaryHeading.Text = GtkMarkupText.Split (Strings.BSummaryB).Title;
		CreateButton.Content = GtkMnemonic.Display (Strings.CreateMail2);

		forceOriginal = paths.Any (path => !MailSizing.IsJpeg (path));
		foreach (string path in paths) {
			try {
				originalTotal += new FileInfo (path).Length;
			} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
			}
		}

		if (forceOriginal) {
			OriginalSize.IsChecked = true;
			foreach (var choice in new[] { TinySize, SmallSize, MediumSize, LargeSize, XLargeSize })
				choice.IsEnabled = false;
		} else {
			SizeButton (MailSizing.ClampIndex (Preferences.Get<int> (Preferences.ExportEmailSize))).IsChecked = true;
		}

		NumberOfPictures.Text = paths.Count.ToString ();
		TotalOriginalSize.Text = MailSizing.FormatSize (originalTotal);
		UpdateEstimatedSize ();

		if (loadTray) {
			LoadTray ();
			if (!forceOriginal && paths.Count > 0)
				_ = CalibrateAsync (paths[0]);
		}
	}

	// The chosen index into MailSizing.Sizes, or null when the dialog was dismissed.
	public int? Result { get; private set; }

	internal int SizeIndex
		=> TinySize.IsChecked == true ? 1
			: SmallSize.IsChecked == true ? 2
			: MediumSize.IsChecked == true ? 3
			: LargeSize.IsChecked == true ? 4
			: XLargeSize.IsChecked == true ? 5
			: 0;

	RadioButton SizeButton (int index)
		=> index switch {
			1 => TinySize,
			2 => SmallSize,
			3 => MediumSize,
			4 => LargeSize,
			5 => XLargeSize,
			_ => OriginalSize,
		};

	void LoadTray ()
	{
		var images = paths.Select (_ => new Image {
			Width = TrayThumbnailWidth,
			Height = TrayThumbnailWidth,
			Margin = new global::Avalonia.Thickness (2),
		}).ToList ();
		Tray.ItemsSource = images;
		var files = paths.ToList ();
		_ = Task.Run (() => {
			for (int i = 0; i < files.Count; i++) {
				Bitmap bitmap;
				try {
					bitmap = AvaloniaPhotoDecoder.DecodeFile (files[i], TrayThumbnailWidth);
				} catch (Exception e) {
					Logger.Log.Warning ($"No tray thumbnail for {files[i]}: {e.Message}");
					continue;
				}
				var image = images[i];
				Dispatcher.UIThread.Post (() => image.Source = bitmap);
			}
		});
	}

	// SendEmail resizes the first photo to medium to learn how this camera's files shrink.
	async Task CalibrateAsync (string path)
	{
		try {
			var calibrated = await Task.Run (() => {
				long original = new FileInfo (path).Length;
				using var request = new FilterRequest (new SafeUri (path));
				new SkiaResizeFilter ((uint)MailSizing.Sizes[MailSizing.MediumIndex]).Convert (request);
				long resized = new FileInfo (request.Current.LocalPath).Length;
				return MailSizing.Calibrate (original, resized);
			});
			scales = calibrated;
			UpdateEstimatedSize ();
		} catch (Exception e) {
			Logger.Log.Warning ($"Keeping the reference size estimate: {e.Message}");
		}
	}

	void UpdateEstimatedSize ()
	{
		if (ApproxNewSize is null)
			return;
		ApproxNewSize.Text = MailSizing.FormatSize (MailSizing.Estimate (originalTotal, SizeIndex, scales));
	}

	void OnSizeToggled (object? sender, RoutedEventArgs e)
		=> UpdateEstimatedSize ();

	void OnCreate (object? sender, RoutedEventArgs e)
	{
		int index = SizeIndex;
		if (!forceOriginal)
			Preferences.Set (Preferences.ExportEmailSize, index);
		Result = index;
		Close ();
	}

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
