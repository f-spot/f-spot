// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot;

namespace FSpot.Avalonia.Dialogs;

// New rolls and a copy into the library, matching the GTK merge dialog.
public readonly record struct CatalogMergeChoice (CatalogRollChoice Rolls, bool CopyFiles, uint RollId)
{
	public static CatalogMergeChoice NewRollsCopied { get; } = new (CatalogRollChoice.New, true, 0);
}
