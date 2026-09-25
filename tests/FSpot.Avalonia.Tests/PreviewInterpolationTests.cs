// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PreviewInterpolationTests
{
	[UiTestMethod]
	public void Apply_SetsHighQualityWhenInterpolationIsOn ()
	{
		AvaloniaApp.Ensure ();
		var image = new Image ();
		PreviewInterpolation.Apply (image, true);
		Assert.AreEqual (BitmapInterpolationMode.HighQuality, RenderOptions.GetBitmapInterpolationMode (image));
		PreviewInterpolation.Apply (image, false);
		Assert.AreEqual (BitmapInterpolationMode.None, RenderOptions.GetBitmapInterpolationMode (image));
	}
}
