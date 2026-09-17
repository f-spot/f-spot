// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public sealed class LoupeLayoutTests
	{
		[TestMethod]
		public void ForPointer_PutsTheHotspotOnThePointerNotTheLensCenter ()
		{
			var layout = LoupeLayout.ForPointer (100, 80, angle: System.Math.PI / 4, radius: 128, inner: 128, border: 6);

			Assert.AreEqual (100, layout.WindowX + layout.HotspotX, 0.5);
			Assert.AreEqual (80, layout.WindowY + layout.HotspotY, 0.5);
			Assert.AreNotEqual (layout.LensX, layout.HotspotX);
			Assert.AreNotEqual (layout.LensY, layout.HotspotY);
			Assert.IsGreaterThan (layout.Radius * 2, layout.Width);
			Assert.IsGreaterThan (layout.Radius * 2, layout.Height);
		}

		[TestMethod]
		public void StemEnd_LiesAlongTheHandleAwayFromTheLens ()
		{
			var layout = LoupeLayout.ForPointer (0, 0, angle: 0, radius: 128, inner: 40, border: 6);
			Assert.AreEqual (layout.LensX + layout.Radius + layout.Border + layout.Inner, layout.StemEndX, 0.5);
			Assert.AreEqual (layout.LensY, layout.StemEndY, 0.5);
		}

		[TestMethod]
		public void ForPointer_DefaultsMatchGtkStartingInnerAndRadius ()
		{
			var layout = LoupeLayout.ForPointer (0, 0);
			Assert.AreEqual (LoupeRegion.DefaultRadius, layout.Radius);
			Assert.AreEqual (LoupeRegion.DefaultRadius, layout.Inner);
		}
	}
}
