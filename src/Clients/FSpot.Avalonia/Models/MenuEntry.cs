//
// MenuEntry.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Windows.Input;

namespace FSpot.Avalonia.Models;

/// <summary>
/// One row of a menu whose items are only known when it opens (versions, tags, applications).
/// The same rows fill the native menu bar, the in-window menu and context menus.
/// </summary>
public sealed class MenuEntry
{
	MenuEntry (string header, bool isSeparator)
	{
		Header = header;
		IsSeparator = isSeparator;
	}

	public static MenuEntry Separator { get; } = new (string.Empty, true);

	public string Header { get; }
	public bool IsSeparator { get; }
	public ICommand? Command { get; private init; }
	public object? CommandParameter { get; private init; }
	public bool IsEnabled { get; private init; } = true;
	public bool? IsChecked { get; private init; }
	public IReadOnlyList<MenuEntry> Children { get; private init; } = [];
	public bool HasChildren => Children.Count > 0;

	public static MenuEntry Item (string header, ICommand? command, object? parameter = null, bool? isChecked = null, bool enabled = true)
		=> new (header, false) { Command = command, CommandParameter = parameter, IsChecked = isChecked, IsEnabled = enabled };

	public static MenuEntry Disabled (string header)
		=> new (header, false) { IsEnabled = false };

	public static MenuEntry Submenu (string header, IReadOnlyList<MenuEntry> children, bool enabled = true)
		=> new (header, false) { Children = children, IsEnabled = enabled && children.Count > 0 };

	// Escapes '_' so a tag or application name does not become an access key.
	public static string Literal (string? text)
		=> (text ?? string.Empty).Replace ("_", "__");
}

/// <summary>
/// Builds the rows of a dynamic menu every time it opens.
/// </summary>
public sealed class MenuSource
{
	readonly Func<IReadOnlyList<MenuEntry>> build;

	public MenuSource (Func<IReadOnlyList<MenuEntry>> build)
		=> this.build = build;

	public IReadOnlyList<MenuEntry> Build ()
		=> build ();
}
