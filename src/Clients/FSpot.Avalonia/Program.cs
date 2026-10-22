using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Svg.Skia;
using Avalonia.Threading;

using FSpot;
using FSpot.Avalonia.Imaging;
using FSpot.Cms;

using Sentry;

namespace FSpot.Avalonia;

static class Program
{
	// Avalonia reads this when its assembly loads. Set it before Main touches Avalonia.
	[ModuleInitializer]
	internal static void OptOutOfAvaloniaTelemetry ()
		=> Environment.SetEnvironmentVariable ("AVALONIA_TELEMETRY_OPTOUT", "1");

	[STAThread]
	public static int Main (string[] args)
	{
		using (FSpotSentry.TryInit (FSpotSentry.AvaloniaClient)) {
			LcmsNativeLibrary.Register ();
			Logger.CreateLogger ();
			MacDisplayLink.WaitForActiveDisplay ();
			try {
				// Before any window chrome. The rasterizer type may not have been touched yet.
				SvgRasterizer.DenyExternalResources ();
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
		SvgRasterizer.DenyExternalResources ();
		return AppBuilder.Configure<App> ()
			.UsePlatformDetect ()
			.WithInterFont ()
			.LogToTrace ()
			.AfterSetup (_ => Dispatcher.UIThread.UnhandledException += (_, e) =>
				SentrySdk.CaptureException (e.Exception));
	}
}
