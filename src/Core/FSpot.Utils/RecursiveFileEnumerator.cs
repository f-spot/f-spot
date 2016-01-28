//
// RecursiveFileEnumerator.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
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

using System.Collections;
using System.Collections.Generic;

using GLib;

namespace FSpot.Utils
{
    public class RecursiveFileEnumerator : IEnumerable<File>
    {
        string root;

        public bool Recurse { get; set; }
        public bool CatchErrors { get; set; }
        public bool IgnoreSymlinks { get; set; }

        public RecursiveFileEnumerator (string root)
        {
            this.root = root;
            Recurse = true;
            CatchErrors = false;
            IgnoreSymlinks = false;
        }

        IEnumerable<File> ScanForFiles (File root)
        {
            FileInfo root_info = null;
            try {
                root_info = root.QueryInfo ("standard::name,standard::type,standard::is-symlink", FileQueryInfoFlags.None, null);
            } catch (GException e) {
                if (!CatchErrors)
                    throw e;
                yield break;
            }

            using (root_info) {
                if (root_info.IsSymlink && IgnoreSymlinks) {
                    yield break;
                } else if (root_info.FileType == FileType.Regular) {
                    yield return root;
                } else if (root_info.FileType == FileType.Directory) {
                    foreach (var child in ScanDirectoryForFiles (root)) {
                        yield return child;
                    }
                }
            }
        }

        IEnumerable<File> ScanDirectoryForFiles (File root_dir)
        {
            FileEnumerator enumerator = null;
            try {
                enumerator = root_dir.EnumerateChildren ("standard::name,standard::type,standard::is-symlink", FileQueryInfoFlags.None, null);
            } catch (GException e) {
                if (!CatchErrors)
                    throw e;
                yield break;
            }

            while (true) {
                FileInfo info = null;
                try {
                    info = enumerator.NextFile ();
                } catch (GException e) {
                    if (!CatchErrors)
                        throw e;
                    continue;
                }

                if (info == null)
                    break;

                File file = root_dir.GetChild (info.Name);
                
                // The code below looks like a duplication of ScanForFiles
                // (which could be invoked here instead), but doing so would
                // lead to a double type query on files (using QueryInfo).
                if (info.IsSymlink && IgnoreSymlinks) {
                    continue;
                }

		if (info.FileType == FileType.Regular) {
                    yield return file;
                } else if (info.FileType == FileType.Directory && Recurse) {
                    foreach (var child in ScanDirectoryForFiles (file)) {
                        yield return child;
                    }
                }
                info.Dispose ();
            }

            enumerator.Close (null);
        }

        public IEnumerator<File> GetEnumerator ()
        {
            var file = FileFactory.NewForUri (root);
            return ScanForFiles (file).GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }
    }
}
