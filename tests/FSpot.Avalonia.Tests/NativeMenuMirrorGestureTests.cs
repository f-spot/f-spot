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
	[UiTestMethod]
	public void Retarget_UsesThePlatformCommandModifier ()
	{
		AvaloniaApp.Ensure ();
		var gesture = PlatformGestures.Retarget (new KeyGesture (Key.C, KeyModifiers.Control | KeyModifiers.Shift));
		Assert.AreEqual (Key.C, gesture.Key);
		Assert.AreEqual (PlatformGestures.CommandModifiers | KeyModifiers.Shift, gesture.KeyModifiers);
	}

	[UiTestMethod]
	public void Retarget_HonorsAFakedCommandModifier ()
	{
		var shifted = PlatformGestures.Retarget (new KeyGesture (Key.C, KeyModifiers.Control | KeyModifiers.Shift), KeyModifiers.Alt);
		Assert.AreEqual (Key.C, shifted.Key);
		Assert.AreEqual (KeyModifiers.Alt | KeyModifiers.Shift, shifted.KeyModifiers);

		var command = PlatformGestures.Retarget (new KeyGesture (Key.C, KeyModifiers.Control | KeyModifiers.Shift), KeyModifiers.Meta);
		Assert.AreEqual (KeyModifiers.Meta | KeyModifiers.Shift, command.KeyModifiers);

		var alreadyControl = PlatformGestures.Retarget (new KeyGesture (Key.C, KeyModifiers.Control | KeyModifiers.Shift), KeyModifiers.Control);
		Assert.AreEqual (KeyModifiers.Control | KeyModifiers.Shift, alreadyControl.KeyModifiers);

		var plain = PlatformGestures.Retarget (new KeyGesture (Key.F9), KeyModifiers.Meta);
		Assert.AreEqual (Key.F9, plain.Key);
		Assert.AreEqual (KeyModifiers.None, plain.KeyModifiers);
	}

	[UiTestMethod]
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
		Assert.Contains (b => Equals (b.Gesture, new KeyGesture (Key.N, KeyModifiers.Control)) && ReferenceEquals (b.Command, importCommand), window.KeyBindings);
		Assert.Contains (b => Equals (b.Gesture, new KeyGesture (Key.Q, KeyModifiers.Control)) && ReferenceEquals (b.Command, quitCommand), window.KeyBindings);
	}

	[UiTestMethod]
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

	[UiTestMethod]
	public void SkipsItemsWithNoCommand ()
	{
		AvaloniaApp.Ensure ();
		var window = new Window ();
		var menu = new NativeMenu ();
		menu.Items.Add (new NativeMenuItem ("Version") { Gesture = new KeyGesture (Key.V, KeyModifiers.Control), IsEnabled = false });

		NativeMenuMirror.ApplyAsKeyBindings (window, menu);

		Assert.IsEmpty (window.KeyBindings);
	}

	[UiTestMethod]
	public void DoesNothingForANullMenu ()
	{
		AvaloniaApp.Ensure ();
		var window = new Window ();

		NativeMenuMirror.ApplyAsKeyBindings (window, null);
		PlatformGestures.ReleaseNativeEquivalents (null, KeyModifiers.Meta);

		Assert.IsEmpty (window.KeyBindings);
	}

	[UiTestMethod]
	public void ReleaseNativeEquivalents_DropsUnmodifiedAndCommandTextChords ()
	{
		AvaloniaApp.Ensure ();
		var sidebar = new CountingCommand ();
		var window = new Window ();
		window.KeyBindings.Add (new KeyBinding { Gesture = new KeyGesture (Key.F9), Command = sidebar });

		var view = new NativeMenuItem ("View") { Menu = new NativeMenu () };
		var fullscreen = new NativeMenuItem ("Fullscreen") { Gesture = new KeyGesture (Key.F11), Command = new CountingCommand () };
		view.Menu.Items.Add (fullscreen);
		var copy = new NativeMenuItem ("Copy") { Gesture = new KeyGesture (Key.C, KeyModifiers.Meta), Command = new CountingCommand () };
		var selectNone = new NativeMenuItem ("Select None") { Gesture = new KeyGesture (Key.A, KeyModifiers.Meta | KeyModifiers.Shift), Command = new CountingCommand () };
		var controlCopy = new NativeMenuItem ("Control Copy") { Gesture = new KeyGesture (Key.C, KeyModifiers.Control), Command = new CountingCommand () };
		var menu = new NativeMenu ();
		menu.Items.Add (new NativeMenuItem ("Sidebar") { Gesture = new KeyGesture (Key.F9), Command = new CountingCommand () });
		menu.Items.Add (view);
		menu.Items.Add (copy);
		menu.Items.Add (selectNone);
		menu.Items.Add (controlCopy);

		NativeMenuMirror.ApplyAsKeyBindings (window, menu);
		PlatformGestures.ReleaseNativeEquivalents (menu, KeyModifiers.Meta);

		Assert.IsNull (menu.Items.OfType<NativeMenuItem> ().Single (item => item.Header as string == "Sidebar").Gesture);
		Assert.IsNull (fullscreen.Gesture);
		Assert.IsNull (copy.Gesture);
		Assert.AreEqual (Key.A, selectNone.Gesture!.Key);
		Assert.AreEqual (KeyModifiers.Meta | KeyModifiers.Shift, selectNone.Gesture.KeyModifiers);
		Assert.AreEqual (Key.C, controlCopy.Gesture!.Key);
		Assert.AreEqual (KeyModifiers.Control, controlCopy.Gesture.KeyModifiers);
		Assert.AreSame (sidebar, window.KeyBindings[0].Command);
		Assert.AreEqual (1, CountGestures (window, Key.F11, KeyModifiers.None));
		Assert.AreEqual (1, CountGestures (window, Key.C, KeyModifiers.Meta));
	}

	static int CountGestures (Window window, Key key, KeyModifiers modifiers)
	{
		int count = 0;
		foreach (var binding in window.KeyBindings) {
			if (binding.Gesture is KeyGesture gesture && gesture.Key == key && gesture.KeyModifiers == modifiers)
				count++;
		}
		return count;
	}

	[UiTestMethod]
	public void GuardYieldingBindings_LetsATextBoxKeepTheKey ()
	{
		AvaloniaApp.Ensure ();
		var slideshow = new CountingCommand ();
		var copy = new CountingCommand ();
		var quit = new CountingCommand ();
		var box = new TextBox ();
		var button = new Button { Content = "ok" };
		var window = new Window {
			Width = 200,
			Height = 80,
			Content = new StackPanel { Children = { box, button } }
		};
		window.KeyBindings.Add (new KeyBinding { Gesture = new KeyGesture (Key.F5), Command = slideshow });
		window.KeyBindings.Add (new KeyBinding { Gesture = new KeyGesture (Key.C, KeyModifiers.Meta), Command = copy });
		window.KeyBindings.Add (new KeyBinding { Gesture = new KeyGesture (Key.Q, KeyModifiers.Meta), Command = quit });
		window.KeyBindings.Add (new KeyBinding { Gesture = new KeyGesture (Key.Escape), Command = new CountingCommand () });
		window.Show ();
		window.UpdateLayout ();
		PlatformGestures.GuardYieldingBindings (window, KeyModifiers.Meta);

		Assert.IsTrue (box.Focus ());
		Press (window, Key.F5, KeyModifiers.None);
		Press (window, Key.C, KeyModifiers.Meta);
		Press (window, Key.Q, KeyModifiers.Meta);
		Assert.AreEqual (0, slideshow.Count);
		Assert.AreEqual (0, copy.Count);
		Assert.AreEqual (1, quit.Count);

		Assert.IsTrue (button.Focus ());
		Press (window, Key.F5, KeyModifiers.None);
		Press (window, Key.C, KeyModifiers.Meta);
		Assert.AreEqual (1, slideshow.Count);
		Assert.AreEqual (1, copy.Count);
		window.Close ();
	}

	static void Press (Window window, Key key, KeyModifiers modifiers)
	{
		var args = new KeyEventArgs {
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key,
			KeyModifiers = modifiers
		};
		foreach (var binding in window.KeyBindings)
			binding.TryHandle (args);
	}

	sealed class RecordingCommand : ICommand
	{
		public bool CanExecute (object? parameter) => true;
		public void Execute (object? parameter) { }
		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}

	sealed class CountingCommand : ICommand
	{
		public int Count { get; private set; }
		public bool CanExecute (object? parameter) => true;
		public void Execute (object? parameter) => Count++;
		public event EventHandler? CanExecuteChanged { add { } remove { } }
	}
}
