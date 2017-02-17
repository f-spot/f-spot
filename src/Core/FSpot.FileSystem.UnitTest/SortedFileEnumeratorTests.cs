//
// SortedFileEnumeratorTests.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Linq;
using FSpot.FileSystem.UnitTest.Mocks;
using GLib;
using Moq;
using NUnit.Framework;

namespace FSpot.FileSystem.UnitTest
{
	[TestFixture]
	public class SortedFileEnumeratorTests
	{
		[Test]
		public void FilesAppearSorted ()
		{
			var file1 = new FileInfo{ Name = "1", FileType = FileType.Regular };
			var file2 = new FileInfo{ Name = "2", FileType = FileType.Regular };

			// 1-2
			var result1 = new SortedFileEnumerator (new[]{ file1, file2 }.GetEnumerator (), true).ToArray ();
			// 2-1
			var result2 = new SortedFileEnumerator (new[]{ file2, file1 }.GetEnumerator (), true).ToArray ();

			CollectionAssert.AreEqual (new[]{ file1, file2 }, result1);
			CollectionAssert.AreEqual (new[]{ file1, file2 }, result2);
		}

		[Test]
		public void DirectoriesAppearSorted ()
		{
			var dir1 = new FileInfo{ Name = "1", FileType = FileType.Directory };
			var dir2 = new FileInfo{ Name = "2", FileType = FileType.Directory };

			// 1-2
			var result1 = new SortedFileEnumerator (new[]{ dir1, dir2 }.GetEnumerator (), true).ToArray ();
			// 2-1
			var result2 = new SortedFileEnumerator (new[]{ dir2, dir1 }.GetEnumerator (), true).ToArray ();

			CollectionAssert.AreEqual (new[]{ dir1, dir2 }, result1);
			CollectionAssert.AreEqual (new[]{ dir1, dir2 }, result2);
		}

		[Test]
		public void DirectoriesAppearBeforeFiles ()
		{
			var file = new FileInfo{ Name = "1", FileType = FileType.Directory };
			var dir = new FileInfo{ Name = "1", FileType = FileType.Regular };

			// file, dir
			var result1 = new SortedFileEnumerator (new[]{ file, dir }.GetEnumerator (), true).ToArray ();
			// dir, file
			var result2 = new SortedFileEnumerator (new[]{ dir, file }.GetEnumerator (), true).ToArray ();

			CollectionAssert.AreEqual (new[]{ dir, file }, result1);
			CollectionAssert.AreEqual (new[]{ dir, file }, result2);
		}

		[Test]
		public void FilesAreSkippedOnExceptions ()
		{
			var file1 = new FileInfo{ Name = "1", FileType = FileType.Regular };
			var file3 = new FileInfo{ Name = "3", FileType = FileType.Regular };

			var list = new Mock<IEnumerator> ();
			list.Setup (l => l.MoveNext ()).ReturnsInOrder (true, new GException (IntPtr.Zero), true, false);
			list.Setup (l => l.Current).ReturnsInOrder (file1, file3);

			// file, dir
			var result = new SortedFileEnumerator (list.Object, true).ToArray ();

			CollectionAssert.AreEqual (new[]{ file1, file3 }, result);
		}

		[Test]
		public void ExceptionIsThrownIfNotCaught ()
		{
			var list = new Mock<IEnumerator> ();
			list.Setup (l => l.MoveNext ()).Throws (new GException (IntPtr.Zero));

			Assert.Throws<GException> (() => new SortedFileEnumerator (list.Object, false));
		}
	}
}
