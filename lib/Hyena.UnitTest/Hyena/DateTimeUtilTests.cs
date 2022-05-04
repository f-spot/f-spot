//
// DateTimeUtilTests.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//
// Copyright (c) 2010 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System;

using NUnit.Framework;

namespace Hyena.Tests
{
	[TestFixture]
	public class DateTimeUtilTests
	{
		[Test]
		public void InvariantString ()
		{
			// Tests are run in Chicago timezone, UTC -6 in the winter, -5 in the summer
			TestInv ("2010-02-18 02:41:00.000 -06:00", new DateTime (2010, 2, 18, 2, 41, 0, 0));
			TestInv ("2010-02-18 02:41:50.123 -06:00", new DateTime (2010, 2, 18, 2, 41, 50, 123));
			TestInv ("2010-10-18 02:01:00.000 -05:00", new DateTime (2010, 10, 18, 2, 1, 0, 0));
		}

		void TestInv (string inv_string, DateTime dt)
		{
			// Make sure we can generate the given string from the DateTime
			Assert.AreEqual (inv_string, DateTimeUtil.ToInvariantString (dt));

			// And vice versa
			if (DateTimeUtil.TryParseInvariant (inv_string, out var parsed_dt))
				Assert.AreEqual (dt, parsed_dt);
			else
				Assert.Fail (string.Format ("TryParseInvariant failed on {0}", inv_string));
		}

		[Test]
		public void FromToSymmetry ()
		{
			// ToTimeT only has precision to the second; so strip off the remainding ticks
			DateTime now = DateTime.Now;
			now = now.Subtract (TimeSpan.FromTicks (now.Ticks % TimeSpan.TicksPerSecond));

			long time_t = DateTimeUtil.ToTimeT (now);
			DateTime now_t = DateTimeUtil.FromTimeT (time_t);

			Assert.AreEqual (DateTimeKind.Local, now.Kind);
			Assert.AreEqual (DateTimeKind.Local, now_t.Kind);
			Assert.AreEqual (now, now_t);
		}
	}
}
