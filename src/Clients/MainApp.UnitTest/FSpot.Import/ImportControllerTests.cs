//
// ImportControllerTests.cs
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

using System;
using FSpot.Core;
using Hyena;
using Mocks;
using NUnit.Framework;

namespace FSpot.Import
{
	[TestFixture]
	public class ImportControllerTests
	{
		static readonly SafeUri targetBaseUri = new  SafeUri ("/photo/store");
		static readonly SafeUri sourceUri = new SafeUri ("/path/to/photo.jpg");
		static readonly SafeUri targetUri = new SafeUri ("/photo/store/2016/02/06/photo.jpg");

		static readonly DateTime date = new DateTime (2016, 2, 6);

		[Test]
		// Analysis disable once InconsistentNaming
		public void FindImportDestination_ReturnsTargetUri_IfSourceIsTarget ()
		{
			var source = PhotoMock.Create (targetUri, date);

			var result = ImportController.FindImportDestination (source, targetBaseUri, null);

			Assert.AreEqual (targetUri, result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void FindImportDestination_ReturnsTargetUri_IfTargetDoesNotExist ()
		{
			var source = PhotoMock.Create (sourceUri, date);
			var fileSystem = new FileSystemMock ();

			var result = ImportController.FindImportDestination (source, targetBaseUri, fileSystem);

			Assert.AreEqual (targetUri, result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void FindImportDestination_ReturnsTargetUriWithIndex_IfTargetDoesExist ()
		{
			var source = PhotoMock.Create (sourceUri, date);
			var fileSystem = new FileSystemMock (new[]{ targetUri });

			var result = ImportController.FindImportDestination (source, targetBaseUri, fileSystem);

			Assert.AreEqual (new SafeUri ("/photo/store/2016/02/06/photo-1.jpg"), result);
		}

		[Test]
		// Analysis disable once InconsistentNaming
		public void FindImportDestination_ReturnsTargetUriWithNextFreeIndex_IfTargetDoesExist ()
		{
			var source = PhotoMock.Create (sourceUri, date);
			var fileSystem = new FileSystemMock (new[] {
				targetUri,
				new SafeUri ("/photo/store/2016/02/06/photo-1.jpg"),
				new SafeUri ("/photo/store/2016/02/06/photo-2.jpg"),
				new SafeUri ("/photo/store/2016/02/06/photo-4.jpg")
			});

			var result = ImportController.FindImportDestination (source, targetBaseUri, fileSystem);

			Assert.AreEqual (new SafeUri ("/photo/store/2016/02/06/photo-3.jpg"), result);
		}
	}
}
