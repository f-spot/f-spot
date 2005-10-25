//
// A very incomplete wrapper for lcms
//

using System;
using System.Collections;
using System.Runtime.InteropServices;

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
		Rgb16se = 264218
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
		Perceptual = 0,
		RelativeColorimetric = 1,
		Saturation = 2,
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

		public GammaTable (ushort [] values) : this (values, 0, values.Length) {}

		public GammaTable (ushort [] values, int start_offset, int length)
		{
			handle = new HandleRef (this, cmsAllocGamma (length));
			unsafe {
				GammaTableStruct *gt = (GammaTableStruct *)handle.Handle;

				ushort *data = & (gt->StartOfData);
				for (int i = 0; i < length; i++) {
					data [i] = values [start_offset + i];
				}
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsFreeGamma (HandleRef handle);
		
		private void Cleanup ()
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
		
		private void Cleanup ()
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
		static extern IntPtr cmsCreate_sRGBProfile();

		public static Profile CreateSRgb () 
		{
			return CreateStandardRgb ();
		}
		

		public static Profile CreateStandardRgb () 
		{
			return srgb;
		}
		
		public static Profile CreateAdobeRgb ()
		{
			System.Console.WriteLine ("FIXME returning invalid Adobe profile");
			// FIXME this needs to either load or generate an Adobe profile 
			return CreateStandardRgb ();
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
		
		public static Profile CreateAbstract (int nLUTPoints,
						      double Bright,
						      double Contrast,
						      double Hue,
						      double Saturation,
						      int TempSrc,
						      int TempDest)
		{
			return new Profile (cmsCreateBCHSWabstractProfile (nLUTPoints,
									   Bright,
									   Contrast,
									   Hue,
									   Saturation,
									   TempSrc,
									   TempDest));
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateLinearizationDeviceLink (IccColorSpace color_space, HandleRef [] tables);
		
		public Profile (IccColorSpace color_space, GammaTable [] gamma)
		{
			HandleRef [] gamma_handles = new HandleRef [gamma.Length];
			for (int i = 0; i < gamma_handles.Length; i++)
				gamma_handles [i] = gamma [i].Handle;

			handle = new HandleRef (this, cmsCreateLinearizationDeviceLink (color_space, gamma_handles));
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateRGBProfile (out ColorCIExyY whitepoint, 
						          out ColorCIExyYTriple primaries,
							  HandleRef [] gamma_table);

		public Profile (ColorCIExyY whitepoint, ColorCIExyYTriple primaries, GammaTable [] gamma)
		{
			HandleRef [] tbls = new HandleRef [3];
			tbls [0] = gamma [0].Handle;
			tbls [1] = gamma [1].Handle;
			tbls [2] = gamma [2].Handle;

			handle = new HandleRef (this, cmsCreateRGBProfile (out whitepoint, out primaries, tbls));

			// FIXME this is only here to avoid a mar
			//foreach (GammaTable t in gamma)
			//System.Console.WriteLine (t.ToString ());
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

		private void Cleanup ()
		{
			if (cmsCloseProfile (this.Handle) == 0)
				throw new Exception ("Error closing Handle");

		}

		~Profile ()
		{
			Cleanup ();
		}
	}
}
