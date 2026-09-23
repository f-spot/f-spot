// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using FSpot.Import;
using FSpot.Resources.Lang;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Tests.Import
{
	[TestClass]
	public class FileDropImportTests
	{
		[TestMethod]
		public void Classify_AcceptsAFolderAndAnImageAndReportsTheOtherFile ()
		{
			string root = Path.Combine (Path.GetTempPath (), "fspot-drop-" + Path.GetRandomFileName ());
			Directory.CreateDirectory (root);
			string jpeg = Path.Combine (root, "photo.jpg");
			string notes = Path.Combine (root, "notes.txt");
			File.WriteAllText (jpeg, "jpg");
			File.WriteAllText (notes, "text");
			try {
				var plan = FileDropImport.Classify (new[] { root, jpeg, notes, "   " }, path => path.EndsWith (".jpg"));

				Assert.Contains (Path.GetFullPath (root), plan.Accepted);
				Assert.Contains (Path.GetFullPath (jpeg), plan.Accepted);
				Assert.HasCount (1, plan.Rejected);
				Assert.AreEqual ("notes.txt", plan.Rejected[0]);
				Assert.StartsWith (Strings.UnsupportedFileFormat, FileDropImport.Report (plan.Rejected));
				Assert.Contains ("notes.txt", FileDropImport.Report (plan.Rejected));
			} finally {
				Directory.Delete (root, true);
			}
		}

		[TestMethod]
		public void Classify_ReportsAMissingPathAndDoesNotThrow ()
		{
			var plan = FileDropImport.Classify (new[] { "not-a-real-file:\0" }, _ => true);

			Assert.IsEmpty (plan.Accepted);
			Assert.HasCount (1, plan.Rejected);
			Assert.IsNull (FileDropImport.Report (new string[0]));
		}
	}
}
