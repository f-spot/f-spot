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
		[DataRow (2020, 6, 15, 12, 30, 45)]
		[DataRow (2020, 1, 15, 12, 30, 45)]
		public void FromToSymmetry (int year, int month, int day, int hour, int minute, int second)
		{
			var local = new DateTime (year, month, day, hour, minute, second, DateTimeKind.Local);

			long unix = DateTimeUtil.FromDateTime (local);
			DateTime roundTrip = DateTimeUtil.ToDateTime (unix);

			Assert.AreEqual (DateTimeKind.Local, roundTrip.Kind);
			Assert.AreEqual (local, roundTrip);
		}
	}
}
