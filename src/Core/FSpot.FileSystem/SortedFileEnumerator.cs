//
// SortedFileEnumerator.cs
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

using GLib;
using System.Collections.Generic;
using System.Collections;

namespace FSpot.FileSystem
{
	public class SortedFileEnumerator : IEnumerable<FileInfo>
	{
		readonly List<FileInfo> files;

		public SortedFileEnumerator (IEnumerator baseEnumerator, bool catchErrors)
		{
			files = new List<FileInfo> ();

			while (true) {
				try {
					if (!baseEnumerator.MoveNext ()) {
						break;
					}
				} catch (GException e) {
					if (!catchErrors)
						throw e;
					continue;
				}
				files.Add ((FileInfo)baseEnumerator.Current);
			}

			files.Sort ((x, y) => {
				if (x.FileType == FileType.Directory) {
					if (y.FileType == FileType.Regular)
						return 1;
					return string.Compare (x.Name, y.Name, System.StringComparison.Ordinal);
				}
				if (y.FileType == FileType.Directory)
					return -1;
				return string.Compare (x.Name, y.Name, System.StringComparison.Ordinal);
			});
		}

		#region IEnumerable implementation

		public IEnumerator<FileInfo> GetEnumerator ()
		{
			return files.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return files.GetEnumerator ();
		}

		#endregion
	}
}
