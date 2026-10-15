// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;

using FSpot.Avalonia.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ShortcutGuardsTests
{
	[UiTestMethod]
	public void UnadornedV_TogglesLoupeWhenFocusIsNotATextInput ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsTrue (ShortcutGuards.AllowsUnadornedKey (new Button ()));
		Assert.IsTrue (ShortcutGuards.AllowsUnadornedKey (null));
	}

	[UiTestMethod]
	public void UnadornedV_DoesNotStealFromATextBox ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (new TextBox ()));
	}

	[UiTestMethod]
	public void TagTree_StopsBeforeItsOwnTreeView_AndATextBoxStillOwnsItsKeys ()
	{
		AvaloniaApp.Ensure ();
		var box = new TextBox ();
		var item = new TreeViewItem { Header = box };
		var tree = new TreeView ();
		tree.Items.Add (item);
		var window = new Window { Width = 200, Height = 120, Content = tree };
		window.Show ();
		window.UpdateLayout ();

		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (item, Key.Delete));
		Assert.IsTrue (ShortcutGuards.AllowsUnadornedKey (item, Key.Delete, tree));
		Assert.IsTrue (box.Focus ());
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (box, Key.Space, tree));
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (box, Key.Enter, tree));
		window.Close ();
	}

	[UiTestMethod]
	public void PlainKeys_LeaveComboSliderAndModifiedTAlone ()
	{
		AvaloniaApp.Ensure ();
		var combo = new ComboBox ();
		var slider = new Slider ();
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (combo, Key.D4));
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (slider, Key.S));
		Assert.IsFalse (ShortcutGuards.IsTagTypingKey (Key.T, KeyModifiers.Control, null));
		Assert.IsTrue (ShortcutGuards.IsTagTypingKey (Key.T, KeyModifiers.None, new Button ()));
		Assert.IsNull (PlainKeyCommand.From (Key.S, KeyModifiers.None, combo));
		Assert.IsNull (RatingKey.From (Key.D4, KeyModifiers.None, slider));
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (new Button (), Key.Space));
		Assert.IsTrue (ShortcutGuards.AllowsUnadornedKey (new Button (), Key.Delete));
	}

	[UiTestMethod]
	public void UnadornedV_RequiresTheVKeyWithNoModifiers ()
	{
		Assert.IsTrue (ShortcutGuards.IsUnadornedLoupeKey (Key.V, KeyModifiers.None));
		Assert.IsFalse (ShortcutGuards.IsUnadornedLoupeKey (Key.V, KeyModifiers.Control));
		Assert.IsFalse (ShortcutGuards.IsUnadornedLoupeKey (Key.L, KeyModifiers.None));
	}

	[UiTestMethod]
	public void ContinueToWindowKeyBindings_IsFalseForVWhenATextBoxHasFocus ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsFalse (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.V, KeyModifiers.None));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new Button (), Key.V, KeyModifiers.None));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.V, KeyModifiers.Control, KeyModifiers.Meta));
		Assert.IsFalse (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.V, KeyModifiers.Meta, KeyModifiers.Meta));
		Assert.IsFalse (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.C, KeyModifiers.Control, KeyModifiers.Control));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new Button (), Key.C, KeyModifiers.Meta, KeyModifiers.Meta));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.A, KeyModifiers.Meta | KeyModifiers.Shift, KeyModifiers.Meta));
		Assert.IsFalse (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.F5, KeyModifiers.None, KeyModifiers.Meta));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new Button (), Key.F5, KeyModifiers.None, KeyModifiers.Meta));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.Escape, KeyModifiers.None, KeyModifiers.Meta));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.A, KeyModifiers.None));
	}
}
