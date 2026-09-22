// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Services;

public sealed class PrintPageSetup
{
	public static PrintPageSetup Letter { get; } = new PrintPageSetup { PaperWidthMm = 215.9, PaperHeightMm = 279.4 };

	public double PaperWidthMm { get; init; } = 215.9;
	public double PaperHeightMm { get; init; } = 279.4;
	public bool Landscape { get; init; }

	public string Caption
		=> $"{PaperWidthMm:0.#}×{PaperHeightMm:0.#} mm{(Landscape ? " landscape" : "")}";

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
