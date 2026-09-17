// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

using FSpot.Core;
using FSpot.Resources.Lang;

namespace FSpot.Editors
{
	// Built-in + shipped extension editors. GTK still instantiates the Pixbuf
	// subclasses from addins; both clients read labels/icons/flags from here
	// so the Edit-page list cannot drift from FSpot.addin.xml.
	public static class PhotoEditorCatalog
	{
		public static readonly PhotoEditorDescriptor Crop = new PhotoEditorDescriptor (
			"Crop", () => Strings.Crop, "crop", needsSelection: true);

		public static readonly PhotoEditorDescriptor RedEye = new PhotoEditorDescriptor (
			"RedEye", () => Strings.RedEyeReduction, "red-eye-remove", needsSelection: true);

		public static readonly PhotoEditorDescriptor Desaturate = new PhotoEditorDescriptor (
			"Desaturate", () => Strings.Desaturate, "color-desaturate", canHandleMultiple: true);

		public static readonly PhotoEditorDescriptor Sepia = new PhotoEditorDescriptor (
			"Sepia", () => Strings.SepiaTone, "color-sepia", canHandleMultiple: true);

		public static readonly PhotoEditorDescriptor Tilt = new PhotoEditorDescriptor (
			"Tilt", () => Strings.Straighten, "align-horizon", hasSettings: true);

		public static readonly PhotoEditorDescriptor SoftFocus = new PhotoEditorDescriptor (
			"SoftFocus", () => Strings.SoftFocus, "filter-soft-focus", hasSettings: true);

		public static readonly PhotoEditorDescriptor AutoStretch = new PhotoEditorDescriptor (
			"AutoStretch", () => Strings.AutoColor, "autocolor", canHandleMultiple: true);

		public static readonly PhotoEditorDescriptor Color = new PhotoEditorDescriptor (
			"Color", () => Strings.AdjustColors, "adjust-colors", hasSettings: true);

		public static readonly PhotoEditorDescriptor Flip = new PhotoEditorDescriptor (
			"Flip", () => Strings.Flip, "object-flip-horizontal", canHandleMultiple: true);

		public static readonly PhotoEditorDescriptor Pixelate = new PhotoEditorDescriptor (
			"Pixelate", () => Strings.Pixelate, null, needsSelection: true);

		public static readonly PhotoEditorDescriptor BW = new PhotoEditorDescriptor (
			"BW", () => Strings.ConvertToBW, null, hasSettings: true);

		public static readonly PhotoEditorDescriptor Resize = new PhotoEditorDescriptor (
			"Resize", () => Strings.Resize, null, hasSettings: true);

		public static readonly PhotoEditorDescriptor Blackout = new PhotoEditorDescriptor (
			"Blackout", () => Strings.Blackout, null, needsSelection: true);

		public static readonly PhotoEditorDescriptor Sharpen = new PhotoEditorDescriptor (
			"Sharpen", () => Strings.Sharpen, null, hasSettings: true);

		public static IReadOnlyList<PhotoEditorDescriptor> All { get; } = new[] {
			Crop, RedEye, Desaturate, Sepia, Tilt, SoftFocus, AutoStretch, Color,
			Flip, Pixelate, BW, Resize, Blackout, Sharpen
		};

		public static IEnumerable<PhotoEditorDescriptor> VisibleIn (LibraryViewMode mode)
			=> All.Where (editor => editor.VisibleIn (mode));
	}
}
