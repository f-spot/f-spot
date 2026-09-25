// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using FSpot.Avalonia.Printing;
using FSpot.Avalonia.Services;
using FSpot.Cms;
using FSpot.Resources.Lang;

using SkiaSharp;

namespace FSpot.Avalonia;

// The GTK print dialog (PrintOperation) with its "Image Settings" tab
// (CustomPrintWidget). Pages are drawn to a PDF and handed to the system spooler.
public partial class PrintWindow : Window
{
	const int PreviewEdge = 360;

	readonly IReadOnlyList<PrintPhoto> photos;
	readonly Profile? printerProfile;
	int previewGeneration;
	string? defaultPrinter;

	public PrintWindow ()
		: this ([], PrintPageSetup.Letter)
	{
	}

	public PrintWindow (IReadOnlyList<PrintPhoto> photos, PrintPageSetup setup, bool listPrinters = true)
	{
		this.photos = photos;
		PageSetup = setup;
		InitializeComponent ();
		foreach (var (button, count) in new[] { (Ppp1, 1), (Ppp2, 2), (Ppp4, 4), (Ppp9, 9), (Ppp20, 20), (Ppp30, 30) })
			button.Content = PrintLayout.GridCaption (count);
		SavePdf.Content = GtkMnemonic.Display (Strings.PrintSaveAsPdf);
		CopiesLabel.Text = GtkMnemonic.Display (Strings.NumberOfCopies);
		AutomationProperties.SetName (Copies, GtkMnemonic.Display (Strings.NumberOfCopies));
		printerProfile = InstalledProfileNames.Find (LibraryPreferences.PrintProfile);
		if (listPrinters)
			FillPrinters ();
		RefreshPaper ();
		RefreshPreview ();
	}

	public PrintPageSetup PageSetup { get; private set; }

	internal PrintOptions Options => new () {
		PhotosPerPage = Ppp2.IsChecked == true ? 2
			: Ppp4.IsChecked == true ? 4
			: Ppp9.IsChecked == true ? 9
			: Ppp20.IsChecked == true ? 20
			: Ppp30.IsChecked == true ? 30
			: 1,
		Repeat = Repeat.IsChecked == true,
		CropMarks = CropMarks.IsChecked == true,
		FullPage = FullPage.IsChecked == true,
		Fit = Fill.IsChecked == true ? PrintFitMode.Fill
			: Scaled.IsChecked == true ? PrintFitMode.Scaled
			: PrintFitMode.Zoom,
		WhiteBorders = WhiteBorders.IsChecked == true,
		CustomText = CustomText.Text ?? string.Empty,
		PrintFileName = PrintFileName.IsChecked == true,
		PrintDate = PrintDate.IsChecked == true,
		PrintTime = PrintTime.IsChecked == true,
		PrintTags = PrintTags.IsChecked == true,
		PrintComment = PrintComment.IsChecked == true,
	};

	void FillPrinters ()
	{
		if (!SystemPrinter.UsesCups) {
			Printer.ItemsSource = new[] { Strings.PrintDefaultPrinter };
			Printer.SelectedIndex = 0;
			return;
		}
		var (printers, fallback) = SystemPrinter.Printers ();
		defaultPrinter = fallback;
		var names = new List<string> { Strings.PrintDefaultPrinter };
		names.AddRange (printers);
		Printer.ItemsSource = names;
		Printer.SelectedIndex = fallback is not null && printers.Contains (fallback) ? names.IndexOf (fallback) : 0;
		PrintButton.IsEnabled = printers.Count > 0 || fallback is not null;
	}

	string? ChosenPrinter
		=> Printer.SelectedIndex > 0 ? Printer.SelectedItem as string : defaultPrinter;

	void OnChanged (object? sender, RoutedEventArgs e)
		=> RefreshPreview ();

	void OnTextChanged (object? sender, TextChangedEventArgs e)
		=> RefreshPreview ();

