/*
 * FSpot.Platform.Null.ThumnailFactory.cs
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
using System.Collections.Generic;
using Gdk;

namespace FSpot.Platform
{
	public class ThumbnailFactory
	{

		static Dictionary<Uri, Pixbuf> cache = new Dictionary<Uri, Pixbuf>();
		public static void SaveThumbnail (Pixbuf pixbuf, Uri image_uri)
		{
			cache[image_uri]=pixbuf.Clone() as Pixbuf;
		}

		public static void SaveThumbnail (Pixbuf pixbuf, Uri image_uri, DateTime original_mtime)
		{
			cache[image_uri]=pixbuf.Clone() as Pixbuf;
		}

		public static void DeleteThumbnail (Uri image_uri)
		{
			cache.Remove (image_uri);
		}

		public static void MoveThumbnail (Uri from_uri, Uri to_uri)
		{
			Pixbuf p;
			if (cache.TryGetValue (from_uri, out p))
				cache[to_uri] = p;
			cache.Remove (from_uri);
		}

		public static bool ThumbnailIsValid (Pixbuf pixbuf, Uri image_uri)
		{
			return cache.ContainsKey(image_uri);
		}

		public static bool ThumbnailIsValid (Pixbuf pixbuf, Uri image_uri, DateTime mtime)
		{
			return cache.ContainsKey(image_uri);
		}

		public static Pixbuf LoadThumbnail (Uri image_uri)
		{
			Pixbuf p;
			if (cache.TryGetValue (image_uri, out p))
				return p.Clone () as Pixbuf;

			return null;
		}

		public static Pixbuf LoadThumbnail (Uri image_uri, int dest_width, int dest_height)
		{
			return null;
		}

		public static bool ThumbnailExists (Uri image_uri)
		{
			return cache.ContainsKey(image_uri);
		}

		public static bool ThumbnailIsRecent (Uri image_uri)
		{
			return cache.ContainsKey(image_uri);
	}
}
