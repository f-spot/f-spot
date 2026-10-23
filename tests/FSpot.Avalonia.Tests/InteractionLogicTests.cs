// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Input;

using FSpot.Avalonia.Controls;
using FSpot.Avalonia.Imaging;
using FSpot.Avalonia.Input;
using FSpot.Avalonia.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class GridNavigationTests
{
	static readonly HashSet<int> None = [];

	static GridKeyResult Press (Key key, int focus, KeyModifiers modifiers = KeyModifiers.None, IReadOnlySet<int>? selection = null, int count = 20)
		=> GridNavigation.Apply (key, modifiers, focus, columns: 4, rows: 2, count, selection ?? None);

	[UiTestMethod]
	public void VimKeysMoveLikeTheArrows ()
	{
		Assert.AreEqual (Press (Key.Down, 5).Focus, Press (Key.J, 5).Focus);
		Assert.AreEqual (9, Press (Key.J, 5).Focus);
		Assert.AreEqual (1, Press (Key.K, 5).Focus);
		Assert.AreEqual (4, Press (Key.H, 5).Focus);
		Assert.AreEqual (6, Press (Key.L, 5).Focus);
	}

	[UiTestMethod]
	public void MovesClampToTheCollection ()
	{
		Assert.AreEqual (0, Press (Key.Up, 2).Focus);
		Assert.AreEqual (19, Press (Key.PageDown, 17).Focus);
		Assert.AreEqual (0, Press (Key.PageUp, 3).Focus);
		Assert.AreEqual (0, Press (Key.Home, 11).Focus);
		Assert.AreEqual (19, Press (Key.End, 2).Focus);
	}

	[UiTestMethod]
	public void PageKeysMoveAScreenOfRows ()
		=> Assert.AreEqual (13, Press (Key.PageDown, 5).Focus);

	[UiTestMethod]
	public void ControlShiftArrowsGoToTheRowEnds ()
	{
		var mods = KeyModifiers.Control | KeyModifiers.Shift;
		Assert.AreEqual (4, Press (Key.Left, 6, mods).Focus);
		Assert.AreEqual (7, Press (Key.Right, 5, mods).Focus);
	}

	[UiTestMethod]
	public void PlainMoveSelectsOnlyTheNewFocus ()
	{
		var result = Press (Key.Right, 2, selection: new HashSet<int> { 0, 1, 2 });
		Assert.AreEqual (GridKeyAction.Move, result.Action);
		Assert.AreSequenceEqual (new[] { 3 }, result.Selection.Order ().ToArray ());
	}

	[UiTestMethod]
	public void ShiftExtendsThenShrinksTheRange ()
	{
		var grown = Press (Key.Right, 2, KeyModifiers.Shift, new HashSet<int> { 2 });
		Assert.AreSequenceEqual (new[] { 2, 3 }, grown.Selection.Order ().ToArray ());

		var shrunk = Press (Key.Left, 3, KeyModifiers.Shift, grown.Selection);
		Assert.AreEqual (2, shrunk.Focus);
		Assert.AreSequenceEqual (new[] { 2 }, shrunk.Selection.Order ().ToArray ());
	}

	[UiTestMethod]
	public void ControlMovesWithoutSelecting ()
	{
		var result = Press (Key.Right, 2, KeyModifiers.Control, new HashSet<int> { 2 });
		Assert.AreEqual (3, result.Focus);
		Assert.AreSequenceEqual (new[] { 2 }, result.Selection.Order ().ToArray ());
	}

	[UiTestMethod]
	public void ControlSpaceTogglesAndSpaceSelects ()
	{
		var toggled = Press (Key.Space, 4, KeyModifiers.Control, new HashSet<int> { 1, 4 });
		Assert.AreEqual (GridKeyAction.Toggle, toggled.Action);
		Assert.AreSequenceEqual (new[] { 1 }, toggled.Selection.Order ().ToArray ());

		var plain = Press (Key.Space, 4, selection: new HashSet<int> { 1, 4 });
		Assert.AreSequenceEqual (new[] { 4 }, plain.Selection.Order ().ToArray ());
	}

	[UiTestMethod]
	public void EnterOpensTheFocusCell ()
	{
		var result = Press (Key.Enter, 7);
		Assert.AreEqual (GridKeyAction.Open, result.Action);
		Assert.AreEqual (7, result.Focus);
	}

	[UiTestMethod]
	public void RandomPicksBelowTheLastCellLikeGtk ()
	{
		int? max = null;
		var result = GridNavigation.Apply (Key.R, KeyModifiers.None, 0, 4, 2, 20, None, (low, high) => {
			max = high;
			return 12;
		});
		Assert.AreEqual (19, max);
		Assert.AreEqual (12, result.Focus);
	}

	[UiTestMethod]
	public void ArrowFromSelectAllDoesNotNeedEveryIndex ()
	{
		var result = GridNavigation.Apply (Key.Right, KeyModifiers.None, 0, 4, 2, 100000, allSelected: true, picked: None);
		Assert.IsFalse (result.AllSelected);
		Assert.AreSequenceEqual (new[] { 1 }, result.Selection.Order ().ToArray ());
	}

	[UiTestMethod]
	public void ShiftRightFromSelectAllKeepsEveryOtherIndex ()
	{
		var result = GridNavigation.Apply (Key.Right, KeyModifiers.Shift, 0, 4, 2, 100000, allSelected: true, picked: None);
		Assert.IsTrue (result.AllSelected);
		Assert.DoesNotContain (1, result.Excluded);
		Assert.Contains (0, result.Excluded);
		Assert.HasCount (1, result.Excluded);
	}

	[UiTestMethod]
	public void OtherKeysAndEmptyGridsDoNothing ()
	{
		Assert.AreEqual (GridKeyAction.None, Press (Key.A, 3).Action);
		Assert.AreEqual (GridKeyAction.None, Press (Key.Right, 0, count: 0).Action);
		Assert.AreEqual (GridKeyAction.None, Press (Key.Right, 0, KeyModifiers.Alt).Action);
	}
}

