using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FSpot.Core;
using DynamicData;
using DynamicData.Binding;

namespace FSpot.AvaloniaUI.ViewModels;

public class PhotoBrowserViewModel : ViewModelBase
{
    private readonly IPhotoService _photoService;
    private readonly ITagService _tagService;
    private readonly SourceList<PhotoItemViewModel> _photoSource;
    private readonly ReadOnlyObservableCollection<PhotoItemViewModel> _photos;
    
    private PhotoItemViewModel? _selectedPhoto;
    private string _searchText = string.Empty;
    private bool _isLoading;
    
    public PhotoBrowserViewModel(IPhotoService photoService, ITagService tagService)
    {
        _photoService = photoService;
        _tagService = tagService;
        
        // Initialize photo collection with reactive updates
        _photoSource = new SourceList<PhotoItemViewModel>();
        
        // Set up filtered and sorted photo collection
        _photoSource
            .Connect()
            .Filter(this.WhenAnyValue(x => x.SearchText)
                .Select(CreateSearchFilter))
            .Sort(SortExpressionComparer<PhotoItemViewModel>.Descending(x => x.Photo.Time))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _photos)
            .Subscribe();

        // Commands
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshPhotosAsync);
        SearchCommand = ReactiveCommand.CreateFromTask<string>(SearchPhotosAsync);
        
        // Load initial photos
        _ = Task.Run(async () => await RefreshPhotosAsync());
    }

    public ReadOnlyObservableCollection<PhotoItemViewModel> Photos => _photos;

    public PhotoItemViewModel? SelectedPhoto
    {
        get => _selectedPhoto;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPhoto, value);
            SelectedPhotoChanged.OnNext(value?.Photo);
        }
    }

    public string SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<string, Unit> SearchCommand { get; }

    // Observables
    public Subject<Photo?> SelectedPhotoChanged { get; } = new();

    public async Task RefreshPhotosAsync()
    {
        try
        {
            IsLoading = true;
            
            var photos = await _photoService.GetAllPhotosAsync();
            var photoViewModels = photos.Select(p => new PhotoItemViewModel(p, _photoService, _tagService));
            
            _photoSource.Clear();
            _photoSource.AddRange(photoViewModels);
        }
        catch (Exception ex)
        {
            // TODO: Add proper error handling/notification
            System.Diagnostics.Debug.WriteLine($"Failed to refresh photos: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchPhotosAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                await RefreshPhotosAsync();
                return;
            }

            IsLoading = true;
            
            var photos = await _photoService.SearchPhotosAsync(searchTerm);
            var photoViewModels = photos.Select(p => new PhotoItemViewModel(p, _photoService, _tagService));
            
            _photoSource.Clear();
            _photoSource.AddRange(photoViewModels);
        }
        catch (Exception ex)
        {
            // TODO: Add proper error handling/notification
            System.Diagnostics.Debug.WriteLine($"Failed to search photos: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private Func<PhotoItemViewModel, bool> CreateSearchFilter(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return _ => true;

        return photo =>
            photo.Photo.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
            photo.FileName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}