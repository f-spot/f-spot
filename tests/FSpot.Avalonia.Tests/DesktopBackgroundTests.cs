// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class DesktopBackgroundTests
{
	[UiTestMethod]
	public void LinuxCommand_UriEncodesAndQuotesAPathWithSpaces ()
	{
		WallpaperCommand command = DesktopBackground.LinuxCommand ("/photos/My Wallpaper/wall.jpg");
		Assert.AreEqual ("/usr/bin/gsettings", command.FileName);
		Assert.AreEqual ("file:///photos/My%20Wallpaper/wall.jpg", command.Arguments[^1]);
		Assert.DoesNotContain ("'", command.Arguments[^1]);
		Assert.DoesNotContain ("\"", command.Arguments[^1]);
		Assert.DoesNotContain ("My Wallpaper", string.Join (" ", command.Arguments));
	}

	[UiTestMethod]
	public void LinuxCommand_PercentEncodesHashAndQuestionMark ()
	{
		WallpaperCommand command = DesktopBackground.LinuxCommand ("/photos/a#b?.jpg");
		Assert.AreEqual ("file:///photos/a%23b%3F.jpg", command.Arguments[^1]);
		Assert.DoesNotContain ("#", command.Arguments[^1]);
		Assert.DoesNotContain ("?", command.Arguments[^1]);
	}

	[UiTestMethod]
	public void LinuxCommand_CollapsesDotSegmentsOnAPosixPath ()
	{
		WallpaperCommand command = DesktopBackground.LinuxCommand ("/photos/../photos/wall.jpg");
		Assert.AreEqual ("file:///photos/wall.jpg", command.Arguments[^1]);
	}

	[UiTestMethod]
	public void LinuxCommand_KeepsADriveLetter ()
	{
		WallpaperCommand command = DesktopBackground.LinuxCommand (@"C:\photos\My Wallpaper\wall.jpg");
		Assert.AreEqual ("file:///C:/photos/My%20Wallpaper/wall.jpg", command.Arguments[^1]);
	}

	[UiTestMethod]
	public void LinuxCommands_SetsTheDarkThemeKeyToo ()
	{
		IReadOnlyList<WallpaperCommand> commands = DesktopBackground.LinuxCommands ("/photos/wall.jpg");
		Assert.HasCount (2, commands);
		Assert.AreEqual ("picture-uri", commands[0].Arguments[2]);
		Assert.AreEqual ("picture-uri-dark", commands[1].Arguments[2]);
		Assert.AreEqual (commands[0].Arguments[^1], commands[1].Arguments[^1]);
	}

	[UiTestMethod]
	public void MacCommand_KeepsAHostilePathOutOfTheAppleScript ()
	{
		const string path = "/photos/a\"b\\c&d\ne.jpg";
		ProcessStartInfo start = DesktopBackground.CreateStartInfo (DesktopBackground.MacCommand (path));

		Assert.AreEqual ("/usr/bin/osascript", start.FileName);
		Assert.AreEqual (string.Empty, start.Arguments);
		Assert.IsTrue (start.RedirectStandardError);
		Assert.AreEqual (path, start.ArgumentList[start.ArgumentList.Count - 1]);
		for (int i = 0; i < start.ArgumentList.Count - 1; i++) {
			Assert.DoesNotContain ("a\"b", start.ArgumentList[i]);
			Assert.DoesNotContain ("&d", start.ArgumentList[i]);
			Assert.DoesNotContain ("\n", start.ArgumentList[i]);
		}
	}

	[UiTestMethod]
	public void IsGnomeDesktop_RequiresGnomeInTheDesktopName ()
	{
		Assert.IsTrue (DesktopBackground.IsGnomeDesktop ("ubuntu:GNOME"));
		Assert.IsTrue (DesktopBackground.IsGnomeDesktop ("GNOME"));
		Assert.IsFalse (DesktopBackground.IsGnomeDesktop ("KDE"));
		Assert.IsFalse (DesktopBackground.IsGnomeDesktop (""));
		Assert.IsFalse (DesktopBackground.IsGnomeDesktop (null));
	}

	[UiTestMethod]
	public void EnsureProcessSucceeded_IncludesStderrFromAFailedTool ()
	{
		var failed = Assert.ThrowsExactly<InvalidOperationException> (() =>
			DesktopBackground.EnsureProcessSucceeded ("/usr/bin/gsettings", 1, "no schema"));
		Assert.Contains ("exited 1", failed.Message);
		Assert.Contains ("no schema", failed.Message);
		DesktopBackground.EnsureProcessSucceeded ("/usr/bin/gsettings", 0, string.Empty);
	}

	[UiTestMethod]
	public void EnsureWindowsWallpaperAccepted_ThrowsWhenTheCallFails ()
	{
		var failed = Assert.ThrowsExactly<InvalidOperationException> (() =>
			DesktopBackground.EnsureWindowsWallpaperAccepted (false));
		Assert.Contains ("SystemParametersInfo", failed.Message);
	}

	[UiTestMethod]
	public void Set_LaunchesWithArgumentListRatherThanASingleArgumentsString ()
	{
		if (OperatingSystem.IsWindows ())
			return;

		var launched = new List<ProcessStartInfo> ();
		Action<ProcessStartInfo> originalStart = DesktopBackground.StartProcess;
		Func<string> originalDesktop = DesktopBackground.CurrentDesktop;
		try {
			DesktopBackground.CurrentDesktop = () => "GNOME";
			DesktopBackground.StartProcess = launched.Add;
			const string path = "/tmp/a\"b\\c&d\ne.jpg";
			DesktopBackground.Set (path);

			IReadOnlyList<WallpaperCommand> commands = OperatingSystem.IsMacOS ()
				? new[] { DesktopBackground.MacCommand (path) }
				: DesktopBackground.LinuxCommands (path);
			Assert.HasCount (commands.Count, launched);
			for (int i = 0; i < commands.Count; i++) {
				Assert.AreEqual (commands[i].FileName, launched[i].FileName);
				Assert.AreEqual (string.Empty, launched[i].Arguments);
				Assert.AreSequenceEqual (commands[i].Arguments.ToArray (), launched[i].ArgumentList.ToArray ());
			}
		} finally {
			DesktopBackground.StartProcess = originalStart;
			DesktopBackground.CurrentDesktop = originalDesktop;
		}
	}

	[UiTestMethod]
	public void Set_OnANonGnomeDesktopDoesNotLaunch ()
	{
		if (!OperatingSystem.IsLinux ())
			return;

		bool launched = false;
		Action<ProcessStartInfo> originalStart = DesktopBackground.StartProcess;
		Func<string> originalDesktop = DesktopBackground.CurrentDesktop;
		try {
			DesktopBackground.CurrentDesktop = () => "KDE";
			DesktopBackground.StartProcess = _ => launched = true;
			var failed = Assert.ThrowsExactly<InvalidOperationException> (() => DesktopBackground.Set ("/tmp/wall.jpg"));
			Assert.Contains ("not supported", failed.Message);
			Assert.IsFalse (launched);
		} finally {
			DesktopBackground.StartProcess = originalStart;
			DesktopBackground.CurrentDesktop = originalDesktop;
		}
	}
}
