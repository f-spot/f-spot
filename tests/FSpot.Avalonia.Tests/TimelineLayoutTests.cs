// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Controls;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class TimelineLayoutTests
{
	[TestMethod]
	public void Split_KeepsTheHandleUnderAShortChart ()
	{
		var chrome = TimelineLayout.Split (TimelineLayout.StripHeight);

		Assert.AreEqual (TimelineLayout.HandleHeight, chrome.Handle);
		Assert.AreEqual (TimelineLayout.LegendHeight, chrome.Legend);
		Assert.AreEqual (TimelineLayout.StripHeight, chrome.Legend + chrome.Bars + chrome.Handle, 0.01);
		Assert.IsGreaterThan (chrome.Handle, chrome.Bars);
	}

	[TestMethod]
	public void Measure_StretchesMonthsToFillTheStrip ()
	{
		var layout = TimelineLayout.Measure (4, 200, 24);

		Assert.AreEqual (50, layout.ColumnWidth);
		Assert.AreEqual (200, layout.ContentWidth);
		Assert.IsFalse (layout.Scrolls);
	}

	[TestMethod]
	public void Measure_KeepsTheMinimumWidthAndScrollsWhenCrowded ()
	{
		var layout = TimelineLayout.Measure (20, 200, 24);

		Assert.AreEqual (24, layout.ColumnWidth);
		Assert.AreEqual (480, layout.ContentWidth);
		Assert.IsTrue (layout.Scrolls);
	}

	[TestMethod]
	public void IndexAt_ReturnsTheColumnUnderThePointer ()
	{
		Assert.AreEqual (0, TimelineLayout.IndexAt (0, 50, 4));
		Assert.AreEqual (2, TimelineLayout.IndexAt (125, 50, 4));
		Assert.AreEqual (-1, TimelineLayout.IndexAt (200, 50, 4));
		Assert.AreEqual (-1, TimelineLayout.IndexAt (-1, 50, 4));
	}

	[TestMethod]
	public void Span_CoversTheInclusiveGlass ()
	{
		var (x, width) = TimelineLayout.Span (2, 0, 40);

		Assert.AreEqual (0, x);
		Assert.AreEqual (120, width);
	}

	[TestMethod]
	public void NearestFilled_SnapsAnEmptyMonthToTheCloserMonthWithPhotos ()
	{
		var filled = new[] { true, false, false, true };

		Assert.AreEqual (0, TimelineLayout.NearestFilled (filled, 0));
		Assert.AreEqual (0, TimelineLayout.NearestFilled (filled, 1));
		Assert.AreEqual (3, TimelineLayout.NearestFilled (filled, 2));
		Assert.AreEqual (1, TimelineLayout.NearestFilled ([false, true, false], 0));
		Assert.AreEqual (-1, TimelineLayout.NearestFilled ([false, false], 1));
		Assert.AreEqual (-1, TimelineLayout.NearestFilled ([], 0));
	}
}
