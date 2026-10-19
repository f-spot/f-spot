// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Services;
using FSpot.Cms;
using FSpot.Imaging;

using SkiaSharp;

namespace FSpot.Avalonia.Printing;

// PrintOperation.OnDrawPage on a Skia canvas: draws the pages to a PDF for the
// printer, or to a bitmap for the dialog preview.
public sealed class PrintRenderer
{
	public const double PrintDpi = 300;

	readonly IReadOnlyList<PrintPhoto> photos;
	readonly PrintOptions options;
	readonly PrintPageSetup setup;
	readonly Profile? printerProfile;

	public PrintRenderer (IReadOnlyList<PrintPhoto> photos, PrintOptions options, PrintPageSetup setup, Profile? printerProfile = null)
	{
		this.photos = photos;
		this.options = options;
		this.setup = setup;
		this.printerProfile = printerProfile;
	}

	public int PageCount => PrintLayout.PageCount (photos.Count, options);

	public void WritePdf (Stream output)
	{
		using var document = SKDocument.CreatePdf (output, new SKDocumentPdfMetadata {
			Creator = "F-Spot",
			RasterDpi = (float)PrintDpi,
		}) ?? throw new InvalidOperationException ("Skia could not create a PDF document");
		float width = (float)setup.PageWidthPoints;
		float height = (float)setup.PageHeightPoints;
		for (int page = 0; page < PageCount; page++) {
			var canvas = document.BeginPage (width, height);
			DrawPage (canvas, page, PrintDpi);
			document.EndPage ();
		}
		document.Close ();
	}

	// The preview the Image Settings tab shows: page 0 at a size that fits maxEdge pixels.
	public SKBitmap RenderPreview (int page, int maxEdge)
	{
		double pw = setup.PageWidthPoints, ph = setup.PageHeightPoints;
		double scale = maxEdge / Math.Max (pw, ph);
		var bitmap = new SKBitmap (Math.Max (1, (int)(pw * scale)), Math.Max (1, (int)(ph * scale)));
		using var canvas = new SKCanvas (bitmap);
		canvas.Clear (SKColors.White);
		canvas.Scale ((float)scale);
		if (page < PageCount)
			DrawPage (canvas, page, 72 * scale);
		return bitmap;
	}

	public void DrawPage (SKCanvas canvas, int page, double dpi)
	{
		var printable = PrintLayout.Printable (setup.PageWidthPoints, setup.PageHeightPoints, options.FullPage);
		var (columns, rows) = PrintLayout.Grid (options.PhotosPerPage);
		double w = printable.Width / columns;
		double h = printable.Height / rows;

		for (int x = 0; x <= columns; x++) {
			for (int y = 0; y <= rows; y++) {
				if (options.CropMarks)
					DrawCropMarks (canvas, printable.X + x * w, printable.Y + y * h, w * .1);
				if (x == columns || y == rows)
					continue;
				if (PrintLayout.PhotoIndex (page, x, y, photos.Count, options) is not int index)
					continue;
				DrawCell (canvas, photos[index], PrintLayout.Cell (printable, columns, rows, x, y), dpi);
			}
		}
	}

	void DrawCell (SKCanvas canvas, PrintPhoto photo, PrintRect cell, double dpi)
	{
		var (decodeWidth, decodeHeight) = PrintLayout.DecodeSize (cell, dpi);
		SKBitmap? bitmap = null;
		try {
			bitmap = PrintImageLoader.Load (photo.Path, Math.Max (decodeWidth, decodeHeight));
			if (bitmap is not null && printerProfile is not null) {
				using Profile? source = EmbeddedProfile.Open (photo.Path);
				PrintImageLoader.ApplyProfile (bitmap, source, printerProfile);
			}
		} catch (Exception e) {
			Logger.Log.Error (e, $"Unable to load image {photo.Path}");
			bitmap?.Dispose ();
			bitmap = null;
		}

		var area = PrintLayout.ImageArea (cell, options.WhiteBorders);
		bool rotated = false;
		if (bitmap is null) {
			DrawMissing (canvas, area);
		} else {
			if (PrintLayout.ShouldRotate (bitmap.Width, bitmap.Height, cell.Width, cell.Height)) {
				var turned = PrintImageLoader.RotateCounterclockwise (bitmap);
				bitmap.Dispose ();
				bitmap = turned;
				rotated = true;
			}
			var rect = PrintLayout.ImageRect (options.Fit, bitmap.Width, bitmap.Height, area);
			canvas.Save ();
			canvas.ClipRect (ToSk (area));
			using (var image = SKImage.FromBitmap (bitmap))
				canvas.DrawImage (image, ToSk (rect), new SKSamplingOptions (SKCubicResampler.Mitchell));
			canvas.Restore ();
			bitmap.Dispose ();
		}

		if (options.WhiteBorders) {
			using var stroke = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = .95f, IsAntialias = true };
			canvas.DrawRect (ToSk (area), stroke);
		}

		string label = PrintLayout.Label (options, photo).Replace ("\t", "    ").Trim ();
		DrawComment (canvas, cell, (rotated ? cell.Width : cell.Height) * .025, label, rotated);
	}

	static void DrawMissing (SKCanvas canvas, PrintRect area)
	{
		using var paint = new SKPaint { Color = new SKColor (0xE0, 0xE0, 0xE0), Style = SKPaintStyle.Fill };
		canvas.DrawRect (ToSk (area), paint);
		using var cross = new SKPaint { Color = new SKColor (0x90, 0x90, 0x90), Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
		canvas.DrawLine ((float)area.X, (float)area.Y, (float)(area.X + area.Width), (float)(area.Y + area.Height), cross);
		canvas.DrawLine ((float)(area.X + area.Width), (float)area.Y, (float)area.X, (float)(area.Y + area.Height), cross);
	}

	// PrintOperation.DrawCropMarks: a dashed cross at a cell corner.
	static void DrawCropMarks (SKCanvas canvas, double x, double y, double length)
	{
		using var paint = new SKPaint {
			Color = SKColors.Black,
			Style = SKPaintStyle.Stroke,
			StrokeWidth = .2f,
			IsAntialias = true,
			PathEffect = SKPathEffect.CreateDash ([(float)(length * .4), (float)(length * .2)], 0),
		};
		canvas.DrawLine ((float)(x - length / 2), (float)y, (float)(x + length / 2), (float)y, paint);
		canvas.DrawLine ((float)x, (float)(y - length / 2), (float)x, (float)(y + length / 2), paint);
	}

	// PrintOperation.DrawComment: the label in the bottom-right corner of the cell,
	// running up the right edge when the photo was turned to fit.
	static void DrawComment (SKCanvas canvas, PrintRect cell, double height, string text, bool rotated)
	{
		if (string.IsNullOrEmpty (text) || height <= 0)
			return;
		using var typeface = SKTypeface.FromFamilyName ("sans-serif") ?? SKTypeface.Default;
		using var font = new SKFont (typeface, (float)height);
		using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
		float right = (float)(cell.X + cell.Width);
		float descent = font.Metrics.Descent;
		canvas.Save ();
		if (rotated) {
			float width = font.MeasureText (text);
			canvas.Translate (right - descent, (float)cell.Y + width);
			canvas.RotateDegrees (-90);
			canvas.DrawText (text, 0, 0, SKTextAlign.Left, font, paint);
		} else {
			canvas.DrawText (text, right, (float)(cell.Y + cell.Height) - descent, SKTextAlign.Right, font, paint);
		}
		canvas.Restore ();
	}

	static SKRect ToSk (PrintRect rect)
		=> SKRect.Create ((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
}
