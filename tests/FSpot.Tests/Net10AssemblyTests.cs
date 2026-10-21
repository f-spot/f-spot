// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

using FSpot.Resources;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public class Net10AssemblyTests
	{
		[TestMethod]
		public void StringsAndResources_AreNet10AndExposeAStringAndAnImage ()
		{
			Assert.AreEqual (".NETCoreApp,Version=v10.0", Framework (typeof (Strings).Assembly));
			Assert.AreEqual (".NETCoreApp,Version=v10.0", Framework (ResourceAssembly.Instance));
			Assert.AreEqual ("F-Spot", Strings.FSpot);

			using Stream image = ResourceAssembly.Instance.GetManifestResourceStream ("flower.png");
			Assert.IsNotNull (image);
			Assert.IsGreaterThan (8, image.Length);
		}

		static string Framework (Assembly assembly)
			=> assembly.GetCustomAttribute<TargetFrameworkAttribute> ().FrameworkName;
	}
}
