//
// Utilities.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Hyena
{
	public class DateTimeUtil
	{
		// FIXME I don't think having a local-unix-epoch makes any sense, I think we should be using
		// all UTC values.  Depending on the time of year in daylight savings timezones, the
		// local-seconds-since-epoch value will differ, which will cause errors, no?
		public static readonly DateTime LocalUnixEpoch = new DateTime (1970, 1, 1).ToLocalTime ();

		public static DateTime ToDateTime (long time)
		{
			return FromTimeT (time);
		}

		public static long FromDateTime (DateTime time)
		{
			return ToTimeT (time);
		}

		static long super_ugly_min_hack = -15768000000; // 500 yrs before epoch...ewww
		public static DateTime FromTimeT (long time)
		{
			return (time <= super_ugly_min_hack) ? DateTime.MinValue : LocalUnixEpoch.AddSeconds (time);
		}

		public static long ToTimeT (DateTime time)
		{
			return (long)time.Subtract (LocalUnixEpoch).TotalSeconds;
		}

		public static string FormatDuration (long time)
		{
			return FormatDuration (TimeSpan.FromSeconds (time));
		}

		public static string FormatDuration (TimeSpan time)
		{
			return FormatDuration (time.Hours, time.Minutes, time.Seconds);
		}

		public static string FormatDuration (int hours, int minutes, int seconds)
		{
			return (hours > 0 ?
					string.Format ("{0}:{1:00}:{2:00}", hours, minutes, seconds) :
					string.Format ("{0}:{1:00}", minutes, seconds));
		}

		const string INVARIANT_FMT = "yyyy-MM-dd HH:mm:ss.fff zzz";
		public static string ToInvariantString (DateTime dt)
		{
			return dt.ToString (INVARIANT_FMT, CultureInfo.InvariantCulture);
		}

		public static bool TryParseInvariant (string str, out DateTime dt)
		{
			return DateTime.TryParseExact (str, INVARIANT_FMT, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
		}
	}
}