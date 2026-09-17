// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

namespace FSpot.Avalonia.Tests;

static class AvaloniaApp
{
	static int started;

	public static void Ensure ()
	{
		if (Interlocked.Exchange (ref started, 1) == 0) {
			AppBuilder.Configure<Application> ()
				.UseHeadless (new AvaloniaHeadlessPlatformOptions ())
				.AfterSetup (_ => AddFluent ())
				.SetupWithoutStarting ();
			return;
		}

		AddFluent ();
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
