// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class MacDisplayLinkTests
{
	[TestMethod]
	public void Wait_ReturnsWhenADisplayLinkAppears ()
	{
		int tries = 0;
		long now = 0;
		int sleeps = 0;

		bool ready = MacDisplayLink.Wait (
			() => ++tries == 3,
			() => now,
			budgetMs: 5_000,
			() => {
				sleeps++;
				now += 200;
			});

		Assert.IsTrue (ready);
		Assert.AreEqual (3, tries);
		Assert.AreEqual (2, sleeps);
	}

	[TestMethod]
	public void Wait_GivesUpWhenNoDisplayAppears ()
	{
		long now = 0;
		int sleeps = 0;

		bool ready = MacDisplayLink.Wait (
			() => false,
			() => now,
			budgetMs: 500,
			() => {
				sleeps++;
				now += 200;
			});

		Assert.IsFalse (ready);
		Assert.IsTrue (sleeps is > 0 and < 10);
	}
}
