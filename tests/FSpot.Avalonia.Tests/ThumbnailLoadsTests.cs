// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ThumbnailLoadsTests
{
	[TestMethod]
	public void EndIfCurrent_LeavesANewerLoadAlone ()
	{
		var loads = new ThumbnailLoads ();
		var item = new object ();
		var first = loads.Begin (item);
		var second = loads.Begin (item);

		Assert.IsTrue (first.IsCancellationRequested);
		loads.EndIfCurrent (item, first);

		Assert.IsTrue (loads.IsLoading (item));
		Assert.IsFalse (second.IsCancellationRequested);

		loads.EndIfCurrent (item, second);
		Assert.IsFalse (loads.IsLoading (item));
		Assert.IsTrue (second.IsCancellationRequested);
	}
}
