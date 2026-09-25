// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Input;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

// The native macOS menu bar turns a NativeMenuItem's Gesture into a working
// key equivalent on its own; nothing does that off macOS. These tests cover
// NativeMenuMirror.ApplyAsKeyBindings, which copies every NativeMenuItem
// gesture onto the window's own KeyBindings so it works everywhere.
[TestClass]
[DoNotParallelize]
public sealed class NativeMenuMirrorGestureTests
{
	[TestMethod]
	public void CopiesGesturesIncludingNestedSubmenus ()
	{
		AvaloniaApp.Ensure ();
		var window = new Window ();
		var importCommand = new RecordingCommand ();
		var quitCommand = new RecordingCommand ();
		var menu = new NativeMenu ();
		var photo = new NativeMenuItem ("Photo") { Menu = new NativeMenu () };
		photo.Menu.Items.Add (new NativeMenuItem ("Import") { Gesture = new KeyGesture (Key.N, KeyModifiers.Control), Command = importCommand });
		photo.Menu.Items.Add (new NativeMenuItem ("Quit") { Gesture = new KeyGesture (Key.Q, KeyModifiers.Control), Command = quitCommand });
		menu.Items.Add (photo);

		NativeMenuMirror.ApplyAsKeyBindings (window, menu);

		Assert.HasCount (2, window.KeyBindings);
		Assert.IsTrue (window.KeyBindings.Any (b => Equals (b.Gesture, new KeyGesture (Key.N, KeyModifiers.Control)) && ReferenceEquals (b.Command, importCommand)));
		Assert.IsTrue (window.KeyBindings.Any (b => Equals (b.Gesture, new KeyGesture (Key.Q, KeyModifiers.Control)) && ReferenceEquals (b.Command, quitCommand)));
	}

	[TestMethod]
	public void DoesNotDuplicateAGestureAlreadyOnTheWindow ()
	{
		AvaloniaApp.Ensure ();
		var existing = new RecordingCommand ();
		var window = new Window ();
		window.KeyBindings.Add (new KeyBinding { Gesture = new KeyGesture (Key.F9), Command = existing });
		var menu = new NativeMenu ();
		menu.Items.Add (new NativeMenuItem ("Sidebar") { Gesture = new KeyGesture (Key.F9), Command = new RecordingCommand () });

		NativeMenuMirror.ApplyAsKeyBindings (window, menu);

		Assert.HasCount (1, window.KeyBindings);
		Assert.AreSame (existing, window.KeyBindings[0].Command);
	}

	[TestMethod]
	public void SkipsItemsWithNoCommand ()
	{
		AvaloniaApp.Ensure ();
		var window = new Window ();
		var menu = new NativeMenu ();
		menu.Items.Add (new NativeMenuItem ("Version") { Gesture = new KeyGesture (Key.V, KeyModifiers.Control), IsEnabled = false });

		NativeMenuMirror.ApplyAsKeyBindings (window, menu);

		Assert.IsEmpty (window.KeyBindings);
	}

	[TestMethod]
	public void DoesNothingForANullMenu ()
	{
		AvaloniaApp.Ensure ();
		var window = new Window ();

		NativeMenuMirror.ApplyAsKeyBindings (window, null);

		Assert.IsEmpty (window.KeyBindings);
	}

	sealed class RecordingCommand : ICommand
	{
		public bool CanExecute (object? parameter) => true;
		public void Execute (object? parameter) { }
		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}
}
