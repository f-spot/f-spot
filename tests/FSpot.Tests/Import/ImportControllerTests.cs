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
		[Test]
		public void FindImportDestinationTest ()
		{
			var fileUri = new SafeUri ("/path/to/photo.jpg");
			var targetBaseUri = new  SafeUri ("/photo/store");
			var targetUri = new SafeUri ("/photo/store/2016/02/06");
			var date = new DateTime (2016, 2, 6);
			var source = PhotoMock.Create (fileUri, date);

			var result = ImportController.FindImportDestination (source, targetBaseUri);

			Assert.AreEqual (targetUri, result);
		}
	}
}
