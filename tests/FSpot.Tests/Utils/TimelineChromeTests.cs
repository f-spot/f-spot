// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Utils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Utils
{
	[TestClass]
	public sealed class TimelineChromeTests
	{
		[TestMethod]
		public void MinHitPx_IsAtLeastTwentyFour ()
		{
			Assert.IsGreaterThanOrEqualTo (24, TimelineChrome.MinHitPx);
		}

		[TestMethod]
		public void DisabledAndFolderCountOpacity_StayReadable ()
		{
			Assert.IsGreaterThanOrEqualTo (0.5, TimelineChrome.DisabledOpacity);
			Assert.IsGreaterThanOrEqualTo (0.7, TimelineChrome.FolderCountOpacity);
		}
	}
}
