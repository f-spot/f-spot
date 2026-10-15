// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class FileIdentityTests
	{
		[TestMethod]
		public void Same_MatchesAMissingFileUnderAnExistingDirectory ()
		{
			string folder = Path.Combine (Path.GetTempPath (), "fspot-id-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (folder);
			string missing = Path.Combine (folder, "missing.jpg");
			string other = Path.Combine (folder, "other.jpg");
			try {
				Assert.IsFalse (File.Exists (missing));
				Assert.IsTrue (FileIdentity.Same (missing, missing));
				Assert.IsFalse (FileIdentity.Same (missing, other));
			} finally {
				Directory.Delete (folder);
			}
		}

		// On Windows the nearest existing ancestor of this name is the drive root.
		// ResolveLinkTarget throws there, and Same must not treat the path as a different file.
		[TestMethod]
		public void Same_MatchesAMissingFileWhoseNearestAncestorIsTheVolumeRoot ()
		{
			string root = Path.GetPathRoot (Path.GetFullPath (Path.GetTempPath ()));
			string missing = Path.Combine (root, "fspot-missing-" + Guid.NewGuid ().ToString ("N") + ".jpg");
			string other = Path.Combine (root, "fspot-other-" + Guid.NewGuid ().ToString ("N") + ".jpg");
			Assert.IsFalse (string.IsNullOrEmpty (root));
			Assert.IsFalse (File.Exists (missing));
			Assert.IsTrue (FileIdentity.Same (missing, missing));
			Assert.IsFalse (FileIdentity.Same (missing, other));
		}
	}
}
