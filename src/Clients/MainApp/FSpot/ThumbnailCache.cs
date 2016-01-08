//
// ThumbnailCache.cs
//
// Author:
//   Ettore Perazzoli <ettore@src.gnome.org>
//   Larry Ewing <lewing@novell.com>
//   Stephane Delcroix <sdelcroix@novell.com>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2003-2008 Novell, Inc.
// Copyright (C) 2003 Ettore Perazzoli
// Copyright (C) 2004-2005 Larry Ewing
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2013 Stephen Shaw
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
using System.Collections.Generic;
using Gdk;

using Hyena;

using FSpot.Utils;

namespace FSpot
{
	public class ThumbnailCache : IDisposable {
	
		#region Types
		class Thumbnail {
			// Uri of the image source
			public SafeUri uri;
	
			// The uncompressed thumbnail.
			public Pixbuf pixbuf;
		}
		#endregion
	
		#region Private members and constants
		const int DEFAULT_CACHE_SIZE = 2;
		int max_count;
		List<Thumbnail> pixbuf_mru;
		Dictionary<SafeUri,Thumbnail> pixbuf_hash;
		static ThumbnailCache defaultcache = new ThumbnailCache (DEFAULT_CACHE_SIZE);
		#endregion
	
		#region Public API
		public ThumbnailCache (int max_count)
		{
			this.max_count = max_count;
			pixbuf_mru = new List<Thumbnail> (max_count);
			pixbuf_hash = new Dictionary<SafeUri, Thumbnail> ();
		}
	
		static public ThumbnailCache Default {
			get {
				return defaultcache;
			}
		}
	
		public void AddThumbnail (SafeUri uri, Pixbuf pixbuf)
		{
			Thumbnail thumbnail = new Thumbnail ();
	
			thumbnail.uri = uri;
			thumbnail.pixbuf = pixbuf;
	
			RemoveThumbnailForUri (uri);
	
			pixbuf_mru.Insert (0, thumbnail);
			pixbuf_hash.Add (uri, thumbnail);
	
			MaybeExpunge ();
		}
	
		public Pixbuf GetThumbnailForUri (SafeUri uri)
		{
			if (! pixbuf_hash.ContainsKey (uri))
				return null;
	
			Thumbnail item = pixbuf_hash [uri];
	
			pixbuf_mru.Remove (item);
			pixbuf_mru.Insert (0, item);
	
	        return item.pixbuf == null ? null : item.pixbuf.ShallowCopy ();
		}
	
		public void RemoveThumbnailForUri (SafeUri uri)
		{
			if (! pixbuf_hash.ContainsKey (uri))
				return;
	
			Thumbnail item = pixbuf_hash [uri];
	
			pixbuf_hash.Remove (uri);
			pixbuf_mru.Remove (item);
	
			item.pixbuf.Dispose ();
		}
		#endregion
	
		public void Dispose ()
		{
			Dispose (true);
			System.GC.SuppressFinalize (this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				foreach (var item in pixbuf_mru) {
					Thumbnail thumb = item;
					pixbuf_hash.Remove (thumb.uri);
					thumb.pixbuf.Dispose ();
				}
				pixbuf_mru.Clear ();
			}
			else
			{
				foreach (var item in pixbuf_mru) {
					Thumbnail thumb = item;
					pixbuf_hash.Remove (thumb.uri);
					thumb.pixbuf.Dispose ();
				}
				pixbuf_mru.Clear ();
			} 

		}

		~ThumbnailCache ()
		{
			Dispose (false);
		}
	
		#region Private utility methods.
		void MaybeExpunge ()
		{
			while (pixbuf_mru.Count > max_count) {
				Thumbnail thumbnail = pixbuf_mru [pixbuf_mru.Count - 1];
	
				pixbuf_hash.Remove (thumbnail.uri);
				pixbuf_mru.RemoveAt (pixbuf_mru.Count - 1);
	
				thumbnail.pixbuf.Dispose ();
			}
		}
		#endregion
	}
}
