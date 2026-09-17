// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public class StringsUtilsTests
	{
		[TestMethod]
		[DataRow ("", "")]
		[DataRow ("plain", "plain")]
		[DataRow ("café", "cafe")]
		[DataRow ("naïve", "naive")]
		[DataRow ("Ångström", "Angstrom")]
		[DataRow ("Ørsted", "Orsted")]
		[DataRow ("Łódź", "Lodz")]
		[DataRow ("Đorđe", "Dorde")]
		public void Simplify_FoldsDiacriticsAndStrokedLetters (string input, string expected)
		{
			Assert.AreEqual (expected, StringsUtils.Simplify (input));
		}
	}
}
