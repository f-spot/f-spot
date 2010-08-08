/*
 * FSpot.Utils.UriExtensions.cs
 *
 * Author(s)
 * 	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot.Utils
{
	
	public static class UriExtensions
	{
		public static Uri GetDirectoryUri (this Uri uri)
		{
			UriBuilder builder = new UriBuilder (uri);
			builder.Path =
				String.Format ("{0}/", System.IO.Path.GetDirectoryName (uri.AbsolutePath));
			
			return builder.Uri;
		}
		
		public static string GetFilename (this Uri uri)
		{
			return System.IO.Path.GetFileName (uri.AbsolutePath);
		}
	}
}
