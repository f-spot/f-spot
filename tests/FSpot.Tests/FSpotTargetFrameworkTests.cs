//
// FSpotTargetFrameworkTests.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Versioning;

using FSpot.Settings;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	// Core is net10.0. The attribute on the loaded assembly is the build a client runs.
	[TestClass]
	public class FSpotTargetFrameworkTests
	{
		[TestMethod]
		public void FSpotAssembly_TargetsNet10 ()
		{
			var tfm = typeof (FSpotConfiguration).Assembly
				.GetCustomAttribute<TargetFrameworkAttribute> ();

			Assert.IsNotNull (tfm, "FSpot.dll has no TargetFrameworkAttribute");
			Assert.AreEqual (".NETCoreApp,Version=v10.0", tfm.FrameworkName);
		}
	}
}