[TestClass]
public sealed class GridGeometryTests
{
	[UiTestMethod]
	public void CellsInRect_FindsEveryTouchedCell ()
	{
		var cells = GridGeometry.CellsInRect (new Rect (50, 50, 120, 100), 100, 100, 4, 10);
		Assert.AreSequenceEqual (new[] { 0, 1, 4, 5 }, cells.ToArray ());
	}

	[UiTestMethod]
	public void CellsInRect_SkipsCellsPastTheEnd ()
	{
		var cells = GridGeometry.CellsInRect (new Rect (0, 150, 400, 10), 100, 100, 4, 6);
		Assert.AreSequenceEqual (new[] { 4, 5 }, cells.ToArray ());
	}

	[UiTestMethod]
	public void Bounded_OrdersTheCorners ()
		=> Assert.AreEqual (new Rect (10, 5, 20, 15), GridGeometry.Bounded (new Point (30, 5), new Point (10, 20)));

	[UiTestMethod]
	public void BandSelection_AddsOrToggles ()
	{
		var initial = new HashSet<int> { 1, 2 };
		Assert.AreSequenceEqual (new[] { 1, 2, 3 }, GridGeometry.BandSelection (initial, [2, 3], toggle: false).Order ().ToArray ());
		Assert.AreSequenceEqual (new[] { 1, 3 }, GridGeometry.BandSelection (initial, [2, 3], toggle: true).Order ().ToArray ());
	}
}

[TestClass]
public sealed class ViewerKeyMapTests
{
	[UiTestMethod]
	public void NavigationKeysMatchGtk ()
	{
		foreach (var key in new[] { Key.PageUp, Key.Back, Key.B })
			Assert.AreEqual (ViewerAction.Previous, ViewerKeyMap.Map (key, KeyModifiers.None, zoomed: true), key.ToString ());
		foreach (var key in new[] { Key.PageDown, Key.Space, Key.N })
			Assert.AreEqual (ViewerAction.Next, ViewerKeyMap.Map (key, KeyModifiers.None, zoomed: true), key.ToString ());
		Assert.AreEqual (ViewerAction.First, ViewerKeyMap.Map (Key.Home, KeyModifiers.None, false));
		Assert.AreEqual (ViewerAction.Last, ViewerKeyMap.Map (Key.End, KeyModifiers.None, false));
	}

