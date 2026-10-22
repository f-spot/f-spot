// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProgramTests
{
	[TestMethod]
	public void OptOutOfAvaloniaTelemetry_SetsTheEnvironmentVariable ()
	{
		string previous = Environment.GetEnvironmentVariable ("AVALONIA_TELEMETRY_OPTOUT");
		try {
			Environment.SetEnvironmentVariable ("AVALONIA_TELEMETRY_OPTOUT", null);
			Program.OptOutOfAvaloniaTelemetry ();
			Assert.AreEqual ("1", Environment.GetEnvironmentVariable ("AVALONIA_TELEMETRY_OPTOUT"));
		} finally {
			Environment.SetEnvironmentVariable ("AVALONIA_TELEMETRY_OPTOUT", previous);
		}
	}
}
