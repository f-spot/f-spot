// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

// A paper the Page Setup dialog offers; Media is the CUPS/IPP media keyword.
public sealed record PaperSize (string Name, double WidthMm, double HeightMm, string Media)
{
	public override string ToString () => Name;
}

public sealed class PrintPageSetup
{
	public static IReadOnlyList<PaperSize> Papers { get; } = [
		new ("US Letter", 215.9, 279.4, "Letter"),
		new ("US Legal", 215.9, 355.6, "Legal"),
		new ("Executive", 184.15, 266.7, "Executive"),
		new ("A3", 297, 420, "A3"),
		new ("A4", 210, 297, "A4"),
		new ("A5", 148, 210, "A5"),
		new ("B5", 176, 250, "ISOB5"),
		new ("4 x 6 in", 101.6, 152.4, "4x6"),
		new ("5 x 7 in", 127, 177.8, "5x7"),
		new ("8 x 10 in", 203.2, 254, "8x10"),
	];

	public static PrintPageSetup Letter { get; } = new PrintPageSetup { PaperWidthMm = 215.9, PaperHeightMm = 279.4 };

	public double PaperWidthMm { get; init; } = 215.9;
	public double PaperHeightMm { get; init; } = 279.4;
	public bool Landscape { get; init; }

	// The sheet as it is printed on: the paper, turned for landscape.
	public double PageWidthMm => Landscape ? PaperHeightMm : PaperWidthMm;
	public double PageHeightMm => Landscape ? PaperWidthMm : PaperHeightMm;

	public const double PointsPerMm = 72 / 25.4;
	public double PageWidthPoints => PageWidthMm * PointsPerMm;
	public double PageHeightPoints => PageHeightMm * PointsPerMm;

	public PaperSize? Paper
		=> Papers.FirstOrDefault (paper => Math.Abs (paper.WidthMm - PaperWidthMm) < 0.5 && Math.Abs (paper.HeightMm - PaperHeightMm) < 0.5);

	public string Caption
		=> $"{PaperWidthMm:0.#}×{PaperHeightMm:0.#} mm{(Landscape ? " landscape" : "")}";

	public static PrintPageSetup FromPaper (PaperSize paper, bool landscape)
		=> new () { PaperWidthMm = paper.WidthMm, PaperHeightMm = paper.HeightMm, Landscape = landscape };

	public static PrintPageSetup? FromPrompt (string? paper, string? orientation)
	{
		if (string.IsNullOrWhiteSpace (paper) || string.IsNullOrWhiteSpace (orientation))
			return null;
		bool landscape = orientation.Trim ().StartsWith ("L", StringComparison.OrdinalIgnoreCase);
		paper = paper.Trim ();
		if (paper.StartsWith ("A4", StringComparison.OrdinalIgnoreCase))
			return new PrintPageSetup { PaperWidthMm = 210, PaperHeightMm = 297, Landscape = landscape };
		if (paper.StartsWith ("Legal", StringComparison.OrdinalIgnoreCase))
			return new PrintPageSetup { PaperWidthMm = 215.9, PaperHeightMm = 355.6, Landscape = landscape };
		return new PrintPageSetup { PaperWidthMm = 215.9, PaperHeightMm = 279.4, Landscape = landscape };
	}
}