	[UiTestMethod]
	public void ArrowsNavigateAtFitAndPanWhenZoomed ()
	{
		Assert.AreEqual (ViewerAction.Next, ViewerKeyMap.Map (Key.Right, KeyModifiers.None, zoomed: false));
		Assert.AreEqual (ViewerAction.Previous, ViewerKeyMap.Map (Key.K, KeyModifiers.None, zoomed: false));
		Assert.AreEqual (ViewerAction.PanRight, ViewerKeyMap.Map (Key.Right, KeyModifiers.None, zoomed: true));
		Assert.AreEqual (ViewerAction.PanUp, ViewerKeyMap.Map (Key.K, KeyModifiers.None, zoomed: true));
	}

	[UiTestMethod]
	public void ZoomKeys ()
	{
		Assert.AreEqual (ViewerAction.ZoomFit, ViewerKeyMap.Map (Key.D0, KeyModifiers.None, false));
		Assert.AreEqual (ViewerAction.ZoomActual, ViewerKeyMap.Map (Key.D1, KeyModifiers.None, false));
		Assert.AreEqual (ViewerAction.ZoomDouble, ViewerKeyMap.Map (Key.NumPad2, KeyModifiers.None, false));
		Assert.AreEqual (ViewerAction.ZoomIn, ViewerKeyMap.Map (Key.OemPlus, KeyModifiers.None, false));
		Assert.AreEqual (ViewerAction.ZoomIn, ViewerKeyMap.Map (Key.OemPlus, KeyModifiers.Shift, false));
		Assert.AreEqual (ViewerAction.ZoomOut, ViewerKeyMap.Map (Key.OemMinus, KeyModifiers.None, false));
	}

	[UiTestMethod]
	public void ControlAndAltPassThrough ()
	{
		Assert.AreEqual (ViewerAction.None, ViewerKeyMap.Map (Key.D1, KeyModifiers.Alt, false));
		Assert.AreEqual (ViewerAction.None, ViewerKeyMap.Map (Key.OemPlus, KeyModifiers.Control, false));
	}
}

[TestClass]
public sealed class PhotoViewZoomTests
{
	[UiTestMethod]
	public void ForScale_ShowsOneOriginalPixelPerDevicePixel ()
	{
		// A 4000 px original decoded at 1000 px and fitted into 500 DIP: fit is 0.5 DIP per preview pixel,
		// and 100 % needs 4 DIP per preview pixel at scale 1.
		double zoom = ViewerZoom.ForScale (1, 500, 500, 1000, 500, 4000, 1);
		Assert.AreEqual (8, zoom, 1e-9);
		Assert.AreEqual (4, ViewerZoom.ForScale (1, 500, 500, 1000, 500, 4000, 2), 1e-9);
	}

	[UiTestMethod]
	public void ForScale_UsesThePreviewWhenTheOriginalIsUnknown ()
		=> Assert.AreEqual (2, ViewerZoom.ForScale (1, 500, 500, 1000, 500, 0, 1), 1e-9);

	[UiTestMethod]
	public void ForScale_ClampsToTheViewerRange ()
		=> Assert.AreEqual (ViewerZoom.Max, ViewerZoom.ForScale (2, 100, 100, 1000, 1000, 10000, 1));

