// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Threading;

using FSpot.Avalonia;

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
			return RunAsync (testMethod);

		// InvokeAsync waits for the test task itself. A posted ContinueWith left that
		// task unobserved if the continuation failed before it reported the result.
		return Dispatcher.UIThread.InvokeAsync (() => RunAsync (testMethod));
	}

	// App.InitializeCore installs this before the window loads a catalog. These tests
	// never start App, and LoadCatalog's PhotoQuery delivers commits through MainThread.
	Task<TestResult[]> RunAsync (ITestMethod testMethod)
	{
		AvaloniaMainThread.InstallOnDispatcher ();
		return base.ExecuteAsync (testMethod);
	}
}
