// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Avalonia.Input;

namespace FSpot.Avalonia;

public static class DragFormats
{
	public static readonly DataFormat<string> TagId = DataFormat.CreateStringApplicationFormat ("x-fspot-tag-id");
	public static readonly DataFormat<string> Photos = DataFormat.CreateStringApplicationFormat ("x-fspot-photos");
	public static readonly DataFormat<string> QueryLiteral = DataFormat.CreateStringApplicationFormat ("x-fspot-query-literal");
	public static readonly DataFormat<string> FolderUri = DataFormat.CreateStringApplicationFormat ("x-fspot-folder-uri");
}