	[UiTestMethod]
	public void AnchorPan_KeepsThePointUnderTheAnchorStill ()
	{
		const double width = 400, height = 300, anchorX = 300, anchorY = 100;
		var pan = ViewerZoom.AnchorPan (1, 2, 0, 0, anchorX, anchorY, width, height);
		// Screen point = centre + pan + (p - centre) * zoom; the anchor's image point must map back to the anchor.
		double imageX = width / 2 + (anchorX - width / 2 - 0) / 1;
		double screenX = width / 2 + pan.X + (imageX - width / 2) * 2;
		double imageY = height / 2 + (anchorY - height / 2 - 0) / 1;
		double screenY = height / 2 + pan.Y + (imageY - height / 2) * 2;
		Assert.AreEqual (anchorX, screenX, 1e-9);
		Assert.AreEqual (anchorY, screenY, 1e-9);
	}
}

[TestClass]
public sealed class LoupeDragTests
{
	[UiTestMethod]
	public void Move_FollowsThePointerFromTheStartHotspot ()
		=> Assert.AreEqual ((110.0, 70.0), LoupeDrag.Move (100, 80, 10, 10, 20, 0));

	[UiTestMethod]
	public void Rotate_TurnsByTheAngleAroundTheHotspot ()
	{
		double angle = LoupeDrag.Rotate (0.5, 0, 0, 10, 0, 0, 10);
		Assert.AreEqual (0.5 + Math.PI / 2, angle, 1e-9);
		Assert.AreEqual (0.5 - Math.PI / 2, LoupeDrag.Rotate (0.5, 0, 0, 10, 0, 0, -10), 1e-9);
	}

	[UiTestMethod]
	public void Rotate_KeepsTheAngleWhenThePointerIsOnTheHotspot ()
		=> Assert.AreEqual (1.0, LoupeDrag.Rotate (1, 5, 5, 5, 5, 9, 9));
}

[TestClass]
public sealed class RatingStepTests
{
	[UiTestMethod]
	public void KeysStepUpAndDown ()
	{
		foreach (var key in new[] { Key.Up, Key.Right, Key.OemPlus, Key.Add })
			Assert.AreEqual (1, RatingStep.KeyDelta (key), key.ToString ());
		foreach (var key in new[] { Key.Down, Key.Left, Key.OemMinus, Key.Subtract })
			Assert.AreEqual (-1, RatingStep.KeyDelta (key), key.ToString ());
		Assert.AreEqual (0, RatingStep.KeyDelta (Key.A));
	}

	[UiTestMethod]
	public void WheelStepsByDirection ()
	{
		Assert.AreEqual (1, RatingStep.WheelDelta (0, 1));
		Assert.AreEqual (-1, RatingStep.WheelDelta (0, -2));
		Assert.AreEqual (1, RatingStep.WheelDelta (1, 0));
		Assert.AreEqual (0, RatingStep.WheelDelta (0, 0));
	}

	[UiTestMethod]
	public void ApplyStaysInRange ()
	{
		Assert.AreEqual (5u, RatingStep.Apply (5, 1));
		Assert.AreEqual (0u, RatingStep.Apply (0, -1));
		Assert.AreEqual (3u, RatingStep.Apply (2, 1));
	}
}

[TestClass]
public sealed class TagCompletionTests
{
	static TagCompletion New (params string[] names)
		=> new (prefix => names.Where (name => name.StartsWith (prefix, StringComparison.OrdinalIgnoreCase)).ToList ());

	[UiTestMethod]
	public void Tab_CompletesTheLastNameAndSelectsTheCompletion ()
	{
		var completion = New ("Vacation", "Valencia");
		var edit = completion.Complete ("Family, va", 10, forward: true);
		Assert.IsNotNull (edit);
		Assert.AreEqual ("Family, vacation", edit.Value.Text);
		Assert.AreEqual (10, edit.Value.SelectionStart);
		Assert.AreEqual (16, edit.Value.SelectionEnd);
	}

	[UiTestMethod]
	public void TabCyclesAndShiftTabGoesBack ()
	{
		var completion = New ("Vacation", "Valencia", "Van");
		Assert.AreEqual ("vacation", completion.Complete ("va", 2, true)!.Value.Text);
		Assert.AreEqual ("valencia", completion.Complete ("vacation", 2, true)!.Value.Text);
		Assert.AreEqual ("vacation", completion.Complete ("valencia", 2, false)!.Value.Text);

		var backwards = New ("Vacation", "Valencia", "Van");
		Assert.AreEqual ("van", backwards.Complete ("va", 2, false)!.Value.Text);
	}

