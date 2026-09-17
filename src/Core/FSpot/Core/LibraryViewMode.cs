// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Core
{
	// Icon grid vs single-photo. GTK MainWindow.ModeType and the Avalonia
	// shell both use this; editor buttons that cannot handle a multi-select
	// hide in IconView (GTK EditorPageWidget.ChangeButtonVisibility).
	public enum LibraryViewMode
	{
		IconView,
		PhotoView
	}
}
