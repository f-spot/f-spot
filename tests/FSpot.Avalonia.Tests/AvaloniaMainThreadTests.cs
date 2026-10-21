// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AvaloniaMainThreadTests
{
	public TestContext TestContext { get; }

	public AvaloniaMainThreadTests (TestContext testContext) => TestContext = testContext;

	[UiTestMethod]
	public void BackgroundProxy_UsesTheHandlerInsteadOfThrowing ()
	{
		var posted = new List<Action> ();
		AvaloniaMainThread.Install (posted.Add);

		Exception? caught = null;
		using var done = new ManualResetEventSlim ();
		var worker = new Thread (() => {
			try {
				MainThread.Invoke (() => { });
			} catch (Exception ex) {
				caught = ex;
			}
			done.Set ();
		});
		worker.Start ();

		Assert.IsTrue (done.Wait (TimeSpan.FromSeconds (2), TestContext.CancellationToken));
		Assert.IsNull (caught);
		Assert.HasCount (1, posted);
	}
}
