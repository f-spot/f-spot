using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FSpot.Core;

namespace FSpot.AvaloniaUI;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<Tag?> GetTagAsync(uint tagId);
    Task<Tag> CreateTagAsync(string name, Tag? parent = null);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(uint tagId);
    Task<IEnumerable<Tag>> GetTagHierarchyAsync();
    Task AddTagToPhotoAsync(Photo photo, Tag tag);
    Task RemoveTagFromPhotoAsync(Photo photo, Tag tag);
    Task<IEnumerable<Tag>> GetPhotoTagsAsync(Photo photo);
    event EventHandler<Tag>? TagAdded;
    event EventHandler<Tag>? TagUpdated;
    event EventHandler<uint>? TagDeleted;
}