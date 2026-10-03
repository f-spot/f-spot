// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;
using FSpot.Imaging;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Editors;

[TestClass]
public sealed class SplineCurveTests
{
	[TestMethod]
	public void Reset_SamplesTheStraightGtkLine ()
	{
		var curve = new SplineCurve ();

		var bytes = curve.ToBytes ();

		Assert.HasCount (256, bytes);
		for (int i = 0; i < bytes.Length; i++)
			Assert.AreEqual ((byte)i, bytes[i], "sample " + i);
	}

	[TestMethod]
	public void Press_InsertsAPointAndLiftsThatTone ()
	{
		var curve = new SplineCurve ();

		// Graph is 256px. A click at the top, halfway across, is farther than
		// GtkCurve's 8px grab radius from either end.
		curve.Press (131, SplineCurve.Radius, 256, 256);

		var bytes = curve.ToBytes ();
		Assert.AreEqual (0, bytes[0]);
		Assert.AreEqual (255, bytes[255]);
		Assert.IsGreaterThan (200, bytes[128]);
	}

	[TestMethod]
	public void DragPastTheEdge_DropsThePointAndRestoresTheLine ()
	{
		var curve = new SplineCurve ();
		curve.Press (131, SplineCurve.Radius, 256, 256);

		curve.Drag (131, -SplineCurve.MinDistance - 1, 256, 256);
		curve.Release ();

		var bytes = curve.ToBytes ();
		Assert.AreEqual (128, bytes[128]);
	}

	[TestMethod]
	public void DraggingEveryHandleOff_LeavesTheGtkFlatMinimum ()
	{
		var curve = new SplineCurve ();

		// A press on the corner handle moves it; it does not insert another point.
		curve.Press (SplineCurve.Radius, SplineCurve.Radius, 256, 256);
		curve.Drag (SplineCurve.Radius, -SplineCurve.MinDistance - 1, 256, 256);
		curve.Release ();
		Assert.HasCount (1, curve.ControlPoints);

		curve.Press (256 + SplineCurve.Radius - 1, SplineCurve.Radius, 256, 256);
		curve.Drag (256 + SplineCurve.Radius - 1, -SplineCurve.MinDistance - 1, 256, 256);
		curve.Release ();

		var bytes = curve.ToBytes ();
		Assert.AreEqual (0, bytes[0]);
		Assert.AreEqual (0, bytes[128]);
		Assert.AreEqual (0, bytes[255]);
	}

	[TestMethod]
	public void LiftedCurve_ChangesTheBlackAndWhitePixels ()
	{
		var curve = new SplineCurve ();
		curve.Press (131, SplineCurve.Radius, 256, 256);
		var input = new ImageBuffer (1, 1, false, 3, [128, 128, 128]);

		var output = PhotoEditorOperations.Process (PhotoEditorCatalog.BW, input, null, new PhotoEditorContext {
			BwRed = 1,
			BwGreen = 1,
			BwBlue = 1,
			BwCurve = curve.ToBytes ()
		});

		Assert.AreNotEqual (128, output.Pixels[0]);
		Assert.AreEqual (output.Pixels[0], output.Pixels[1]);
		Assert.AreEqual (output.Pixels[0], output.Pixels[2]);
	}

	[TestMethod]
	public void BwCurve_NotifiesSoTheLivePreviewRenders ()
	{
		var context = new PhotoEditorContext ();
		string name = null;
		context.PropertyChanged += (_, e) => name = e.PropertyName;

		context.BwCurve = new byte[256];

		Assert.AreEqual (nameof (PhotoEditorContext.BwCurve), name);
	}
}
