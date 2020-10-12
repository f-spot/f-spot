//
// FSpotConfigurationTests.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2019 Stephen Shaw
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NUnit.Framework;

using Shouldly;

using FSpot.Settings;

namespace FSpot.UnitTest.Settings
{
	[TestFixture]
	class FSpotConfigurationTests
	{
		[Test]
		public void CheckPackageName ()
		{
			// This shouldn't change, but, this test will catch someone changing it
			//  For now at least
			FSpotConfiguration.Package.ShouldBe ("f-spot");
		}

		[Test]
		public void CheckCurrentVersion ()
		{
			FSpotConfiguration.Version.ShouldBe ("0.9.0");
		}
	}
}
