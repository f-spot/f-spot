// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;

using FSpot.Avalonia.Controls;
using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LoupeOverlayTests
{
	[TestMethod]
	public void Layout_OffsetsTheLensFromThePointerHotspot ()
	{
		AvaloniaApp.Ensure ();
		var overlay = new LoupeOverlay { HotspotX = 100, HotspotY = 80 };
		var canvas = new Canvas { Width = 400, Height = 300 };
		canvas.Children.Add (overlay);
		var window = new Window {
			Width = 400,
			Height = 300,
			Content = canvas
		};
		window.Show ();

		var layout = LoupeLayout.ForPointer (100, 80);
		Assert.AreEqual (layout.WindowX, Canvas.GetLeft (overlay), 0.5);
		Assert.AreEqual (layout.WindowY, Canvas.GetTop (overlay), 0.5);
		Assert.AreEqual (layout.Width, overlay.Width, 0.5);
		Assert.AreEqual (layout.Height, overlay.Height, 0.5);
		Assert.AreNotEqual (layout.LensX, layout.HotspotX);
		Assert.IsGreaterThan (layout.Radius * 2, overlay.Width);
	}
}
