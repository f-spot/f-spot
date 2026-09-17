// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;
using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Core.Tests
{
	[TestClass]
	public class PhotoInfoFormatterTests
	{
		[TestMethod]
		public void Name_UsesThePhotoNameWithoutMetadata ()
		{
			var photo = new FilePhoto (new SafeUri ("file:///tmp/vacation.jpg"));

			Assert.AreEqual ("vacation.jpg", PhotoInfoFormatter.Name (photo));
		}

		[TestMethod]
		public void Name_EmptyWhenThePhotoIsMissing ()
		{
			Assert.AreEqual (string.Empty, PhotoInfoFormatter.Name (null));
		}

		[TestMethod]
		public void Size_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.Size (null));
		}

		[TestMethod]
		public void Exposure_NoneWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenNoneParen, PhotoInfoFormatter.Exposure (null));
		}

		[TestMethod]
		public void FocalLength_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.FocalLength (null));
		}

		[TestMethod]
		public void Camera_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.Camera (null));
		}

		[TestMethod]
		public void Creator_UnknownWhenMetadataIsMissing ()
		{
			Assert.AreEqual (Strings.ParenUnknownParen, PhotoInfoFormatter.Creator (null));
		}

		[TestMethod]
		public void Size_ReadsWidthAndHeightFromAJpeg ()
		{
			var uri = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
			using (var metadata = FSpot.Utils.MetadataUtils.Parse (uri)) {
				Assert.IsNotNull (metadata);
				string size = PhotoInfoFormatter.Size (metadata);
				Assert.AreNotEqual (Strings.ParenUnknownParen, size);
				Assert.Contains ("x", size);
			}
		}
	}
}
