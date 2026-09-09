//
// ImportControllerTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

using Mocks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Import
{
	[TestClass]
	public class ImportControllerTests
	{
		[TestMethod]
		public void FindImportDestinationTest ()
		{
			var fileUri = new SafeUri ("/path/to/photo.jpg");
			var targetBaseUri = new SafeUri ("/photo/store");
			var targetUri = new SafeUri ("/photo/store/2016/02/06");
			var date = new DateTime (2016, 2, 6);
			var source = PhotoMock.Create (fileUri, date);

			var result = ImportController.FindImportDestination (source, targetBaseUri);

			Assert.AreEqual (targetUri, result);
		}
	}
}
