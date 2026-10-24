// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Core;
using FSpot.Utils;

namespace FSpot.Avalonia.Printing;

// What PrintOperation.OnDrawPage reads from each IPhoto, captured up front so the
// renderer never touches the database off the UI thread.
public sealed record PrintPhoto (string Path, string Name, string Date, string Time, string Tags, string Description)
{
	public static PrintPhoto From (IPhoto photo, string path)
	{
		var tags = photo.Tags ?? [];
		return new PrintPhoto (
			path,
			photo.Name ?? string.Empty,
			PhotoDateCaption.FormatLocal (photo, wide: false),
			PhotoDateCaption.FormatTime (photo.Time.ToLocalTime ()),
			string.Join (", ", tags.Select (tag => tag.Name)),
			photo.Description ?? string.Empty);
	}
}

public readonly record struct PrintRect (double X, double Y, double Width, double Height);

// The page arithmetic of the GTK PrintOperation, free of any drawing API.
public static class PrintLayout
{
	// Printer margin used when "Full Page (no margin)" is off: a quarter inch, the
	// usual hardware margin GTK reports for a generic printer.
	public const double MarginPoints = 18;

	public static (int Columns, int Rows) Grid (int photosPerPage)
		=> photosPerPage switch {
			2 => (1, 2),
			4 => (2, 2),
			9 => (3, 3),
			12 => (3, 4),
			20 => (4, 5),
			30 => (5, 6),
			_ => (1, 1),
		};

	// The radio button captions of CustomPrintWidget: "1", "2", "2 x 2" ... "5 x 6".
	public static string GridCaption (int photosPerPage)
	{
		var (columns, rows) = Grid (photosPerPage);
		return columns == 1 ? rows.ToString (CultureInfo.CurrentCulture) : $"{columns} x {rows}";
	}

	public static int PageCount (int photoCount, PrintOptions options)
	{
		if (photoCount <= 0)
			return 0;
		return options.Repeat
			? photoCount
			: (int)Math.Ceiling (1.0 * photoCount / Math.Max (1, options.PhotosPerPage));
	}

	// Which photo goes in cell (x, y) of a page, or null for an empty cell.
	public static int? PhotoIndex (int page, int x, int y, int photoCount, PrintOptions options)
	{
		var (columns, _) = Grid (options.PhotosPerPage);
		int index = options.Repeat ? page : page * options.PhotosPerPage + y * columns + x;
		return index >= 0 && index < photoCount ? index : null;
	}

	// The drawable area of the page; the whole sheet when printing full page.
	public static PrintRect Printable (double pageWidth, double pageHeight, bool fullPage)
		=> fullPage
			? new PrintRect (0, 0, pageWidth, pageHeight)
			: new PrintRect (MarginPoints, MarginPoints,
				Math.Max (1, pageWidth - 2 * MarginPoints), Math.Max (1, pageHeight - 2 * MarginPoints));

	public static PrintRect Cell (PrintRect printable, int columns, int rows, int x, int y)
	{
		double w = printable.Width / columns;
		double h = printable.Height / rows;
		return new PrintRect (printable.X + x * w, printable.Y + y * h, w, h);
	}

	// PrintOperation rotates a photo whose orientation disagrees with its cell's.
	public static bool ShouldRotate (double imageWidth, double imageHeight, double cellWidth, double cellHeight)
		=> Math.Sign (imageWidth / imageHeight - 1.0) != Math.Sign (cellWidth / cellHeight - 1.0);

	// PrintOperation.DrawImage: the scale applied to the image in each fit mode.
	public static (double ScaleX, double ScaleY) Scale (PrintFitMode fit, double imageWidth, double imageHeight, double width, double height)
	{
		switch (fit) {
		case PrintFitMode.Zoom: {
				double s = Math.Max (width / imageWidth, height / imageHeight);
				return (s, s);
			}
		case PrintFitMode.Fill:
			return (width / imageWidth, height / imageHeight);
		default: {
				double s = Math.Min (width / imageWidth, height / imageHeight);
				return (s, s);
			}
		}
	}

	// The area a photo is placed in: the cell, shrunk to 95% and inset 2.5% for white borders.
	public static PrintRect ImageArea (PrintRect cell, bool whiteBorders)
		=> whiteBorders
			? new PrintRect (cell.X + cell.Width * .025, cell.Y + cell.Height * .025, cell.Width * .95, cell.Height * .95)
			: cell;

	// Where the image lands inside its area: centred, and possibly larger than the area (Zoom clips).
	public static PrintRect ImageRect (PrintFitMode fit, double imageWidth, double imageHeight, PrintRect area)
	{
		var (sx, sy) = Scale (fit, imageWidth, imageHeight, area.Width, area.Height);
		double w = imageWidth * sx;
		double h = imageHeight * sy;
		return new PrintRect (area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
	}

	public static string Label (PrintOptions options, PrintPhoto photo)
		=> string.Format (CultureInfo.CurrentCulture, options.LabelFormat,
			options.CustomText, photo.Name, photo.Date, photo.Time, photo.Tags, photo.Description);

	// The pixel size PrintOperation asks the loader for: the cell at the print resolution.
	public static (int Width, int Height) DecodeSize (PrintRect cell, double dpi)
		=> ((int)Math.Ceiling (cell.Width / 72.0 * dpi), (int)Math.Ceiling (cell.Height / 72.0 * dpi));
}
