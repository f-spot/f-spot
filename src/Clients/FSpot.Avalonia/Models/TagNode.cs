//
// TagNode.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

using FSpot.Core;

namespace FSpot.Avalonia.Models;

public sealed class TagNode
{
	public TagNode (Tag tag)
	{
		Tag = tag;
		Name = tag.Name;
		foreach (var child in tag is Category category ? category.Children : Array.Empty<Tag> ())
			Children.Add (new TagNode (child));
	}

	public Tag Tag { get; }
	public string Name { get; }
	public string AutomationId => "tag." + Tag.Id;
	public ObservableCollection<TagNode> Children { get; } = [];
}
