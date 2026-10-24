// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.ViewModels;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AdjustTimePhotoItemTests
{
	[TestMethod]
	public void TimeCaption_NamesYearMonthDayAndA24HourClock ()
	{
		var item = new AdjustTimePhotoItem ("a.jpg", new DateTime (2026, 1, 2, 15, 4, 5), "/a.jpg");
		Assert.AreEqual ("2026-01-02 15:04:05", item.TimeCaption);
	}
}
