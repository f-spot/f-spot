// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

using FSpot.Avalonia.Input;
using FSpot.Avalonia.Models;
using FSpot.Core;
using FSpot.Query;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PointerDragTests
{
	[UiTestMethod]
	public void For_UsesTheTagIdFolderUriOrPhotoPayload ()
	{
		AvaloniaApp.Ensure ();
		var tag = new TagNode (new Tag (null, 7, "Vacation"));
		var uri = new SafeUri ("file:///photos/beach");
		var folder = new FolderNode (new FolderTreeNode ("Beach", uri, 1, []), false);

		var tagPayload = PointerDrag.For (tag);
		var folderPayload = PointerDrag.For (folder);
		var photoPayload = PointerDrag.For (new ViewModels.PhotoItemViewModel (new Stub ()));

		Assert.IsNotNull (tagPayload);
		Assert.AreEqual (DragFormats.TagId, tagPayload.Value.Format);
		Assert.AreEqual ("7", tagPayload.Value.Value);
		Assert.IsNotNull (folderPayload);
		Assert.AreEqual (DragFormats.FolderUri, folderPayload.Value.Format);
		Assert.AreEqual (uri.AbsoluteUri, folderPayload.Value.Value);
		Assert.IsNotNull (photoPayload);
		Assert.AreEqual (DragFormats.Photos, photoPayload.Value.Format);
		Assert.IsNull (PointerDrag.For ("text"));
	}

	[UiTestMethod]
	public void Effects_AcceptTagsOnTheGridAndOnlyFilesOnTheEmptyLibrary ()
	{
		AvaloniaApp.Ensure ();
		using var tag = new DataTransfer ();
		tag.Add (DataTransferItem.Create (DragFormats.TagId, "1"));

		Assert.AreEqual (DragDropEffects.Copy, CatalogDrop.Effects (CatalogDropKind.PhotoGrid, tag));
		Assert.AreEqual (DragDropEffects.None, CatalogDrop.Effects (CatalogDropKind.Library, tag));
		Assert.AreEqual (DragDropEffects.Copy, CatalogDrop.Effects (CatalogDropKind.TagTree, tag));
		Assert.AreEqual (DragDropEffects.None, CatalogDrop.Effects (CatalogDropKind.FindBar, tag));
	}

	[UiTestMethod]
	public void Effects_AcceptPhotosOnTheGridButNeverImportTheirFiles ()
	{
		AvaloniaApp.Ensure ();
		using var photos = new DataTransfer ();
		photos.Add (DataTransferItem.Create (DragFormats.Photos, "1"));

		Assert.AreEqual (DragDropEffects.Copy, CatalogDrop.Effects (CatalogDropKind.PhotoGrid, photos));
		Assert.AreEqual (DragDropEffects.None, CatalogDrop.Effects (CatalogDropKind.Library, photos));
		Assert.AreEqual (DragDropEffects.None, CatalogDrop.Effects (CatalogDropKind.PhotoView, photos));
		Assert.IsFalse (CatalogDrop.IsExternalFileDrop (photos));
	}

	[UiTestMethod]
	public void Effects_AcceptOnlyTagsOnThePhotoView ()
	{
		AvaloniaApp.Ensure ();
		using var tag = new DataTransfer ();
		tag.Add (DataTransferItem.Create (DragFormats.TagId, "3"));
		using var folder = new DataTransfer ();
		folder.Add (DataTransferItem.Create (DragFormats.FolderUri, "file:///x"));

		Assert.AreEqual (DragDropEffects.Copy, CatalogDrop.Effects (CatalogDropKind.PhotoView, tag));
		Assert.AreEqual (DragDropEffects.None, CatalogDrop.Effects (CatalogDropKind.PhotoView, folder));
	}

	[UiTestMethod]
	public void Transfer_CarriesThePayloadAndNoFilesWhenNoneResolve ()
	{
		AvaloniaApp.Ensure ();
		using var data = PointerDrag.Transfer (new PointerDrag.Payload (DragFormats.Photos, "1"), []);

		Assert.AreEqual ("1", CatalogDrop.Read (data, DragFormats.Photos));
		Assert.IsFalse (((IDataTransfer)data).Contains (DataFormat.File));
	}

	[UiTestMethod]
	public void ReparentQuestion_NamesTheMergedPhotoAndTheNewParent ()
	{
		var single = ViewModels.MainWindowViewModel.ReparentQuestion (new Stub ("parent.jpg"), [new Stub ("child.jpg")]);
		var many = ViewModels.MainWindowViewModel.ReparentQuestion (new Stub ("parent.jpg"), [new Stub ("a.jpg"), new Stub ("b.jpg")]);

		Assert.AreEqual ("Really reparent \"child.jpg\" as version of \"parent.jpg\"?", single);
		Assert.AreEqual ("Really reparent 2 photos as versions of \"parent.jpg\"?", many);
	}

	sealed class Stub : IPhoto
	{
		public Stub (string name = "a.jpg")
		{
			Name = name;
			DefaultVersion = new Version ();
			Versions = new[] { DefaultVersion };
		}

		public DateTime Time { get; } = DateTime.UnixEpoch;
		public Tag[] Tags { get; } = [];
		public string Description => string.Empty;
		public string Name { get; }
		public uint Rating => 0;
		public IPhotoVersion DefaultVersion { get; }
		public IEnumerable<IPhotoVersion> Versions { get; }

		sealed class Version : IPhotoVersion
		{
			public Version ()
			{
				Uri = new SafeUri ("file:///a.jpg", true);
				BaseUri = Uri;
			}

			public string Name => "Original";
			public bool IsProtected => true;
			public SafeUri BaseUri { get; }
			public string Filename => "a.jpg";
			public string ImportMD5 => string.Empty;
			public SafeUri Uri { get; set; }
		}
	}
}
