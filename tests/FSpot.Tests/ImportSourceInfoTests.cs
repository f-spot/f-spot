// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public class ImportSourceInfoTests
	{
		[TestMethod]
		public void Record_RoundTripsNameRootAndIconAndComparesEqual ()
		{
			var root = new SafeUri ("file:///photos/camera", true);
			var first = new ImportSourceInfo ("Camera", root, ImportSourceInfo.CameraIcon);
			var second = new ImportSourceInfo ("Camera", new SafeUri ("file:///photos/camera", true), ImportSourceInfo.CameraIcon);

			Assert.AreEqual ("Camera", first.Name);
			Assert.AreEqual (root, first.Root);
			Assert.AreEqual (ImportSourceInfo.CameraIcon, first.IconName);
			Assert.AreEqual (first, second);
		}
	}
}
