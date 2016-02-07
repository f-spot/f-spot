//
// PhotoFileTrackerTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Linq;
using Hyena;
using Mocks;
using Moq;
using NUnit.Framework;

namespace FSpot.Import
{
	[TestFixture]
	public class PhotoFileTrackerTests
	{
		static readonly SafeUri sourceUri = new SafeUri ("/path/to/photo.jpg");
		static readonly SafeUri xmpSourceUri = new SafeUri ("/path/to/photo.xmp");

		static readonly SafeUri targetUri = new SafeUri ("/photo/store/2016/02/06/photo.jpg");
		static readonly SafeUri xmpTargetUri = new SafeUri ("/photo/store/2016/02/06/photo.xmp");

		[Test]
		public void InitialListsAreEmpty ()
		{
			var tracker = new PhotoFileTracker (null);

			Assert.AreEqual (0, tracker.OriginalFiles.Count ());
			Assert.AreEqual (0, tracker.CopiedFiles.Count ());
		}

		[Test]
		public void DontCopyExistingFile ()
		{
			var photo = PhotoMock.Create (targetUri);
			var fileSystem = new FileSystemMock ();
			var tracker = new PhotoFileTracker (fileSystem);

			tracker.CopyIfNeeded (photo, targetUri);

			Assert.AreEqual (0, tracker.OriginalFiles.Count ());
			Assert.AreEqual (0, tracker.CopiedFiles.Count ());
			Assert.AreEqual (targetUri, photo.DefaultVersion.Uri);
			fileSystem.FileMock.Verify (f => f.Copy (It. IsAny<SafeUri> (), It.IsAny<SafeUri> (), It.IsAny<bool> ()), Times.Never);
		}

		[Test]
		public void CopyNewFile ()
		{
			var photo = PhotoMock.Create (sourceUri);
			var fileSystem = new FileSystemMock ();
			var tracker = new PhotoFileTracker (fileSystem);

			tracker.CopyIfNeeded (photo, targetUri);

			CollectionAssert.AreEquivalent (new[]{ sourceUri }, tracker.OriginalFiles);
			CollectionAssert.AreEquivalent (new[]{ targetUri }, tracker.CopiedFiles);
			Assert.AreEqual (targetUri, photo.DefaultVersion.Uri);
			fileSystem.FileMock.Verify (f => f.Copy (sourceUri, targetUri, false), Times.Once);
		}

		[Test]
		public void XmpIsCopiedIfItExists ()
		{
			var photo = PhotoMock.Create (sourceUri);
			var fileSystem = new FileSystemMock (xmpSourceUri);
			var tracker = new PhotoFileTracker (fileSystem);

			tracker.CopyIfNeeded (photo, targetUri);

			CollectionAssert.AreEquivalent (new[]{ sourceUri, xmpSourceUri }, tracker.OriginalFiles);
			CollectionAssert.AreEquivalent (new[]{ targetUri, xmpTargetUri }, tracker.CopiedFiles);
			Assert.AreEqual (targetUri, photo.DefaultVersion.Uri);
			fileSystem.FileMock.Verify (f => f.Copy (sourceUri, targetUri, false), Times.Once);
			fileSystem.FileMock.Verify (f => f.Copy (xmpSourceUri, xmpTargetUri, true), Times.Once);
		}
	}
}
