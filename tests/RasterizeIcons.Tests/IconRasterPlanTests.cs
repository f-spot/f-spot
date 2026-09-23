// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RasterizeIcons.Tests;

[TestClass]
public sealed class IconRasterPlanTests
{
	string root = null!;
	string src = null!;
	string dest = null!;

	[TestInitialize]
	public void Setup ()
	{
		root = Path.Combine (Path.GetTempPath (), "icon-plan-" + Guid.NewGuid ().ToString ("N"));
		src = Path.Combine (root, "src");
		dest = Path.Combine (root, "out");
		Directory.CreateDirectory (src);
		Directory.CreateDirectory (dest);
	}

	[TestCleanup]
	public void Cleanup ()
	{
		if (Directory.Exists (root))
			Directory.Delete (root, recursive: true);
	}

	[TestMethod]
	public void SizedSvg_MapsToTheSameStemPng ()
	{
		File.WriteAllText (Path.Combine (src, "tag-16.svg"), "<svg/>");
		var jobs = IconRasterPlan.Create (src, dest);
		Assert.HasCount (1, jobs);
		Assert.AreEqual (16, jobs[0].Size);
		Assert.AreEqual (Path.Combine (dest, "tag-16.png"), jobs[0].PngPath);
		Assert.AreEqual (Path.Combine (src, "tag-16.svg"), jobs[0].SvgPath);
		Assert.IsTrue (jobs[0].NeedsWrite ());
	}

	[TestMethod]
	public void UnsizedSvg_UsesExistingPngSizes ()
	{
		File.WriteAllText (Path.Combine (src, "camera-photo.svg"), "<svg/>");
		File.WriteAllBytes (Path.Combine (dest, "camera-photo-16.png"), [0]);
		File.WriteAllBytes (Path.Combine (dest, "camera-photo-32.png"), [0]);
		var sizes = IconRasterPlan.Create (src, dest).Select (job => job.Size).OrderBy (size => size).ToArray ();
		Assert.AreSequenceEqual (new[] { 16, 32 }, sizes);
	}

	[TestMethod]
	public void UnsizedSvg_WithoutPngs_UsesDefaultSizes ()
	{
		File.WriteAllText (Path.Combine (src, "new-icon.svg"), "<svg/>");
		var sizes = IconRasterPlan.Create (src, dest).Select (job => job.Size).OrderBy (size => size).ToArray ();
		Assert.AreSequenceEqual (IconRasterPlan.DefaultSizes, sizes);
	}

	[TestMethod]
	public void ExistingPngWithoutExactSvg_UsesNearestSizedSvg ()
	{
		File.WriteAllText (Path.Combine (src, "adjust-colors-22.svg"), "<svg/>");
		File.WriteAllBytes (Path.Combine (dest, "adjust-colors-24.png"), [0]);
		var jobs = IconRasterPlan.Create (src, dest);
		Assert.Contains (job => job.Size == 24 && job.SvgPath.EndsWith ("adjust-colors-22.svg", StringComparison.Ordinal), jobs);
	}

	[TestMethod]
	public void NeedsWrite_FalseWhenPngIsUpToDate ()
	{
		string svg = Path.Combine (src, "tag-16.svg");
		string png = Path.Combine (dest, "tag-16.png");
		File.WriteAllText (svg, "<svg/>");
		File.WriteAllBytes (png, [1]);
		File.SetLastWriteTimeUtc (png, File.GetLastWriteTimeUtc (svg).AddMinutes (1));
		var job = IconRasterPlan.Create (src, dest).Single ();
		Assert.IsFalse (job.NeedsWrite ());
	}

	[TestMethod]
	public void NeedsWrite_TrueWhenSvgIsNewer ()
	{
		string svg = Path.Combine (src, "tag-16.svg");
		string png = Path.Combine (dest, "tag-16.png");
		File.WriteAllBytes (png, [1]);
		File.WriteAllText (svg, "<svg/>");
		File.SetLastWriteTimeUtc (svg, File.GetLastWriteTimeUtc (png).AddMinutes (1));
		var job = IconRasterPlan.Create (src, dest).Single ();
		Assert.IsTrue (job.NeedsWrite ());
	}

	[TestMethod]
	public void WritePng_CreatesAPngWhenMissing ()
	{
		string svg = Path.Combine (src, "dot.svg");
		string png = Path.Combine (dest, "dot-16.png");
		File.WriteAllText (svg, """<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16"><rect width="16" height="16" fill="#f00"/></svg>""");
		IconRasterizer.WritePng (new IconRasterJob (svg, png, 16));
		Assert.IsTrue (File.Exists (png));
		Assert.IsGreaterThan (32, new FileInfo (png).Length);
	}

	[TestMethod]
	public void PngOnlyArtwork_IsNotPlanned ()
	{
		File.WriteAllBytes (Path.Combine (dest, "crop-22.png"), [0]);
		Assert.IsEmpty (IconRasterPlan.Create (src, dest));
	}
}
