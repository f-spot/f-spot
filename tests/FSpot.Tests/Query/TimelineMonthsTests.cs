// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace FSpot.Query.Tests
{
	[TestClass]
	public sealed class TimelineMonthsTests
	{
		[TestMethod]
		public void Group_Empty_ReturnsNoBuckets ()
		{
			Assert.IsEmpty (TimelineMonths.Group (Array.Empty<IPhoto> ()));
		}

		[TestMethod]
		public void Group_CountsAndOrdersByMonthStart ()
		{
			var photos = new[] {
				Photo (new DateTime (2026, 12, 31)),
				Photo (new DateTime (2026, 1, 1)),
				Photo (new DateTime (2026, 1, 15)),
				Photo (new DateTime (2026, 12, 1))
			};

			var buckets = TimelineMonths.Group (photos);

			Assert.HasCount (2, buckets);
			Assert.AreEqual (new DateTime (2026, 1, 1), buckets[0].Start);
			Assert.AreEqual (new DateTime (2026, 2, 1), buckets[0].End);
			Assert.AreEqual (2, buckets[0].Count);
			Assert.AreEqual (new DateTime (2026, 12, 1), buckets[1].Start);
			Assert.AreEqual (new DateTime (2027, 1, 1), buckets[1].End);
			Assert.AreEqual (2, buckets[1].Count);
		}

		[TestMethod]
		public void Group_DecemberRollsEndIntoTheNextYear ()
		{
			var bucket = TimelineMonths.Group (new[] { Photo (new DateTime (2025, 12, 25)) }).Single ();

			Assert.AreEqual (new DateTime (2025, 12, 1), bucket.Start);
			Assert.AreEqual (new DateTime (2026, 1, 1), bucket.End);
		}

		[TestMethod]
		public void Span_Empty_ReturnsNoBuckets ()
		{
			Assert.IsEmpty (TimelineMonths.Span (Array.Empty<IPhoto> ()));
		}

		[TestMethod]
		public void Span_OmitsMonthsThatHaveNoPhotos ()
		{
			var photos = new[] {
				Photo (new DateTime (2026, 1, 15)),
				Photo (new DateTime (2026, 1, 20)),
				Photo (new DateTime (2026, 12, 1))
			};

			var buckets = TimelineMonths.Span (photos);

			Assert.HasCount (2, buckets);
			Assert.AreEqual (new DateTime (2026, 1, 1), buckets[0].Start);
			Assert.AreEqual (2, buckets[0].Count);
			Assert.AreEqual (new DateTime (2026, 12, 1), buckets[1].Start);
			Assert.AreEqual (1, buckets[1].Count);
			Assert.IsFalse (buckets.Any (bucket => bucket.Count == 0));
		}

		[TestMethod]
		public void Span_CrossesYearBoundaries ()
		{
			var buckets = TimelineMonths.Span (new[] {
				Photo (new DateTime (2025, 12, 25)),
				Photo (new DateTime (2026, 2, 1))
			});

			Assert.HasCount (2, buckets);
			Assert.AreEqual (new DateTime (2025, 12, 1), buckets[0].Start);
			Assert.AreEqual (1, buckets[0].Count);
			Assert.AreEqual (new DateTime (2026, 2, 1), buckets[1].Start);
			Assert.AreEqual (1, buckets[1].Count);
		}

		[TestMethod]
		public void InclusiveRange_MapsMinAndMaxMonthIndicesToADateSpan ()
		{
			var months = TimelineMonths.Span (new[] {
				Photo (new DateTime (2026, 1, 15)),
				Photo (new DateTime (2026, 6, 1))
			});

			var span = TimelineMonths.InclusiveRange (months, 0, 1);
			Assert.AreEqual (new DateTime (2026, 1, 1), span.Start);
			Assert.AreEqual (new DateTime (2026, 7, 1), span.End);

			var swapped = TimelineMonths.InclusiveRange (months, 1, 0);
			Assert.AreEqual (span.Start, swapped.Start);
			Assert.AreEqual (span.End, swapped.End);
		}

		[TestMethod]
		public void InclusiveRange_SingleIndex_IsThatMonth ()
		{
			var months = TimelineMonths.Span (new[] {
				Photo (new DateTime (2026, 3, 10)),
				Photo (new DateTime (2026, 5, 1))
			});
			var span = TimelineMonths.InclusiveRange (months, 0, 0);
			Assert.AreEqual (new DateTime (2026, 3, 1), span.Start);
			Assert.AreEqual (new DateTime (2026, 4, 1), span.End);
		}

		static IPhoto Photo (DateTime time)
		{
			var mock = new Mock<IPhoto> ();
			mock.Setup (p => p.Time).Returns (time);
			return mock.Object;
		}
	}
}
