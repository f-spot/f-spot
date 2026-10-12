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
	public async Task ADecodeInsideTheAsyncSlotDoesNotWaitForItself ()
	{
		int value = await DecodeGate.RunAsync (() => DecodeGate.Run (() => 7), TestContext.CancellationToken);
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
