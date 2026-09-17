using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using FSpot;
using FSpot.Avalonia.Imaging;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Settings;

using Hyena;

using TinyIoC;

namespace FSpot.Avalonia;

public partial class App : Application
{
	public static Db? Database { get; private set; }

	public override void Initialize ()
		=> AvaloniaXamlLoader.Load (this);

	void OnAboutClicked (object? sender, EventArgs e)
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: Window owner })
			_ = new AboutWindow ().ShowDialog (owner);
		else
			new AboutWindow ().Show ();
	}

	public override void OnFrameworkInitializationCompleted ()
	{
		InitializeCore ();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
			desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
			desktop.MainWindow = new MainWindow (Database);
			desktop.Exit += (_, _) => Database?.Dispose ();
		}

		base.OnFrameworkInitializationCompleted ();
	}

	static void InitializeCore ()
	{
		var container = TinyIoCContainer.Current;
		FSpot.FileSystem.ModuleController.RegisterTypes (container);
		FSpot.Imaging.ModuleController.RegisterTypes (container);
		FSpot.Import.ModuleController.RegisterTypes (container);
		FSpot.Thumbnail.ModuleController.RegisterTypes (container);
		ImagingServices.Backend = new AvaloniaImagingBackend ();

		FSpotConfiguration.PhotoUri = new SafeUri (Preferences.Get<string> (Preferences.StoragePath));

		if (!Directory.Exists (FSpotConfiguration.BaseDirectory))
			Directory.CreateDirectory (FSpotConfiguration.BaseDirectory);

		Database = new Db (
			container.Resolve<IImageFileFactory> (),
			container.Resolve<FSpot.Thumbnail.IThumbnailService> (),
			new SilentUpdaterUI ());
		string path = Path.Combine (FSpotConfiguration.BaseDirectory, FSpotConfiguration.DatabaseName);
		try {
			Database.Init (path, true);
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Failed to open the F-Spot catalog at {Path}", path);
		}
	}
}
