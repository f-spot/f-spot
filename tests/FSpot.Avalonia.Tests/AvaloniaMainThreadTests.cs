// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class AvaloniaMainThreadTests
{
	[TestMethod]
	public void BackgroundProxy_UsesTheHandlerInsteadOfThrowing ()
	{
		var posted = new List<InvokeHandler> ();
		AvaloniaMainThread.Install (posted.Add);

		Exception? caught = null;
		using var done = new ManualResetEventSlim ();
		var worker = new Thread (() => {
			try {
				ThreadAssist.ProxyToMain (() => { });
			} catch (Exception ex) {
				caught = ex;
			}
			done.Set ();
		});
		worker.Start ();

		Assert.IsTrue (done.Wait (TimeSpan.FromSeconds (2)));
		Assert.IsNull (caught);
		Assert.HasCount (1, posted);
	}
}
