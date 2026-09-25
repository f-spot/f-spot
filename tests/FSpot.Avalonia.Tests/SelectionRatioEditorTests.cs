// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.ViewModels;
using FSpot.Editors;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class SelectionRatioEditorTests
{
	static SelectionRatioEditor TwoRows ()
		=> new (new[] { new CropConstraint ("A", 2), new CropConstraint ("B", 3) });

	[UiTestMethod]
	public void Add_AppendsANewSelectionAtOneToOne ()
	{
		var editor = TwoRows ();

		editor.AddCommand.Execute (null);

		Assert.HasCount (3, editor.Rows);
		Assert.AreEqual (Strings.NewSelection, editor.Rows[2].Label);
		Assert.AreEqual (1.0, editor.Rows[2].Ratio);
		Assert.AreSame (editor.Rows[2], editor.Selected);
	}

	[UiTestMethod]
	public void DeleteAndMove_NeedASelection ()
	{
		var editor = TwoRows ();

		Assert.IsFalse (editor.DeleteCommand.CanExecute (null));
		Assert.IsFalse (editor.MoveUpCommand.CanExecute (null));
		editor.Selected = editor.Rows[0];
		Assert.IsTrue (editor.DeleteCommand.CanExecute (null));
	}

	[UiTestMethod]
	public void Delete_RemovesTheSelectedRow ()
	{
		var editor = TwoRows ();
		editor.Selected = editor.Rows[0];

		editor.DeleteCommand.Execute (null);

		Assert.HasCount (1, editor.Rows);
		Assert.AreEqual ("B", editor.Rows[0].Label);
	}

	[UiTestMethod]
	public void MoveUpAndDown_SwapNeighboursAndKeepSelection ()
	{
		var editor = TwoRows ();
		editor.Selected = editor.Rows[1];

		editor.MoveUpCommand.Execute (null);
		Assert.AreEqual ("B", editor.Rows[0].Label);
		Assert.AreEqual ("B", editor.Selected?.Label);

		editor.MoveUpCommand.Execute (null);
		Assert.AreEqual ("B", editor.Rows[0].Label);

		editor.MoveDownCommand.Execute (null);
		Assert.AreEqual ("A", editor.Rows[0].Label);
		Assert.AreEqual ("B", editor.Rows[1].Label);
	}

	[UiTestMethod]
	public void RatioText_ParsesFractionsAndRejectsGarbage ()
	{
		var editor = TwoRows ();
		var row = editor.Rows[0];

		row.RatioText = "3:4";
		Assert.AreEqual (4.0 / 3.0, row.Ratio, 1e-9);

		row.RatioText = "nonsense";
		Assert.AreEqual (4.0 / 3.0, row.Ratio, 1e-9);
		row.CommitRatioText ();
		Assert.AreEqual ((4.0 / 3.0).ToString (System.Globalization.CultureInfo.CurrentCulture), row.RatioText);
	}

	[UiTestMethod]
	public void ToConstraints_KeepsRowOrderLabelsAndRatios ()
	{
		var editor = TwoRows ();
		editor.Rows[1].Label = "Wide";

		var result = editor.ToConstraints ();

		Assert.HasCount (2, result);
		Assert.AreEqual ("A", result[0].Label);
		Assert.AreEqual (2, result[0].XyRatio);
		Assert.AreEqual ("Wide", result[1].Label);
		Assert.AreEqual (3, result[1].XyRatio);
	}
}
