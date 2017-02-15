//  TestOf_AndTerm.cs
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//  Copyright (c) 2011 sshaw
using NUnit.Framework;
using FSpot.Query;

namespace FSpot
{
	[TestFixture]
	public class AndTermTests
	{
		//private AndTerm andTerm = null;
		AndTerm andTerm = null;

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
	}
}