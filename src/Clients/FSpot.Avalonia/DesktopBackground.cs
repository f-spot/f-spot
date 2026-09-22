// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FSpot.Avalonia;

public static class DesktopBackground
{
	public static (string FileName, string Arguments)? CommandFor (string path)
	{
		if (string.IsNullOrWhiteSpace (path))
			return null;
		if (OperatingSystem.IsMacOS ())
			return MacCommand (path);
		if (OperatingSystem.IsLinux ())
			return LinuxCommand (path);
		return null;
	}

	public static (string FileName, string Arguments) LinuxCommand (string path)
	{
		string full = Path.GetFullPath (path).Replace ('\\', '/');
		var builder = new UriBuilder {
			Scheme = Uri.UriSchemeFile,
			Host = string.Empty,
			Path = full
		};
		return ("gsettings", "set org.gnome.desktop.background picture-uri " + builder.Uri.AbsoluteUri);
	}

	public static (string FileName, string Arguments) MacCommand (string path)
	{
		string escaped = path.Replace ("\\", "\\\\").Replace ("\"", "\\\"");
		return ("osascript", "-e \"tell application \\\"System Events\\\" to tell every desktop to set picture to POSIX file \\\"" + escaped + "\\\"\"");
	}

	public static void Set (string path)
	{
		if (OperatingSystem.IsWindows ()) {
			SetWindowsWallpaper (path);
			return;
		}
		var command = CommandFor (path);
		if (command is null)
			return;
		var start = new ProcessStartInfo {
			FileName = command.Value.FileName,
			UseShellExecute = false
		};
		if (OperatingSystem.IsLinux ()) {
			foreach (string arg in command.Value.Arguments.Split (' ', StringSplitOptions.RemoveEmptyEntries))
				start.ArgumentList.Add (arg);
		} else {
			start.Arguments = command.Value.Arguments;
		}
		Process.Start (start);
	}

	const uint SpiSetDeskWallpaper = 20;
	const uint SpifUpdateIniFile = 0x01;
	const uint SpifSendWinIniChange = 0x02;

	[DllImport ("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern bool SystemParametersInfo (uint uiAction, uint uiParam, string pvParam, uint fWinIni);

	static void SetWindowsWallpaper (string path)
		=> SystemParametersInfo (SpiSetDeskWallpaper, 0, path, SpifUpdateIniFile | SpifSendWinIniChange);
}
