// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.Printing;

// CustomPrintWidget.FitMode: how a photo fills its cell on the page.
public enum PrintFitMode
{
	Zoom,
	Scaled,
	Fill,
}

// The Image Settings tab of the GTK print dialog (CustomPrintWidget), as plain data.
public sealed record PrintOptions
{
	// The six radio buttons of the "Photos per page" frame, in GTK order.
	public static IReadOnlyList<int> PhotosPerPageChoices { get; } = [1, 2, 4, 9, 20, 30];

	public int PhotosPerPage { get; init; } = 1;
	public bool Repeat { get; init; }
	public bool CropMarks { get; init; }
	public bool FullPage { get; init; }
	public PrintFitMode Fit { get; init; } = PrintFitMode.Zoom;
	public bool WhiteBorders { get; init; }
	public string CustomText { get; init; } = string.Empty;
	public bool PrintFileName { get; init; }
	public bool PrintDate { get; init; }
	public bool PrintTime { get; init; }
	public bool PrintTags { get; init; }
	public bool PrintComment { get; init; }

	// CustomPrintWidget.PrintLabelFormat: {0} comment, {1} name, {2} date, {3} time, {4} tags, {5} description.
	public string LabelFormat {
		get {
			string format = "{0}";
			if (PrintTags)
				format += "\t{4}";
			if (PrintFileName)
				format += "\t{1}";
			if (PrintDate)
				format += "\t{2}";
			if (PrintTime)
				format += " {3}";
			if (PrintComment)
				format += "\t{5}";
			return format;
		}
	}
}
