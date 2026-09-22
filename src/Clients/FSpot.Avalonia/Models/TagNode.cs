//
// TagNode.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using FSpot.Core;

namespace FSpot.Avalonia.Models;

public sealed partial class TagNode : ObservableObject
{
	public TagNode (Tag tag)
	{
		Tag = tag;
		name = tag.Name;
		foreach (var child in tag is Category category ? category.Children : Array.Empty<Tag> ())
			Children.Add (new TagNode (child));
	}

	public Tag Tag { get; }

	[ObservableProperty]
	string name;

	[ObservableProperty]
	bool isRenaming;

	public string? IconName => Tag.ThemeIconName;
	public string IconPath => TagIcons.UriFor (Tag.ThemeIconName);
	public string AutomationId => "tag." + Tag.Id;
	public ObservableCollection<TagNode> Children { get; } = [];
}
