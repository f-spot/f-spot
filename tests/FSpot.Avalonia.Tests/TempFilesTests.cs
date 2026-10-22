// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Avalonia.Tests;

[TestClass]
public sealed class TempFilesTests
{
	[TestMethod]
	public void DeleteOlderThan_RemovesAStalePrintFileAndKeepsANewOne ()
	{
		string root = Path.Combine (Path.GetTempPath (), "fspot-temp-" + Guid.NewGuid ().ToString ("N"));
		Directory.CreateDirectory (root);
		try {
			string stale = Path.Combine (root, "f-spot-print-old.pdf");
			string fresh = Path.Combine (root, "f-spot-print-new.pdf");
			File.WriteAllText (stale, "old");
			File.WriteAllText (fresh, "new");
			File.SetLastWriteTimeUtc (stale, DateTime.UtcNow.AddDays (-2));

			TempFiles.DeleteOlderThan (root, "f-spot-print-", TimeSpan.FromDays (1));

			Assert.IsFalse (File.Exists (stale));
			Assert.IsTrue (File.Exists (fresh));
		} finally {
			Directory.Delete (root, true);
		}
	}
}
