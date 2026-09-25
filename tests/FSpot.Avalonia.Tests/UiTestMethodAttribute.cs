// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// Headless Avalonia owns one dispatcher thread. Test methods run on the pool,
// so anything that constructs a control has to hop onto that dispatcher first.
public sealed class UiTestMethodAttribute : TestMethodAttribute
{
	public UiTestMethodAttribute ([System.Runtime.CompilerServices.CallerFilePath] string declaringFilePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int declaringLineNumber = -1)
		: base (declaringFilePath, declaringLineNumber)
	{
	}

	public override Task<TestResult[]> ExecuteAsync (ITestMethod testMethod)
	{
		AvaloniaApp.Ensure ();
		if (Dispatcher.UIThread.CheckAccess ())
			return base.ExecuteAsync (testMethod);

		var done = new TaskCompletionSource<TestResult[]> (TaskCreationOptions.RunContinuationsAsynchronously);
		Dispatcher.UIThread.Post (() => {
			base.ExecuteAsync (testMethod).ContinueWith (finished => {
				if (finished.IsCanceled)
					done.SetCanceled ();
				else if (finished.IsFaulted)
					done.SetException (finished.Exception!.InnerExceptions);
				else
					done.SetResult (finished.Result);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
		});
		return done.Task;
	}
}
