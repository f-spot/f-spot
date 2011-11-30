//  TestOf_AndTerm.cs
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//  Copyright (c) 2011 sshaw
using System;
using NUnit.Framework;
using FSpot;

namespace FSpot
{
	[TestFixture]
	public class TestOf_AndTerm
	{
		//private AndTerm andTerm = null;
		private AndTerm andTerm = null;

		[TestFixtureSetUp]
		public void TestFixtureSetUp ()
		{
			andTerm = new AndTerm (null, null);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			andTerm = null;
		}

		[Test]
		public void TestCase ()
		{
			Assert.AreEqual(0, 0);
		}
	}
}