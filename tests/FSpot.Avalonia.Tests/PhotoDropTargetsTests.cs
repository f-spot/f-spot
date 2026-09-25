// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.ViewModels;
using FSpot.Core;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class PhotoDropTargetsTests
{
	[UiTestMethod]
	public void ForTagDrop_UsesTheWholeSelectionWhenTheTargetIsSelected ()
	{
		var a = Photo ("/a.jpg");
		var b = Photo ("/b.jpg");
		a.Selected = true;
		b.Selected = true;

		var targets = PhotoDropTargets.ForTagDrop (b, new[] { a, b });

		Assert.AreSequenceEqual (new[] { a, b }, targets);
	}

	[UiTestMethod]
	public void ForTagDrop_UsesOnlyTheTargetWhenItIsNotSelected ()
	{
		var a = Photo ("/a.jpg");
		var b = Photo ("/b.jpg");
		a.Selected = true;

		var targets = PhotoDropTargets.ForTagDrop (b, new[] { a, b });

		Assert.AreSequenceEqual (new[] { b }, targets);
	}

	[UiTestMethod]
	public void ForTagDrop_EmptySelectionWithoutATarget_IsEmpty ()
	{
		var a = Photo ("/a.jpg");

		Assert.IsEmpty (PhotoDropTargets.ForTagDrop (null, new[] { a }));
	}

	static PhotoItemViewModel Photo (string path)
		=> new (new StubPhoto (path));

	sealed class StubVersion : IPhotoVersion
	{
		public StubVersion (string path)
		{
			Uri = new SafeUri ("file://" + path, true);
			BaseUri = Uri;
			Filename = System.IO.Path.GetFileName (path);
		}

		public string Name => "Original";
		public bool IsProtected => true;
		public SafeUri BaseUri { get; }
		public string Filename { get; }
		public string ImportMD5 => string.Empty;
		public SafeUri Uri { get; set; }
	}

	sealed class StubPhoto : IPhoto
	{
		public StubPhoto (string path)
		{
			Name = System.IO.Path.GetFileName (path);
			DefaultVersion = new StubVersion (path);
			Versions = new[] { DefaultVersion };
		}

		public DateTime Time => DateTime.UnixEpoch;
		public Tag[] Tags => Array.Empty<Tag> ();
		public string Description => string.Empty;
		public string Name { get; }
		public uint Rating => 0;
		public IPhotoVersion DefaultVersion { get; }
		public IEnumerable<IPhotoVersion> Versions { get; }
	}
}