	void RefreshPaper ()
		=> PaperCaption.Text = string.Format (Strings.PaperSizeXxYmm,
			Math.Round (PageSetup.PageWidthMm, 1), Math.Round (PageSetup.PageHeightMm, 1));

	// CustomPrintWidget's preview: redrawn off the UI thread on every change; a newer
	// request supersedes an older one still rendering.
	void RefreshPreview ()
	{
		if (Preview is null || CustomText is null)
			return;
		int generation = ++previewGeneration;
		_ = RenderPreviewAsync (generation, new PrintRenderer (photos, Options, PageSetup, printerProfile));
	}

	async Task RenderPreviewAsync (int generation, PrintRenderer renderer)
	{
		try {
			byte[] png = await Task.Run (() => {
				using var bitmap = renderer.RenderPreview (0, PreviewEdge);
				using var image = SKImage.FromBitmap (bitmap);
				using var data = image.Encode (SKEncodedImageFormat.Png, 100);
				return data.ToArray ();
			});
			if (generation != previewGeneration)
				return;
			var old = Preview.Source as Bitmap;
			Preview.Source = new Bitmap (new MemoryStream (png));
			old?.Dispose ();
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Unable to render the print preview");
		}
	}

	void OnPageSetup (object? sender, RoutedEventArgs e)
		=> _ = PageSetupAsync ();

	async Task PageSetupAsync ()
	{
		var dialog = new PageSetupWindow (PageSetup);
		await dialog.ShowDialog (this);
		if (dialog.Result is null)
			return;
		PageSetup = dialog.Result;
		RefreshPaper ();
		RefreshPreview ();
	}

	async Task<string> RenderPdfAsync (string path)
	{
		var renderer = new PrintRenderer (photos, Options, PageSetup, printerProfile);
		await Task.Run (() => {
			using var stream = File.Create (path);
			renderer.WritePdf (stream);
		});
		return path;
	}

	void OnPrint (object? sender, RoutedEventArgs e)
		=> _ = PrintAsync ();

	async Task PrintAsync ()
	{
		IsEnabled = false;
		string path = Path.Combine (Path.GetTempPath (), $"f-spot-print-{Guid.NewGuid ():N}.pdf");
		try {
			await RenderPdfAsync (path);
			await SystemPrinter.PrintAsync (path, ChosenPrinter, (int)(Copies.Value ?? 1),
				PageSetup.Paper?.Media, Strings.MyPhotos);
			Close ();
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Printing failed");
			await MessageDialog.ShowAsync (this, Strings.Print2, string.Format (Strings.PrintFailedXMessage, ex.Message), MessageDialogKind.Error);
		} finally {
			IsEnabled = true;
			// lp has copied the job into the spool by the time it exits; the Windows
			// print verb still reads the file after Process.Start returns.
			if (SystemPrinter.UsesCups)
				TryDelete (path);
		}
	}

	static void TryDelete (string path)
	{
		try {
			File.Delete (path);
		} catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
		}
	}

	void OnSavePdf (object? sender, RoutedEventArgs e)
		=> _ = SavePdfAsync ();

	async Task SavePdfAsync ()
	{
		var file = await StorageProvider.SaveFilePickerAsync (new FilePickerSaveOptions {
			Title = GtkMnemonic.Display (Strings.PrintSaveAsPdf),
			SuggestedFileName = "output.pdf",
			DefaultExtension = "pdf",
			FileTypeChoices = [new FilePickerFileType ("PDF") { Patterns = ["*.pdf"], MimeTypes = ["application/pdf"] }],
		});
		string? path = file?.TryGetLocalPath ();
		if (path is null)
			return;
		IsEnabled = false;
		try {
			await RenderPdfAsync (path);
			Close ();
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Printing to a file failed");
			await MessageDialog.ShowAsync (this, Strings.Print2, string.Format (Strings.PrintFailedXMessage, ex.Message), MessageDialogKind.Error);
		} finally {
			IsEnabled = true;
		}
	}

	void OnCancel (object? sender, RoutedEventArgs e)
		=> Close ();
}
