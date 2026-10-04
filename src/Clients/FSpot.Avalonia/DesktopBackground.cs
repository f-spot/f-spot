// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FSpot.Avalonia;

public readonly record struct WallpaperCommand (string FileName, IReadOnlyList<string> Arguments);

public static class DesktopBackground
{
	public static WallpaperCommand? CommandFor (string path)
	{
		if (string.IsNullOrWhiteSpace (path))
			return null;
		if (OperatingSystem.IsMacOS ())
			return MacCommand (path);
		if (OperatingSystem.IsLinux ())
			return LinuxCommand (path);
		return null;
	}

	public static WallpaperCommand LinuxCommand (string path)
		=> LinuxCommands (path)[0];

	// picture-uri and picture-uri-dark, so a GNOME dark theme does not keep the previous image.
	public static IReadOnlyList<WallpaperCommand> LinuxCommands (string path)
	{
		string uri = FileUri.FromPath (path);
		return new[] {
			GnomeCommand ("picture-uri", uri),
			GnomeCommand ("picture-uri-dark", uri)
		};
	}

	// The path is argv, never text inside the AppleScript. A quote in the path must not end the script.
	public static WallpaperCommand MacCommand (string path)
		=> new ("/usr/bin/osascript", new[] {
			"-e", "on run argv",
			"-e", "tell application \"System Events\" to tell every desktop to set picture to POSIX file (item 1 of argv)",
			"-e", "end run",
			path
		});

	public static void Set (string path)
	{
		if (string.IsNullOrWhiteSpace (path))
			return;
		if (OperatingSystem.IsWindows ()) {
			EnsureWindowsWallpaperAccepted (SetWindowsWallpaper (path));
			return;
		}

		if (OperatingSystem.IsLinux () && !IsGnomeDesktop (CurrentDesktop ()))
			throw new InvalidOperationException ("Setting the desktop background is not supported on this desktop.");

		IReadOnlyList<WallpaperCommand> commands;
		if (OperatingSystem.IsMacOS ())
			commands = new[] { MacCommand (path) };
		else if (OperatingSystem.IsLinux ())
			commands = LinuxCommands (path);
		else
			throw new InvalidOperationException ("Setting the desktop background is not supported on this desktop.");

		foreach (WallpaperCommand command in commands)
			Run (command);
	}

	internal static Func<string> CurrentDesktop = () => Environment.GetEnvironmentVariable ("XDG_CURRENT_DESKTOP") ?? string.Empty;

	internal static Action<ProcessStartInfo> StartProcess = DefaultStart;

	internal static bool IsGnomeDesktop (string? desktop)
		=> !string.IsNullOrEmpty (desktop) && desktop.Contains ("GNOME", StringComparison.OrdinalIgnoreCase);

	internal static ProcessStartInfo CreateStartInfo (WallpaperCommand command)
	{
		var start = new ProcessStartInfo {
			FileName = command.FileName,
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true
		};
		foreach (string argument in command.Arguments)
			start.ArgumentList.Add (argument);
		return start;
	}

	internal static void EnsureProcessSucceeded (string fileName, int exitCode, string stderr)
	{
		if (exitCode != 0)
			throw new InvalidOperationException ($"{fileName} exited {exitCode}: {stderr}");
	}

	internal static void EnsureWindowsWallpaperAccepted (bool accepted)
	{
		if (!accepted)
			throw new InvalidOperationException ($"SystemParametersInfo failed ({Marshal.GetLastWin32Error ()}).");
	}

	static WallpaperCommand GnomeCommand (string key, string uri)
		=> new ("/usr/bin/gsettings", new[] { "set", "org.gnome.desktop.background", key, uri });

	static void Run (WallpaperCommand command)
	{
		try {
			StartProcess (CreateStartInfo (command));
		} catch (Exception ex) when (ex is Win32Exception or FileNotFoundException) {
			throw new InvalidOperationException ("Setting the desktop background is not supported on this desktop.", ex);
		}
	}

	static void DefaultStart (ProcessStartInfo start)
	{
		using var process = Process.Start (start) ?? throw new InvalidOperationException ($"{start.FileName} did not start.");
		// gsettings and osascript write a few lines. Read after exit so the exit code is the result.
		process.WaitForExit ();
		string stderr = process.StandardError.ReadToEnd ();
		_ = process.StandardOutput.ReadToEnd ();
		EnsureProcessSucceeded (start.FileName, process.ExitCode, stderr);
	}

	const uint SpiSetDeskWallpaper = 20;
	const uint SpifUpdateIniFile = 0x01;
	const uint SpifSendWinIniChange = 0x02;

	[DllImport ("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	static extern bool SystemParametersInfo (uint uiAction, uint uiParam, string pvParam, uint fWinIni);

	static bool SetWindowsWallpaper (string path)
		=> SystemParametersInfo (SpiSetDeskWallpaper, 0, path, SpifUpdateIniFile | SpifSendWinIniChange);
}
