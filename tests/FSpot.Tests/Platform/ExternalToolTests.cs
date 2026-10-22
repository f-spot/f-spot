// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.Imaging;
using FSpot.Platform;

using Hyena;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot.Platform.Tests
{
	[TestClass]
	public sealed class ExternalToolTests
	{
		string root;

		[TestInitialize]
		public void CreateRoot ()
		{
			root = Path.Combine (Path.GetTempPath (), "fspot-tool-" + Guid.NewGuid ().ToString ("N"));
			Directory.CreateDirectory (root);
		}

		[TestCleanup]
		public void DeleteRoot ()
		{
			ExternalTool.SearchDirectories = name => (Environment.GetEnvironmentVariable ("PATH") ?? string.Empty).Split (Path.PathSeparator);
			if (Directory.Exists (root))
				Directory.Delete (root, true);
		}

		[TestMethod]
		public void Resolve_ReturnsTheAbsolutePathAndSkipsTheWorkingDirectory ()
		{
			string tool = Path.Combine (root, ExternalTool.FileNameFor ("lp", OperatingSystem.IsWindows ()));
			File.WriteAllText (tool, "tool");
			ExternalTool.SearchDirectories = _ => new[] { ".", string.Empty, root };

			string resolved = ExternalTool.Resolve ("lp");

			Assert.AreEqual (Path.GetFullPath (tool), resolved);
			Assert.IsTrue (Path.IsPathRooted (resolved));
		}

		[TestMethod]
		public void FileNameFor_OnWindowsAppendsExeAndLeavesAnExistingExtension ()
		{
			Assert.AreEqual ("lp.exe", ExternalTool.FileNameFor ("lp", windows: true));
			Assert.AreEqual ("dcraw.exe", ExternalTool.FileNameFor ("dcraw", windows: true));
			Assert.AreEqual ("tool.bat", ExternalTool.FileNameFor ("tool.bat", windows: true));
			Assert.AreEqual ("lp", ExternalTool.FileNameFor ("lp", windows: false));
		}

		[TestMethod]
		public void Resolve_MissingToolThrows ()
		{
			ExternalTool.SearchDirectories = _ => new[] { root };

			Assert.ThrowsExactly<FileNotFoundException> (() => ExternalTool.Resolve ("dcraw"));
		}

		[TestMethod]
		public void Dcraw_StartsFromTheResolvedPath ()
		{
			string tool = Path.Combine (root, ExternalTool.FileNameFor ("dcraw", OperatingSystem.IsWindows ()));
			File.WriteAllText (tool, "not a program");
			ExternalTool.SearchDirectories = _ => new[] { root };
			var photo = new SafeUri (Path.Combine (root, "photo.nef"));

			var error = Assert.ThrowsExactly<IOException> (() => DCRawImageFile.OpenRawImageStream (photo));

			Assert.Contains (Path.GetFullPath (tool), error.Message);
		}
	}
}
