// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Cms.Tests
{
	// No liblcms2 here. Windows CI has none, and that is the platform whose
	// colour-directory list was empty.
	[TestClass]
	public class ProfileDirectoryTests
	{
		[TestMethod]
		public void Directories_OnWindows_UseTheSystemColorDirectory ()
		{
			string root = @"D:\Windows";
			string spool = Path.Combine (root, "System32", "spool", "drivers", "color");

			var paths = ProfileCatalog.Directories (CmsPlatform.Windows, @"C:\Users\person", root).ToArray ();

			Assert.Contains (spool, paths);
			Assert.DoesNotContain (path => path.Contains ("AppData") || path.Contains (".color"), paths);
		}

		[TestMethod]
		public void Directories_OnWindows_UseTheDriveWindowsWhenTheSystemRootIsMissing ()
		{
			string spool = Path.Combine (@"C:\Windows", "System32", "spool", "drivers", "color");

			Assert.Contains (spool, ProfileCatalog.Directories (CmsPlatform.Windows, @"C:\Users\person", null));
			Assert.Contains (spool, ProfileCatalog.Directories (CmsPlatform.Windows, @"C:\Users\person", ""));
		}

		[TestMethod]
		public void Directories_OnLinuxAndMac_KeepTheExistingTrees ()
		{
			var linux = ProfileCatalog.Directories (CmsPlatform.Linux, "/home/person", null).ToArray ();
			Assert.Contains ("/usr/share/color/icc", linux);
			Assert.Contains ("/home/person/.color/icc", linux);
			Assert.Contains ("/usr/local/share/color/icc", linux);

			var mac = ProfileCatalog.Directories (CmsPlatform.Mac, "/Users/person", null).ToArray ();
			Assert.Contains ("/System/Library/ColorSync/Profiles", mac);
			Assert.Contains ("/Users/person/Library/ColorSync/Profiles", mac);
		}
	}
}
