#if ENABLE_NUNIT
using NUnit.Framework;
using Gdk;
using System.IO;

namespace FSpot.Filters.Tests
{
	[TestFixture]
	public class ColorFilterTests : ImageTest
	{
		[Test]
		public void GoGoGadgetStretch ()
		{
			Process ("autostretch.png");
		}

#if false
		[Test]
		public void StretchRealFile ()
		{
			string path = "/home/lewing/Desktop/img_0081.jpg";
			FilterRequest req = new FilterRequest (path);
			FilterSet set = new FilterSet ();
			set.Add (new AutoStretchFilter ());
			set.Add (new UniqueNameFilter (Path.GetDirectoryName (path)));
			set.Convert (req);
			req.Preserve (req.Current);
		}
#endif

		public void Process (string name)
		{
			string path = CreateFile (name, 120);
			using (FilterRequest req = new FilterRequest (path)) {
				IFilter filter = new AutoStretchFilter ();
				Assert.IsTrue (filter.Convert (req), "Filter failed to operate");
				Assert.IsTrue (System.IO.File.Exists (req.Current.LocalPath),
					       "Error: Did not create " + req.Current.LocalPath);
				Assert.IsTrue (new FileInfo (req.Current.LocalPath).Length > 0,
					       "Error: " + req.Current.LocalPath + "is Zero length");
				//req.Preserve (req.Current);
				using (ImageFile img = ImageFile.Create (req.Current)) {
					Pixbuf pixbuf = img.Load ();
					Assert.IsNotNull (pixbuf);
				}
			}
		}
	}
}
#endif	

