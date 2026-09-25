//
// ImportSourceHistory.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace FSpot.Avalonia.ViewModels;

// Folders picked through "Choose Folder..." earlier this session, offered
// again in the source picker. Mirrors ImportDialog.historySources in
// FSpot.Gtk: a static, in-memory (not persisted) list shared by every Import
// window opened while F-Spot keeps running.
public static class ImportSourceHistory
{
	static readonly Dictionary<string, ImportSourceOption> byPath = new ();

	public static IReadOnlyList<ImportSourceOption> All => [.. byPath.Values];

	public static ImportSourceOption Remember (string label, string path)
	{
		var option = ImportSourceOption.ForSource (label, path);
		byPath[path] = option;
		return option;
	}

	// For tests only, so one test's history doesn't leak into another's.
	internal static void Clear () => byPath.Clear ();
}
