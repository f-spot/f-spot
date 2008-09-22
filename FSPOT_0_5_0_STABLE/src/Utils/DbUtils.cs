/*
 * FSpot.Utils.DbUtils.cs
 *
 * Author(s):
 *	Ettore Perazzoli <ettore@perazzoli.org>
 *	Larry Ewing <lewing@gnome.org>
 * 
 * This is free software. See COPYING for details.
 */

using System;

namespace FSpot.Utils
{
	public static class DbUtils {
#if USE_CORRECT_FUNCTION
		public static DateTime DateTimeFromUnixTime (long unix_time)
		{
			DateTime date_time = new DateTime (1970, 1, 1);
			return date_time.AddSeconds (unix_time).ToLocalTime ();
		}
	
		public static long UnixTimeFromDateTime (DateTime date_time)
		{
			return (long) (date_time.ToUniversalTime () - new DateTime (1970, 1, 1)).TotalSeconds;
		}
#else
		public static DateTime DateTimeFromUnixTime (long unix_time)
		{
			DateTime date_time = new DateTime (1970, 1, 1).ToLocalTime ();
			return date_time.AddSeconds (unix_time);
		}
		
		public static long UnixTimeFromDateTime (DateTime date_time)
		{
			return (long) (date_time - new DateTime (1970, 1, 1).ToLocalTime ()).TotalSeconds;
		}
#endif
	}
}
