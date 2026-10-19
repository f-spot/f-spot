// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace FSpot.Avalonia;

// A window has no parent, so an application style never reaches it. The direction
// is applied when the window opens, from the UI culture at that moment.
static class CultureFlow
{
	[ModuleInitializer]
	internal static void Install ()
		=> Window.WindowOpenedEvent.AddClassHandler<Window> (Apply, RoutingStrategies.Direct);

	static void Apply (Window window, RoutedEventArgs _)
	{
		window.FlowDirection = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
			? FlowDirection.RightToLeft
			: FlowDirection.LeftToRight;
	}
}
