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

namespace Hyena
{
	public static class DateTimeUtil
	{
		// FIXME I don't think having a local-unix-epoch makes any sense, I think we should be using
		// all UTC values.  Depending on the time of year in daylight savings timezones, the
		// local-seconds-since-epoch value will differ, which will cause errors, no?
		static readonly DateTime LocalUnixEpoch = new DateTime (1970, 1, 1).ToLocalTime ();

		const long SecondsBeforeEpochTreatedAsMinValue = -15768000000; // 500 yrs before epoch

		public static DateTime ToDateTime (long time)
		{
			return time <= SecondsBeforeEpochTreatedAsMinValue ? DateTime.MinValue : LocalUnixEpoch.AddSeconds (time);
		}

		public static long FromDateTime (DateTime time)
		{
			return (long)time.Subtract (LocalUnixEpoch).TotalSeconds;
		}
	}
}