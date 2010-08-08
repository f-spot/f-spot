/*
 * FSpot.Utils.FileExtensions.cs
 *
 * Author(s)
 * 	Paul Wellner Bou <paul@purecodes.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using Mono.Unix;
using GLib;
using Hyena;

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

			var children = file.EnumerateChildren ("standard::name", GLib.FileQueryInfoFlags.None, null);
			foreach (GLib.FileInfo child in children) {
				var child_file = FileFactory.NewForPath (Path.Combine (file.Path, child.Name));
				child_file.DeleteRecursive ();
			}
			file.Delete (null);
		}
	}
}
