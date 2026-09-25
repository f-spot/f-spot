// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Input;

using FSpot.Avalonia.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class PlainKeyCommandTests
{
	[UiTestMethod]
	public void RecognizesGtksRotateSharpenAndFindKeys ()
	{
		AvaloniaApp.Ensure ();
		Assert.AreEqual (PlainKeyCommandKind.RotateLeft, PlainKeyCommand.From (Key.OemOpenBrackets, KeyModifiers.None, null));
		Assert.AreEqual (PlainKeyCommandKind.RotateRight, PlainKeyCommand.From (Key.OemCloseBrackets, KeyModifiers.None, null));
		Assert.AreEqual (PlainKeyCommandKind.Sharpen, PlainKeyCommand.From (Key.S, KeyModifiers.None, null));
		Assert.AreEqual (PlainKeyCommandKind.ShowFind, PlainKeyCommand.From (Key.OemQuestion, KeyModifiers.None, null));
	}

	[UiTestMethod]
	public void IgnoresKeysWithModifiersAndUnmappedKeys ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsNull (PlainKeyCommand.From (Key.S, KeyModifiers.Control, null));
		Assert.IsNull (PlainKeyCommand.From (Key.OemOpenBrackets, KeyModifiers.Shift, null));
		Assert.IsNull (PlainKeyCommand.From (Key.A, KeyModifiers.None, null));
	}

	[UiTestMethod]
	public void DoesNotStealFromATextBox ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsNull (PlainKeyCommand.From (Key.S, KeyModifiers.None, new TextBox ()));
		Assert.IsNull (PlainKeyCommand.From (Key.OemQuestion, KeyModifiers.None, new TextBox ()));
		Assert.IsNull (PlainKeyCommand.From (Key.OemOpenBrackets, KeyModifiers.None, new TextBox ()));
	}
}
