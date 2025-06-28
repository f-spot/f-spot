using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FSpot.Core;
using FSpot.Database;

namespace FSpot.AvaloniaUI;

public class TagService : ITagService
{
    private readonly ILogger<TagService> _logger;
    private TagStore? _tagStore;

    public TagService(ILogger<TagService> logger)
    {
        _logger = logger;
    }

    public event EventHandler<Tag>? TagAdded;
    public event EventHandler<Tag>? TagUpdated;
    public event EventHandler<uint>? TagDeleted;

    private TagStore TagStore
    {
        get
        {
            if (_tagStore == null)
            {
                var db = new Db();
                _tagStore = db.Tags;
            }
            return _tagStore;
        }
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var tags = TagStore.GetAll();
                _logger.LogInformation("Retrieved {Count} tags from database", tags.Length);
                return tags.AsEnumerable();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all tags");
            return Enumerable.Empty<Tag>();
        }
    }

    public async Task<Tag?> GetTagAsync(uint tagId)
    {
        try
        {
            return await Task.Run(() =>
            {
                var tag = TagStore.Get(tagId);
                _logger.LogDebug("Retrieved tag {TagId}", tagId);
                return tag;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag {TagId}", tagId);
            return null;
        }
    }

    public async Task<Tag> CreateTagAsync(string name, Tag? parent = null)
    {
        try
        {
            return await Task.Run(() =>
            {
                var tag = TagStore.CreateCategory(parent, name);
                TagAdded?.Invoke(this, tag);
                _logger.LogInformation("Created tag: {TagName}", name);
                return tag;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tag: {TagName}", name);
            throw;
        }
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        try
        {
            await Task.Run(() =>
            {
                TagStore.Commit(tag);
                TagUpdated?.Invoke(this, tag);
                _logger.LogInformation("Updated tag: {TagName}", tag.Name);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update tag: {TagName}", tag.Name);
            throw;
        }
    }

    public async Task DeleteTagAsync(uint tagId)
    {
        try
        {
            await Task.Run(() =>
            {
                var tag = TagStore.Get(tagId);
                if (tag != null)
                {
                    TagStore.Remove(tag);
                    TagDeleted?.Invoke(this, tagId);
                    _logger.LogInformation("Deleted tag: {TagId}", tagId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete tag: {TagId}", tagId);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetTagHierarchyAsync()
    {
        try
        {
            return await Task.Run(() =>
            {
                var allTags = TagStore.GetAll();
                var rootTags = allTags.Where(t => t.Category == null);
                _logger.LogInformation("Retrieved tag hierarchy with {Count} root tags", rootTags.Count());
                return rootTags;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tag hierarchy");
            return Enumerable.Empty<Tag>();
        }
    }

    public async Task AddTagToPhotoAsync(Photo photo, Tag tag)
    {
        try
        {
            await Task.Run(() =>
            {
                photo.AddTag(tag);
                // Note: Photo saving should be handled by PhotoService
                _logger.LogInformation("Added tag {TagName} to photo {PhotoId}", tag.Name, photo.Id);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tag {TagName} to photo {PhotoId}", tag.Name, photo.Id);
            throw;
        }
    }

    public async Task RemoveTagFromPhotoAsync(Photo photo, Tag tag)
    {
        try
        {
            await Task.Run(() =>
            {
                photo.RemoveTag(tag);
                // Note: Photo saving should be handled by PhotoService
                _logger.LogInformation("Removed tag {TagName} from photo {PhotoId}", tag.Name, photo.Id);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tag {TagName} from photo {PhotoId}", tag.Name, photo.Id);
            throw;
        }
    }

    public async Task<IEnumerable<Tag>> GetPhotoTagsAsync(Photo photo)
    {
        try
        {
            return await Task.Run(() =>
            {
                var tags = photo.Tags;
                _logger.LogDebug("Retrieved {Count} tags for photo {PhotoId}", tags.Length, photo.Id);
                return tags.AsEnumerable();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags for photo {PhotoId}", photo.Id);
            return Enumerable.Empty<Tag>();
        }
    }
}