//
// FileExtensions.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Paul Wellner Bou <paul@purecodes.org>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2010 Paul Wellner Bou
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

namespace FSpot.Utils
{
	public static class FileExtensions
	{
		public static bool CopyRecursive (this GLib.File source, GLib.File target, GLib.FileCopyFlags flags, GLib.Cancellable cancellable, GLib.FileProgressCallback callback)
		{
			bool result = true;
			
			GLib.FileType ft = source.QueryFileType (GLib.FileQueryInfoFlags.None, cancellable);
			
			if (ft != GLib.FileType.Directory) {
				Hyena.Log.DebugFormat ("Copying \"{0}\" to \"{1}\"", source.Path, target.Path);
				return source.Copy (target, flags, cancellable, callback);
			}
			
			if (!target.Exists) {
				Hyena.Log.DebugFormat ("Creating directory: \"{0}\"", target.Path);
				result = result && target.MakeDirectoryWithParents (cancellable);
			}
			
			GLib.FileEnumerator fe = source.EnumerateChildren ("standard::name", GLib.FileQueryInfoFlags.None, cancellable);
			GLib.FileInfo fi = fe.NextFile ();
			while (fi != null) {
				GLib.File source_file = GLib.FileFactory.NewForPath (Path.Combine (source.Path, fi.Name));
				GLib.File target_file = GLib.FileFactory.NewForPath (Path.Combine (target.Path, fi.Name));
				result = result && source_file.CopyRecursive(target_file, flags, cancellable, callback);
				fi = fe.NextFile ();
			}
			fe.Close (cancellable);
			fe.Dispose ();
			return result;
		}

		public static void DeleteRecursive (this GLib.File file)
		{
			// FIXME: no cancellation support

			var type = file.QueryFileType (FileQueryInfoFlags.None, null);
			if (type != FileType.Directory) {
				file.Delete (null);
				return;
			}

			using (var children = file.EnumerateChildren ("standard::name", GLib.FileQueryInfoFlags.None, null)) {
				foreach (GLib.FileInfo child in children) {
					var child_file = FileFactory.NewForPath (Path.Combine (file.Path, child.Name));
					child_file.DeleteRecursive ();
				}
			}
			file.Delete (null);
		}
	}
}
