// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class CancelSlotTests
{
	[TestMethod]
	public void Renew_CancelsThePreviousSourceAndTheOwnerDisposesIt ()
	{
		CancellationTokenSource? slot = null;
		var first = CancelSlot.Renew (ref slot);
		var second = CancelSlot.Renew (ref slot);

		Assert.IsTrue (first.IsCancellationRequested);
		_ = first.Token;
		Assert.IsFalse (second.IsCancellationRequested);

		CancelSlot.Finish (ref slot, first);
		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = first.Token);
		_ = second.Token;

		CancelSlot.Finish (ref slot, second);
		Assert.ThrowsExactly<ObjectDisposedException> (() => _ = second.Token);
		Assert.IsNull (slot);
	}

	[TestMethod]
	public void Release_DisposesTheSharedSourceAfterTheLoadLeaves ()
	{
		var shared = new SharedCancel ();
		var token = shared.Enter ();

		shared.Release ();

		Assert.IsFalse (shared.IsDisposed);
		Assert.ThrowsExactly<OperationCanceledException> (() => token.ThrowIfCancellationRequested ());
		shared.Leave ();
		Assert.IsTrue (shared.IsDisposed);
	}
}
