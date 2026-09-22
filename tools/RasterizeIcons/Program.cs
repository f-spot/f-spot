// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using RasterizeIcons;

if (args.Length < 1) {
	Console.Error.WriteLine ("usage: RasterizeIcons <icons-dir>");
	return 1;
}

string icons = Path.GetFullPath (args[0]);
string src = Path.Combine (icons, "src");
var jobs = IconRasterPlan.Create (src, icons);
int written = 0;
int failed = 0;
foreach (var job in jobs) {
	if (!job.NeedsWrite ())
		continue;
	try {
		IconRasterizer.WritePng (job);
		written++;
		Console.WriteLine (Path.GetRelativePath (icons, job.PngPath));
	} catch (Exception ex) {
		failed++;
		Console.Error.WriteLine ($"{Path.GetRelativePath (icons, job.SvgPath)}: {ex.Message}");
	}
}

Console.WriteLine (written == 0 && failed == 0 ? "RasterizeIcons: up to date" : $"RasterizeIcons: wrote {written} png(s), {failed} failed");
return failed == 0 ? 0 : 1;
