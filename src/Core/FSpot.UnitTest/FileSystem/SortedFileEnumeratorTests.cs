//
// SortedFileEnumeratorTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using FSpot.FileSystem;

using Hyena;

using Moq;

using NUnit.Framework;

namespace FSpot.UnitTest.FileSystem
{
	[TestFixture]
	public class SortedFileEnumeratorTests
	{
		IFileSystem GetFileSystemMock ()
		{
			return Mock.Of<IFileSystem> (m =>
				m.File == Mock.Of<IFile> () &&
				m.Directory == Mock.Of<IDirectory> ());
		}

		void AddFile (IFileSystem fileSystem, SafeUri file)
		{
			Mock.Get (fileSystem.File).Setup (m => m.Exists (file)).Returns (true);
		}

		void AddDirectory (IFileSystem fileSystem, SafeUri dir)
		{
			Mock.Get (fileSystem.Directory).Setup (m => m.Exists (dir)).Returns (true);
		}

		[Test]
		public void FilesAppearSorted ()
		{
			var file1 = new SafeUri ("/1");
			var file2 = new SafeUri ("/2");

			var fileSystem = GetFileSystemMock ();
			AddFile (fileSystem, file1);
			AddFile (fileSystem, file2);

			// 1-2
			var result1 = new SortedFileEnumerator (new[] { file1, file2 }, fileSystem).ToArray ();
			// 2-1
			var result2 = new SortedFileEnumerator (new[] { file2, file1 }, fileSystem).ToArray ();

			CollectionAssert.AreEqual (new[] { file1, file2 }, result1);
			CollectionAssert.AreEqual (new[] { file1, file2 }, result2);
		}

		[Test]
		public void DirectoriesAppearSorted ()
		{
			var dir1 = new SafeUri ("/1");
			var dir2 = new SafeUri ("/2");

			var fileSystem = GetFileSystemMock ();
			AddDirectory (fileSystem, dir1);
			AddDirectory (fileSystem, dir2);

			// 1-2
			var result1 = new SortedFileEnumerator (new[] { dir1, dir2 }, fileSystem).ToArray ();
			// 2-1
			var result2 = new SortedFileEnumerator (new[] { dir2, dir1 }, fileSystem).ToArray ();

			CollectionAssert.AreEqual (new[] { dir1, dir2 }, result1);
			CollectionAssert.AreEqual (new[] { dir1, dir2 }, result2);
		}

		[Test]
		public void DirectoriesAppearBeforeFiles ()
		{
			var file = new SafeUri ("/1");
			var dir = new SafeUri ("/2");

			var fileSystem = GetFileSystemMock ();
			AddFile (fileSystem, file);
			AddDirectory (fileSystem, dir);

			// file, dir
			var result1 = new SortedFileEnumerator (new[] { file, dir }, fileSystem).ToArray ();
			// dir, file
			var result2 = new SortedFileEnumerator (new[] { dir, file }, fileSystem).ToArray ();

			CollectionAssert.AreEqual (new[] { dir, file }, result1);
			CollectionAssert.AreEqual (new[] { dir, file }, result2);
		}
	}
}
