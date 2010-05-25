using System;
using System.Collections;
using System.Collections.Generic;
using GLib;

namespace FSpot.Utils
{
    public class RecursiveFileEnumerator : IEnumerable<File>
    {
        string root;
        bool recurse;
        bool catch_no_permission;

        public RecursiveFileEnumerator (string root) : this (root, true, false)
        {
        }

        public RecursiveFileEnumerator (string root, bool recurse) : this (root, recurse, false)
        {
        }

        public RecursiveFileEnumerator (string root, bool recurse, bool catch_no_permission)
        {
            this.root = root;
            this.recurse = recurse;
            this.catch_no_permission = catch_no_permission;
        }

        IEnumerable<File> ScanForFiles (File root)
        {
            GLib.FileInfo root_info = null;
            try {
                root_info = root.QueryInfo ("standard::name,standard::type", FileQueryInfoFlags.None, null);
            } catch (GLib.GException e) {
                if (!catch_no_permission)
                    throw e;
                yield break;
            }

             using (root_info) {
                if (root_info.FileType == FileType.Regular) {
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
            GLib.FileEnumerator enumerator = null;
            try {
                enumerator = root_dir.EnumerateChildren ("standard::name,standard::type", FileQueryInfoFlags.None, null);
            } catch (GLib.GException e) {
                if (!catch_no_permission)
                    throw e;
                yield break;
            }

            foreach (FileInfo info in enumerator) {
                File file = root_dir.GetChild (info.Name);
                
                // The code below looks like a duplication of ScanForFiles
                // (which could be invoked here instead), but doing so would
                // lead to a double type query on files (using QueryInfo).
                if (info.FileType == FileType.Regular) {
                    yield return file;
                } else if (info.FileType == FileType.Directory && recurse) {
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
