//
// DotNetFile.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2016 Daniel Köb
// Copyright (C) 2019 Stephen Shaw
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
using System.IO;
using System.Web;

using Hyena;

namespace FSpot.FileSystem
{
	public class DotNetFile : IFile
	{
		public bool Exists (SafeUri uri)
			=> File.Exists (uri.AbsolutePath);

		public bool IsSymlink (SafeUri uri)
		{
			// FIXME, this might return a false positive
			var pathInfo = new FileInfo (uri.AbsolutePath);
			return pathInfo.Attributes.HasFlag (FileAttributes.ReparsePoint);
		}

		public void Copy (SafeUri source, SafeUri destination, bool overwrite)
			=> File.Copy (source.AbsolutePath, destination.AbsolutePath, overwrite);

		public void Delete (SafeUri uri)
			=> File.Delete (uri.AbsolutePath);

		public string GetMimeType (SafeUri uri)
			=> MimeMapping.GetMimeMapping (uri.AbsolutePath);

		public DateTime GetMTime (SafeUri uri)
			=> File.GetLastWriteTime (uri.AbsolutePath);

		public long GetSize (SafeUri uri)
			=> new FileInfo (uri.AbsolutePath).Length;

		public Stream Read (SafeUri uri)
			=> new FileStream (uri.AbsolutePath, FileMode.Open, FileAccess.Read);
	}
}
