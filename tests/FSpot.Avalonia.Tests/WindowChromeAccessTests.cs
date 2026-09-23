// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

using FSpot.Avalonia.Input;

using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class WindowChromeAccessTests
{
	[TestMethod]
	public void GtkMnemonic_ReadsTheUnderscoredCharacter ()
	{
		Assert.AreEqual (Key.I, GtkMnemonic.Key ("_Import..."));
		Assert.AreEqual ("Import...", GtkMnemonic.Display ("_Import..."));
		Assert.AreEqual (Key.N, GtkMnemonic.Key ("I_nclude subfolders"));
		Assert.AreEqual ("Include subfolders", GtkMnemonic.Display ("I_nclude subfolders"));
		Assert.AreEqual (Key.F, GtkMnemonic.Key ("Import _from:"));
		Assert.IsNull (GtkMnemonic.Key ("Import"));
		Assert.AreEqual ("A_B", GtkMnemonic.Display ("A__B"));
		Assert.IsNull (GtkMnemonic.Key ("A__B"));
	}

	[TestMethod]
	public void ImportChrome_ExposesTheImportAccessKey ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			var import = window.GetVisualDescendants ().OfType<Button> ()
				.First (button => AutomationProperties.GetAutomationId (button) == AutomationIds.ToolbarImport);
			var access = import.GetVisualDescendants ().OfType<AccessText> ().Single ();
			string? accessKey = access.AccessKey;
			Assert.IsNotNull (accessKey);
			Assert.AreEqual ("I", accessKey);
			Assert.AreEqual (Key.I, import.HotKey?.Key);
			Assert.AreEqual (KeyModifiers.Alt, import.HotKey?.KeyModifiers);

			var menu = window.GetVisualDescendants ().OfType<Menu> ()
				.SelectMany (AllMenuItems)
				.Select (item => GtkMnemonic.Display (item.Header as string ?? string.Empty))
				.ToList ();
			Assert.Contains ("Import...", menu);
		} finally {
			window.Close ();
		}
	}

	static IEnumerable<MenuItem> AllMenuItems (Menu menu)
	{
		foreach (var item in menu.Items.OfType<MenuItem> ()) {
			yield return item;
			foreach (var child in Nested (item))
				yield return child;
		}
	}

	static IEnumerable<MenuItem> Nested (MenuItem item)
	{
		foreach (var child in item.Items.OfType<MenuItem> ()) {
			yield return child;
			foreach (var grandchild in Nested (child))
				yield return grandchild;
		}
	}

	[TestMethod]
	public void AboutWindow_ClosesOnEscapeWithoutACloseButtonOrCatalogSentence ()
	{
		AvaloniaApp.Ensure ();
		var window = new AboutWindow ();
		window.Show ();
		try {
			Assert.IsEmpty (window.GetVisualDescendants ().OfType<Button> ());
			string text = string.Join ("\n", window.GetVisualDescendants ().OfType<TextBlock> ().Select (block => block.Text));
			Assert.Contains (Strings.FSpot, text);
			Assert.DoesNotContain ("catalog", text);
			Assert.DoesNotContain ("GTK", text);
			var logo = window.GetVisualDescendants ().OfType<Image> ()
				.Single (image => AutomationProperties.GetAutomationId (image) == AutomationIds.AboutLogo);
			Assert.IsNotNull (logo.Source);

			bool closed = false;
			window.Closed += (_, _) => closed = true;
			window.RaiseEvent (new KeyEventArgs {
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Escape,
				KeyModifiers = KeyModifiers.None,
				Source = window
			});
			Assert.IsTrue (closed);
		} finally {
			if (window.IsVisible)
				window.Close ();
		}
	}

	[TestMethod]
	public void PhotoGrid_KeepsATagDropTheEmptyLibraryPanelWouldReject ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			var grid = window.GetVisualDescendants ().OfType<ListBox> ()
				.Single (list => AutomationProperties.GetAutomationId (list) == AutomationIds.PhotoGrid);
			var empty = window.GetVisualDescendants ().OfType<StackPanel> ()
				.Single (panel => AutomationProperties.GetAutomationId (panel) == AutomationIds.PhotoGridEmpty);
			var library = (Panel)empty.Parent!;

			using var tag = new DataTransfer ();
			tag.Add (DataTransferItem.Create (DragFormats.TagId, "1"));
			var overGrid = new DragEventArgs (DragDrop.DragOverEvent, tag, grid, new Point (8, 8), KeyModifiers.None) {
				RoutedEvent = DragDrop.DragOverEvent
			};
			grid.RaiseEvent (overGrid);
			Assert.AreEqual (DragDropEffects.Copy, overGrid.DragEffects);
			Assert.IsTrue (overGrid.Handled);

			using var rejected = new DataTransfer ();
			rejected.Add (DataTransferItem.Create (DragFormats.TagId, "1"));
			var overLibrary = new DragEventArgs (DragDrop.DragOverEvent, rejected, library, new Point (8, 8), KeyModifiers.None) {
				RoutedEvent = DragDrop.DragOverEvent
			};
			library.RaiseEvent (overLibrary);
			Assert.AreEqual (DragDropEffects.None, overLibrary.DragEffects);
		} finally {
			window.Close ();
		}
	}

	[TestMethod]
	public void StatusBar_IsShorterThanTheToolbar ()
	{
		AvaloniaApp.Ensure ();
		var window = new MainWindow (null) { Width = 900, Height = 600 };
		window.Show ();
		try {
			window.UpdateLayout ();
			var status = window.GetVisualDescendants ().OfType<Grid> ()
				.Single (grid => AutomationProperties.GetAutomationId (grid) == AutomationIds.StatusBar);
			var toolbar = window.GetVisualDescendants ().OfType<Button> ()
				.Single (button => AutomationProperties.GetAutomationId (button) == AutomationIds.ToolbarImport);
			var slider = window.GetVisualDescendants ().OfType<Slider> ()
				.Single (item => AutomationProperties.GetAutomationId (item) == AutomationIds.ZoomSlider);
			Assert.IsGreaterThan (status.Bounds.Height, toolbar.Bounds.Height);
			Assert.IsLessThan (32, slider.Bounds.Height);
			Assert.IsLessThan (32, status.Bounds.Height);
		} finally {
			window.Close ();
		}
	}

	[TestMethod]
	public void RatingKey_DoesNotRateWhileATextBoxIsFocused ()
	{
		AvaloniaApp.Ensure ();
		Assert.IsNull (RatingKey.From (Key.D2, KeyModifiers.None, new TextBox ()));
	}

	[TestMethod]
	public void MnemonicCheckBox_RegistersItsAccessKey ()
	{
		AvaloniaApp.Ensure ();
		var window = new Window { Width = 400, Height = 200, Content = new CheckBox { Content = Strings.DetectDuplicates2 } };
		window.Show ();
		try {
			var access = window.GetVisualDescendants ().OfType<AccessText> ().Single ();
			string? accessKey = access.AccessKey;
			Assert.IsNotNull (accessKey);
			Assert.AreEqual ("D", accessKey);
		} finally {
			window.Close ();
		}
	}

	[TestMethod]
	public async Task FolderPicker_IsInTheAutomationTreeAndReturnsTheTypedPath ()
	{
		AvaloniaApp.Ensure ();
		var window = new FolderPickerWindow (Strings.ChooseFolder, _ => Task.FromResult<string?> ("/from/browse"));
		window.Show ();
		try {
			Assert.AreEqual (AutomationIds.FolderPicker, AutomationProperties.GetAutomationId (window));
			var path = window.GetVisualDescendants ().OfType<TextBox> ().Single ();
			var browse = window.GetVisualDescendants ().OfType<Button> ()
				.Single (button => AutomationProperties.GetAutomationId (button) == AutomationIds.FolderPickerBrowse);
			Assert.AreEqual (AutomationIds.FolderPickerPath, AutomationProperties.GetAutomationId (path));

			await window.BrowseAsync ();
			Assert.AreEqual ("/from/browse", path.Text);

			Assert.IsNull (FolderPick.Chosen ("  "));
			path.Text = "  /tmp/photos  ";
			Assert.AreEqual ("/tmp/photos", window.Accept ());
		} finally {
			window.Close ();
		}
	}
}
