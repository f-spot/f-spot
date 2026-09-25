// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class DesktopBackgroundTests
{
	[UiTestMethod]
	public void LinuxCommand_UriEncodesAndQuotesAPathWithSpaces ()
	{
		var command = DesktopBackground.LinuxCommand ("/photos/My Wallpaper/wall.jpg");
		Assert.AreEqual ("gsettings", command.FileName);
		string uri = command.Arguments.Split (' ', StringSplitOptions.RemoveEmptyEntries).Last ();
		Assert.AreEqual ("file:///photos/My%20Wallpaper/wall.jpg", uri);
		Assert.DoesNotContain ("'", uri);
		Assert.DoesNotContain ("\"", uri);
		Assert.DoesNotContain ("My Wallpaper", command.Arguments);
	}

	[UiTestMethod]
	public void LinuxCommand_PercentEncodesHashAndQuestionMark ()
	{
		var command = DesktopBackground.LinuxCommand ("/photos/a#b?.jpg");
		string uri = command.Arguments.Split (' ', StringSplitOptions.RemoveEmptyEntries).Last ();
		Assert.AreEqual ("file:///photos/a%23b%3F.jpg", uri);
		Assert.DoesNotContain ("#", uri);
		Assert.DoesNotContain ("?", uri);
	}
}
