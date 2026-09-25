// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using FSpot.Imaging;
using FSpot.Resources.Lang;
using FSpot.Settings;

namespace FSpot.Editors
{
	public enum CropConstraintKind
	{
		Normal,
		AddCustom,
		SameAsPhoto
	}

	public sealed class CropConstraint
	{
		public CropConstraint (string label, double xyRatio, CropConstraintKind kind = CropConstraintKind.Normal)
		{
			Label = label ?? string.Empty;
			XyRatio = xyRatio;
			Kind = kind;
		}

		public string Label { get; }
		public double XyRatio { get; }
		public CropConstraintKind Kind { get; }

		public override string ToString () => Label;
	}

	// Same XML shape GTK's SelectionRatioDialog.SelectionConstraint writes
	// through XmlSerializer, so both clients share the CustomCropRatios pref.
	[XmlRoot ("SelectionConstraint")]
	public sealed class SelectionConstraintXml
	{
		public string Label { get; set; }
		public double XyRatio { get; set; }
	}

	// The crop editor's ratio combo (CropEditor.PopulateConstraints) and the
	// selection clamp GTK's ImageView applies while dragging (ConstrainSelection).
	public static class CropConstraints
	{
		static readonly XmlSerializer serializer = new XmlSerializer (typeof (SelectionConstraintXml));

		public static IReadOnlyList<CropConstraint> Defaults => new[] {
			new CropConstraint (Strings.FourByThreeBook, 4.0 / 3.0),
			new CropConstraint (Strings.FourBySixPostcard, 6.0 / 4.0),
			new CropConstraint (Strings.FiveBySevenL2L, 7.0 / 5.0),
			new CropConstraint (Strings.EightByTen, 10.0 / 8.0),
			new CropConstraint (Strings.Square, 1.0)
		};

		public static IReadOnlyList<CropConstraint> Build (IEnumerable<CropConstraint> custom)
		{
			var list = new List<CropConstraint> {
				new CropConstraint (Strings.NoConstraint, 0),
				new CropConstraint (Strings.SameAsPhoto, 0, CropConstraintKind.SameAsPhoto)
			};
			if (custom != null)
				list.AddRange (custom);
			list.AddRange (Defaults);
			list.Add (new CropConstraint (Strings.CustomRatios, 0, CropConstraintKind.AddCustom));
			return list;
		}

		public static double RatioFor (CropConstraint constraint, int photoWidth, int photoHeight)
		{
			if (constraint == null)
				return 0;
			switch (constraint.Kind) {
			case CropConstraintKind.Normal:
				return constraint.XyRatio;
			case CropConstraintKind.SameAsPhoto:
				return photoWidth > 0 && photoHeight > 0 ? (double)photoWidth / photoHeight : 0;
			default:
				return 0;
			}
		}

		public static IReadOnlyList<CropConstraint> Deserialize (IEnumerable<string> xml)
		{
			var list = new List<CropConstraint> ();
			if (xml == null)
				return list;
			foreach (string entry in xml) {
				if (string.IsNullOrWhiteSpace (entry))
					continue;
				try {
					using var reader = XmlReader.Create (new StringReader (entry));
					if (serializer.Deserialize (reader) is SelectionConstraintXml item)
						list.Add (new CropConstraint (item.Label, item.XyRatio));
				} catch (InvalidOperationException ex) {
					Logger.Log.Warning (ex, "Skipping unreadable custom crop ratio");
				}
			}
			return list;
		}

		public static string[] Serialize (IEnumerable<CropConstraint> constraints)
		{
			var list = new List<string> ();
			foreach (var constraint in constraints) {
				using var writer = new StringWriter (CultureInfo.InvariantCulture);
				serializer.Serialize (writer, new SelectionConstraintXml { Label = constraint.Label, XyRatio = constraint.XyRatio });
				list.Add (writer.ToString ());
			}
			return list.ToArray ();
		}

		public static IReadOnlyList<CropConstraint> LoadCustom ()
			=> Deserialize (Preferences.Get<string[]> (Preferences.CustomCropRatios));

		public static void SaveCustom (IEnumerable<CropConstraint> constraints)
			=> Preferences.Set (Preferences.CustomCropRatios, Serialize (constraints));

		// SelectionRatioDialog.ParseRatio + HandleRatioEdited: a number, or
		// "a/b" / "a:b"; stored as >= 1 so orientation follows the drag.
		public static bool TryParseRatio (string text, out double ratio)
		{
			ratio = 0;
			if (string.IsNullOrWhiteSpace (text))
				return false;
			text = text.Trim ();
			if (!double.TryParse (text, NumberStyles.Float, CultureInfo.CurrentCulture, out ratio)) {
				int split = text.IndexOfAny (new[] { '/', ':' });
				if (split < 0
					|| !double.TryParse (text.Substring (0, split), NumberStyles.Float, CultureInfo.CurrentCulture, out double x)
					|| !double.TryParse (text.Substring (split + 1), NumberStyles.Float, CultureInfo.CurrentCulture, out double y)
					|| y == 0)
					return false;
				ratio = x / y;
			}
			if (double.IsNaN (ratio) || double.IsInfinity (ratio) || ratio <= 0)
				return false;
			if (ratio < 1.0)
				ratio = 1.0 / ratio;
			return true;
		}

		// Port of ImageView.ConstrainSelection: flip the ratio to match the
		// selection's orientation, then shrink/grow to fit inside the image.
		public static PixelRect Constrain (PixelRect sel, double xyRatio, int imageWidth, int imageHeight)
		{
			if (xyRatio <= 0 || sel.IsEmpty || imageWidth <= 0 || imageHeight <= 0)
				return sel;

			double constrain = xyRatio;
			if (sel.Width > sel.Height && xyRatio < 1 || sel.Width < sel.Height && xyRatio > 1)
				constrain = 1.0 / constrain;

			double ratio = (double)sel.Width / sel.Height;
			int height = sel.Height;
			int width = sel.Width;
			if (ratio > constrain) {
				height = (int)(sel.Width / constrain);
				if (height > imageHeight) {
					height = sel.Height;
					width = (int)(height * constrain);
				}
			} else {
				width = (int)(height * constrain);
				if (width > imageWidth) {
					width = sel.Width;
					height = (int)(width / constrain);
				}
			}

			return new PixelRect (
				sel.X + width < imageWidth ? sel.X : imageWidth - width,
				sel.Y + height < imageHeight ? sel.Y : imageHeight - height,
				width, height);
		}
	}
}
