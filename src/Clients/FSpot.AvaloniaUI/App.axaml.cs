using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FSpot.AvaloniaUI.ViewModels;
using FSpot.AvaloniaUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

namespace FSpot.AvaloniaUI;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Configure dependency injection
        ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var mainWindowViewModel = ServiceProvider?.GetRequiredService<MainWindowViewModel>();
                
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowViewModel,
                };
                
                Log.Information("Main window created successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to create main window");
                desktop.Shutdown();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
        
        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<PhotoBrowserViewModel>();
        services.AddTransient<PhotoViewerViewModel>();
        services.AddTransient<TagManagerViewModel>();
        
        // Services
        services.AddSingleton<IPhotoService, PhotoService>();
        services.AddSingleton<ITagService, TagService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
        
        ServiceProvider = services.BuildServiceProvider();
        
        Log.Information("Dependency injection configured");
    }
}