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

namespace FSpot.Utils
{
	public static class FileExtensions
	{
		public static bool CopyRecursive (this GLib.File source, GLib.File target, GLib.FileCopyFlags flags, GLib.Cancellable cancellable, GLib.FileProgressCallback callback)
		{
			bool result = true;
			try {
				result = result && source.Copy (target, flags, cancellable, callback);
			} catch (GLib.GException e) {
				// copy recursively, assuming that source is a directory
				// TODO: what is better: catching an exception each time a directory is copied or
				// checking if source is a directory and/or checking here if
				// error message == 'Can't recursively copy directory'?
				GLib.FileType ft = source.QueryFileType (GLib.FileQueryInfoFlags.None, null);
				if (ft.GetType () != GLib.FileType.Directory.GetType ())
					throw e;
				
				if (!target.Exists)
					target.MakeDirectoryWithParents (null);
				
				GLib.FileEnumerator fe = source.EnumerateChildren ("standard::*", GLib.FileQueryInfoFlags.None, null);
				GLib.FileInfo fi = fe.NextFile ();
				while (fi != null) {
					GLib.File source_file = GLib.FileFactory.NewForPath (Path.Combine (source.Path, fi.Name));
					GLib.File target_file = GLib.FileFactory.NewForPath (Path.Combine (target.Path, fi.Name));
					Log.Debug (String.Format (Catalog.GetString("Copying {0} -> {1}"), source_file.Path, target_file.Path));
					result = result && source_file.CopyRecursive(target_file, flags, cancellable, callback);
					fi = fe.NextFile ();
				}
				fe.Close (cancellable);
			}
			
			return result;
		}
	}
}
