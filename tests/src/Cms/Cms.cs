using NUnit.Framework;

namespace Cms.Tests
{
	[TestFixture]
	public class ProfileTests {
		[Test]
		public void LoadSave ()
		{
			Profile srgb = Profile.CreateStandardRgb ();
			byte [] data = srgb.Save ();
			Assert.IsNotNull (data);
			Profile result = new Profile (data);
			Assert.AreEqual (result.ProductName, srgb.ProductName);
			Assert.AreEqual (result.ProductDescription, srgb.ProductDescription);
			Assert.AreEqual (result.Model, srgb.Model);
		}
	}

	[TestFixture]
	public class GammaTableTests {
		[Test]
		public void TestAlloc ()
		{
			ushort [] values = new ushort [] { 0, 0x00ff, 0xffff };
			GammaTable t = new GammaTable (values);
			for (int i = 0; i < values.Length; i++) {
				Assert.AreEqual (t[i], values [i]);
			}
		} 
	}

	[TestFixture]
	public class ColorCIExyYTests
	{
		[Test]
		public void TestTempTable1000 ()
		{
			ColorCIExyY wp = ColorCIExyY.WhitePointFromTemperature (1000);
			Assert.AreEqual (0.652756059, wp.x);
			Assert.AreEqual (0.344456906, wp.y);
		}

		[Test]
		public void TestTempReader ()
		{
			for (int i = 1000; i <= 25000; i += 10000)
				ColorCIExyY.WhitePointFromTemperature (i);
		}
		
		[Test]
		public void TestTempTable10000 ()
		{
			ColorCIExyY wp = ColorCIExyY.WhitePointFromTemperature (10000);
			Assert.AreEqual (0.280635904, wp.x);
			Assert.AreEqual (0.288290916, wp.y);
		}
	}
}
