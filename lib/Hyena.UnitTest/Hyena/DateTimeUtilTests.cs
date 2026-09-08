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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyena.Tests
{
	[TestClass]
	public class DateTimeUtilTests
	{
		[TestMethod]
		public void FromToSymmetry ()
		{
			// FromDateTime only has precision to the second; strip the remaining ticks.
			DateTime now = DateTime.Now;
			now = now.Subtract (TimeSpan.FromTicks (now.Ticks % TimeSpan.TicksPerSecond));

			long time_t = DateTimeUtil.FromDateTime (now);
			DateTime now_t = DateTimeUtil.ToDateTime (time_t);

			Assert.AreEqual (DateTimeKind.Local, now.Kind);
			Assert.AreEqual (DateTimeKind.Local, now_t.Kind);
			Assert.AreEqual (now, now_t);
		}
	}
}
