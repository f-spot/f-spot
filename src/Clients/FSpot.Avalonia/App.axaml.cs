using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using FSpot;
using FSpot.Avalonia.Imaging;
using FSpot.Database;
using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Settings;

using Hyena;

namespace FSpot.Avalonia;

public partial class App : Application
{
	public static Db? Database { get; private set; }
	internal static ServiceGraph? Services { get; private set; }

	public override void Initialize ()
	{
		AvaloniaXamlLoader.Load (this);
		AppearanceTheme.Apply (this);
	}

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
			if (ViewerLaunch.Paths (desktop.Args) is { } viewPaths) {
				desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;
				desktop.MainWindow = new ViewerWindow (viewPaths);
			} else {
				desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
				desktop.MainWindow = new MainWindow (Database);
			}
			desktop.Exit += (_, _) => Database?.Dispose ();
		}

		base.OnFrameworkInitializationCompleted ();
	}

	static void InitializeCore ()
	{
		AvaloniaMainThread.InstallOnDispatcher ();

		Services = CoreServices.CreateDefault ();
		ImagingServices.Backend = new AvaloniaImagingBackend ();

		FSpotConfiguration.PhotoUri = SafeUri.FromPath (Preferences.Get<string> (Preferences.StoragePath));

		if (!Directory.Exists (FSpotConfiguration.BaseDirectory))
			Directory.CreateDirectory (FSpotConfiguration.BaseDirectory);

		Database = new Db (Services.Images, Services.Thumbnails, new AvaloniaUpdaterUI ());
		string path = Path.Combine (FSpotConfiguration.BaseDirectory, FSpotConfiguration.DatabaseName);
		try {
			Database.Init (path, true);
		} catch (Exception ex) {
			Logger.Log.Error (ex, "Failed to open the F-Spot catalog at {Path}", path);
			RepairDatabase (Database, ex);
		}

		FSpotSentry.UseTagCatalog (Database?.Tags);
	}

	// Matches FSpot.Gtk's App.Database: when the catalog fails to load,
	// move the broken file aside and start over with a fresh one, telling
	// the person where their old database went (RepairDbDialog).
	static void RepairDatabase (Db database, Exception loadError)
	{
		try {
			string backupPath = database.Repair ();
			MessageDialog.ShowWithoutOwner (
				Strings.ErrorLoadingDatabase,
				string.Format (Strings.FSpotEncounteredAnErrorWhileLoadingPhotoDatabaseMovedToX, backupPath));
		} catch (Exception repairError) {
			Logger.Log.Error (repairError, "Failed to repair the F-Spot catalog");
		}
	}
}
