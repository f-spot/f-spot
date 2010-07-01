/*
 * RollStore.cs
 *
 * Author(s)
 *	Bengt Thuree
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

using System;
using Hyena;

namespace FSpot
{
	public class Roll : DbItem
	{
		// The time is always in UTC.
		private DateTime time;
		public DateTime Time {
			get { return time; }
		}
	
		public Roll (uint id, long unix_time) : base (id)
		{
			time = DateTimeUtil.ToDateTime (unix_time);
		}
	}
}
