// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Core;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using Hyena;

namespace FSpot.Tests
{
	[TestClass]
	public sealed class PhotoFileActionsTests
	{
		[TestMethod]
		public void SelectedPaths_AreTheDefaultVersionPathsInSelectionOrder ()
		{
			var photos = new[] {
				Photo ("/photos/one.jpg"),
				Photo ("/photos/two.jpg")
			};

			var paths = PhotoFileActions.SelectedPaths (photos);

			Assert.HasCount (2, paths);
			Assert.AreEqual ("/photos/one.jpg", paths[0]);
			Assert.AreEqual ("/photos/two.jpg", paths[1]);
		}

		[TestMethod]
		public void CopyToFolder_WritesEachSelectedFile ()
		{
			string sourceDir = Path.Combine (Path.GetTempPath (), "fspot-export-src-" + Guid.NewGuid ().ToString ("N"));
			string destDir = Path.Combine (Path.GetTempPath (), "fspot-export-dst-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (sourceDir);
			string one = Path.Combine (sourceDir, "one.jpg");
			string two = Path.Combine (sourceDir, "two.jpg");
			File.WriteAllText (one, "a");
			File.WriteAllText (two, "b");
			try {
				var written = PhotoFileActions.CopyToFolder (new[] { one, two }, destDir);

				Assert.HasCount (2, written);
				Assert.IsTrue (File.Exists (Path.Combine (destDir, "one.jpg")));
				Assert.IsTrue (File.Exists (Path.Combine (destDir, "two.jpg")));
				Assert.AreEqual ("a", File.ReadAllText (Path.Combine (destDir, "one.jpg")));
			} finally {
				Directory.Delete (sourceDir, true);
				if (Directory.Exists (destDir))
					Directory.Delete (destDir, true);
			}
		}

		static IPhoto Photo (string path)
		{
			var version = new Mock<IPhotoVersion> ();
			version.Setup (v => v.Uri).Returns (new SafeUri ("file://" + path, true));
			var photo = new Mock<IPhoto> ();
			photo.Setup (p => p.DefaultVersion).Returns (version.Object);
			photo.Setup (p => p.Name).Returns (Path.GetFileName (path));
			return photo.Object;
		}
	}
}
