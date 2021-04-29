//
// Profile.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FSpot.Cms
{
	public class Profile : IDisposable
	{
		static readonly Profile srgb = new Profile (NativeMethods.CmsCreateSRGBProfile ());

		static Profile ()
		{
			//TODO
			//SetErrorAction (ErrorAction.Show);
		}

		bool disposed;

		public HandleRef Handle { get; private set; }

		Profile () : this (NativeMethods.CmsCreateProfilePlaceholder ())
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
			var wp = new ColorCIExyY (.3127, .329, 1.0);
			var primaries = new ColorCIExyYTriple (
				new ColorCIExyY (.64, .33, 1.0),
				new ColorCIExyY (.21, .71, 1.0),
				new ColorCIExyY (.15, .06, 1.0));
			var tc = new ToneCurve (2.2);
			var tcs = new ToneCurve[] { tc, tc, tc, tc };

			return new Profile (wp, primaries, tcs);
		}

		public static Profile CreateLab (ColorCIExyY wp)
		{
			return new Profile (NativeMethods.CmsCreateLabProfile (out wp));
		}

		public static Profile CreateLab ()
		{
			return new Profile (NativeMethods.CmsCreateLabProfile (IntPtr.Zero));
		}

		public static Profile CreateGray (ColorCIExyY whitePoint, ToneCurve transfer)
		{
			if (transfer == null)
				return new Profile (NativeMethods.CmsCreateGrayProfile (ref whitePoint, new ToneCurve (2.2).Handle));

			return new Profile (NativeMethods.CmsCreateGrayProfile (ref whitePoint, transfer.Handle));
		}

		public static Profile GetScreenProfile (Gdk.Screen screen)
		{
			if (screen == null)
				throw new ArgumentNullException (nameof (screen));

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
			var gamma = new ToneCurve (Math.Pow (10, -Bright / 100));
			var line = new ToneCurve (1.0);
			var tables = new ToneCurve[] { gamma, line, line };
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
							  ToneCurve[] tables,
							  ColorCIExyY srcWp,
							  ColorCIExyY destWp)
		{
			if (tables == null) {
				var gamma = new ToneCurve (Math.Pow (10, -Bright / 100));
				var line = new ToneCurve (1.0);
				tables = new ToneCurve[] { gamma, line, line };
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

		public Profile (IccColorSpace colorSpace, ToneCurve[] gamma)
		{
			Handle = new HandleRef (this, NativeMethods.CmsCreateLinearizationDeviceLink (colorSpace, CopyHandles (gamma)));
		}

		static HandleRef[] CopyHandles (ToneCurve[] gamma)
		{
			if (gamma == null)
				return null;

			var gamma_handles = new HandleRef[gamma.Length];
			for (int i = 0; i < gamma_handles.Length; i++)
				gamma_handles[i] = gamma[i].Handle;

			return gamma_handles;
		}

		public Profile (ColorCIExyY whitepoint, ColorCIExyYTriple primaries, ToneCurve[] gamma)
		{
			Handle = new HandleRef (this, NativeMethods.CmsCreateRGBProfile (out whitepoint, out primaries, CopyHandles (gamma)));
		}

		public Profile (string path)
		{
			Handle = new HandleRef (this, NativeMethods.CmsOpenProfileFromFile (path, "r"));

			if (Handle.Handle == IntPtr.Zero)
				throw new CmsException ($"Error opening ICC profile in file {path}");
		}

		public byte[] Save ()
		{
			unsafe {
				uint length = 0;
				if (NativeMethods.CmsSaveProfileToMem (Handle, null, ref length)) {
					byte[] data = new byte[length];
					fixed (byte* data_p = &data[0]) {
						if (NativeMethods.CmsSaveProfileToMem (Handle, data_p, ref length)) {
							return data;
						}
					}
				}
			}
			throw new SaveException ("Error Saving Profile");
		}

		public Profile (byte[] data) : this (data, 0, data.Length)
		{
			if (data == null)
				throw new ArgumentNullException (nameof (data));
		}

		public Profile (byte[] data, int startOffset, int length)
		{
			if (startOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (startOffset), "startOffset < 0");

			if (data == null)
				throw new ArgumentNullException (nameof (data));

			if (data.Length - startOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (startOffset), "startOffset > data.Length");

			if (data.Length - length - startOffset < 0)
				throw new ArgumentOutOfRangeException (nameof (length), "startOffset + length > data.Length");

			IntPtr profileh;
			unsafe {
				fixed (byte* start = &data[startOffset]) {
					profileh = NativeMethods.CmsOpenProfileFromMem (start, (uint)length);
				}
			}

			if (profileh == IntPtr.Zero)
				throw new CmsException ("Invalid Profile Data");

			Handle = new HandleRef (this, profileh);
		}

		public ColorCIEXYZ MediaWhitePoint {
			get {
				IntPtr ptr = NativeMethods.CmsReadTag (Handle, NativeMethods.CmsTagSignature.MediaWhitePoint);
				if (ptr == IntPtr.Zero)
					throw new CmsException ("unable to retrieve white point from profile");
				return ColorCIEXYZ.FromPtr (ptr);
			}
		}

		public ColorCIEXYZ MediaBlackPoint {
			get {
				IntPtr ptr = NativeMethods.CmsReadTag (Handle, NativeMethods.CmsTagSignature.MediaBlackPoint);
				if (ptr == IntPtr.Zero)
					throw new CmsException ("unable to retrieve white point from profile");
				return ColorCIEXYZ.FromPtr (ptr);
			}
		}

		public ColorCIEXYZTriple Colorants {
			get {
				IntPtr rPtr = NativeMethods.CmsReadTag (Handle, NativeMethods.CmsTagSignature.RedColorant);
				if (rPtr == IntPtr.Zero)
					throw new CmsException ("Unable to retrieve red profile colorant");
				IntPtr gPtr = NativeMethods.CmsReadTag (Handle, NativeMethods.CmsTagSignature.GreenColorant);
				if (gPtr == IntPtr.Zero)
					throw new CmsException ("Unable to retrieve green profile colorant");
				IntPtr bPtr = NativeMethods.CmsReadTag (Handle, NativeMethods.CmsTagSignature.BlueColorant);
				if (bPtr == IntPtr.Zero)
					throw new CmsException ("Unable to retrieve blue profile colorant");
				return new ColorCIEXYZTriple (
					ColorCIEXYZ.FromPtr (rPtr),
					ColorCIEXYZ.FromPtr (gPtr),
					ColorCIEXYZ.FromPtr (bPtr)
					);
			}
		}

		public IccColorSpace ColorSpace {
			get { return (IccColorSpace)NativeMethods.CmsGetColorSpace (Handle); }
		}

		public IccProfileClass DeviceClass {
			get { return (IccProfileClass)NativeMethods.CmsGetDeviceClass (Handle); }
		}

		public string Model {
			get {
				lock (srgb) {
					var ret = new StringBuilder (128);
					_ = NativeMethods.CmsGetProfileInfo (
						Handle,
						NativeMethods.CmsProfileInfo.Model,
						"en", "US",
						ret,
						ret.Capacity
						);
					return ret.ToString ();
				}
			}
		}

		public string ProductName {
			get {
				lock (srgb) {
					var ret = new StringBuilder (128);
					_ = NativeMethods.CmsGetProfileInfo (
						Handle,
						NativeMethods.CmsProfileInfo.Manufacturer,
						"en", "US",
						ret,
						ret.Capacity
						);
					return ret.ToString ();
				}
			}
		}

		public string ProductDescription {
			get {
				lock (srgb) {
					var ret = new StringBuilder (128);
					_ = NativeMethods.CmsGetProfileInfo (
						Handle,
						NativeMethods.CmsProfileInfo.Description,
						"en", "US",
						ret,
						ret.Capacity
						);
					return ret.ToString ();
				}
			}
		}

		enum ErrorAction
		{
			Abort,
			Show,
			Ignore
		}

		static void SetErrorAction (ErrorAction act)
		{
			NativeMethods.CmsErrorAction ((int)act);
		}

		public override string ToString ()
		{
			return ProductName;
		}

		protected Profile (IntPtr handle)
		{
			Handle = new HandleRef (this, handle);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing) {
				// free managed resources
			}
			// free unmanaged resources
			_ = NativeMethods.CmsCloseProfile (Handle);
		}

		~Profile ()
		{
			Dispose (false);
		}
	}
}
