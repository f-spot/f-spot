//
// LogicalTermTests.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Models;

using NUnit.Framework;

namespace FSpot.Query.Tests
{
	[TestFixture]
	public class LogicalTermTests
	{
		[Test]
		public void SomeTests ()
		{
			var c10 = new Tag (null, new Guid (), "tag10");
			var c11 = new Tag (null, new Guid (), "tag11");
			var c12 = new Tag (c11, new Guid (), "tag12");

			var t1 = new Tag (null, new Guid (), "tag1");
			var t2 = new Tag (null, new Guid (), "tag2");
			var t3 = new Tag (c10, new Guid (), "tag3");
			var t4 = new Tag (c11, new Guid (), "tag4");
			var t5 = new Tag (c12, new Guid (), "tag5");

			var tt10 = new TagTerm (c10);
			var tt11 = new TagTerm (c11);
			var tt12 = new TagTerm (c12);

			var tt1 = new TagTerm (t1);
			var tt2 = new TagTerm (t2);
			var tt3 = new TagTerm (t3);
			var tt4 = new TagTerm (t4);
			var tt5 = new TagTerm (t5);

			object[] tests = {
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id = 1)) ", tt1,
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (2, 3))) ", new OrOperator (tt2, tt3),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (3, 4, 5))) ", new OrOperator (tt3, tt4, tt5),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (10, 3))) ", new OrOperator (tt10),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (10, 3, 3))) ", new OrOperator (tt10, tt3),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (11, 12, 5, 4))) ", new OrOperator (tt11),
			};

			for (int i = 0; i < tests.Length; i += 2) {
				//System.Console.WriteLine ((tests[i+1] as LogicalTerm).SqlClause ());
				//System.Console.WriteLine (tests[i]);
				Assert.AreEqual (tests[i] as string, (tests[i + 1] as LogicalTerm).SqlClause ());
			}
		}
	}
}