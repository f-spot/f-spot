#if ENABLE_TESTS
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
			Tag t1 = new Tag (null, 1, "tag1");
			Tag t2 = new Tag (null, 2, "tag2");
			Tag t3 = new Tag (null, 3, "tag3");
			Tag t4 = new Tag (null, 4, "tag4");
			Tag t5 = new Tag (null, 5, "tag5");

			TagTerm tt1 = new TagTerm (t1);
			TagTerm tt2 = new TagTerm (t2);
			TagTerm tt3 = new TagTerm (t3);
			TagTerm tt4 = new TagTerm (t4);
			TagTerm tt5 = new TagTerm (t5);

			object [] tests = {
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id = 1)) ", tt1,
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (2, 3))) ", new OrTerm (tt2, tt3),
				" (photos.id IN (SELECT photo_id FROM photo_tags WHERE tag_id IN (3, 4, 5))) ", new OrTerm (tt3, tt4, tt5),
				
			};
	
			for (int i=0; i < tests.Length; i+=2) {
				//System.Console.WriteLine ((tests[i+1] as LogicalTerm).SqlClause ());
				//System.Console.WriteLine (tests[i]);
				Assert.AreEqual (tests[i] as string, (tests[i+1] as LogicalTerm).SqlClause ());
			}
		}
	}
}
#endif
