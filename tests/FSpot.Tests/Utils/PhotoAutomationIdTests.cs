// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Core;
using FSpot.Utils;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Utils
{
	[TestClass]
	public sealed class PhotoAutomationIdTests
	{
		[TestMethod]
		public void For_CatalogPhoto_UsesTheCatalogId ()
		{
			var photo = new Photo (null, null, 42, DateTimeUtil.FromDateTime (DateTime.UtcNow));
			photo.AddVersionUnsafely (Photo.OriginalVersionId, new SafeUri ("file:///photos/x.jpg"), "x.jpg", string.Empty, "Original", true);
			Assert.AreEqual ("photo.42", PhotoAutomationId.For (photo));
		}

		[TestMethod]
		public void For_NonCatalogPhoto_FallsBackToAStablePathId ()
		{
			var photo = new FilePhoto (new SafeUri ("/library/trip/img.jpg"));
			string id = PhotoAutomationId.For (photo);
			Assert.StartsWith ("photo.", id);
			Assert.DoesNotContain ("42", id);
			Assert.Contains ("img.jpg", id);
		}
	}
}
