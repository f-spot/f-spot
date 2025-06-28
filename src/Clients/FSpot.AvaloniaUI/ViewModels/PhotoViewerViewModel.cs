using ReactiveUI;
using System;
using System.IO;
using System.Threading.Tasks;
using FSpot.Core;
using Avalonia.Media.Imaging;
using System.Reactive;

namespace FSpot.AvaloniaUI.ViewModels;

public class PhotoViewerViewModel : ViewModelBase
{
    private readonly IPhotoService _photoService;
    private Photo? _currentPhoto;
    private Bitmap? _image;
    private bool _isLoading;
    private string _imageInfo = string.Empty;

    public PhotoViewerViewModel(IPhotoService photoService)
    {
        _photoService = photoService;
        
        // Commands
        ZoomInCommand = ReactiveCommand.Create(ZoomIn);
        ZoomOutCommand = ReactiveCommand.Create(ZoomOut);
        ZoomToFitCommand = ReactiveCommand.Create(ZoomToFit);
        ZoomToActualSizeCommand = ReactiveCommand.Create(ZoomToActualSize);
    }

    public Photo? CurrentPhoto
    {
        get => _currentPhoto;
        private set => this.RaiseAndSetIfChanged(ref _currentPhoto, value);
    }

    public Bitmap? Image
    {
        get => _image;
        private set => this.RaiseAndSetIfChanged(ref _image, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string ImageInfo
    {
        get => _imageInfo;
        private set => this.RaiseAndSetIfChanged(ref _imageInfo, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> ZoomInCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomToFitCommand { get; }
    public ReactiveCommand<Unit, Unit> ZoomToActualSizeCommand { get; }

    public async Task LoadPhoto(Photo photo)
    {
        if (photo == CurrentPhoto) return;

        try
        {
            IsLoading = true;
            CurrentPhoto = photo;
            
            var imagePath = await _photoService.GetFullSizeImagePathAsync(photo);
            if (imagePath != null && File.Exists(imagePath))
            {
                using var fileStream = File.OpenRead(imagePath);
                Image = new Bitmap(fileStream);
                
                UpdateImageInfo(photo, imagePath);
            }
            else
            {
                Image = null;
                ImageInfo = "Image file not found";
            }
        }
        catch (Exception ex)
        {
            Image = null;
            ImageInfo = $"Failed to load image: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateImageInfo(Photo photo, string imagePath)
    {
        try
        {
            var fileInfo = new FileInfo(imagePath);
            var size = Image?.PixelSize;
            
            ImageInfo = $"{Path.GetFileName(imagePath)} | " +
                       $"{size?.Width ?? 0} x {size?.Height ?? 0} | " +
                       $"{FormatFileSize(fileInfo.Length)} | " +
                       $"{photo.Time:yyyy-MM-dd HH:mm}";
        }
        catch
        {
            ImageInfo = Path.GetFileName(imagePath);
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void ZoomIn()
    {
        // TODO: Implement zoom functionality
    }

    private void ZoomOut()
    {
        // TODO: Implement zoom functionality
    }

    private void ZoomToFit()
    {
        // TODO: Implement zoom to fit functionality
    }

    private void ZoomToActualSize()
    {
        // TODO: Implement zoom to actual size functionality
    }
}