/*
 * Cms.Profile.cs A very incomplete wrapper for lcms
 *
 * Author(s):
 *	Larry Ewing  <lewing@novell.com>
 *
 * This is free software. See COPYING for details.
 */

using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Serialization;

namespace Cms {
	public class Profile : IDisposable {
		static Profile srgb = new Profile (NativeMethods.CmsCreateSRGBProfile());

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

		private Profile () : this (NativeMethods.CmsCreateProfilePlaceholder ())
		{
			
		}

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

		public static Profile CreateLab (ColorCIExyY wp)
		{
			return new Profile (NativeMethods.CmsCreateLabProfile (out wp));
		}			

		public static Profile CreateLab ()
		{
			return new Profile (NativeMethods.CmsCreateLabProfile (IntPtr.Zero));
		}			

		public static Profile CreateGray (ColorCIExyY whitePoint, GammaTable transfer)
		{
			if (transfer == null)
				return new Profile (NativeMethods.CmsCreateGrayProfile (ref whitePoint, new GammaTable (4096, 2.2).Handle));
			else
				return new Profile (NativeMethods.CmsCreateGrayProfile (ref whitePoint, transfer.Handle));
		}

		public static Profile GetScreenProfile (Gdk.Screen screen)
		{
			if (screen == null)
				throw new ArgumentNullException ("screen");

			IntPtr profile = NativeMethods.FScreenGetProfile (screen.Handle);
			
			if (profile == IntPtr.Zero)
				return null;
			
			return new Profile (profile);
		}


	
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
						      ColorCIExyY srcWp,
						      ColorCIExyY destWp)
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
			System.Console.WriteLine ("s {0} {1} {2}", Saturation, srcWp, destWp);
			*/
			return new Profile (NativeMethods.FCmsCreateBCHSWabstractProfile (nLUTPoints,
									     Exposure,
									     0.0, //Bright,
									     Contrast,
									     Hue,
									     Saturation,
									     ref srcWp,
									     ref destWp,
									     CopyHandles (tables)));
		}

		public Profile (IccColorSpace color_space, GammaTable [] gamma)
		{
			handle = new HandleRef (this, NativeMethods.CmsCreateLinearizationDeviceLink (color_space, CopyHandles (gamma)));
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

		public Profile (ColorCIExyY whitepoint, ColorCIExyYTriple primaries, GammaTable [] gamma)
		{
			handle = new HandleRef (this, NativeMethods.CmsCreateRGBProfile (out whitepoint, out primaries, CopyHandles (gamma)));
		}

		public Profile (string path) 
		{
			handle = new HandleRef (this, NativeMethods.CmsOpenProfileFromFile (path, "r"));

			if (handle.Handle == IntPtr.Zero)
				throw new CmsException ("Error opening ICC profile in file " + path);
		}

		public byte [] Save ()
		{
			unsafe {
				uint length = 0;
				if (NativeMethods.CmsSaveProfileToMem (this.Handle, null, ref length)) {
					byte [] data = new byte [length];
					fixed (byte * data_p = &data [0]) {
						if (NativeMethods.CmsSaveProfileToMem (this.Handle, data_p, ref length)) {
							return data;
						}
					}
				}
			}
			throw new SaveException ("Error Saving Profile");
		}

		public Profile (byte [] data) : this (data, 0, data.Length) {
			if (data == null)
				throw new ArgumentNullException ("data");	
		}

		public Profile (byte [] data, int start_offset, int length)
		{
			if (start_offset < 0)
				throw new System.ArgumentOutOfRangeException ("start_offset < 0");

			if (data == null)
				throw new ArgumentNullException ("data");

			if (data.Length - start_offset < 0)
				throw new System.ArgumentOutOfRangeException ("start_offset > data.Length");

			if (data.Length - length - start_offset < 0)
				throw new System.ArgumentOutOfRangeException ("start_offset + length > data.Length");
			 
			IntPtr profileh;
			unsafe {
				fixed (byte * start = & data [start_offset]) {
					profileh = NativeMethods.CmsOpenProfileFromMem (start, (uint)length);
				}
			}
			
			if (profileh == IntPtr.Zero)
				throw new CmsException ("Invalid Profile Data");
			else 
				this.handle = new HandleRef (this, profileh);
		}

		public ColorCIEXYZ MediaWhitePoint {
			get {
				ColorCIEXYZ wp;
				if (!NativeMethods.CmsTakeMediaWhitePoint (out wp, handle))
					throw new CmsException ("unable to retrieve white point from profile");
				return wp;
			}
		}

		public ColorCIEXYZ MediaBlackPoint {
			get {
				ColorCIEXYZ black;
				if (!NativeMethods.CmsTakeMediaBlackPoint (out black, handle))
					throw new CmsException ("unable to retrieve white point from profile");
				
				return black;
			}
		}
		
		public ColorCIEXYZTriple Colorants {
			get {
				ColorCIEXYZTriple colors;
				if (!NativeMethods.CmsTakeColorants (out colors, handle))
					throw new CmsException ("Unable to retrieve profile colorants");
				
				return colors;
			}				
		}
		
		public uint ColorSpace {
			get {
				return NativeMethods.cmsGetColorSpace (this.handle);
			}
		}
		
		public string Model {
			get {
				lock (srgb) {
					return Marshal.PtrToStringAnsi (NativeMethods.CmsTakeModel (handle));
				}
			}
		}

		public string ProductName {
			get {
				lock (srgb) {
					return Marshal.PtrToStringAnsi (NativeMethods.CmsTakeProductName (handle));
				}
			}
		}

		public string ProductDescription {
			get {
				lock (srgb) {
					return Marshal.PtrToStringAnsi (NativeMethods.CmsTakeProductDesc (handle));
				}
			}
		}

		private enum ErrorAction {
			Abort,
			Show,
			Ignore
		}

		private static void SetErrorAction (ErrorAction act)
		{
			NativeMethods.CmsErrorAction ((int) act);
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
			if (NativeMethods.CmsCloseProfile (this.Handle) == 0)
				throw new CmsException ("Error closing Handle");

		}

		~Profile ()
		{
			Cleanup ();
		}

	}

}
