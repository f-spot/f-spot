// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Imaging;

using TagLib.Image;

namespace FSpot.Editors
{
	// GTK stores the selection in raw pixbuf coordinates. Load() returns an
	// upright buffer, so crop, red-eye, pixelate, and blackout map that rectangle once.
	public static class GtkApplyContext
	{
		public static PhotoEditorContext Create (PixelRect rawSelection, ImageOrientation pixbufOrientation, int redEyeThreshold)
		{
			return new PhotoEditorContext {
				Selection = rawSelection,
				Orientation = pixbufOrientation,
				RedEyeThreshold = redEyeThreshold
			};
		}
	}
}
