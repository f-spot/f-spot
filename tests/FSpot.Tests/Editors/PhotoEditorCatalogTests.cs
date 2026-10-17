// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using FSpot.Core;
using FSpot.Editors;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Editors
{
	[TestClass]
	public sealed class PhotoEditorCatalogTests
	{
		[TestMethod]
		public void Labels_ComeFromFSpotStrings ()
		{
			Assert.AreEqual (Strings.Crop, PhotoEditorCatalog.Crop.Label);
			Assert.AreEqual (Strings.RedEyeReduction, PhotoEditorCatalog.RedEye.Label);
			Assert.AreEqual (Strings.Desaturate, PhotoEditorCatalog.Desaturate.Label);
			Assert.AreEqual (Strings.SepiaTone, PhotoEditorCatalog.Sepia.Label);
			Assert.AreEqual (Strings.Straighten, PhotoEditorCatalog.Tilt.Label);
			Assert.AreEqual (Strings.SoftFocus, PhotoEditorCatalog.SoftFocus.Label);
			Assert.AreEqual (Strings.AutoColor, PhotoEditorCatalog.AutoStretch.Label);
			Assert.AreEqual (Strings.AdjustColors, PhotoEditorCatalog.Color.Label);
			Assert.AreEqual (Strings.Flip, PhotoEditorCatalog.Flip.Label);
			Assert.AreEqual (Strings.Pixelate, PhotoEditorCatalog.Pixelate.Label);
			Assert.AreEqual (Strings.ConvertToBW, PhotoEditorCatalog.BW.Label);
			Assert.AreEqual (Strings.Resize, PhotoEditorCatalog.Resize.Label);
			Assert.AreEqual (Strings.Blackout, PhotoEditorCatalog.Blackout.Label);
			Assert.AreEqual (Strings.Sharpen, PhotoEditorCatalog.Sharpen.Label);
		}

		[TestMethod]
		public void VisibleIn_IconView_OnlyMultiPhotoEditors ()
		{
			var visible = PhotoEditorCatalog.VisibleIn (LibraryViewMode.IconView)
				.Select (editor => editor.Id).ToList ();

			Assert.IsTrue (visible.All (id => PhotoEditorCatalog.All.Single (editor => editor.Id == id).CanHandleMultiple));
			Assert.Contains (PhotoEditorCatalog.Desaturate.Id, visible);
			Assert.Contains (PhotoEditorCatalog.Sepia.Id, visible);
			Assert.Contains (PhotoEditorCatalog.AutoStretch.Id, visible);
			Assert.Contains (PhotoEditorCatalog.Flip.Id, visible);
			Assert.DoesNotContain (PhotoEditorCatalog.Crop.Id, visible);
			Assert.DoesNotContain (PhotoEditorCatalog.RedEye.Id, visible);
			Assert.DoesNotContain (PhotoEditorCatalog.Color.Id, visible);
		}

		[TestMethod]
		public void VisibleIn_PhotoView_ShowsEveryEditor ()
		{
			var visible = PhotoEditorCatalog.VisibleIn (LibraryViewMode.PhotoView)
				.Select (editor => editor.Id).ToList ();

			Assert.AreSequenceEqual (PhotoEditorCatalog.All.Select (editor => editor.Id).ToList (), visible);
		}

		[TestMethod]
		public void Capabilities_MatchGtkConstructors ()
		{
			Assert.IsTrue (PhotoEditorCatalog.Crop.NeedsSelection);
			Assert.IsFalse (PhotoEditorCatalog.Crop.CanHandleMultiple);
			Assert.IsFalse (PhotoEditorCatalog.Crop.HasSettings);

			Assert.IsTrue (PhotoEditorCatalog.RedEye.NeedsSelection);
			Assert.IsTrue (PhotoEditorCatalog.Desaturate.CanHandleMultiple);
			Assert.IsTrue (PhotoEditorCatalog.Sepia.CanHandleMultiple);
			Assert.IsTrue (PhotoEditorCatalog.Tilt.HasSettings);
			Assert.IsTrue (PhotoEditorCatalog.SoftFocus.HasSettings);
			Assert.IsTrue (PhotoEditorCatalog.AutoStretch.CanHandleMultiple);
			Assert.IsTrue (PhotoEditorCatalog.Color.HasSettings);
			Assert.IsTrue (PhotoEditorCatalog.Flip.CanHandleMultiple);
			Assert.IsTrue (PhotoEditorCatalog.Pixelate.NeedsSelection);
			Assert.IsTrue (PhotoEditorCatalog.BW.HasSettings);
			Assert.IsTrue (PhotoEditorCatalog.Resize.HasSettings);
			Assert.IsTrue (PhotoEditorCatalog.Blackout.NeedsSelection);
			Assert.IsTrue (PhotoEditorCatalog.Tilt.HasCoreOperation);
			Assert.IsTrue (PhotoEditorCatalog.SoftFocus.HasCoreOperation);
			Assert.IsTrue (PhotoEditorCatalog.Desaturate.HasCoreOperation);
			Assert.IsTrue (PhotoEditorCatalog.Sharpen.HasSettings);
			Assert.IsTrue (PhotoEditorCatalog.Sharpen.HasCoreOperation);
			Assert.IsFalse (PhotoEditorCatalog.Sharpen.CanHandleMultiple);
		}
	}
}
