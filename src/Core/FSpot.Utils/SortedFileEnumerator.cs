//
// SortedFileEnumerator.cs
//
// Author:
//   Daniel Köb <daniel.koeb@peony.at>
//
// Copyright (C) 2014 Daniel Köb
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
using GLib;
using System.Collections.Generic;

namespace FSpot.Utils
{
    public class SortedFileEnumerator
    {
        private readonly FileEnumerator baseEnumerator;
        private readonly List<FileInfo> files;
        private int currentFile;

        public SortedFileEnumerator (FileEnumerator baseEnumerator, bool catchErrors)
        {
            this.baseEnumerator = baseEnumerator;
            files = new List<FileInfo> ();
            currentFile = 0;

            while (true) {
                FileInfo info = null;
                try {
                    info = baseEnumerator.NextFile ();
                } catch (GException e) {
                    if (!catchErrors)
                        throw e;
                    continue;
                }
                if (info == null)
                    break;
                files.Add (info);
            }

            files.Sort((x, y) => {
                if (x.FileType == FileType.Directory)
                {
                    if (y.FileType == FileType.Regular)
                        return 1;
                    return x.Name.CompareTo(y.Name);
                }
                else {
                    if (y.FileType == FileType.Directory)
                        return -1;
                    return x.Name.CompareTo(y.Name);
                }
            });
        }

        public FileInfo NextFile ()
        {
            return currentFile >= files.Count ? null : files [currentFile++];
        }

        public bool Close (Cancellable cancellable)
        {
            return baseEnumerator.Close (cancellable);
        }
    }
}

