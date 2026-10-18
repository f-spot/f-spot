// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ShownChromeTests
{
	[UiTestMethod]
	[DoNotParallelize]
	public void ShownWindows_DoNotDisplayAttributeText ()
	{
		var windows = new Window[] {
			new MainWindow (null, new NullThumbnails ()) { Width = 800, Height = 500 },
			new AboutWindow (),
			new ImportWindow ()
		};
		try {
			foreach (Window window in windows) {
				window.Show ();
				var leaks = window.GetVisualDescendants ()
					.Select (VisibleText)
					.Where (text => text?.Contains ("=\"", StringComparison.Ordinal) == true)
					.ToList ();
				Assert.IsEmpty (leaks, window.GetType ().Name + " shows attribute text: " + string.Join ("; ", leaks));
			}
		} finally {
			foreach (Window window in windows)
				window.Close ();
		}
	}

	static string? VisibleText (Visual visual)
		=> visual switch {
			TextBlock block => block.Text,
			ContentControl control => control.Content as string,
			_ => null
		};
}
