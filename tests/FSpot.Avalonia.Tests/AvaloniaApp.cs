// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace FSpot.Avalonia.Tests;

static class AvaloniaApp
{
	static readonly ManualResetEventSlim Ready = new (false);
	static int started;

	public static void Ensure ()
	{
		if (Interlocked.Exchange (ref started, 1) == 0) {
			var thread = new Thread (() => {
				AppBuilder.Configure<Application> ()
					.UseHeadless (new AvaloniaHeadlessPlatformOptions ())
					.SetupWithoutStarting ();
				Ready.Set ();
				Dispatcher.UIThread.MainLoop (CancellationToken.None);
			}) {
				IsBackground = true,
				Name = "Avalonia tests"
			};
			thread.Start ();
		}

		// Every caller waits until SetupWithoutStarting has run. Touching
		// Dispatcher before that binds the render loop to the test thread.
		Ready.Wait ();
		if (Dispatcher.UIThread.CheckAccess ())
			AddFluent ();
		else
			Dispatcher.UIThread.Invoke (AddFluent);
	}

	static void AddFluent ()
	{
		if (Application.Current is null)
			return;
		if (Application.Current.Styles.OfType<FluentTheme> ().Any ())
			return;
		Application.Current.Styles.Add (new FluentTheme ());
	}
}
