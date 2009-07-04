using NUnit.Framework;
namespace FSpot.Filters.Test
{
	[TestFixture]
	public class OrientationFilterTests : ImageTest {
		[Test]
		public void TestNoop ()
		{
			string path = CreateFile ("test.jpg", 50);
			FilterRequest req = new FilterRequest (path);
			IFilter filter = new OrientationFilter ();
			Assert.IsFalse (filter.Convert (req), "Orientation Filter changed a normal file");
		}
	}
}
