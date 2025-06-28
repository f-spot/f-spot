using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using FSpot.AvaloniaUI.ViewModels;
using FSpot.AvaloniaUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using FSpot.Settings;
using FSpot.Database;
using Mono.Addins;

namespace FSpot.AvaloniaUI;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Initialize logging early
            InitializeLogging();
            
            Log.Information("F-Spot Avalonia starting up...");
            
            // Initialize F-Spot core systems
            await InitializeFSpotCoreAsync();
            
            // Build and run Avalonia app
            return BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application failed to start");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .UseSkia();

    private static void InitializeLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/fspot-avalonia-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    private static async Task InitializeFSpotCoreAsync()
    {
        try
        {
            Log.Information("Initializing F-Spot core systems...");
            
            // Initialize settings
            FSpotConfiguration.SetupPaths();
            
            // Initialize Mono.Addins
            AddinManager.Initialize();
            
            // Initialize database
            var dbPath = FSpotConfiguration.DatabasePath;
            Log.Information("Database path: {DatabasePath}", dbPath);
            
            if (!System.IO.File.Exists(dbPath))
            {
                Log.Information("Database not found, will create new one");
            }
            
            // Database initialization will be handled by the PhotoStore when first accessed
            Log.Information("F-Spot core systems initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize F-Spot core systems");
            throw;
        }
    }
}