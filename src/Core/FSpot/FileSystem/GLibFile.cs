//
// GLibFile.cs
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

using System.IO;
using GLib;
using Hyena;

namespace FSpot.FileSystem
{
	class GLibFile : IFile
	{
		#region IFile implementation

		public bool Exists (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			return file.Exists && file.QueryFileType (FileQueryInfoFlags.None, null) == FileType.Regular;
		}

		public bool IsSymlink (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			using (var root_info = file.QueryInfo ("standard::is-symlink", FileQueryInfoFlags.None, null)) {
				return root_info.IsSymlink;
			}
		}

		public void Copy (SafeUri source, SafeUri destination, bool overwrite)
		{
			var source_file = FileFactory.NewForUri (source);
			var destination_file = FileFactory.NewForUri (destination);
			var flags = FileCopyFlags.AllMetadata;
			if (overwrite)
				flags |= FileCopyFlags.Overwrite;
			source_file.Copy (destination_file, flags, null, null);
		}

		public void Delete (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			file.Delete ();
		}

		public string GetMimeType (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			using (var info = file.QueryInfo ("standard::content-type", FileQueryInfoFlags.None, null)) {
				return info.ContentType;
			}
		}

		public ulong GetMTime (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			using (var info = file.QueryInfo ("time::modified", FileQueryInfoFlags.None, null)) {
				return info.GetAttributeULong ("time::modified");
			}
		}

		public long GetSize (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			using (var info = file.QueryInfo ("standard::size", FileQueryInfoFlags.None, null)) {
				return info.Size;
			}
		}

		public Stream Read (SafeUri uri)
		{
			var file = FileFactory.NewForUri (uri);
			return new GioStream (file.Read (null));
		}

		#endregion
	}
}

