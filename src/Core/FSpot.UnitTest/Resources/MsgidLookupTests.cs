// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.UnitTest.Resources
{
	[TestClass]
	public class MsgidLookupTests
	{
		[TestCleanup]
		public void RestoreCulture ()
		{
			Strings.Culture = null;
		}

		[TestMethod]
		public void GetString_UnknownMsgid_ReturnsTheMsgid ()
		{
			Assert.AreEqual ("not a real string", MsgidLookup.GetString ("not a real string"));
		}

		[TestMethod]
		public void GetString_EnglishMsgid_ReturnsInvariantWhenCultureIsNeutral ()
		{
			Strings.Culture = CultureInfo.InvariantCulture;

			Assert.AreEqual ("Rotate _Left", MsgidLookup.GetString ("Rotate _Left"));
		}

		[TestMethod]
		public void GetString_GermanCulture_ReturnsPoTranslationForAKnownMsgid ()
		{
			Strings.Culture = new CultureInfo ("de");

			Assert.AreEqual ("_Links drehen", MsgidLookup.GetString ("Rotate _Left"));
			Assert.AreEqual ("Foto kopieren", MsgidLookup.GetString ("Copy Photo"));
		}
	}
}
