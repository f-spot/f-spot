//
// CatalogDrop.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

using FSpot.Avalonia.Models;
using FSpot.Avalonia.ViewModels;

namespace FSpot.Avalonia.Input;

public enum CatalogDropKind
{
	Library,
	PhotoGrid,
	TagTree,
	FindBar,
	PhotoView
}

public static class CatalogDrop
{
	public static readonly AttachedProperty<CatalogDropKind?> KindProperty =
		AvaloniaProperty.RegisterAttached<Control, CatalogDropKind?> ("Kind", typeof (CatalogDrop));

	static readonly AttachedProperty<bool> ListeningProperty =
		AvaloniaProperty.RegisterAttached<Control, bool> ("Listening", typeof (CatalogDrop));

	static CatalogDrop ()
		=> KindProperty.Changed.AddClassHandler<Control> (OnKindChanged);

	public static void SetKind (Control element, CatalogDropKind? value) => element.SetValue (KindProperty, value);
	public static CatalogDropKind? GetKind (Control element) => element.GetValue (KindProperty);

	public static DragDropEffects Effects (CatalogDropKind kind, IDataTransfer transfer)
		=> kind switch {
			CatalogDropKind.Library => IsExternalFileDrop (transfer) ? DragDropEffects.Copy : DragDropEffects.None,
			CatalogDropKind.PhotoGrid => AcceptsPhotoGrid (transfer) ? DragDropEffects.Copy : DragDropEffects.None,
			CatalogDropKind.TagTree => transfer.Contains (DragFormats.Photos) || transfer.Contains (DragFormats.TagId)
				? DragDropEffects.Copy
				: DragDropEffects.None,
			CatalogDropKind.FindBar => transfer.Contains (DragFormats.FolderUri) ? DragDropEffects.Copy : DragDropEffects.None,
			CatalogDropKind.PhotoView => transfer.Contains (DragFormats.TagId) ? DragDropEffects.Copy : DragDropEffects.None,
			_ => DragDropEffects.None
		};

	public static bool AcceptsPhotoGrid (IDataTransfer transfer)
		=> transfer.Contains (DataFormat.File) || transfer.Contains (DragFormats.TagId) || transfer.Contains (DragFormats.Photos);

	/// <summary>
	/// Photos dragged out of our own grid also carry their files; like GTK, a drop that started
	/// inside F-Spot never imports.
	/// </summary>
	public static bool IsExternalFileDrop (IDataTransfer transfer)
		=> transfer.Contains (DataFormat.File) && !transfer.Contains (DragFormats.Photos);

	static void OnKindChanged (Control control, AvaloniaPropertyChangedEventArgs _)
	{
		if (control.GetValue (ListeningProperty))
			return;
		control.SetValue (ListeningProperty, true);
		control.AddHandler (DragDrop.DragOverEvent, OnDragOver);
		control.AddHandler (DragDrop.DropEvent, OnDrop);
	}

	static void OnDragOver (object? sender, DragEventArgs e)
	{
		if (sender is not Control control || GetKind (control) is not CatalogDropKind kind)
			return;
		e.DragEffects = Effects (kind, e.DataTransfer);
		if (kind is CatalogDropKind.PhotoGrid or CatalogDropKind.PhotoView)
			e.Handled = true;
	}

	static void OnDrop (object? sender, DragEventArgs e)
	{
		if (sender is not Control control || GetKind (control) is not CatalogDropKind kind)
			return;
		if (WindowCommand.Model (control) is not MainWindowViewModel viewModel)
			return;
		switch (kind) {
		case CatalogDropKind.Library:
			ApplyLibrary (viewModel, e, null);
			break;
		case CatalogDropKind.PhotoGrid:
			if (e.DataTransfer.Contains (DragFormats.Photos))
				UiTasks.Forget (viewModel.MergeDroppedPhotosAsync (PhotoUnderPointer (e, control)));
			else
				ApplyLibrary (viewModel, e, PhotoUnderPointer (e, control));
			e.Handled = true;
			break;
		case CatalogDropKind.PhotoView:
			viewModel.AttachDroppedTagToSelection (Read (e.DataTransfer, DragFormats.TagId));
			e.Handled = true;
			break;
		case CatalogDropKind.TagTree:
			ApplyTagTree (viewModel, e);
			break;
		case CatalogDropKind.FindBar:
			ApplyFindBar (viewModel, e);
			break;
		}
	}

