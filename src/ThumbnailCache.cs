/*
 * FSpot.ThumbnailCache.cs
 *
 * Author(s):
 * 	Ettore Perazzoli
 *	Larry Ewing  <lewing@novell.com>
 *	Staphen Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.Collections;
using Gdk;

using FSpot.Utils;

namespace FSpot
{
public class ThumbnailCache : IDisposable {

	// Types.

	private class Thumbnail {
		// Uri of the image source
		public Uri uri;

		// The uncompressed thumbnail.
		public Pixbuf pixbuf;
	}


	// Private members and constants

	private const int DEFAULT_CACHE_SIZE = 2;

	private int max_count;
	private ArrayList pixbuf_mru;
	private Hashtable pixbuf_hash = new Hashtable ();

	static private ThumbnailCache defaultcache = new ThumbnailCache (DEFAULT_CACHE_SIZE);


	// Public API

	public ThumbnailCache (int max_count)
	{
		this.max_count = max_count;
		pixbuf_mru = new ArrayList (max_count);
	}

	static public ThumbnailCache Default {
		get {
			return defaultcache;
		}
	}

	public void AddThumbnail (Uri uri, Pixbuf pixbuf)
	{
		Thumbnail thumbnail = new Thumbnail ();

		thumbnail.uri = uri;
		thumbnail.pixbuf = pixbuf;

		RemoveThumbnailForUri (uri);

		pixbuf_mru.Insert (0, thumbnail);
		pixbuf_hash.Add (uri, thumbnail);

		MaybeExpunge ();
	}

	public Pixbuf GetThumbnailForUri (Uri uri)
	{
		if (! pixbuf_hash.ContainsKey (uri))
			return null;

		Thumbnail item = pixbuf_hash [uri] as Thumbnail;

		pixbuf_mru.Remove (item);
		pixbuf_mru.Insert (0, item);

		return PixbufUtils.ShallowCopy (item.pixbuf);
	}

	public void RemoveThumbnailForUri (Uri uri)
	{
		if (! pixbuf_hash.ContainsKey (uri))
			return;

		Thumbnail item = pixbuf_hash [uri] as Thumbnail;

		pixbuf_hash.Remove (uri);
		pixbuf_mru.Remove (item);

		item.pixbuf.Dispose ();
	}

	public void Dispose ()
	{
		foreach (object item in pixbuf_mru) {
			Thumbnail thumb = item as Thumbnail;
			pixbuf_hash.Remove (thumb.uri);
			thumb.pixbuf.Dispose ();
		}
		pixbuf_mru.Clear ();
		System.GC.SuppressFinalize (this);
	}

	~ThumbnailCache ()
	{
		Log.Debug ("Finalizer called on {0}. Should be Disposed", GetType ());		
		foreach (object item in pixbuf_mru) {
			Thumbnail thumb = item as Thumbnail;
			pixbuf_hash.Remove (thumb.uri);
			thumb.pixbuf.Dispose ();
		}
		pixbuf_mru.Clear ();
	}

	// Private utility methods.

	private void MaybeExpunge ()
	{
		while (pixbuf_mru.Count > max_count) {
			Thumbnail thumbnail = pixbuf_mru [pixbuf_mru.Count - 1] as Thumbnail;

			pixbuf_hash.Remove (thumbnail.uri);
			pixbuf_mru.RemoveAt (pixbuf_mru.Count - 1);

			thumbnail.pixbuf.Dispose ();
		}
	}
}
}
