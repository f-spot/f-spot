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
using System.Text.RegularExpressions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyena.Tests
{
	[TestClass]
	public class DateTimeUtilTests
	{
		[TestMethod]
		public void InvariantString ()
		{
			TestInv (new DateTime (2010, 2, 18, 2, 41, 0, 0));
			TestInv (new DateTime (2010, 2, 18, 2, 41, 50, 123));
			TestInv (new DateTime (2010, 10, 18, 2, 1, 0, 0));
		}

		void TestInv (DateTime dt)
		{
			string inv_string = DateTimeUtil.ToInvariantString (dt);
			Assert.IsTrue (Regex.IsMatch (inv_string, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}$"));

			if (DateTimeUtil.TryParseInvariant (inv_string, out var parsed_dt))
				Assert.AreEqual (dt, parsed_dt);
			else
				Assert.Fail (string.Format ("TryParseInvariant failed on {0}", inv_string));
		}

		[TestMethod]
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