	static void ApplyLibrary (MainWindowViewModel viewModel, DragEventArgs e, PhotoItemViewModel? target)
	{
		if (e.DataTransfer.Contains (DragFormats.Photos))
			return;
		var paths = DroppedPaths (e.DataTransfer);
		if (paths.Count > 0) {
			UiTasks.Forget (viewModel.ImportDroppedAsync (paths));
			return;
		}
		viewModel.AttachDroppedTag (Read (e.DataTransfer, DragFormats.TagId), target);
	}

	static void ApplyTagTree (MainWindowViewModel viewModel, DragEventArgs e)
	{
		// Empty tree space has the window model as its data, not a tag. Do not fall back to the selection.
		if ((e.Source as Control)?.DataContext is not TagNode node)
			return;
		if (e.DataTransfer.Contains (DragFormats.TagId)) {
			if (!uint.TryParse (Read (e.DataTransfer, DragFormats.TagId), out uint id))
				return;
			var child = FindTag (viewModel.Tags, id);
			if (child is not null && !ReferenceEquals (child, node))
				viewModel.ReparentTagCommand.Execute (((TagNode Child, TagNode Parent)?) (child, node));
			return;
		}
		if (e.DataTransfer.Contains (DragFormats.Photos))
			viewModel.AttachSelectionToTag (node);
	}

	static void ApplyFindBar (MainWindowViewModel viewModel, DragEventArgs e)
	{
		string? uri = Read (e.DataTransfer, DragFormats.FolderUri);
		if (string.IsNullOrEmpty (uri))
			return;
		var match = viewModel.Folders.SelectMany (Walk).FirstOrDefault (folder => folder.Uri.AbsoluteUri == uri);
		if (match is not null)
			viewModel.ActivateFolderCommand.Execute (match);
	}

	static PhotoItemViewModel? PhotoUnderPointer (DragEventArgs e, Control dropRoot)
	{
		var fromSource = Ancestor<PhotoItemViewModel> (e.Source as Visual);
		if (fromSource is not null)
			return fromSource;
		var hit = dropRoot.InputHitTest (e.GetPosition (dropRoot));
		return Ancestor<PhotoItemViewModel> (hit as Visual);
	}

	static List<string> DroppedPaths (IDataTransfer transfer)
	{
		var paths = new List<string> ();
		if (!transfer.Contains (DataFormat.File))
			return paths;
		foreach (var item in transfer.GetItems (DataFormat.File)) {
			if (item.TryGetValue (DataFormat.File) is not IStorageItem storage)
				continue;
			string? path = storage.TryGetLocalPath ();
			if (!string.IsNullOrWhiteSpace (path))
				paths.Add (path);
		}
		return paths;
	}

	public static string? Read (IDataTransfer? transfer, DataFormat<string> format)
	{
		if (transfer is null || !transfer.Contains (format))
			return null;
		foreach (var item in transfer.GetItems (format)) {
			if (item.TryGetValue (format) is string value)
				return value;
		}
		return null;
	}

	static TagNode? FindTag (IEnumerable<TagNode> nodes, uint id)
	{
		foreach (var node in nodes) {
			if (node.Tag.Id == id)
				return node;
			var child = FindTag (node.Children, id);
			if (child is not null)
				return child;
		}
		return null;
	}

	static IEnumerable<FolderNode> Walk (FolderNode node)
	{
		yield return node;
		foreach (var child in node.Children)
			foreach (var nested in Walk (child))
				yield return nested;
	}

	static T? Ancestor<T> (Visual? start) where T : class
	{
		for (var visual = start; visual is not null; visual = visual.GetVisualParent ()) {
			if (visual is Control { DataContext: T value })
				return value;
		}
		return null;
	}
}
