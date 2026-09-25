// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using FSpot.Avalonia.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LoupeKeyBindingTests
{
	[UiTestMethod]
	public void UnadornedV_TogglesWhenFocusIsNotATextInput ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var window = new LoupeGuardWindow (command) {
			Width = 200,
			Height = 80,
			Content = new Button { Content = "ok" }
		};
		window.Show ();

		RaiseV (window);

		Assert.AreEqual (1, command.Count);
	}

	[UiTestMethod]
	public void UnadornedV_DoesNotToggleWhenATextBoxIsFocused ()
	{
		AvaloniaApp.Ensure ();
		var command = new RecordingCommand ();
		var box = new TextBox ();
		var window = new LoupeGuardWindow (command) {
			Width = 200,
			Height = 80,
			Content = box
		};
		window.Show ();
		box.Focus ();
		Assert.IsTrue (box.IsFocused);

		RaiseV (window, box);

		Assert.AreEqual (0, command.Count);
	}

	static void RaiseV (Window window, object? source = null)
	{
		var args = new KeyEventArgs {
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.V,
			KeyModifiers = KeyModifiers.None,
			Source = source ?? window
		};
		window.RaiseEvent (args);
	}

	sealed class LoupeGuardWindow : Window
	{
		readonly ICommand toggle;

		public LoupeGuardWindow (ICommand toggle)
			=> this.toggle = toggle;

		protected override void OnKeyDown (KeyEventArgs e)
		{
			var focused = FocusManager?.GetFocusedElement () ?? e.Source;
			if (ShortcutGuards.IsUnadornedLoupeKey (e.Key, e.KeyModifiers)) {
				if (ShortcutGuards.AllowsUnadornedKey (focused)) {
					toggle.Execute (null);
					e.Handled = true;
				}
				return;
			}
			base.OnKeyDown (e);
		}
	}

	sealed class RecordingCommand : ICommand
	{
		public int Count { get; private set; }

		public bool CanExecute (object? parameter) => true;

		public void Execute (object? parameter) => Count++;

		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}
}
