// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public class IconNameMapTests
	{
		[TestMethod]
		public void StockId_MapsFreedesktopPlaybackNamesToGtkStock ()
		{
			Assert.AreEqual ("gtk-media-play", IconNameMap.StockId ("media-playback-start"));
			Assert.AreEqual ("gtk-media-pause", IconNameMap.StockId ("media-playback-pause"));
		}

		[TestMethod]
		public void StockId_LeavesUnknownNamesUnchanged ()
		{
			Assert.AreEqual ("object-rotate-left", IconNameMap.StockId ("object-rotate-left"));
			Assert.AreEqual ("gtk-add", IconNameMap.StockId ("gtk-add"));
		}
	}
}
