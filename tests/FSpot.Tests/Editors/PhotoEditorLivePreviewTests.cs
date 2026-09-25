// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

using FSpot.Editors;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Editors
{
	[TestClass]
	public sealed class PhotoEditorLivePreviewTests
	{
		[TestMethod]
		public void HasLivePreview_IsTheGtkUpdatePreviewEditors ()
		{
			var live = new HashSet<string> {
				PhotoEditorCatalog.Tilt.Id, PhotoEditorCatalog.SoftFocus.Id,
				PhotoEditorCatalog.Color.Id, PhotoEditorCatalog.BW.Id
			};
			foreach (var editor in PhotoEditorCatalog.All)
				Assert.AreEqual (live.Contains (editor.Id), PhotoEditorSession.HasLivePreview (editor), editor.Id);
			Assert.IsFalse (PhotoEditorSession.HasLivePreview (null));
		}

		[TestMethod]
		public void Context_RaisesPropertyChangedForSliderSettingsOnly ()
		{
			var context = new PhotoEditorContext ();
			var changed = new List<string> ();
			context.PropertyChanged += (sender, e) => changed.Add (e.PropertyName);

			context.TiltDegrees = 5;
			context.TiltDegrees = 5;
			context.SoftFocusRadius = 0.2;
			context.ColorHue = 10;
			context.BwRed = 50;

			Assert.AreSequenceEqual (new[] {
				nameof (PhotoEditorContext.TiltDegrees),
				nameof (PhotoEditorContext.SoftFocusRadius),
				nameof (PhotoEditorContext.ColorHue),
				nameof (PhotoEditorContext.BwRed)
			}, changed);
		}

		[TestMethod]
		public void ImageToView_InvertsMapPoint ()
		{
			var view = PhotoViewTransform.ImageToView (200, 100, 400, 100, 2, 5, -3, 100, 50);
			var back = PhotoViewTransform.MapPoint (200, 100, 400, 100, 2, 5, -3, view.X, view.Y);

			Assert.AreEqual (100, back.X);
			Assert.AreEqual (50, back.Y);
		}
	}
}
