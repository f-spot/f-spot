//
// GtkMarkupText.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia;

static class GtkMarkupText
{
	public static (string Title, string Detail) Split (string? markup)
	{
		if (string.IsNullOrWhiteSpace (markup))
			return (string.Empty, string.Empty);
		const string open = "<small>";
		const string close = "</small>";
		int start = markup.IndexOf (open, StringComparison.Ordinal);
		if (start < 0)
			return (markup.Trim (), string.Empty);
		int end = markup.IndexOf (close, start, StringComparison.Ordinal);
		string title = markup[..start].Trim ();
		string detail = end < 0
			? markup[(start + open.Length)..].Trim ()
			: markup[(start + open.Length)..end].Trim ();
		return (title, detail);
	}

	// Several Strings entries carry a literal "<b>...</b>" Pango wrapper (frame captions such
	// as "Reference Photo"); Avalonia's TextBlock does not render that markup, so strip it and
	// apply boldness with FontWeight instead.
	public static string StripBold (string? markup)
	{
		if (string.IsNullOrEmpty (markup))
			return markup ?? string.Empty;
		return markup
			.Replace ("<b>", string.Empty, StringComparison.Ordinal)
			.Replace ("</b>", string.Empty, StringComparison.Ordinal)
			.Trim ();
	}
}
