//
// FolderNode.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

using FSpot;
using FSpot.Query;
using FSpot.Resources.Lang;

using Hyena;

namespace FSpot.Avalonia.Models;

public sealed class FolderNode
{
	public FolderNode (FolderTreeNode node, bool isRoot, int libraryTotal = 0)
	{
		IsRoot = isRoot;
		Name = isRoot && node.Name == "file" ? Strings.Filesystem : node.Name;
		Uri = node.Uri;
		Count = node.Count;
		Share = FolderShare.Fraction (node.Count, libraryTotal);
		foreach (var child in node.Children)
			Children.Add (new FolderNode (child, false, libraryTotal));
	}

	public bool IsRoot { get; }
	public string Name { get; }
	public SafeUri Uri { get; }
	public int Count { get; }
	public double Share { get; }
	public double ShareWidth => Share * 48;
	public string Caption => $"{Name}  {Count}";
	public string AutomationId => "folder." + Uri.AbsoluteUri.Replace (':', '.').Replace ('/', '.');
	public ObservableCollection<FolderNode> Children { get; } = [];
}
