// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace RasterizeIcons;

public sealed record IconRasterJob (string SvgPath, string PngPath, int Size)
{
	public bool NeedsWrite ()
	{
		if (!File.Exists (PngPath))
			return true;
		return File.GetLastWriteTimeUtc (SvgPath) > File.GetLastWriteTimeUtc (PngPath);
	}
}

public static class IconRasterPlan
{
	public static readonly int[] DefaultSizes = [16, 22, 24, 32];

	static readonly Regex SizedName = new (@"^(.*)-(\d+)$", RegexOptions.CultureInvariant);

	public static IReadOnlyList<IconRasterJob> Create (string svgDir, string pngDir)
	{
		var svgs = Directory.Exists (svgDir)
			? Directory.GetFiles (svgDir, "*.svg")
			: [];
		var pngs = Directory.Exists (pngDir)
			? Directory.GetFiles (pngDir, "*.png")
			: [];

		var svgByStem = new Dictionary<string, List<(int? Size, string Path)>> (StringComparer.OrdinalIgnoreCase);
		foreach (string svg in svgs) {
			(string stem, int? size) = ParseStem (Path.GetFileNameWithoutExtension (svg));
			if (!svgByStem.TryGetValue (stem, out var list)) {
				list = [];
				svgByStem[stem] = list;
			}
			list.Add ((size, svg));
		}

		var jobs = new Dictionary<string, IconRasterJob> (StringComparer.OrdinalIgnoreCase);

		foreach (var (stem, list) in svgByStem) {
			foreach (var (size, svg) in list) {
				if (size is int exact) {
					Add (jobs, svg, Path.Combine (pngDir, $"{stem}-{exact}.png"), exact);
					continue;
				}

				var existing = ExistingSizes (pngs, stem);
				int[] sizes = existing.Count > 0 ? existing.ToArray () : DefaultSizes;
				foreach (int outputSize in sizes)
					Add (jobs, svg, Path.Combine (pngDir, $"{stem}-{outputSize}.png"), outputSize);
			}
		}

		foreach (string png in pngs) {
			(string stem, int? size) = ParseStem (Path.GetFileNameWithoutExtension (png));
			if (size is not int pngSize || !svgByStem.TryGetValue (stem, out var list))
				continue;
			string? svg = SourceSvg (list, pngSize);
			if (svg is null)
				continue;
			Add (jobs, svg, png, pngSize);
		}

		return jobs.Values.OrderBy (job => job.PngPath, StringComparer.OrdinalIgnoreCase).ToArray ();
	}

	static void Add (Dictionary<string, IconRasterJob> jobs, string svg, string png, int size)
		=> jobs[png] = new IconRasterJob (svg, png, size);

	static string? SourceSvg (List<(int? Size, string Path)> svgs, int size)
	{
		string? exact = svgs.FirstOrDefault (item => item.Size == size).Path;
		if (exact is not null)
			return exact;
		string? unsized = svgs.FirstOrDefault (item => item.Size is null).Path;
		if (unsized is not null)
			return unsized;
		return svgs
			.Where (item => item.Size is int s && s <= size)
			.OrderByDescending (item => item.Size)
			.Select (item => item.Path)
			.FirstOrDefault ()
			?? svgs.OrderBy (item => item.Size ?? int.MaxValue).Select (item => item.Path).FirstOrDefault ();
	}

	static List<int> ExistingSizes (string[] pngs, string stem)
	{
		var sizes = new List<int> ();
		string prefix = stem + "-";
		foreach (string png in pngs) {
			(string pngStem, int? size) = ParseStem (Path.GetFileNameWithoutExtension (png));
			if (size is int s && string.Equals (pngStem, stem, StringComparison.OrdinalIgnoreCase))
				sizes.Add (s);
		}
		sizes.Sort ();
		return sizes;
	}

	static (string Stem, int? Size) ParseStem (string name)
	{
		var match = SizedName.Match (name);
		if (match.Success && int.TryParse (match.Groups[2].Value, out int size) && size > 0)
			return (match.Groups[1].Value, size);
		return (name, null);
	}
}
