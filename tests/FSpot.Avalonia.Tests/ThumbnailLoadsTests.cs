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
		loads.EndIfCurrent (item, first.Token);

		Assert.IsTrue (loads.IsLoading (item));
		Assert.IsFalse (second.IsCancellationRequested);

		loads.EndIfCurrent (item, second.Token);
		Assert.IsFalse (loads.IsLoading (item));
		Assert.IsTrue (second.IsCancellationRequested);
		loads.Finish (first);
		loads.Finish (second);
	}

	[TestMethod]
	public void End_CancelsTheLoadWithoutDisposingItsToken ()
	{
		var loads = new ThumbnailLoads ();
		var source = loads.Begin (new object ());

		loads.EndAll ();

		// The decode is still returning from its wait. Dispose here makes Token
		// throw ObjectDisposedException, which is not cancellation.
		Assert.IsTrue (source.IsCancellationRequested);
		_ = source.Token;
		loads.Finish (source);
		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = source.Token);
	}
}
