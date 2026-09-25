//
// MainWindow.Editors.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Editors;

namespace FSpot.Avalonia;

public partial class MainWindow
{
	public async Task<IReadOnlyList<CropConstraint>?> EditCustomCropRatiosAsync (IReadOnlyList<CropConstraint> custom)
	{
		var dialog = new SelectionRatioWindow (custom);
		await dialog.ShowDialog (this);
		return dialog.Result;
	}
}
