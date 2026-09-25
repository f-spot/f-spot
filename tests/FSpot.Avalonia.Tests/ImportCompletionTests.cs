// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Resources.Lang;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class ImportCompletionTests
{
	[UiTestMethod]
	public void ShouldReloadCatalog_OnlyWhenSomethingImported ()
	{
		Assert.IsFalse (ImportCompletion.ShouldReloadCatalog (0));
		Assert.IsTrue (ImportCompletion.ShouldReloadCatalog (1));
	}

	[UiTestMethod]
	public void FormatFailures_Empty_IsEmpty ()
	{
		Assert.AreEqual (string.Empty, ImportCompletion.FormatFailures (Array.Empty<SafeUri> ()));
	}

	[UiTestMethod]
	public void FormatFailures_ListsFilenamesUnderTheImportFailuresHeader ()
	{
		string message = ImportCompletion.FormatFailures (new[] {
			new SafeUri ("file:///photos/broken.jpg", true),
			new SafeUri ("file:///photos/also-bad.png", true)
		});

		Assert.StartsWith (Strings.ImportFailures, message);
		Assert.Contains ("broken.jpg", message);
		Assert.Contains ("also-bad.png", message);
	}
}
