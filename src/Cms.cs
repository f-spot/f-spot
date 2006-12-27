//
// A very incomplete wrapper for lcms
//

using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace Cms {
	public enum Format : uint {
		Rgb8  = 262169,
		Rgba8 = 262297,
		Rgba8Planar = 266393,
		Gbr8  = 263193,
		Rgb16 = 262170,
		Rgb16Planar = 266266,
		Rgba16 = 262298,
		Rgba16se = 264436,
		Rgb16se = 264218,
		Lab8 = 655385,
	        Lab16 = 655386,
		Xyz16 = 589858,
		Yxy16 = 917530
	}

	public enum IccColorSpace : uint {
		XYZ                        = 0x58595A20,  /* 'XYZ ' */
		Lab                        = 0x4C616220,  /* 'Lab ' */
		Luv                        = 0x4C757620,  /* 'Luv ' */
		YCbCr                      = 0x59436272,  /* 'YCbr' */
		Yxy                        = 0x59787920,  /* 'Yxy ' */
		Rgb                        = 0x52474220,  /* 'RGB ' */
		Gray                       = 0x47524159,  /* 'GRAY' */
		Hsv                        = 0x48535620,  /* 'HSV ' */
		Hls                        = 0x484C5320,  /* 'HLS ' */
		Cmyk                       = 0x434D594B,  /* 'CMYK' */
		Cmy                        = 0x434D5920,  /* 'CMY ' */
		Color2                     = 0x32434C52,  /* '2CLR' */
		Color3                     = 0x33434C52,  /* '3CLR' */
		Color4                     = 0x34434C52,  /* '4CLR' */
		Color5                     = 0x35434C52,  /* '5CLR' */
		Color6                     = 0x36434C52,  /* '6CLR' */
		Color7                     = 0x37434C52,  /* '7CLR' */
		Color8                     = 0x38434C52,  /* '8CLR' */
		Color9                     = 0x39434C52,  /* '9CLR' */
		Color10                    = 0x41434C52,  /* 'ACLR' */
		Color11                    = 0x42434C52,  /* 'BCLR' */
		Color12                    = 0x43434C52,  /* 'CCLR' */
		Color13                    = 0x44434C52,  /* 'DCLR' */
		Color14                    = 0x45434C52,  /* 'ECLR' */
		Color15                    = 0x46434C52,  /* 'FCLR' */
	}

	public enum Intent {
		Perceptual           = 0,
		RelativeColorimetric = 1,
		Saturation           = 2,
		AbsoluteColorimetric = 3
	}

	public struct ColorCIExyY {
		public double x;
		public double y;
		public double Y;
		
		public ColorCIExyY (double x, double y, double Y)
		{
			this.x = x;
			this.y = y;
			this.Y = Y;
		}

		public static ColorCIExyY WhitePointFromTemperature (int temp)
		{
			double x, y;
			CctTable.GetXY (temp, out x, out y);
			return new ColorCIExyY (x, y, 1.0);
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern bool cmsWhitePointFromTemp(int TempSrc,  out ColorCIExyY white_point);

		public static ColorCIExyY WhitePointFromTemperatureCIE (int temp)
		{
			ColorCIExyY wp;
			cmsWhitePointFromTemp (temp, out wp);
			return wp;
		}

		public static ColorCIExyY WhitePointFromTemperatureResource (int temp, string name)
		{
			ColorCIExyY wp;
			//const int line_size = 0x1e;

			using (Stream stream = Assembly.GetExecutingAssembly ().GetManifestResourceStream (name)) {
				StreamReader reader = new StreamReader (stream, System.Text.Encoding.ASCII);
				string line = null;
				for (int i = 0; i <= temp - 1000; i++) {
					line = reader.ReadLine ();
				}
				
				//System.Console.WriteLine (line);
				string [] subs = line.Split ('\t');
				int ptemp = int.Parse (subs [0]);
				if (ptemp != temp)
					throw new System.Exception (String.Format ("{0} != {1}", ptemp, temp));
				
				double x = double.Parse (subs [1]);
				double y = double.Parse (subs [2]);
				wp = new ColorCIExyY (x, y, 1.0);
				return wp;
			}			
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsxyY2XYZ (out ColorCIEXYZ dest, ref ColorCIExyY src);

		public ColorCIEXYZ ToXYZ ()
		{
			ColorCIEXYZ dest;
			cmsxyY2XYZ (out dest, ref this);
			
			return dest;
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsD50_xyY();

		public static ColorCIExyY D50 {
			get {
				IntPtr ptr = cmsD50_xyY ();
				return (ColorCIExyY) Marshal.PtrToStructure (ptr, typeof (ColorCIExyY));
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return this.ToXYZ ().ToLab (wp);
		}

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			return this.ToXYZ ().ToLab (wp);
		}

		public override string ToString ()
		{
			return String.Format ("(x={0}, y={1}, Y={2})", x, y, Y);
		}
		
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests {
			[Test]
			public void TestTempTable1000 ()
			{
				ColorCIExyY wp = WhitePointFromTemperature (1000);
				Assert.AreEqual (0.652756059, wp.x);
				Assert.AreEqual (0.344456906, wp.y);
			}

			[Test]
			public void TestTempReader ()
			{
				for (int i = 1000; i <= 25000; i += 10000)
					WhitePointFromTemperature (i);
			}
			
			[Test]
			public void TestTempTable10000 ()
			{
				ColorCIExyY wp = WhitePointFromTemperature (10000);
				Assert.AreEqual (0.280635904, wp.x);
				Assert.AreEqual (0.288290916, wp.y);
			}
		}
#endif
	}

	public struct ColorCIEXYZ {
		public double x;
		public double y;
		public double z;
		
		public ColorCIEXYZ (double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsXYZ2xyY (out ColorCIExyY dest, ref ColorCIEXYZ source);
		
		public ColorCIExyY ToxyY ()
		{
			ColorCIExyY dest;
			cmsXYZ2xyY (out dest, ref this);
			
			return dest;
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsXYZ2Lab (ref ColorCIEXYZ wp, out ColorCIELab lab, ref ColorCIEXYZ xyz);

		public ColorCIELab ToLab (ColorCIEXYZ wp)
		{
			ColorCIELab lab;
			cmsXYZ2Lab (ref wp, out lab, ref this);

			return lab;
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsD50_XYZ();

		public static ColorCIEXYZ D50 {
			get {
				IntPtr ptr = cmsD50_XYZ ();
				return (ColorCIEXYZ) Marshal.PtrToStructure (ptr, typeof (ColorCIEXYZ));
			}
		}

		public ColorCIELab ToLab (ColorCIExyY wp)
		{
			return ToLab (wp.ToXYZ ());
		}

		public override string ToString ()
		{
			return String.Format ("(x={0}, y={1}, z={2})", x, y, z);
		}


	}

	public struct ColorCIELab {
		public double L;
		public double a;
		public double b;

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsLab2LCh (out ColorCIELCh lch, ref ColorCIELab lab);

		public ColorCIELCh ToLCh ()
		{
			ColorCIELCh lch;
			cmsLab2LCh (out lch, ref this);

			return lch;
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsLab2XYZ (ref ColorCIEXYZ wp, out ColorCIEXYZ xyz, ref ColorCIELab lab);
		
		public ColorCIEXYZ ToXYZ (ColorCIEXYZ wp)
		{
			ColorCIEXYZ xyz;
			cmsLab2XYZ (ref wp, out xyz, ref this);

			return xyz;
		}

		public override string ToString ()
		{
			return String.Format ("(L={0}, a={1}, b={2})", L, a, b);
		}
	}

	public struct ColorCIELCh {
		public double L;
		public double C;
		public double h;
		
		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsLCh2Lab (out ColorCIELab lab, ref ColorCIELCh lch);
		
		public ColorCIELab ToLab ()
		{
			ColorCIELab lab;
			cmsLCh2Lab (out lab, ref this);

			return lab;
		}
	}

	public struct ColorCIExyYTriple {
		public ColorCIExyY Red;
		public ColorCIExyY Green;
		public ColorCIExyY Blue;

		public ColorCIExyYTriple (ColorCIExyY red, ColorCIExyY green, ColorCIExyY blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}
	}

	public struct ColorCIEXYZTriple {
		public ColorCIEXYZ Red;
		public ColorCIEXYZ Blue;
		public ColorCIEXYZ Green;

		ColorCIEXYZTriple (ColorCIEXYZ red, ColorCIEXYZ green, ColorCIEXYZ blue)
		{
			Red = red;
			Blue = blue;
			Green = green;
		}
	}

	public class GammaTable : IDisposable {
		private HandleRef handle;
		public HandleRef Handle {
			get {
				return handle;
			}
		}
		
		internal struct GammaTableStruct {
			public int Count;
			public ushort StartOfData;  // ushort array Count entries long
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsBuildGamma (int entry_count, double gamma);

		public GammaTable (int count, double gamma)
		{
			handle = new HandleRef (this, cmsBuildGamma (count, gamma));
		}

		/*
		  Does build a parametric curve based on parameters.
		  Params[] does contain Gamma, a, b, c, d, e, f

		  Type 1 :
		  X = Y ^ Gamma   | Gamma = Params[0]
		  
		  Type 2 :
		  CIE 122-1966
		  Y = (aX + b)^Gamma  | X >= -b/a
		  Y = 0               | else
		  Type 3:
		  
		  IEC 61966-3
		  Y = (aX + b)^Gamma | X <= -b/a
		  Y = c              | else
		  
		  Type 4:
		  IEC 61966-2.1 (sRGB)
		  Y = (aX + b)^Gamma | X >= d
		  Y = cX             | X < d
		  
		  Type 5:
		  
		  Y = (aX + b)^Gamma + e | X <= d
		  Y = cX + f             | else
		  
		  Giving negative values for Type does result in reversed curve.
		*/

		// FIXME type should be an enum
		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsBuildParametricGamma (int entry_count, int type, double [] values);
		
		public GammaTable (int entry_count, int type, double [] values)
		{
			handle = new HandleRef (this, cmsBuildParametricGamma (entry_count, type, values));
		}
		

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsAllocGamma (int entry_count);

		[DllImport ("libfspot")]
		static extern IntPtr f_cms_gamma_table_new (ushort [] values, int start, int length);

		[DllImport ("libfspot")]
		static extern IntPtr f_cms_gamma_table_get_values (HandleRef table);

		[DllImport ("libfspot")]
		static extern int f_cms_gamma_table_get_count (HandleRef table);

		public GammaTable (ushort [] values) : this (values, 0, values.Length)
		{
		}

		public int Count {
			get {
				return f_cms_gamma_table_get_count (handle);
			}
		}

		public IntPtr Values {
			get {
				return f_cms_gamma_table_get_values (handle);
			}
		}

		public ushort this [int index] {
			get {
				unsafe {
					if (handle.Handle == (IntPtr)0)
						throw new ArgumentException ();
					
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException (String.Format ("index {0} outside of count {1} for {2}", index, Count, handle.Handle));

					ushort *data = (ushort *)Values;
					return data [index];
				}
			}
			set {
				unsafe {
					if (handle.Handle == (IntPtr)0)
						throw new ArgumentException ();
					
					if (index < 0 || index >= Count)
						throw new ArgumentOutOfRangeException (String.Format ("index {0} outside of count {1} for handle {2}", index, Count, handle.Handle));


					ushort *data = (ushort *)Values;
					data [index] = value;
				}
			}
		}
		
		public GammaTable (ushort [] values, int start_offset, int length)
		{
#if true
			handle = new HandleRef (this, f_cms_gamma_table_new (values, start_offset, length));
			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXhandle = {0}", handle.Handle);
#else
			handle = new HandleRef (this, cmsAllocGamma (length));
			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXhandle = {0}", handle.Handle);
			for (int i = 0; i < length; i++)
				this [i] = values [start_offset + i];
#endif
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsFreeGamma (HandleRef handle);
		
		protected virtual void Cleanup ()
		{
			cmsFreeGamma (handle);
		}
		
		public void Dispose ()
		{
			Cleanup ();
			System.GC.SuppressFinalize (this);
		}
		
		~GammaTable ()
		{
			Cleanup ();
		}


#if ENABLE_NUNIT
		[TestFixture]
		public class Tests {
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
#endif 

	}
	
	public class Transform : IDisposable {
		private HandleRef handle;
		public HandleRef Handle {
			get {
				return handle;
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateMultiprofileTransform (HandleRef [] hProfiles,
								     int nProfiles,
								     Format InputFormat,
								     Format OutputFormat,
								     int Intent,
								     uint dwFlags);

		public Transform (Profile [] profiles,
				  Format input_format,
				  Format output_format,
				  Intent intent, uint flags)
		{
			HandleRef [] handles = new HandleRef [profiles.Length];
			for (int i = 0; i < profiles.Length; i++) {
				handles [i] = profiles [i].Handle;
			}
			
			this.handle = new HandleRef (this, cmsCreateMultiprofileTransform (handles, handles.Length, 
											   input_format,
											   output_format, 
											   (int)intent, flags));
		}
		
		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateTransform(HandleRef Input,
							Format InputFormat,
							HandleRef Output,
							Format OutputFormat,
							int Intent,
							uint dwFlags);

		public Transform (Profile input, Format input_format,
				  Profile output, Format output_format,
				  Intent intent, uint flags)
		{
			this.handle = new HandleRef (this, cmsCreateTransform (input.Handle, input_format,
									       output.Handle, output_format,
									       (int)intent, flags));
		}
		
		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsDoTransform (HandleRef hTransform, IntPtr InputBuffer, IntPtr OutputBuffer, uint size);
		
		// Fixme this should probably be more type stafe 
		public void Apply (IntPtr input, IntPtr output, uint size)
		{
			cmsDoTransform (Handle, input, output, size);
		}
		
		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsDeleteTransform(HandleRef hTransform);
		
		public void Dispose () 
		{
			Cleanup ();
			System.GC.SuppressFinalize (this);
		}
		
		protected virtual void Cleanup ()
		{
			cmsDeleteTransform (this.handle);
		}
		
		~Transform () 
		{
			Cleanup ();
		}
	       
	}

	public class Profile : IDisposable {
		static Profile srgb = new Profile (cmsCreate_sRGBProfile());

		static Profile ()
		{
			SetErrorAction (ErrorAction.Show);
		}

		private HandleRef handle;
		public HandleRef Handle {
			get {
				return handle;
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr _cmsCreateProfilePlaceholder ();
		
		private Profile () : this (_cmsCreateProfilePlaceholder ())
		{
			
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreate_sRGBProfile ();

		public static Profile CreateSRgb () 
		{
			return CreateStandardRgb ();
		}
		

		public static Profile CreateStandardRgb () 
		{
			return srgb;
		}
		
		public static Profile CreateAlternateRgb ()
		{
			// FIXME I'm basing this off the values set in the camera
			// exif data when the adobe profile is selected.  They could
			// easily be off
			ColorCIExyY wp = new ColorCIExyY (.3127, .329, 1.0);
			ColorCIExyYTriple primaries = new ColorCIExyYTriple (
				new ColorCIExyY (.64, .33, 1.0),
				new ColorCIExyY (.21, .71, 1.0),
				new ColorCIExyY (.15, .06, 1.0));
			
			GammaTable g = new GammaTable (4096, 2.2);
			GammaTable [] gamma = new GammaTable [] { g, g, g, g};

			return new Profile (wp, primaries, gamma);
		}

		[DllImport ("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateLabProfile (out ColorCIExyY WhitePoint);

		public static Profile CreateLab (ColorCIExyY wp)
		{
			return new Profile (cmsCreateLabProfile (out wp));
		}			

		[DllImport ("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateLabProfile (IntPtr foo);

		public static Profile CreateLab ()
		{
			return new Profile (cmsCreateLabProfile (IntPtr.Zero));
		}			

		[DllImport ("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateGrayProfile (ref ColorCIExyY white_point,
							   HandleRef transfer_function);
		
		public static Profile CreateGray (ColorCIExyY white_point, GammaTable transfer)
		{
			if (transfer == null)
				return new Profile (cmsCreateGrayProfile (ref white_point, new GammaTable (4096, 2.2).Handle));
			else
				return new Profile (cmsCreateGrayProfile (ref white_point, transfer.Handle));
		}

		[DllImport ("libfspot")]
		static extern IntPtr f_screen_get_profile (IntPtr screen);

		public static Profile GetScreenProfile (Gdk.Screen screen)
		{
			IntPtr profile = f_screen_get_profile (screen.Handle);
			
			if (profile == IntPtr.Zero)
				return null;
			
			return new Profile (profile);
		}


		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateBCHSWabstractProfile(int nLUTPoints,
								   double Bright, 
								   double Contrast,
								   double Hue,
								   double Saturation,
								   int TempSrc, 
								   int TempDest);

		[DllImport("libfspot")]
		static extern IntPtr f_cmsCreateBCHSWabstractProfile(int nLUTPoints,
								     double Exposure,
								     double Bright, 
								     double Contrast,
								     double Hue,
								     double Saturation,
								     ref ColorCIExyY src_wp, 
								     ref ColorCIExyY dest_wp,
								     HandleRef [] tables);
		
		public static Profile CreateAbstract (int nLUTPoints,
						      double Exposure,
						      double Bright,
						      double Contrast,
						      double Hue,
						      double Saturation,
						      int TempSrc,
						      int TempDest)
		{
#if true			
			GammaTable gamma = new GammaTable (1024, Math.Pow (10, -Bright/100));
			GammaTable line = new GammaTable (1024, 1.0);
			GammaTable [] tables = new GammaTable [] { gamma, line, line };
			return CreateAbstract (nLUTPoints, Exposure, 0.0, Contrast, Hue, Saturation, tables, 
					       ColorCIExyY.WhitePointFromTemperature (TempSrc), 
					       ColorCIExyY.WhitePointFromTemperature (TempDest));
#else
			GammaTable [] tables = null;
			return CreateAbstract (nLUTPoints, Exposure, Bright, Contrast, Hue, Saturation, tables, 
					       ColorCIExyY.WhitePointFromTemperature (TempSrc), 
					       ColorCIExyY.WhitePointFromTemperature (TempDest));
#endif
		}

		public static Profile CreateAbstract (int nLUTPoints,
						      double Exposure,
						      double Bright,
						      double Contrast,
						      double Hue,
						      double Saturation,
						      GammaTable [] tables,
						      ColorCIExyY src_wp,
						      ColorCIExyY dest_wp)
		{
			if (tables == null) {
				GammaTable gamma = new GammaTable (1024, Math.Pow (10, -Bright/100));
				GammaTable line = new GammaTable (1024, 1.0);
				tables = new GammaTable [] { gamma, line, line };
			}

			/*
			System.Console.WriteLine ("e {0}", Exposure);
			System.Console.WriteLine ("b {0}", Bright);
			System.Console.WriteLine ("c {0}", Contrast);
			System.Console.WriteLine ("h {0}", Hue);
			System.Console.WriteLine ("s {0} {1} {2}", Saturation, src_wp, dest_wp);
			*/
			return new Profile (f_cmsCreateBCHSWabstractProfile (nLUTPoints,
									     Exposure,
									     0.0, //Bright,
									     Contrast,
									     Hue,
									     Saturation,
									     ref src_wp,
									     ref dest_wp,
									     CopyHandles (tables)));
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateLinearizationDeviceLink (IccColorSpace color_space, HandleRef [] tables);
		
		public Profile (IccColorSpace color_space, GammaTable [] gamma)
		{
			handle = new HandleRef (this, cmsCreateLinearizationDeviceLink (color_space, CopyHandles (gamma)));
		}

		private static HandleRef [] CopyHandles (GammaTable [] gamma)
		{
			if (gamma == null)
				return null;

			HandleRef [] gamma_handles = new HandleRef [gamma.Length];
			for (int i = 0; i < gamma_handles.Length; i++)
				gamma_handles [i] = gamma [i].Handle;
			
			return gamma_handles;
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateRGBProfile (out ColorCIExyY whitepoint, 
						          out ColorCIExyYTriple primaries,
							  HandleRef [] gamma_table);

		public Profile (ColorCIExyY whitepoint, ColorCIExyYTriple primaries, GammaTable [] gamma)
		{
			handle = new HandleRef (this, cmsCreateRGBProfile (out whitepoint, out primaries, CopyHandles (gamma)));
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern int cmsCloseProfile (HandleRef hprofile);

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsOpenProfileFromFile (string ICCProfile, string sAccess);

		public Profile (string path) 
		{
			handle = new HandleRef (this, cmsOpenProfileFromFile (path, "r"));

			if (handle.Handle == IntPtr.Zero)
				throw new Exception ("Error opening ICC profile in file " + path);
		}


		[DllImport("liblcms-1.0.0.dll")]
		static extern unsafe bool _cmsSaveProfileToMem (HandleRef profile, byte *mem, ref uint length);

		public byte [] Save ()
		{
			unsafe {
				uint length = 0;
				if (_cmsSaveProfileToMem (this.Handle, null, ref length)) {
					byte [] data = new byte [length];
					fixed (byte * data_p = &data [0]) {
						if (_cmsSaveProfileToMem (this.Handle, data_p, ref length)) {
							return data;
						}
					}
				}
			}
			throw new SaveException ("Error Saving Profile");
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern unsafe IntPtr cmsOpenProfileFromMem (byte *data, uint length);

		public Profile (byte [] data) : this (data, 0, data.Length) {}

		public Profile (byte [] data, int start_offset, int length)
		{
			if (start_offset < 0)
				throw new System.ArgumentOutOfRangeException ("start_offset < 0");

			if (data.Length - start_offset < 0)
				throw new System.ArgumentOutOfRangeException ("start_offset > data.Length");

			if (data.Length - length - start_offset < 0)
				throw new System.ArgumentOutOfRangeException ("start_offset + length > data.Length");
			 
			IntPtr profileh;
			unsafe {
				fixed (byte * start = & data [start_offset]) {
					profileh = cmsOpenProfileFromMem (start, (uint)length);
				}
			}
			
			if (profileh == IntPtr.Zero)
				throw new System.Exception ("Invalid Profile Data");
			else 
				this.handle = new HandleRef (this, profileh);
		}

		[DllImport("liblcms-1.0.0.dll")]
		extern static bool cmsTakeMediaWhitePoint (out ColorCIEXYZ wp, HandleRef handle);
		
		public ColorCIEXYZ MediaWhitePoint {
			get {
				ColorCIEXYZ wp;
				if (!cmsTakeMediaWhitePoint (out wp, handle))
					throw new ApplicationException ("unable to retrieve white point from profile");
				return wp;
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		extern static bool cmsTakeMediaBlackPoint (out ColorCIEXYZ black, HandleRef handle);
		
		public ColorCIEXYZ MediaBlackPoint {
			get {
				ColorCIEXYZ black;
				if (!cmsTakeMediaBlackPoint (out black, handle))
					throw new ApplicationException ("unable to retrieve white point from profile");
				
				return black;
			}
		}
		
		[DllImport("liblcms-1.0.0.dll")]
		extern static bool cmsTakeColorants (out ColorCIEXYZTriple colors, HandleRef handle);

		public ColorCIEXYZTriple Colorants {
			get {
				ColorCIEXYZTriple colors;
				if (! cmsTakeColorants (out colors, handle))
					throw new ApplicationException ("Unable to retrieve profile colorants");
				
				return colors;
			}				
		}
		
		[DllImport("liblcms-1.0.0.dll")]
		extern static IntPtr cmsTakeModel (HandleRef handle);
		
		public string Model {
			get {
				lock (srgb) {
					return Marshal.PtrToStringAnsi (cmsTakeModel (handle));
				}
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		extern static IntPtr cmsTakeProductName (HandleRef handle);

		public string ProductName {
			get {
				lock (srgb) {
					return Marshal.PtrToStringAnsi (cmsTakeProductName (handle));
				}
			}
		}
		[DllImport("liblcms-1.0.0.dll")]
		extern static IntPtr cmsTakeProductDesc (HandleRef handle);

		public string ProductDescription {
			get {
				lock (srgb) {
					return Marshal.PtrToStringAnsi (cmsTakeProductDesc (handle));
				}
			}
		}

		private enum ErrorAction {
			Abort,
			Show,
			Ignore
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsErrorAction (ErrorAction action);

		private static void SetErrorAction (ErrorAction act)
		{
			cmsErrorAction (act);
		}

		public override string ToString ()
		{
			return ProductName;
		}

		protected Profile (IntPtr handle)
		{
			this.handle = new HandleRef (this, handle);
		}

		public void Dispose () 
		{
			Cleanup ();

			System.GC.SuppressFinalize (this);
		}

		protected virtual void Cleanup ()
		{
			if (cmsCloseProfile (this.Handle) == 0)
				throw new Exception ("Error closing Handle");

		}

		~Profile ()
		{
			Cleanup ();
		}

#if ENABLE_NUNIT
		[TestFixture]
		public class Tests {
			[Test]
			public void LoadSave ()
			{
				Profile srgb = CreateStandardRgb ();
				byte [] data = srgb.Save ();
				Assert.IsNotNull (data);
				Profile result = new Profile (data);
				Assert.AreEqual (result.ProductName, srgb.ProductName);
				Assert.AreEqual (result.ProductDescription, srgb.ProductDescription);
				Assert.AreEqual (result.Model, srgb.Model);
			}
		}
#endif 		
	}

	public class SaveException : System.Exception {
		public SaveException (string message) : base (message)
		{
		}
	}
}
