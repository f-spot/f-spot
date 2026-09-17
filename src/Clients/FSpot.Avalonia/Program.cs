using Avalonia;
using Avalonia.Svg.Skia;
using Avalonia.Threading;

using FSpot;
using FSpot.Cms;

using Sentry;

namespace FSpot.Avalonia;

static class Program
{
	[STAThread]
	public static int Main (string[] args)
	{
		using (FSpotSentry.TryInit (FSpotSentry.AvaloniaClient)) {
			LcmsNativeLibrary.Register ();
			Logger.CreateLogger ();
			MacDisplayLink.WaitForActiveDisplay ();
			try {
				BuildAvaloniaApp ().StartWithClassicDesktopLifetime (args);
			} catch (Exception ex) {
				SentrySdk.CaptureException (ex);
				SentrySdk.Flush (TimeSpan.FromSeconds (2));
				throw;
			}
		}

		return 0;
	}

	public static AppBuilder BuildAvaloniaApp ()
	{
		GC.KeepAlive (typeof (SvgImageExtension).Assembly);
		return AppBuilder.Configure<App> ()
			.UsePlatformDetect ()
			.WithInterFont ()
			.LogToTrace ()
			.AfterSetup (_ => Dispatcher.UIThread.UnhandledException += (_, e) =>
				SentrySdk.CaptureException (e.Exception));
	}
}
