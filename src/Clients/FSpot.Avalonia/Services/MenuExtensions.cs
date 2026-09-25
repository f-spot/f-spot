//
// MenuExtensions.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Core;

namespace FSpot.Avalonia.Services;

/// <summary>
/// The GTK /FSpot/Menus extension points: "Export to" (/FSpot/Menus/Exports),
/// "Tools" (/FSpot/Menus/Tools) and the photo context menu (/FSpot/Menus/PhotoPopup).
/// </summary>
public enum MenuExtensionPoint
{
	Exports,
	Tools,
	PhotoPopup
}

/// <summary>
/// A menu item an extension adds. <see cref="Run"/> receives the photos the command acts on.
/// </summary>
public sealed record MenuContribution (
	string Id,
	string Label,
	Func<IReadOnlyList<IPhoto>, Task> Run,
	Func<IReadOnlyList<IPhoto>, bool>? IsEnabled = null);

public static class MenuExtensions
{
	static readonly object sync = new ();
	static readonly Dictionary<MenuExtensionPoint, List<MenuContribution>> contributions = new ();

	public static event EventHandler? Changed;

	public static void Register (MenuExtensionPoint point, MenuContribution contribution)
	{
		ArgumentNullException.ThrowIfNull (contribution);
		lock (sync) {
			if (!contributions.TryGetValue (point, out var list))
				contributions[point] = list = [];
			list.RemoveAll (existing => existing.Id == contribution.Id);
			list.Add (contribution);
		}
		Changed?.Invoke (null, EventArgs.Empty);
	}

	public static bool Unregister (MenuExtensionPoint point, string id)
	{
		bool removed;
		lock (sync)
			removed = contributions.TryGetValue (point, out var list) && list.RemoveAll (existing => existing.Id == id) > 0;
		if (removed)
			Changed?.Invoke (null, EventArgs.Empty);
		return removed;
	}

	public static IReadOnlyList<MenuContribution> For (MenuExtensionPoint point)
	{
		lock (sync)
			return contributions.TryGetValue (point, out var list) ? list.ToArray () : [];
	}
}
