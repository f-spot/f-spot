// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	[TestClass]
	public class CctTableTests
	{
		[TestMethod]
		public void GetXY_ReturnsTheEndsOfTheTable ()
		{
			CctTable.GetXY (1000, out double lowX, out double lowY);
			CctTable.GetXY (25000, out double highX, out double highY);

			Assert.AreEqual (0.652756059, lowX, 1e-9);
			Assert.AreEqual (0.344456906, lowY, 1e-9);
			Assert.AreEqual (0.252521543, highX, 1e-9);
			Assert.AreEqual (0.25222221, highY, 1e-9);
		}

		[TestMethod]
		public void GetXY_RejectsATemperatureOutsideTheTable ()
		{
			var below = Assert.ThrowsExactly<System.ArgumentOutOfRangeException> (() => CctTable.GetXY (999, out _, out _));
			var above = Assert.ThrowsExactly<System.ArgumentOutOfRangeException> (() => CctTable.GetXY (25001, out _, out _));

			Assert.AreEqual ("temp", below.ParamName);
			Assert.AreEqual ("temp", above.ParamName);
		}
	}
}
