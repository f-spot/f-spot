// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using FSpot.Editors;
using FSpot.Imaging;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Editors
{
	[TestClass]
	public sealed class PhotoEditorSessionTests
	{
		[TestMethod]
		public void InstantApply_IsTheCoreToolsWithNoSelectionOrSettings ()
		{
			foreach (var editor in PhotoEditorCatalog.All) {
				bool expected = PhotoEditorOperations.CanProcess (editor)
					&& !editor.NeedsSelection
					&& !editor.HasSettings;
				Assert.AreEqual (expected, PhotoEditorSession.IsInstant (editor), editor.Id);
			}
		}

		[TestMethod]
		public void NeedsSession_IsTheCoreToolsThatNeedUi ()
		{
			foreach (var editor in PhotoEditorCatalog.All) {
				bool expected = PhotoEditorOperations.CanProcess (editor)
					&& (editor.NeedsSelection || editor.HasSettings);
				Assert.AreEqual (expected, PhotoEditorSession.NeedsSession (editor), editor.Id);
			}
		}

		[TestMethod]
		public void TiltAndSoftFocus_NeedASettingsSession ()
		{
			Assert.IsFalse (PhotoEditorSession.IsInstant (PhotoEditorCatalog.Tilt));
			Assert.IsTrue (PhotoEditorSession.NeedsSession (PhotoEditorCatalog.Tilt));
			Assert.IsFalse (PhotoEditorSession.IsInstant (PhotoEditorCatalog.SoftFocus));
			Assert.IsTrue (PhotoEditorSession.NeedsSession (PhotoEditorCatalog.SoftFocus));
		}

		[TestMethod]
		public void CreateContext_CopiesTiltAndSoftFocusSettings ()
		{
			var tilt = new PhotoEditorSession (PhotoEditorCatalog.Tilt);
			tilt.Context.TiltDegrees = -12;
			Assert.AreEqual (-12, tilt.CreateContext ().TiltDegrees);

			var soft = new PhotoEditorSession (PhotoEditorCatalog.SoftFocus);
			Assert.AreEqual (0.5, soft.Context.SoftFocusRadius);
			soft.Context.SoftFocusRadius = 0.25;
			Assert.AreEqual (0.25, soft.CreateContext ().SoftFocusRadius);
		}

		[TestMethod]
		public void Sharpen_StartsAtGtkDefaultsAndCanCommit ()
		{
			var session = new PhotoEditorSession (PhotoEditorCatalog.Sharpen);
			Assert.AreEqual (0.5, session.Context.SharpenAmount);
			Assert.AreEqual (5, session.Context.SharpenRadius);
			Assert.AreEqual (0, session.Context.SharpenThreshold);
			Assert.IsTrue (session.CanCommit);
			Assert.IsTrue (PhotoEditorSession.NeedsSession (PhotoEditorCatalog.Sharpen));
		}

		[TestMethod]
		public void Crop_CannotCommitUntilThereIsASelection ()
		{
			var session = new PhotoEditorSession (PhotoEditorCatalog.Crop);
			Assert.IsFalse (session.CanCommit);

			session.Selection = new PixelRect (10, 20, 30, 40);
			Assert.IsTrue (session.CanCommit);
			Assert.AreEqual (new PixelRect (10, 20, 30, 40), session.CreateContext ().Selection);
		}

		[TestMethod]
		public void Color_CanCommitWithoutASelection ()
		{
			var session = new PhotoEditorSession (PhotoEditorCatalog.Color);
			session.Context.ColorExposure = 1.5;
			session.Context.ColorTemperature = 6500;

			Assert.IsTrue (session.CanCommit);
			var context = session.CreateContext ();
			Assert.AreEqual (1.5, context.ColorExposure);
			Assert.AreEqual (6500, context.ColorTemperature);
			Assert.IsTrue (context.Selection.IsEmpty);
		}

		[TestMethod]
		public void Resize_StartsAtThePhotoLongEdge ()
		{
			var session = new PhotoEditorSession (PhotoEditorCatalog.Resize, photoMaxEdge: 2000);
			Assert.AreEqual (2000, session.Context.ResizeMaxEdge);
			Assert.IsTrue (session.CanCommit);
		}

		[TestMethod]
		public void CreateContext_LeavesOrientationTopLeftBecauseLoadAlreadyOrients ()
		{
			var session = new PhotoEditorSession (PhotoEditorCatalog.Crop);
			session.Selection = new PixelRect (1, 2, 3, 4);
			Assert.AreEqual (TagLib.Image.ImageOrientation.TopLeft, session.CreateContext ().Orientation);
		}

		[TestMethod]
		public void ApplyCaption_MatchesGtkButtons ()
		{
			Assert.AreEqual (Strings.Adjust, new PhotoEditorSession (PhotoEditorCatalog.Color).ApplyCaption);
			Assert.AreEqual (Strings.FixExclamation, new PhotoEditorSession (PhotoEditorCatalog.RedEye).ApplyCaption);
			Assert.AreEqual (Strings.Apply, new PhotoEditorSession (PhotoEditorCatalog.Crop).ApplyCaption);
		}

		[TestMethod]
		public void Hint_UsesTheGtkSelectionCopy ()
		{
			Assert.AreEqual (Strings.SelectTheAreaThatNeedsCropping,
				new PhotoEditorSession (PhotoEditorCatalog.Crop).Hint);
			Assert.AreEqual (Strings.SelectTheEyesYouWishToFix,
				new PhotoEditorSession (PhotoEditorCatalog.RedEye).Hint);
			Assert.AreEqual (Strings.SelectTheAreaThatYouWantPixelated,
				new PhotoEditorSession (PhotoEditorCatalog.Pixelate).Hint);
			Assert.AreEqual (Strings.SelectTheAreaThatYouWantBlackedOut,
				new PhotoEditorSession (PhotoEditorCatalog.Blackout).Hint);
		}

		[TestMethod]
		public void NeedsSession_CoversEveryCoreEditorThatIsNotInstant ()
		{
			var leftover = PhotoEditorCatalog.All
				.Where (editor => PhotoEditorOperations.CanProcess (editor)
					&& !PhotoEditorSession.IsInstant (editor)
					&& !PhotoEditorSession.NeedsSession (editor))
				.Select (editor => editor.Id)
				.ToList ();
			Assert.IsEmpty (leftover);
		}
	}
}
