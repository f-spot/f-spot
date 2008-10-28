/*
 * FSpot.Platform.GnomeThumnailFactory.cs
 *
 * Author(s):
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * Copyright 2008 Novell, Inc
 *
 * This is free software, See COPYING for details
 */

using System;
using FSpot.Utils;
using Gdk;

namespace FSpot.Platform
{
	public class ThumbnailFactory
	{
		static Gnome.ThumbnailFactory gnome_thumbnail_factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Large);

		public static void SaveThumbnail (Pixbuf pixbuf, Uri image_uri)
		{
			if (pixbuf == null)
				throw new ArgumentNullException ("pixbuf");
			if (image_uri == null)
				throw new ArgumentNullException ("image_uri");

			Gnome.Vfs.FileInfo vfs = new Gnome.Vfs.FileInfo (image_uri.ToString ());
			DateTime mtime = vfs.Mtime;
			SaveThumbnail (pixbuf, image_uri, mtime);
		}

		public static void SaveThumbnail (Pixbuf pixbuf, Uri image_uri, DateTime original_mtime)
		{
			if (pixbuf == null)
				throw new ArgumentNullException ("pixbuf");
			if (image_uri == null)
				throw new ArgumentNullException ("image_uri");

			gnome_thumbnail_factory.SaveThumbnail (pixbuf, UriUtils.UriToStringEscaped (image_uri), original_mtime);
		}

		public static void DeleteThumbnail (Uri image_uri)
		{
			if (image_uri == null)
				throw new ArgumentNullException ("image_uri");
			try {
				if (System.IO.File.Exists (PathForUri (image_uri)))
					System.IO.File.Delete (PathForUri (image_uri));
			} catch (Exception e) {
				Log.DebugException (e);
			}
		}

		public static void MoveThumbnail (Uri from_uri, Uri to_uri)
		{
			if (from_uri == null)
				throw new ArgumentNullException ("from_uri");
			if (to_uri == null)
				throw new ArgumentNullException ("to_uri");
			System.IO.File.Move (PathForUri (from_uri), PathForUri (to_uri));
		}

		public static bool ThumbnailIsValid (Pixbuf pixbuf, Uri image_uri)
		{
			if (image_uri == null)
				throw new ArgumentNullException ("image_uri");

			try {
				Gnome.Vfs.FileInfo vfs = new Gnome.Vfs.FileInfo (image_uri.ToString ());
				DateTime mtime = vfs.Mtime;
				return ThumbnailIsValid (pixbuf, image_uri, mtime);
			} catch (System.IO.FileNotFoundException) {
				// If the original file is not on disk, the thumbnail is as valid as it's going to get
				return true;
			} catch (System.Exception e) {
				Log.DebugException (e);
				return false;
			}
		}

		public static bool ThumbnailIsValid (Pixbuf pixbuf, Uri image_uri, DateTime mtime)
		{
			if (pixbuf == null)
				throw new ArgumentNullException ("pixbuf");
			if (image_uri == null)
				throw new ArgumentNullException ("image_uri");

			try {
				return  Gnome.Thumbnail.IsValid (pixbuf, UriUtils.UriToStringEscaped (image_uri), mtime);
			} catch (System.IO.FileNotFoundException) {
				// If the original file is not on disk, the thumbnail is as valid as it's going to get
				return true;
			} catch (System.Exception e) {
				System.Console.WriteLine (e);
				return false;
			}
		}

		public static Pixbuf LoadThumbnail (Uri image_uri)
		{
			if (image_uri == null)
				throw new ArgumentNullException ("image_uri");
#if GSD_2_24
			if (System.IO.File.Exists (PathForUri (image_uri)))
				Utils.Unix.Touch (PathForUri (image_uri));
#endif
			try {
				return new Pixbuf (PathForUri (image_uri));
			} catch {
				return null;
			}
		}

		public static Pixbuf LoadThumbnail (Uri image_uri, int dest_width, int dest_height)
		{
			using (Pixbuf p = LoadThumbnail (image_uri)) {
				return Gnome.Thumbnail.ScaleDownPixbuf (p, dest_width, dest_height);
			}
		}

		public static bool ThumbnailExists (Uri image_uri)
		{
			return System.IO.File.Exists (PathForUri (image_uri));
		}

		public static bool ThumbnailIsRecent (Uri image_uri)
		{
			if (!image_uri.IsFile)
				Log.Debug ("FIXME: compute timestamp on non file uri too");

			return image_uri.IsFile && System.IO.File.Exists (PathForUri (image_uri)) && System.IO.File.GetLastWriteTime (PathForUri (image_uri)) >= System.IO.File.GetLastWriteTime (image_uri.AbsolutePath);
		}

		static string PathForUri (Uri uri)
		{
			return Gnome.Thumbnail.PathForUri (UriUtils.UriToStringEscaped (uri), Gnome.ThumbnailSize.Large);
		}
	}
}
