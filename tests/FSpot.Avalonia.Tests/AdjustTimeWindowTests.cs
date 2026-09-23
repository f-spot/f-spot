// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AdjustTimeWindowTests
{
	[TestMethod]
	public void TryParseHours_AcceptsASignedNumber ()
	{
		Assert.IsTrue (AdjustTimeWindow.TryParseHours ("1.5", out double hours));
		Assert.AreEqual (1.5, hours);
		Assert.IsTrue (AdjustTimeWindow.TryParseHours ("-3", out hours));
		Assert.AreEqual (-3, hours);
	}

	[TestMethod]
	public void TryParseHours_RejectsBlankAndWords ()
	{
		Assert.IsFalse (AdjustTimeWindow.TryParseHours ("  ", out _));
		Assert.IsFalse (AdjustTimeWindow.TryParseHours ("noon", out _));
		Assert.IsFalse (AdjustTimeWindow.TryParseHours (null, out _));
	}
}
