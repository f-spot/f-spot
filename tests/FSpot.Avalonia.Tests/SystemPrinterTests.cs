// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Avalonia.Printing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class SystemPrinterTests
{
	[UiTestMethod]
	public void ParsePrinters_ReadsOneDestinationPerLine ()
	{
		var printers = SystemPrinter.ParsePrinters ("Office_Laser\nPhoto_Inkjet\n\nOffice_Laser\n");
		Assert.AreSequenceEqual (new[] { "Office_Laser", "Photo_Inkjet" }, printers.ToArray ());
	}

	[UiTestMethod]
	public void ParseDefault_ReadsTheSystemDefault ()
	{
		Assert.AreEqual ("Office_Laser", SystemPrinter.ParseDefault ("system default destination: Office_Laser\n"));
		Assert.IsNull (SystemPrinter.ParseDefault ("no system default destination\n"));
		Assert.IsNull (SystemPrinter.ParseDefault (string.Empty));
	}

	[UiTestMethod]
	public void LpArguments_CarryPrinterCopiesAndMedia ()
	{
		var args = SystemPrinter.LpArguments ("/tmp/a.pdf", "Laser", 2, "A4", "My Photos");
		Assert.AreSequenceEqual (new[] { "-d", "Laser", "-n", "2", "-t", "My Photos", "-o", "media=A4", "/tmp/a.pdf" }, args.ToArray ());

		var bare = SystemPrinter.LpArguments ("/tmp/a.pdf", null, 0, null, "x");
		Assert.AreSequenceEqual (new[] { "-n", "1", "-t", "x", "/tmp/a.pdf" }, bare.ToArray ());
	}
}
