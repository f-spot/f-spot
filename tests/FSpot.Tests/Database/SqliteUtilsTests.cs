// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Database.Tests
{
	[TestClass]
	public class SqliteUtilsTests
	{
		enum Sample { Zero, One, Two }

		[TestMethod]
		public void FromDbFormat_MapsNullValueTypesToTheirDefault ()
		{
			Assert.AreEqual (DateTime.MinValue, SqliteUtils.FromDbFormat (typeof (DateTime), null));
			Assert.AreEqual (TimeSpan.MinValue, SqliteUtils.FromDbFormat (typeof (TimeSpan), null));
			Assert.AreEqual (0, SqliteUtils.FromDbFormat (typeof (int), DBNull.Value));
			Assert.IsNull (SqliteUtils.FromDbFormat (typeof (string), DBNull.Value));
		}

		[TestMethod]
		public void FromDbFormat_ConvertsUnixTimeAndMilliseconds ()
		{
			const long unix = 1_000_000;
			Assert.AreEqual (DateTimeUtil.ToDateTime (unix), SqliteUtils.FromDbFormat (typeof (DateTime), unix));
			Assert.AreEqual (TimeSpan.FromMilliseconds (1500), SqliteUtils.FromDbFormat (typeof (TimeSpan), 1500L));
		}

		[TestMethod]
		public void FromDbFormat_ReadsBoolAndEnumFromIntegers ()
		{
			Assert.IsTrue ((bool)SqliteUtils.FromDbFormat (typeof (bool), 1L));
			Assert.IsFalse ((bool)SqliteUtils.FromDbFormat (typeof (bool), 0L));
			Assert.AreEqual (Sample.Two, SqliteUtils.FromDbFormat (typeof (Sample), 2));
		}

		[TestMethod]
		public void NormalizeDbValue_PromotesSmallIntegersAndFloats ()
		{
			Assert.IsNull (SqliteUtils.NormalizeDbValue (DBNull.Value));
			Assert.AreEqual (7L, SqliteUtils.NormalizeDbValue (7));
			Assert.AreEqual (7L, SqliteUtils.NormalizeDbValue ((short)7));
			Assert.AreEqual (1.5d, SqliteUtils.NormalizeDbValue (1.5f));
			Assert.AreEqual ("keep", SqliteUtils.NormalizeDbValue ("keep"));
		}

		[TestMethod]
		public void GetAs_NarrowsLongAndDoubleToTheRequestedType ()
		{
			Assert.AreEqual (3, SqliteUtils.GetAs (3L, typeof (int)));
			Assert.AreEqual (3u, SqliteUtils.GetAs (3L, typeof (uint)));
			Assert.AreEqual (1.25f, SqliteUtils.GetAs (1.25d, typeof (float)));
			Assert.AreEqual ("x", SqliteUtils.GetAs ("x", typeof (string)));
		}
	}
}
