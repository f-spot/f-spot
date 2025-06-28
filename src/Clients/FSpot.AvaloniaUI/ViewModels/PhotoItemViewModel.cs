using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FSpot.Core;
using Avalonia.Media.Imaging;
using System.Reactive.Linq;
using System.Reactive;

namespace FSpot.AvaloniaUI.ViewModels;

public class PhotoItemViewModel : ViewModelBase
{
    private readonly IPhotoService _photoService;
    private readonly ITagService _tagService;
    private Bitmap? _thumbnail;
    private bool _isSelected;
    private bool _isLoadingThumbnail;

    public PhotoItemViewModel(Photo photo, IPhotoService photoService, ITagService tagService)
    {
        Photo = photo;
        _photoService = photoService;
        _tagService = tagService;
        
        // Commands
        LoadThumbnailCommand = ReactiveCommand.CreateFromTask(LoadThumbnailAsync);
        
        // Auto-load thumbnail when created
        _ = Task.Run(async () => await LoadThumbnailAsync());
    }

    public Photo Photo { get; }

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    public bool IsLoadingThumbnail
    {
        get => _isLoadingThumbnail;
        private set => this.RaiseAndSetIfChanged(ref _isLoadingThumbnail, value);
    }

    public string FileName => Path.GetFileName(Photo.DefaultVersion.Uri.LocalPath);
    
    public string FilePath => Photo.DefaultVersion.Uri.LocalPath;
    
    public DateTime DateTaken => Photo.Time;
    
    public string Description => Photo.Description ?? string.Empty;
    
    public int Rating => Photo.Rating;
    
    public string[] TagNames => Photo.Tags.Select(t => t.Name).ToArray();

    // Commands
    public ReactiveCommand<Unit, Unit> LoadThumbnailCommand { get; }

    private async Task LoadThumbnailAsync()
    {
        if (Thumbnail != null || IsLoadingThumbnail)
            return;

        try
        {
            IsLoadingThumbnail = true;

            // Try to load thumbnail from service first
            var thumbnailData = await _photoService.GetThumbnailAsync(Photo, 256);
            
            if (thumbnailData != null)
            {
                using var stream = new MemoryStream(thumbnailData);
                Thumbnail = new Bitmap(stream);
            }
            else
            {
                // Fall back to loading and resizing the full image
                await LoadFullImageAsThumbnailAsync();
            }
        }
        catch (Exception ex)
        {
            // TODO: Set default/error thumbnail
            System.Diagnostics.Debug.WriteLine($"Failed to load thumbnail for {FileName}: {ex.Message}");
        }
        finally
        {
            IsLoadingThumbnail = false;
        }
    }

    private async Task LoadFullImageAsThumbnailAsync()
    {
        try
        {
            var imagePath = await _photoService.GetFullSizeImagePathAsync(Photo);
            if (imagePath != null && File.Exists(imagePath))
            {
                // Load and resize image for thumbnail
                using var fileStream = File.OpenRead(imagePath);
                var originalBitmap = new Bitmap(fileStream);
                
                // Calculate thumbnail size maintaining aspect ratio
                const int maxSize = 256;
                var scale = Math.Min((double)maxSize / originalBitmap.PixelSize.Width, 
                                   (double)maxSize / originalBitmap.PixelSize.Height);
                
                var newWidth = (int)(originalBitmap.PixelSize.Width * scale);
                var newHeight = (int)(originalBitmap.PixelSize.Height * scale);
                
                // Create thumbnail
                // Note: This is a simplified approach. In production, you'd want to use
                // SkiaSharp or similar for better performance and quality
                var resized = originalBitmap.CreateScaledBitmap(new Avalonia.PixelSize(newWidth, newHeight));
                Thumbnail = resized;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create thumbnail from full image: {ex.Message}");
        }
    }
}