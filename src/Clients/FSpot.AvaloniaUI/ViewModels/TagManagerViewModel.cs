using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using FSpot.Core;
using DynamicData;

namespace FSpot.AvaloniaUI.ViewModels;

public class TagManagerViewModel : ViewModelBase
{
    private readonly ITagService _tagService;
    private readonly SourceList<TagItemViewModel> _tagSource;
    private readonly ReadOnlyObservableCollection<TagItemViewModel> _tags;
    
    private TagItemViewModel? _selectedTag;
    private bool _isLoading;

    public TagManagerViewModel(ITagService tagService)
    {
        _tagService = tagService;
        
        // Initialize tag collection
        _tagSource = new SourceList<TagItemViewModel>();
        
        _tagSource
            .Connect()
            .Sort(SortExpressionComparer<TagItemViewModel>.Ascending(x => x.Tag.Name))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _tags)
            .Subscribe();

        // Commands
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshTagsAsync);
        CreateTagCommand = ReactiveCommand.CreateFromTask<string>(CreateTagAsync);
        DeleteTagCommand = ReactiveCommand.CreateFromTask<TagItemViewModel>(DeleteTagAsync);
        
        // Load initial tags
        _ = Task.Run(async () => await RefreshTagsAsync());
    }

    public ReadOnlyObservableCollection<TagItemViewModel> Tags => _tags;

    public TagItemViewModel? SelectedTag
    {
        get => _selectedTag;
        set => this.RaiseAndSetIfChanged(ref _selectedTag, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    // Commands
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<string, Unit> CreateTagCommand { get; }
    public ReactiveCommand<TagItemViewModel, Unit> DeleteTagCommand { get; }

    public async Task RefreshTagsAsync()
    {
        try
        {
            IsLoading = true;
            
            var tags = await _tagService.GetAllTagsAsync();
            var tagViewModels = tags.Select(t => new TagItemViewModel(t, _tagService));
            
            _tagSource.Clear();
            _tagSource.AddRange(tagViewModels);
        }
        catch (Exception ex)
        {
            // TODO: Add proper error handling/notification
            System.Diagnostics.Debug.WriteLine($"Failed to refresh tags: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateTagAsync(string tagName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tagName))
                return;

            var newTag = await _tagService.CreateTagAsync(tagName);
            var tagViewModel = new TagItemViewModel(newTag, _tagService);
            
            _tagSource.Add(tagViewModel);
        }
        catch (Exception ex)
        {
            // TODO: Add proper error handling/notification
            System.Diagnostics.Debug.WriteLine($"Failed to create tag: {ex.Message}");
        }
    }

    private async Task DeleteTagAsync(TagItemViewModel tagViewModel)
    {
        try
        {
            await _tagService.DeleteTagAsync(tagViewModel.Tag.Id);
            _tagSource.Remove(tagViewModel);
            
            if (SelectedTag == tagViewModel)
                SelectedTag = null;
        }
        catch (Exception ex)
        {
            // TODO: Add proper error handling/notification
            System.Diagnostics.Debug.WriteLine($"Failed to delete tag: {ex.Message}");
        }
    }
}

public class TagItemViewModel : ViewModelBase
{
    private readonly ITagService _tagService;

    public TagItemViewModel(Tag tag, ITagService tagService)
    {
        Tag = tag;
        _tagService = tagService;
    }

    public Tag Tag { get; }

    public string Name => Tag.Name;
    
    public bool IsCategory => Tag.IsCategory;
    
    public string? CategoryName => Tag.Category?.Name;
    
    public int Level => GetTagLevel(Tag);

    private static int GetTagLevel(Tag tag)
    {
        int level = 0;
        var current = tag.Category;
        while (current != null)
        {
            level++;
            current = current.Category;
        }
        return level;
    }
}