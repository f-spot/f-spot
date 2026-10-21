// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.Utils.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Imaging.Tests
{
	[TestClass]
	public class DerivedVersionFileTests
	{
		[TestMethod]
		public void Publish_OverwritesAnExistingDestination ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-derived-" + Path.GetRandomFileName ());
			Directory.CreateDirectory (root);
			try {
				SafeUri source = ImageTestHelper.CreateTempFile ("taglib-sample.jpg");
				string destPath = Path.Combine (root, "version.jpg");
				File.WriteAllBytes (destPath, new byte[] { 1, 2, 3 });

				DerivedVersionFile.Publish (source, SafeUri.FromPath (destPath), temp => File.Copy (source.AbsolutePath, temp));

				byte[] written = File.ReadAllBytes (destPath);
				Assert.IsGreaterThan (3, written.Length);
				Assert.AreNotEqual (1, written[0]);
			} finally {
				if (Directory.Exists (root))
					Directory.Delete (root, true);
			}
		}
	}
}
