//
// LogicalTermTests.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using FSpot.Core;

using NUnit.Framework;

namespace FSpot.Query.Tests
{
	[TestFixture]
	public class LogicalTermTests
	{
		[Test]
		public void SomeTests ()
		{
			var c10 = new Category (null, 10, "tag10");
			var c11 = new Category (null, 11, "tag11");
			var c12 = new Category (c11, 12, "tag12");

			var t1 = new Tag (null, 1, "tag1");
			var t2 = new Tag (null, 2, "tag2");
			var t3 = new Tag (c10, 3, "tag3");
			var t4 = new Tag (c11, 4, "tag4");
			var t5 = new Tag (c12, 5, "tag5");

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