#if ENABLE_NUNIT
using NUnit.Framework;
#endif
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests : ImageTest {
			[Test]
			public void TestNoop ()
			{
				string path = CreateFile ("test.jpg", 50);
				FilterRequest req = new FilterRequest (path);
				IFilter filter = new OrientationFilter ();
				Assert.IsFalse (filter.Convert (req), "Orientation Filter changed a normal file");
			}
		}
#endif

