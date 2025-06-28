using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FSpot.Core;
using System.Windows.Input;

namespace FSpot.AvaloniaUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IPhotoService _photoService;
    private readonly ITagService _tagService;

    private PhotoBrowserViewModel? _photoBrowser;
    private PhotoViewerViewModel? _photoViewer;
    private TagManagerViewModel? _tagManager;
    private string _statusText = "Ready";
    private bool _isLoading;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IPhotoService photoService,
        ITagService tagService)
    {
        _logger = logger;
        _photoService = photoService;
        _tagService = tagService;

        // Initialize commands
        ImportPhotosCommand = ReactiveCommand.CreateFromTask(ImportPhotosAsync);
        ExitCommand = ReactiveCommand.Create(Exit);
        ShowAboutCommand = ReactiveCommand.Create(ShowAbout);
        
        // Initialize view models
        InitializeViewModels();
        
        _logger.LogInformation("MainWindowViewModel initialized");
    }

    public PhotoBrowserViewModel PhotoBrowser
    {
        get => _photoBrowser ??= new PhotoBrowserViewModel(_photoService, _tagService);
        set => this.RaiseAndSetIfChanged(ref _photoBrowser, value);
    }

    public PhotoViewerViewModel PhotoViewer
    {
        get => _photoViewer ??= new PhotoViewerViewModel(_photoService);
        set => this.RaiseAndSetIfChanged(ref _photoViewer, value);
    }

    public TagManagerViewModel TagManager
    {
        get => _tagManager ??= new TagManagerViewModel(_tagService);
        set => this.RaiseAndSetIfChanged(ref _tagManager, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string WindowTitle => $"F-Spot Photo Manager - Avalonia";

    // Commands
    public ReactiveCommand<Unit, Unit> ImportPhotosCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }
    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; }

    private void InitializeViewModels()
    {
        try
        {
            // Initialize photo browser first
            _ = PhotoBrowser;
            
            // Subscribe to photo selection changes
            PhotoBrowser.SelectedPhotoChanged
                .Subscribe(photo =>
                {
                    if (photo != null)
                    {
                        PhotoViewer.LoadPhoto(photo);
                    }
                });

            StatusText = "Application initialized";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize view models");
            StatusText = "Failed to initialize application";
        }
    }

    private async Task ImportPhotosAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Importing photos...";

            // TODO: Implement file dialog for photo import
            // For now, just refresh the photo list
            await PhotoBrowser.RefreshPhotosAsync();

            StatusText = "Import completed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import photos");
            StatusText = "Import failed";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Exit()
    {
        Environment.Exit(0);
    }

    private void ShowAbout()
    {
        // TODO: Implement about dialog
        StatusText = "F-Spot Photo Manager - Avalonia UI Version";
    }
}