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
	[TestMethod]
	public void UnadornedV_TogglesLoupeWhenFocusIsNotATextInput ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsTrue (ShortcutGuards.AllowsUnadornedKey (new Button ()));
		Assert.IsTrue (ShortcutGuards.AllowsUnadornedKey (null));
	}

	[TestMethod]
	public void UnadornedV_DoesNotStealFromATextBox ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsFalse (ShortcutGuards.AllowsUnadornedKey (new TextBox ()));
	}

	[TestMethod]
	public void UnadornedV_RequiresTheVKeyWithNoModifiers ()
	{
		Assert.IsTrue (ShortcutGuards.IsUnadornedLoupeKey (Key.V, KeyModifiers.None));
		Assert.IsFalse (ShortcutGuards.IsUnadornedLoupeKey (Key.V, KeyModifiers.Control));
		Assert.IsFalse (ShortcutGuards.IsUnadornedLoupeKey (Key.L, KeyModifiers.None));
	}

	[TestMethod]
	public void ContinueToWindowKeyBindings_IsFalseForVWhenATextBoxHasFocus ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsFalse (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.V, KeyModifiers.None));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new Button (), Key.V, KeyModifiers.None));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.V, KeyModifiers.Control));
		Assert.IsTrue (ShortcutGuards.ContinueToWindowKeyBindings (new TextBox (), Key.A, KeyModifiers.None));
	}
}
