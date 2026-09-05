//  TestOf_AndTerm.cs
//  Author:
//       Stephen Shaw <sshaw@decriptor.com>
//  Copyright (c) 2011 sshaw
using FSpot.Query;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FSpot
{
	[TestClass]
	[TestCategory("RequiresGtk")]
	public class AndTermTests
	{
		//private AndTerm andTerm = null;
		AndTerm andTerm = null;

		[TestInitialize]
		public void OneTimeSetUp ()
		{
			andTerm = new AndTerm (null, null);
		}

		[TestCleanup]
		public void OneTimeTearDown ()
		{
			andTerm = null;
		}
	}
}