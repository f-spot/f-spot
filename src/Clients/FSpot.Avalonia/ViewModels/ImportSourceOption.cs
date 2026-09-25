//
// ImportSourceOption.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Avalonia.ViewModels;

public enum ImportSourceKind
{
	ChooseFolder,
	Source,
}

// One row of the "Import from:" picker: either the always-present "Choose
// Folder..." entry, or a source with a path to scan (a detected camera/card,
// or a folder picked earlier this session), matching FSpot.Gtk's
// ImportDialog source combo (FSpot.Import.ImportSource plus its
// "Choose Folder..." and history entries).
public sealed record ImportSourceOption (string Label, string? Path, ImportSourceKind Kind)
{
	public static ImportSourceOption ChooseFolder (string label)
		=> new (label, null, ImportSourceKind.ChooseFolder);

	public static ImportSourceOption ForSource (string label, string path)
		=> new (label, path, ImportSourceKind.Source);

	public override string ToString () => Label;
}
