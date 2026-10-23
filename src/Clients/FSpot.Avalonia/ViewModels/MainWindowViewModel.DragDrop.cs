// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot.Avalonia.ViewModels;

public partial class MainWindowViewModel
{
	/// <summary>
	/// Local paths of the default version of every selected photo, for dragging them out to other
	/// applications as files (GTK's icon view offers the selection as a uri list).
	/// </summary>
	public IReadOnlyList<string> DragOutPaths ()
		=> SelectionPaths ().Distinct ().ToList ();

	/// <summary>
	/// The selected photos dropped onto <paramref name="target"/> become versions of it after the
	/// user confirms. Dropping onto a photo that is itself selected does nothing, as in GTK.
	/// </summary>
	public async Task MergeDroppedPhotosAsync (PhotoItemViewModel? target)
	{
		if (database is null || target is null || target.Selected || target.Photo is not Photo parent)
			return;
		var photos = CollectSelectedPhotos ()
			.Where (photo => !ReferenceEquals (photo, parent))
			.ToArray ();
		if (photos.Length == 0)
			return;

		if (!await shell.ConfirmAsync (ReparentQuestion (parent, photos),
				Strings.ThisMakesThePhotosAppearAsASingleOneInLibraryTheVersionsCanBeDetachedUsingThePhotoMenu,
				Strings.ReparentMnemonic))
			return;
		var store = database.Photos;
		try {
			PhotoVersionWorkflow.Reparent (parent, photos, store.Commit, store.Remove);
		} catch (Exception ex) {
			await shell.ShowWarningAsync (GtkMnemonic.Display (Strings.ReparentMnemonic),
				string.Format (Strings.ReceivedExceptionX, ex.Message));
		}
		ReloadPhotos ();
	}

	/// <summary>
	/// A tag dropped on the single-photo view goes on the selected photos (GTK's photo view drop).
	/// </summary>
	public void AttachDroppedTagToSelection (string? tagIdText)
	{
		if (!uint.TryParse (tagIdText, out uint id))
			return;
		var node = FindTagNodeById (Tags, id);
		if (node is null)
			return;
		AttachTagToCurrentSelection (node.Tag);
	}

	/// <summary>
	/// The confirmation heading for merging <paramref name="photos"/> into <paramref name="parent"/>.
	/// </summary>
	public static string ReparentQuestion (IPhoto parent, IReadOnlyList<IPhoto> photos)
		=> photos.Count <= 1
			? string.Format (Strings.ReallyReparentXAsVersionOfY, photos.Count == 0 ? string.Empty : photos[0].Name, parent.Name)
			: string.Format (Strings.ReallyReparentZPhotosAsVersionsOfY, string.Empty, parent.Name, photos.Count);
}
