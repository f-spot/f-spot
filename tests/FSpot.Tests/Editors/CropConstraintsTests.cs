// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using FSpot.Editors;
using FSpot.Imaging;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Editors
{
	[TestClass]
	public sealed class CropConstraintsTests
	{
		[TestMethod]
		public void Build_MatchesGtkComboOrder ()
		{
			var list = CropConstraints.Build (new[] { new CropConstraint ("Mine", 3) });

			var labels = list.Select (c => c.Label).ToArray ();
			Assert.AreSequenceEqual (new[] {
				Strings.NoConstraint, Strings.SameAsPhoto, "Mine",
				Strings.FourByThreeBook, Strings.FourBySixPostcard, Strings.FiveBySevenL2L,
				Strings.EightByTen, Strings.Square, Strings.CustomRatios
			}, labels);
			Assert.AreEqual (CropConstraintKind.SameAsPhoto, list[1].Kind);
			Assert.AreEqual (CropConstraintKind.AddCustom, list[^1].Kind);
			Assert.AreEqual (6.0 / 4.0, list[4].XyRatio);
		}

		[TestMethod]
		public void RatioFor_SameAsPhotoUsesThePhotoAspect ()
		{
			var same = new CropConstraint ("same", 0, CropConstraintKind.SameAsPhoto);

			Assert.AreEqual (1.5, CropConstraints.RatioFor (same, 300, 200));
			Assert.AreEqual (0, CropConstraints.RatioFor (same, 0, 0));
			Assert.AreEqual (0, CropConstraints.RatioFor (new CropConstraint ("add", 0, CropConstraintKind.AddCustom), 300, 200));
			Assert.AreEqual (1.25, CropConstraints.RatioFor (new CropConstraint ("n", 1.25), 300, 200));
		}

		[TestMethod]
		public void Serialize_RoundTripsInGtkXmlShape ()
		{
			var xml = CropConstraints.Serialize (new[] { new CropConstraint ("Wide", 2.5) });

			Assert.Contains ("<SelectionConstraint", xml[0]);
			Assert.Contains ("<Label>Wide</Label>", xml[0]);
			Assert.Contains ("<XyRatio>2.5</XyRatio>", xml[0]);
			var back = CropConstraints.Deserialize (xml);
			Assert.HasCount (1, back);
			Assert.AreEqual ("Wide", back[0].Label);
			Assert.AreEqual (2.5, back[0].XyRatio);
		}

		[TestMethod]
		public void Deserialize_ReadsGtkWrittenEntriesAndSkipsGarbage ()
		{
			string gtk = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\n<SelectionConstraint xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n  <Label>Panorama</Label>\n  <XyRatio>3</XyRatio>\n</SelectionConstraint>";

			var list = CropConstraints.Deserialize (new[] { gtk, "not xml", null, "" });

			Assert.HasCount (1, list);
			Assert.AreEqual ("Panorama", list[0].Label);
			Assert.AreEqual (3, list[0].XyRatio);
			Assert.IsEmpty (CropConstraints.Deserialize (null));
		}

		[TestMethod]
		[DataRow ("1.5", 1.5)]
		[DataRow ("16:9", 16.0 / 9.0)]
		[DataRow ("4/3", 4.0 / 3.0)]
		[DataRow ("3/4", 4.0 / 3.0)]
		[DataRow ("0.5", 2.0)]
		public void TryParseRatio_AcceptsNumbersAndFractionsNormalizedToAtLeastOne (string text, double expected)
		{
			Assert.IsTrue (CropConstraints.TryParseRatio (text, out double ratio));
			Assert.AreEqual (expected, ratio, 1e-9);
		}

		[TestMethod]
		[DataRow ("")]
		[DataRow ("abc")]
		[DataRow ("4/0")]
		[DataRow ("0")]
		[DataRow ("-2")]
		public void TryParseRatio_RejectsInvalidText (string text)
			=> Assert.IsFalse (CropConstraints.TryParseRatio (text, out _));

		[TestMethod]
		public void Constrain_ZeroRatioLeavesTheSelectionAlone ()
		{
			var sel = new PixelRect (1, 2, 30, 40);

			var result = CropConstraints.Constrain (sel, 0, 100, 100);

			Assert.AreEqual (sel, result);
		}

		[TestMethod]
		public void Constrain_GrowsTheShortSideToMatchTheRatio ()
		{
			var result = CropConstraints.Constrain (new PixelRect (0, 0, 40, 10), 2.0, 100, 100);

			Assert.AreEqual (new PixelRect (0, 0, 40, 20), result);
		}

		[TestMethod]
		public void Constrain_FlipsTheRatioForPortraitSelections ()
		{
			var result = CropConstraints.Constrain (new PixelRect (0, 0, 10, 40), 2.0, 100, 100);

			Assert.AreEqual (new PixelRect (0, 0, 20, 40), result);
		}

		[TestMethod]
		public void Constrain_StaysInsideTheImage ()
		{
			var result = CropConstraints.Constrain (new PixelRect (80, 80, 40, 10), 1.0, 100, 100);

			Assert.AreEqual (40, result.Width);
			Assert.AreEqual (40, result.Height);
			Assert.AreEqual (60, result.X);
			Assert.AreEqual (60, result.Y);
		}
	}
}
