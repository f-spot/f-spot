// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.Tests;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Filters.Tests
{
	[TestClass]
	public sealed class UniqueNameFilterTests
	{
		TemporaryDirectories directories;

		[TestInitialize]
		public void Setup ()
			=> directories = new TemporaryDirectories ();

		[TestCleanup]
		public void Cleanup ()
			=> directories.DeleteAll ();

		[TestMethod]
		public void Convert_KeepsTheOriginalNameWhenTheDestinationIsFree ()
		{
			string destDir = directories.Create ("unique-dest");
			string source = Path.Combine (directories.Create ("unique-src"), "holiday.jpg");
			File.WriteAllText (source, "photo");

			using var request = new FilterRequest (new SafeUri (source));
			Assert.IsTrue (new UniqueNameFilter (new SafeUri (destDir)).Convert (request));

			string dest = Path.Combine (destDir, "holiday.jpg");
			Assert.IsTrue (File.Exists (dest));
			Assert.AreEqual ("photo", File.ReadAllText (dest));
			Assert.AreEqual (dest, request.Current.LocalPath);
			Assert.IsTrue (File.Exists (source));
		}

		[TestMethod]
		public void Convert_NumbersTheCopyWhenTheNameIsTaken ()
		{
			string destDir = directories.Create ("unique-dest");
			File.WriteAllText (Path.Combine (destDir, "holiday.jpg"), "taken");
			string source = Path.Combine (directories.Create ("unique-src"), "holiday.jpg");
			File.WriteAllText (source, "photo");

			using var request = new FilterRequest (new SafeUri (source));
			Assert.IsTrue (new UniqueNameFilter (new SafeUri (destDir)).Convert (request));

			string dest = Path.Combine (destDir, "holiday-1.jpg");
			Assert.IsTrue (File.Exists (dest));
			Assert.AreEqual ("photo", File.ReadAllText (dest));
			Assert.AreEqual (dest, request.Current.LocalPath);
			Assert.AreEqual ("taken", File.ReadAllText (Path.Combine (destDir, "holiday.jpg")));
		}

		[TestMethod]
		public void Convert_SkipsAlreadyNumberedNames ()
		{
			string destDir = directories.Create ("unique-dest");
			File.WriteAllText (Path.Combine (destDir, "holiday.jpg"), "taken");
			File.WriteAllText (Path.Combine (destDir, "holiday-1.jpg"), "also-taken");
			string source = Path.Combine (directories.Create ("unique-src"), "holiday.jpg");
			File.WriteAllText (source, "photo");

			using var request = new FilterRequest (new SafeUri (source));
			Assert.IsTrue (new UniqueNameFilter (new SafeUri (destDir)).Convert (request));

			string dest = Path.Combine (destDir, "holiday-2.jpg");
			Assert.AreEqual (dest, request.Current.LocalPath);
			Assert.AreEqual ("photo", File.ReadAllText (dest));
		}
	}
}
