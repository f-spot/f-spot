// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Imaging;
using FSpot.Resources.Lang;

using TagLib.Image;

namespace FSpot.Editors
{
	// Widgets collect selection/settings; this is the session Core (and both
	// clients) use to decide instant-apply vs commit, and to build the context
	// PhotoEditorOperations already understands.
	public sealed class PhotoEditorSession
	{
		public PhotoEditorSession (PhotoEditorDescriptor editor, int photoMaxEdge = 0)
		{
			Editor = editor ?? throw new ArgumentNullException (nameof (editor));
			Context = new PhotoEditorContext ();
			if (photoMaxEdge > 0)
				Context.ResizeMaxEdge = photoMaxEdge;
		}

		public PhotoEditorDescriptor Editor { get; }
		public PhotoEditorContext Context { get; }

		public PixelRect Selection {
			get => Context.Selection;
			set => Context.Selection = value;
		}

		public bool CanCommit
			=> PhotoEditorOperations.CanProcess (Editor)
				&& (!Editor.NeedsSelection || !Selection.IsEmpty);

		public string ApplyCaption {
			get {
				if (Editor.Id == PhotoEditorCatalog.Color.Id)
					return Strings.Adjust;
				if (Editor.Id == PhotoEditorCatalog.RedEye.Id)
					return Strings.FixExclamation;
				return Strings.Apply;
			}
		}

		public string Hint {
			get {
				if (Editor.Id == PhotoEditorCatalog.Crop.Id)
					return Strings.SelectTheAreaThatNeedsCropping;
				if (Editor.Id == PhotoEditorCatalog.RedEye.Id)
					return Strings.SelectTheEyesYouWishToFix;
				if (Editor.Id == PhotoEditorCatalog.Pixelate.Id)
					return Strings.SelectTheAreaThatYouWantPixelated;
				if (Editor.Id == PhotoEditorCatalog.Blackout.Id)
					return Strings.SelectTheAreaThatYouWantBlackedOut;
				return string.Empty;
			}
		}

		public static bool IsInstant (PhotoEditorDescriptor editor)
			=> PhotoEditorOperations.CanProcess (editor)
				&& !editor.NeedsSelection
				&& !editor.HasSettings;

		// GTK's Tilt, SoftFocus, Color and BW editors call UpdatePreview on
		// every slider change; the rest only render on Apply.
		public static bool HasLivePreview (PhotoEditorDescriptor editor)
			=> editor != null
				&& (editor.Id == PhotoEditorCatalog.Tilt.Id
					|| editor.Id == PhotoEditorCatalog.SoftFocus.Id
					|| editor.Id == PhotoEditorCatalog.Color.Id
					|| editor.Id == PhotoEditorCatalog.BW.Id);

		public static bool NeedsSession (PhotoEditorDescriptor editor)
			=> PhotoEditorOperations.CanProcess (editor)
				&& (editor.NeedsSelection || editor.HasSettings);

		// Preview and BaseImageFile.Load both apply EXIF, so the buffer is
		// already in display space; TransformRect would double-map if we
		// passed the file's orientation.
		public PhotoEditorContext CreateContext ()
		{
			return new PhotoEditorContext {
				Selection = Context.Selection,
				Orientation = ImageOrientation.TopLeft,
				RedEyeThreshold = Context.RedEyeThreshold,
				ResizeMaxEdge = Context.ResizeMaxEdge,
				ColorExposure = Context.ColorExposure,
				ColorBrightness = Context.ColorBrightness,
				ColorContrast = Context.ColorContrast,
				ColorHue = Context.ColorHue,
				ColorSaturation = Context.ColorSaturation,
				ColorTemperature = Context.ColorTemperature,
				ColorTint = Context.ColorTint,
				BwRed = Context.BwRed,
				BwGreen = Context.BwGreen,
				BwBlue = Context.BwBlue,
				BwCurve = Context.BwCurve,
				TiltDegrees = Context.TiltDegrees,
				SoftFocusRadius = Context.SoftFocusRadius,
				SharpenAmount = Context.SharpenAmount,
				SharpenRadius = Context.SharpenRadius,
				SharpenThreshold = Context.SharpenThreshold
			};
		}
	}
}
