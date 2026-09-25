// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;

using FSpot.Avalonia.Controls;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class UniformVirtualizingWrapPanelTests
{
	[UiTestMethod]
	public void FiveThousandPhotos_RealizeAWindowNotTheCatalog ()
	{
		AvaloniaApp.Ensure ();
		var list = new ListBox {
			Width = 320,
			Height = 240,
			ItemsPanel = new FuncTemplate<Panel?> (() => new UniformVirtualizingWrapPanel {
				ItemWidth = 100,
				ItemHeight = 100
			}),
			ItemsSource = Enumerable.Range (0, 5_000).Select (index => index.ToString ()).ToList ()
		};
		var window = new Window { Width = 360, Height = 280, Content = list };
		window.Show ();
		try {
			Dispatcher.UIThread.RunJobs ();
			var panel = list.ItemsPanelRoot as UniformVirtualizingWrapPanel;
			Assert.IsNotNull (panel);
			Assert.IsGreaterThan (0, panel.RealizedCount);
			Assert.IsLessThan (80, panel.RealizedCount);
			Assert.IsGreaterThan (240, ((ILogicalScrollable)panel).Extent.Height);

			((ILogicalScrollable)panel).Offset = new Vector (0, 5_000);
			Dispatcher.UIThread.RunJobs ();
			panel.InvalidateMeasure ();
			panel.Measure (new Size (320, 240));
			panel.Arrange (new Rect (0, 0, 320, 240));
			Assert.IsGreaterThan (0, panel.RealizedCount);
			Assert.IsLessThan (80, panel.RealizedCount);
		} finally {
			window.Close ();
		}
	}
}
