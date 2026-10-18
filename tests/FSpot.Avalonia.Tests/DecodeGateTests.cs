// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Threading;

using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Printing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class DecodeGateTests
{
	public TestContext TestContext { get; }

	public DecodeGateTests (TestContext testContext) => TestContext = testContext;

	[TestMethod]
	public void Run_AlreadyCancelled_DoesNotDecodeAndLeavesTheGateFree ()
	{
		using var hold = DecodeGate.Enter ();
		using var cts = new CancellationTokenSource ();
		cts.Cancel ();
		int calls = 0;
		Exception? error = null;
		using var finished = new ManualResetEventSlim (false);
		var thread = new Thread (() => {
			try {
				DecodeGate.Run (() => {
					calls++;
					return 1;
				}, cts.Token);
			} catch (Exception ex) {
				error = ex;
			} finally {
				finished.Set ();
			}
		}) {
			IsBackground = true,
		};
		thread.Start ();

		try {
			// Wait(token) throws before taking a permit. Wait() would sit here until the hold ends.
			bool completedWhileHeld = finished.Wait (TimeSpan.FromSeconds (2), TestContext.CancellationToken);
			Assert.IsTrue (completedWhileHeld, "a cancelled Run waited for a gate another decode still held");
			Assert.IsInstanceOfType<OperationCanceledException> (error);
			Assert.AreEqual (0, calls);
		} finally {
			hold.Dispose ();
			finished.Wait (TimeSpan.FromSeconds (2), TestContext.CancellationToken);
			thread.Join (TimeSpan.FromSeconds (2));
		}

		Assert.AreEqual (4, DecodeGate.Run (() => 4, TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task ADecodeInsideTheAsyncSlotDoesNotWaitForItself ()
	{
		int value = await DecodeGate.RunAsync (() => DecodeGate.Run (() => 7, TestContext.CancellationToken), TestContext.CancellationToken);
		Assert.AreEqual (7, value);
	}

	[UiTestMethod]
	public async Task ASecondPhotoDecodeWaitsUntilTheFirstLeaves ()
	{
		AvaloniaApp.Ensure ();
		string path = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		await AssertDecodeWaitsAsync (() => {
			using (AvaloniaPhotoDecoder.DecodeFile (path, 32)) {
			}
		});
	}

	[UiTestMethod]
	public async Task APrintDecodeWaitsUntilThePhotoDecodeLeaves ()
	{
		AvaloniaApp.Ensure ();
		string path = Path.Combine (AppContext.BaseDirectory, "taglib-sample.jpg");
		await AssertDecodeWaitsAsync (() => {
			using (PrintImageLoader.Load (path, 32)) {
			}
		});
	}

	async Task AssertDecodeWaitsAsync (Action decode)
	{
		var token = TestContext.CancellationToken;
		using var held = DecodeGate.Enter ();
		Exception? error = null;
		using var finished = new ManualResetEventSlim (false);
		var worker = Task.Run (() => {
			try {
				decode ();
			} catch (Exception ex) {
				error = ex;
			}

			finished.Set ();
		}, token);

		bool ranWhileHeld = finished.Wait (TimeSpan.FromMilliseconds (300), token);
		held.Dispose ();
		Assert.IsTrue (finished.Wait (TimeSpan.FromSeconds (10), token));
		await worker.WaitAsync (token);
		Assert.IsNull (error);
		Assert.IsFalse (ranWhileHeld);
	}
}
