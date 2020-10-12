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

using NUnit.Framework;
using FSpot.Core;

namespace FSpot.Query.Tests
{
	[TestFixture]
	public class LogicalTermTests
	{
		[Test]
		public void SomeTests ()
		{
			Category c10 = new Category (null, 10, "tag10");
			Category c11 = new Category (null, 11, "tag11");
			Category c12 = new Category (c11, 12, "tag12");

			Tag t1 = new Tag (null, 1, "tag1");
			Tag t2 = new Tag (null, 2, "tag2");
			Tag t3 = new Tag (c10, 3, "tag3");
			Tag t4 = new Tag (c11, 4, "tag4");
			Tag t5 = new Tag (c12, 5, "tag5");

			TagTerm tt10 = new TagTerm (c10);
			TagTerm tt11 = new TagTerm (c11);
			TagTerm tt12 = new TagTerm (c12);

			TagTerm tt1 = new TagTerm (t1);
			TagTerm tt2 = new TagTerm (t2);
			TagTerm tt3 = new TagTerm (t3);
			TagTerm tt4 = new TagTerm (t4);
			TagTerm tt5 = new TagTerm (t5);

			object [] tests = {
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id = 1)) ", tt1,
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (2, 3))) ", new OrOperator (tt2, tt3),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (3, 4, 5))) ", new OrOperator (tt3, tt4, tt5),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (10, 3))) ", new OrOperator (tt10),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (10, 3, 3))) ", new OrOperator (tt10, tt3),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (11, 12, 5, 4))) ", new OrOperator (tt11),
			};
	
			for (int i=0; i < tests.Length; i+=2) {
				//System.Console.WriteLine ((tests[i+1] as LogicalTerm).SqlClause ());
				//System.Console.WriteLine (tests[i]);
				Assert.AreEqual (tests[i] as string, (tests[i+1] as LogicalTerm).SqlClause ());
			}
		}
	}
}