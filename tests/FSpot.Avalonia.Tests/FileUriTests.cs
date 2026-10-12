// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// gsettings and mailto use this encoder. It percent-encodes spaces and treats
// every backslash as a separator. SafeUri does neither.
[TestClass]
public sealed class FileUriTests
{
	[TestMethod]
	public void ASpaceIsPercentEncoded ()
	{
		Assert.AreEqual ("file:///photos/My%20Wallpaper/wall.jpg", FileUri.FromPath ("/photos/My Wallpaper/wall.jpg"));
	}

	[TestMethod]
	public void ABackslashIsASeparator ()
	{
		Assert.AreEqual ("file:///tmp/a/b.jpg", FileUri.FromPath ("/tmp/a\\b.jpg"));
	}

	[TestMethod]
	public void AParentSegmentResolvesAndATrailingSlashIsDropped ()
	{
		Assert.AreEqual ("file:///photos/wall.jpg", FileUri.FromPath ("/photos/../photos/wall.jpg"));
		Assert.AreEqual ("file:///photos/wall.jpg", FileUri.FromPath ("/photos/wall.jpg/"));
	}

	[TestMethod]
	public void ADriveLetterKeepsItsColon ()
	{
		Assert.AreEqual ("file:///C:/photos/My%20Wallpaper/wall.jpg", FileUri.FromPath ("C:/photos/My Wallpaper/wall.jpg"));
	}

	[TestMethod]
	public void ARootIsOnlyTheFilePrefix ()
	{
		Assert.AreEqual ("file://", FileUri.FromPath ("/"));
	}

	[TestMethod]
	public void ARelativePathUsesTheFullPath ()
	{
		string full = Path.GetFullPath ("wall.jpg");
		Assert.AreEqual (FileUri.FromPath (full), FileUri.FromPath ("wall.jpg"));
	}

	[TestMethod]
	public void ARootedDriveShapedSegmentIsEscaped ()
	{
		Assert.AreEqual ("file:///c%3A/x", FileUri.FromPath ("/c:/x"));
	}
}
