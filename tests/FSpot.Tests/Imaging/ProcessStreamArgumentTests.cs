// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public class ProcessStreamArgumentTests
	{
		[TestMethod]
		public void AddArguments_KeepsSpacesQuotesAndCarriageReturnAsSeparateEntries ()
		{
			var start = new ProcessStartInfo ("decoder");
			ProcessStream.AddArguments (start, ["/my photos/a.nef", "say \"hi\"", "a\r-Z"]);

			Assert.HasCount (3, start.ArgumentList);
			Assert.AreEqual ("/my photos/a.nef", start.ArgumentList[0]);
			Assert.AreEqual ("say \"hi\"", start.ArgumentList[1]);
			Assert.AreEqual ("a\r-Z", start.ArgumentList[2]);
		}

		[TestMethod]
		public void AddArguments_RejectsANullArgument ()
		{
			var start = new ProcessStartInfo ("decoder");
			Assert.ThrowsExactly<ArgumentException> (() => ProcessStream.AddArguments (start, ["ok", null]));
		}
	}
}
