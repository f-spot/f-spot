//
// FileSystemMock.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2016 Daniel Köb
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using FSpot.FileSystem;

using Hyena;

using Moq;

namespace Mocks
{
	public class FileSystemMock : IFileSystem
	{
		readonly Mock<IFile> file_mock;
		readonly Mock<IDirectory> directory_mock;

		public Mock<IFile> FileMock {
			get {
				return file_mock;
			}
		}

		public Mock<IDirectory> DirectoryMock {
			get {
				return directory_mock;
			}
		}

		public FileSystemMock (params SafeUri[] existingFiles)
		{
			file_mock = new Mock<IFile> ();
			file_mock.Setup (m => m.Exists (It.IsIn (existingFiles))).Returns (true);

			directory_mock = new Mock<IDirectory> ();
		}

		public void SetFile (SafeUri file)
		{
			SetFile (file, DateTime.MinValue);
		}

		public void SetFile (SafeUri file, DateTime mTime)
		{
			SetFile (file, mTime, new byte[]{ });
		}

		public void SetFile (SafeUri file, DateTime mTime, byte[] contents)
		{
			file_mock.Setup (m => m.Exists (file)).Returns (true);
			file_mock.Setup (m => m.GetMTime (file)).Returns (mTime);
			file_mock.Setup (m => m.Read (file)).Returns (new MemoryStream (contents));
		}

		public void SetDirectory (SafeUri directory)
		{
			directory_mock.Setup (m => m.Exists (directory)).Returns (true);
		}

		#region IFileSystem implementation

		public IFile File {
			get {
				return file_mock.Object;
			}
		}

		public IDirectory Directory {
			get {
				return directory_mock.Object;
			}
		}

		public IPath Path {
			get {
				return new Mock<IPath> ().Object;
			}
		}

		#endregion
	}
}