	[UiTestMethod]
	public void NothingTypedOrNoMatchCompletesNothing ()
	{
		var completion = New ("Vacation");
		Assert.IsNull (completion.Complete ("Family, ", 8, true));
		Assert.IsNull (completion.Complete ("zz", 2, true));
		Assert.IsFalse (completion.IsActive);
	}

	[UiTestMethod]
	public void CaretBeforeTheEndCompletesNothing ()
		=> Assert.IsNull (New ("Vacation").Complete ("va, x", 1, true));

	[UiTestMethod]
	public void Finish_AddsACommaAfterTheCompletion ()
	{
		var completion = New ("Vacation");
		var edit = completion.Complete ("va", 2, true)!.Value;
		var done = completion.Finish (edit.Text, edit.SelectionEnd);
		Assert.AreEqual ("vacation, ", done!.Value.Text);
		Assert.AreEqual (10, done.Value.SelectionStart);
		Assert.IsFalse (completion.IsActive);
		Assert.IsNull (completion.Finish ("x", 1));
	}
}

[TestClass]
public sealed class TimelineLimitsTests
{
	[UiTestMethod]
	public void HitTest_FindsTheHandleOnItsEdge ()
	{
		Assert.AreEqual (TimelineLimit.Min, TimelineLimits.HitTest (21, 1, 3, 20));
		Assert.AreEqual (TimelineLimit.Max, TimelineLimits.HitTest (78, 1, 3, 20));
		Assert.AreEqual (TimelineLimit.None, TimelineLimits.HitTest (50, 1, 3, 20));
	}

	[UiTestMethod]
	public void HitTest_SplitsASingleMonthBetweenTheHandles ()
	{
		// One 8 px month: min sits at x = 16 and max at x = 24, both within reach of 19 to 21.
		Assert.AreEqual (TimelineLimit.Min, TimelineLimits.HitTest (19, 2, 2, 8));
		Assert.AreEqual (TimelineLimit.Max, TimelineLimits.HitTest (23, 2, 2, 8));
	}

	[UiTestMethod]
	public void Drop_LandsOnTheMonthUnderTheHandleAndKeepsOrder ()
	{
		Assert.AreEqual ((2, 5), TimelineLimits.Drop (TimelineLimit.Min, 45, 0, 5, 20, 8));
		Assert.AreEqual ((0, 3), TimelineLimits.Drop (TimelineLimit.Max, 70, 0, 5, 20, 8));
		Assert.AreEqual ((5, 5), TimelineLimits.Drop (TimelineLimit.Min, 150, 0, 5, 20, 8));
		Assert.AreEqual ((0, 7), TimelineLimits.Drop (TimelineLimit.Max, 900, 0, 5, 20, 8));
	}

	[UiTestMethod]
	public void IsFull_CoversTheWholeStrip ()
	{
		Assert.IsTrue (TimelineLimits.IsFull (0, 7, 8));
		Assert.IsFalse (TimelineLimits.IsFull (1, 7, 8));
	}
}

[TestClass]
public sealed class TagNamesTests
{
	[UiTestMethod]
	public void ACaseChangeIsNotTaken ()
		=> Assert.IsFalse (TagNames.IsTakenByAnother ("Beach", "beach", _ => true));

	[UiTestMethod]
	public void AnotherTagsNameIsTaken ()
	{
		Assert.IsTrue (TagNames.IsTakenByAnother ("Beach", "Sea", name => name == "Sea"));
		Assert.IsFalse (TagNames.IsTakenByAnother ("Beach", "Sand", name => name == "Sea"));
		Assert.IsFalse (TagNames.IsTakenByAnother ("Beach", "Beach", _ => true));
	}
}
