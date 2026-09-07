//
// FSpotConfigurationTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace FSpot.UnitTest.Settings
{
	[TestClass]
	public class FSpotConfigurationTests
	{
		[TestMethod]
		public void CheckPackageName ()
		{
			// This shouldn't change, but, this test will catch someone changing it
			//  For now at least
			Assert.AreEqual ("f-spot", FSpotConfiguration.Package);
		}

		[TestMethod]
		public void CheckCurrentVersion ()
		{
			Assert.AreEqual ("0.9.0", FSpotConfiguration.Version);
		}
	}
}
