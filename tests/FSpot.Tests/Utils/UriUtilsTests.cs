// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Utils.Tests
{
	[TestClass]
	public class UriUtilsTests
	{
		[TestMethod]
		public void EscapeString_Null_ReturnsEmpty ()
		{
			Assert.AreEqual (string.Empty, UriUtils.EscapeString (null, false, false, false));
		}

		[TestMethod]
		public void EscapeString_EscapesSpaceAndLeavesUnreserved ()
		{
			Assert.AreEqual ("a%20b-_.", UriUtils.EscapeString ("a b-_.", false, false, false));
		}

		[TestMethod]
		public void EscapeString_PreservesAnExistingHexEscape ()
		{
			Assert.AreEqual ("%20keep", UriUtils.EscapeString ("%20keep", false, false, false));
		}

		[TestMethod]
		public void EscapeString_ReservedOnlyWhenAsked ()
		{
			Assert.AreEqual ("a/b", UriUtils.EscapeString ("a/b", false, false, false));
			Assert.AreEqual ("a%2Fb", UriUtils.EscapeString ("a/b", true, false, false));
		}

		[TestMethod]
		public void EscapeString_BracketsAndHashFollowTheirFlags ()
		{
			Assert.AreEqual ("[x]", UriUtils.EscapeString ("[x]", false, false, false));
			Assert.AreEqual ("%5Bx%5D", UriUtils.EscapeString ("[x]", false, false, true));
			Assert.AreEqual ("a#b", UriUtils.EscapeString ("a#b", false, false, false));
			Assert.AreEqual ("a%23b", UriUtils.EscapeString ("a#b", false, true, false));
		}
	}
}
