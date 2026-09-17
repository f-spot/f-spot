// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public class GtkIconSizeMapTests
	{
		// Gtk.IconSize.Invalid is 0. ResourceLoader used to RenderIcon with that for
		// every pixel size that was not 16 or 32, which is how "Invalid icon size 0"
		// showed up on Mac for FSpot-22 and FSpot-48 at startup.
		[TestMethod]
		[DataRow (16, 2)]  // SmallToolbar
		[DataRow (22, 3)]  // LargeToolbar
		[DataRow (24, 3)]
		[DataRow (32, 5)]  // Dnd
		[DataRow (48, 6)]  // Dialog
		public void TryMapPixels_MapsToolbarAndDialogSizes (int pixels, int gtkIconSize)
		{
			Assert.IsTrue (GtkIconSizeMap.TryMapPixels (pixels, out int mapped));
			Assert.AreEqual (gtkIconSize, mapped);
		}

		[TestMethod]
		[DataRow (0)]
		[DataRow (1)]
		[DataRow (64)]
		[DataRow (256)]
		public void TryMapPixels_DoesNotInventAStockSize (int pixels)
		{
			Assert.IsFalse (GtkIconSizeMap.TryMapPixels (pixels, out int mapped));
			Assert.AreEqual (0, mapped);
		}
	}
}
