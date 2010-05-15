using System;
using System.Collections;
using System.Collections.Generic;
using GLib;

namespace FSpot.Utils
{
    public class RecursiveFileEnumerator : IEnumerable<File>
    {
        Uri root;
		bool recurse;

        public RecursiveFileEnumerator (Uri root) : this (root, true)
        {
        }

		public RecursiveFileEnumerator (Uri root, bool recurse)
		{
			this.root = root;
			this.recurse = recurse;
		}

        IEnumerable<File> ScanForFiles (File root)
        {
            var enumerator = root.EnumerateChildren ("standard::name,standard::type", FileQueryInfoFlags.None, null);
            foreach (FileInfo info in enumerator) {
                File file = root.GetChild (info.Name);
                
                if (info.FileType == FileType.Regular) {
                    yield return file;
                } else if (info.FileType == FileType.Directory && recurse) {
                    foreach (var child in ScanForFiles (file)) {
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
