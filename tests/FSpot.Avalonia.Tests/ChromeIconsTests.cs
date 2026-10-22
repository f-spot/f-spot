// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.VisualTree;

using FSpot.Avalonia.Controls;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ChromeIconsTests
{
	static readonly string[] EditorNamesWithSvg = [
		"adjust-colors", "align-horizon", "autocolor", "color-desaturate", "color-sepia"
	];

	static readonly string[] TagNamesWithSvg = [
		"emblem-people", "emblem-places", "emblem-event"
	];

	[UiTestMethod]
	public void AppLogo_IsThePackedFSpotSvg ()
	{
		Assert.EndsWith (".svg", ChromeIcons.AppLogo);
		Assert.Contains ("f-spot.svg", ChromeIcons.AppLogo);
		Assert.StartsWith ("avares://", ChromeIcons.AppLogo);
		Assert.IsTrue (File.Exists (Path.Combine (IconsSrc (), "f-spot.svg")));
	}

	[UiTestMethod]
	public void WindowIcon_IsTheLargePngTheSwitcherCanScale ()
	{
		Assert.EndsWith (".png", ChromeIcons.WindowIcon);
		Assert.Contains ("f-spot-256.png", ChromeIcons.WindowIcon);
		string path = Path.Combine (RepoPaths.Root (), "icons", "f-spot-256.png");
		Assert.IsTrue (File.Exists (path));
		var (width, height) = PngSize (path);
		Assert.IsGreaterThanOrEqualTo (128, width);
		Assert.AreEqual (width, height);
		Assert.IsTrue (File.Exists (Path.Combine (IconsSrc (), "f-spot.svg")),
			"In-app logo stays SVG even though the OS window slot is PNG.");
	}

	static (int Width, int Height) PngSize (string path)
	{
		using var stream = File.OpenRead (path);
		Span<byte> header = stackalloc byte[24];
		Assert.AreEqual (header.Length, stream.Read (header));
		Assert.AreEqual (0x89, header[0]);
		int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
		int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
		return (width, height);
	}

	[UiTestMethod]
	public void PreferredFile_UsesSvgWhenBothSvgAndPngExist ()
	{
		var packed = new HashSet<string> (StringComparer.OrdinalIgnoreCase) {
			"adjust-colors-22.svg", "adjust-colors-22.png"
		};
		Assert.AreEqual ("adjust-colors-22.svg", ChromeIcons.PreferredFile ("adjust-colors", packed));
	}

	[UiTestMethod]
	public void PreferredFile_PrefersUnsizedSvg ()
	{
		var packed = new HashSet<string> (StringComparer.OrdinalIgnoreCase) {
			"emblem-people.svg", "emblem-people-16.svg"
		};
		Assert.AreEqual ("emblem-people.svg", ChromeIcons.PreferredFile ("emblem-people", packed));
	}

	[UiTestMethod]
	public void PreferredFile_FallsBackToPngWhenNoSvgExists ()
	{
		var packed = new HashSet<string> (StringComparer.OrdinalIgnoreCase) { "crop-22.png" };
		Assert.AreEqual ("crop-22.png", ChromeIcons.PreferredFile ("crop", packed));
		Assert.IsNull (ChromeIcons.PreferredFile ("missing", packed));
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void EditorIcons_UseSvgWhenIconsSrcHasTheArtwork ()
	{
		AvaloniaApp.Ensure ();
		foreach (string name in EditorNamesWithSvg) {
			string? uri = EditorIcons.UriFor (name);
			Assert.IsNotNull (uri, name);
			Assert.EndsWith (".svg", uri, name);
			Assert.StartsWith ("avares://FSpot.Avalonia/Icons/", uri, name);
			Assert.IsTrue (SrcHas (Path.GetFileName (uri)!), Path.GetFileName (uri));
		}
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void TagIcons_UseSvgWhenIconsSrcHasTheArtwork ()
	{
		AvaloniaApp.Ensure ();
		foreach (string name in TagNamesWithSvg) {
			string uri = TagIcons.UriFor (name);
			Assert.EndsWith (".svg", uri, name);
			Assert.IsTrue (SrcHas (Path.GetFileName (uri)!), Path.GetFileName (uri));
		}
		Assert.EndsWith (".svg", TagIcons.UriFor (null));
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void PngOnlyChrome_StaysPngWhenNoSvgExists ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsFalse (SrcHasStem ("emblem-favorite"));
		Assert.EndsWith (".png", TagIcons.UriFor ("emblem-favorite"));
		Assert.IsFalse (SrcHasStem ("crop"));
		Assert.EndsWith (".png", EditorIcons.UriFor ("crop"));
		Assert.IsFalse (SrcHasStem ("red-eye-remove"));
		Assert.EndsWith (".png", EditorIcons.UriFor ("red-eye-remove"));
		Assert.IsFalse (SrcHasStem ("filter-soft-focus"));
		Assert.EndsWith (".png", EditorIcons.UriFor ("filter-soft-focus"));
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void ChromeIcon_RendersPngUrisWithImageNotSvg ()
	{
		AvaloniaApp.Ensure ();
		string png = EditorIcons.UriFor ("crop")!;
		Assert.EndsWith (".png", png);
		var icon = ShownIcon (png);
		var svg = icon.GetVisualDescendants ().OfType<global::Avalonia.Svg.Skia.Svg> ().FirstOrDefault ();
		Assert.IsTrue (svg is null || svg.Path is null || !svg.Path.EndsWith (".png", StringComparison.OrdinalIgnoreCase));
		Assert.IsGreaterThanOrEqualTo (1, icon.GetVisualDescendants ().OfType<global::Avalonia.Controls.Image> ().Count ());
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void ChromeIcon_UsesAVectorSourceForSvg ()
	{
		AvaloniaApp.Ensure ();
		string svgUri = EditorIcons.UriFor ("adjust-colors")!;
		var icon = new ChromeIcon {
			Width = 22,
			Height = 22,
			Uri = svgUri
		};
		var image = Assert.IsInstanceOfType<global::Avalonia.Controls.Image> (icon.Children.Single ());
		Assert.IsNotInstanceOfType<global::Avalonia.Media.Imaging.Bitmap> (image.Source);
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void ChromeIcon_RendersSvgUrisAsAnImage ()
	{
		AvaloniaApp.Ensure ();
		string svgUri = EditorIcons.UriFor ("adjust-colors")!;
		Assert.EndsWith (".svg", svgUri);
		var icon = ShownIcon (svgUri);
		Assert.IsGreaterThanOrEqualTo (1, icon.GetVisualDescendants ().OfType<global::Avalonia.Controls.Image> ().Count ());
		var svg = icon.GetVisualDescendants ().OfType<global::Avalonia.Svg.Skia.Svg> ().FirstOrDefault ();
		Assert.IsTrue (svg is null || svg.Path is null || svg.Path.EndsWith (".svg", StringComparison.OrdinalIgnoreCase));
	}

	[UiTestMethod]
	[DoNotParallelize]
	public void PackedAppLogoSvg_CanBeOpened ()
	{
		AvaloniaApp.Ensure ();
		using var stream = AssetLoader.Open (new Uri (ChromeIcons.AppLogo));
		Assert.IsGreaterThan (0, stream.Length);
	}

	static ChromeIcon ShownIcon (string uri, double rasterScale = 1)
	{
		var icon = new ChromeIcon {
			Width = 22,
			Height = 22,
			RasterScale = rasterScale,
			Uri = uri
		};
		var window = new Window { Width = 40, Height = 40, Content = icon };
		window.Show ();
		icon.UpdateLayout ();
		return icon;
	}

	static bool SrcHas (string fileName)
		=> File.Exists (Path.Combine (IconsSrc (), fileName));

	static bool SrcHasStem (string stem)
		=> Directory.EnumerateFiles (IconsSrc (), "*.svg")
			.Select (path => StripSize (Path.GetFileNameWithoutExtension (path)))
			.Contains (stem);

	static string StripSize (string name)
		=> System.Text.RegularExpressions.Regex.Replace (name, @"-\d+$", string.Empty);

	static string IconsSrc ()
		=> Path.Combine (RepoPaths.Root (), "icons", "src");
}
