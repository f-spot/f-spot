// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class HoverPreviewTests
{
	[UiTestMethod]
	public void Edge_IsOneThirdOfTheAverageScreenEdge ()
	{
		Assert.AreEqual (500, HoverPreview.Edge (1920, 1080));
		Assert.AreEqual (1, HoverPreview.Edge (0, 0));
		Assert.AreEqual (100, HoverPreview.Edge (600, 0));
	}

	[UiTestMethod]
	public void Caption_IncludesTheDescriptionTimeAndName ()
	{
		var time = new DateTime (2026, 6, 1, 15, 0, 0);
		string caption = HoverPreview.Caption ("Picnic", time, "beach.jpg");

		Assert.AreEqual ("Picnic" + Environment.NewLine + $"{time}   beach.jpg", caption);
	}

	[UiTestMethod]
	public void Caption_OmitsABlankDescription ()
	{
		var time = new DateTime (2026, 6, 1);
		Assert.AreEqual ($"{time}   beach.jpg", HoverPreview.Caption (string.Empty, time, "beach.jpg"));
		Assert.AreEqual ($"{time}   beach.jpg", HoverPreview.Caption (null, time, "beach.jpg"));
	}
}
