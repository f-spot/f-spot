/*
 * FSpot.Platform.Gnome.ThumnailFactory.cs
 *
 * Author(s):
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright 2008 Novell, Inc
 *
 * This is free software, See COPYING for details
 */

using Hyena;
using System;
using FSpot.Utils;
using Gdk;

namespace FSpot.Platform
{
	public static class ThumbnailFactory
	{
		static Gnome.ThumbnailFactory gnome_thumbnail_factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);

		public static void SaveThumbnail (Pixbuf pixbuf, SafeUri imageUri)
		{
			if (pixbuf == null)
				throw new ArgumentNullException ("pixbuf");
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");

			GLib.File gfile = GLib.FileFactory.NewForUri (imageUri);
			GLib.FileInfo info = gfile.QueryInfo ("time::modified", GLib.FileQueryInfoFlags.None, null);
			DateTime mtime = Mono.Unix.Native.NativeConvert.ToDateTime ((long)info.GetAttributeULong ("time::modified"));

			SaveThumbnail (pixbuf, imageUri, mtime);
		}

		public static void SaveThumbnail (Pixbuf pixbuf, SafeUri imageUri, DateTime originalMtime)
		{
			if (pixbuf == null)
				throw new ArgumentNullException ("pixbuf");
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");

			gnome_thumbnail_factory.SaveThumbnail (pixbuf, imageUri, originalMtime);
		}

		public static void DeleteThumbnail (SafeUri imageUri)
		{
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");
			try {
				if (System.IO.File.Exists (PathForUri (imageUri)))
					System.IO.File.Delete (PathForUri (imageUri));
			} catch (Exception e) {
				Log.DebugException (e);
			}
		}

		public static void MoveThumbnail (SafeUri fromUri, SafeUri toUri)
		{
			if (fromUri == null)
				throw new ArgumentNullException ("fromUri");
			if (toUri == null)
				throw new ArgumentNullException ("toUri");
			System.IO.File.Move (PathForUri (fromUri), PathForUri (toUri));
		}

		public static bool ThumbnailIsValid (Pixbuf pixbuf, SafeUri imageUri)
		{
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");

			try {
				GLib.File gfile = GLib.FileFactory.NewForUri (imageUri);
				if (!gfile.Exists)
					return true;
				GLib.FileInfo info = gfile.QueryInfo ("time::modified", GLib.FileQueryInfoFlags.None, null);
				DateTime mtime = Mono.Unix.Native.NativeConvert.ToDateTime ((long)info.GetAttributeULong ("time::modified"));
				return ThumbnailIsValid (pixbuf, imageUri, mtime);
			} catch (System.IO.FileNotFoundException) {
				// If the original file is not on disk, the thumbnail is as valid as it's going to get
				return true;
			} catch (System.Exception e) {
				Log.DebugException (e);
				return false;
			}
		}

		public static bool ThumbnailIsValid (Pixbuf pixbuf, SafeUri imageUri, DateTime mtime)
		{
			if (pixbuf == null)
				throw new ArgumentNullException ("pixbuf");
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");

			try {
				return  Gnome.Thumbnail.IsValid (pixbuf, imageUri, mtime);
			} catch (System.IO.FileNotFoundException) {
				// If the original file is not on disk, the thumbnail is as valid as it's going to get
				return true;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				return false;
			}
		}

		public static Pixbuf LoadThumbnail (SafeUri imageUri)
		{
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");
#if GSD_2_24
			if (System.IO.File.Exists (PathForUri (imageUri)))
				Utils.Unix.Touch (PathForUri (imageUri));
#endif
			try {
				return new Pixbuf (PathForUri (imageUri));
			} catch {
				return null;
			}
		}

		public static Pixbuf LoadThumbnail (SafeUri imageUri, int destWidth, int destHeight)
		{
			using (Pixbuf p = LoadThumbnail (imageUri)) {
				return Gnome.Thumbnail.ScaleDownPixbuf (p, destWidth, destHeight);
			}
		}

		public static bool ThumbnailExists (SafeUri imageUri)
		{
			return System.IO.File.Exists (PathForUri (imageUri));
		}

		public static bool ThumbnailIsRecent (SafeUri imageUri)
		{
			if (imageUri == null)
				throw new ArgumentNullException ("imageUri");

			if (!imageUri.IsFile)
				Log.Debug ("FIXME: compute timestamp on non file uri too");

			if (!System.IO.File.Exists (imageUri.AbsolutePath))
				return true;

			return imageUri.IsFile && System.IO.File.Exists (PathForUri (imageUri)) && System.IO.File.GetLastWriteTime (PathForUri (imageUri)) >= System.IO.File.GetLastWriteTime (imageUri.AbsolutePath);
		}

		static string PathForUri (SafeUri uri)
		{
			return Gnome.Thumbnail.PathForUri (uri, Gnome.ThumbnailSize.Large);
		}
	}
}
