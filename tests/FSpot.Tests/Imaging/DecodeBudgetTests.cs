// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Imaging
{
	[TestClass]
	public sealed class DecodeBudgetTests
	{
		[TestMethod]
		public void Exceeds_AllowsTheCapAndRefusesOnePixelPastIt ()
		{
			// 16000×12000 is exactly 192 megapixels. One more row is over.
			Assert.IsFalse (DecodeBudget.Exceeds (16000, 12000));
			Assert.IsTrue (DecodeBudget.Exceeds (16000, 12001));
			Assert.IsTrue (DecodeBudget.Exceeds (50000, 50000));
		}

		[TestMethod]
		public void Exceeds_RefusesANonPositiveDimension ()
		{
			Assert.IsTrue (DecodeBudget.Exceeds (0, 10));
			Assert.IsTrue (DecodeBudget.Exceeds (10, -1));
		}

		[TestMethod]
		public void ThrowIfExceeds_NamesTheSharedFailure ()
		{
			var missed = Assert.ThrowsExactly<InvalidDataException> (() =>
				DecodeBudget.ThrowIfExceeds (50000, 50000));
			Assert.AreEqual (DecodeBudget.TooLargeMessage, missed.Message);
			Assert.AreEqual ("The image is too large to decode.", missed.Message);

			DecodeBudget.ThrowIfExceeds (100, 50);
		}

		[TestMethod]
		public void LimitForTest_LowersWhatExceedsReadsUntilDisposed ()
		{
			using (DecodeBudget.LimitForTest (1))
				Assert.IsTrue (DecodeBudget.Exceeds (4, 4));

			Assert.IsFalse (DecodeBudget.Exceeds (4, 4));
		}

		[TestMethod]
		public void LimitForTest_LetsAnotherThreadReadTheLoweredCap ()
		{
			bool exceeded = false;
			Exception error = null;
			var limit = DecodeBudget.LimitForTest (1);
			var reader = new Thread (() => {
				try {
					exceeded = DecodeBudget.Exceeds (4, 4);
				} catch (Exception ex) {
					error = ex;
				}
			});
			try {
				reader.Start ();
				bool finished = reader.Join (TimeSpan.FromSeconds (2));
				Assert.IsTrue (finished, "a reader blocked on the test limit");
				Assert.IsNull (error);
				Assert.IsTrue (exceeded);
			} finally {
				limit.Dispose ();
				reader.Join (TimeSpan.FromSeconds (2));
			}
		}
	}
}
