//
// NativeMethods.cs
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
	static class NativeMethods
	{
		public const string lcmsLib = "liblcms-2.0.0.dll";

		[DllImport (lcmsLib, EntryPoint = "cmsWhitePointFromTemp", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool CmsWhitePointFromTemp (int TempSrc, out ColorCIExyY white_point);

		[DllImport (lcmsLib, EntryPoint = "cmsxyY2XYZ", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsxyY2XYZ (out ColorCIEXYZ dest, ref ColorCIExyY src);

		[DllImport (lcmsLib, EntryPoint = "cmsD50_xyY", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsD50xyY ();

		[DllImport (lcmsLib, EntryPoint = "cmsXYZ2xyY", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsXYZ2xyY (out ColorCIExyY dest, ref ColorCIEXYZ source);

		[DllImport (lcmsLib, EntryPoint = "cmsXYZ2Lab", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsXYZ2Lab (ref ColorCIEXYZ wp, out ColorCIELab lab, ref ColorCIEXYZ xyz);

		[DllImport (lcmsLib, EntryPoint = "cmsD50_XYZ", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsD50XYZ ();

		[DllImport (lcmsLib, EntryPoint = "cmsLab2LCh", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsLab2LCh (out ColorCIELCh lch, ref ColorCIELab lab);

		[DllImport (lcmsLib, EntryPoint = "cmsLab2XYZ", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsLab2XYZ (ref ColorCIEXYZ wp, out ColorCIEXYZ xyz, ref ColorCIELab lab);

		[DllImport (lcmsLib, EntryPoint = "cmsLCh2Lab", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsLCh2Lab (out ColorCIELab lab, ref ColorCIELCh lch);

		[DllImport (lcmsLib, EntryPoint = "cmsBuildGamma", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsBuildGamma (int contextID, double gamma);

		[DllImport (lcmsLib, EntryPoint = "cmsBuildParametricToneCurve", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsBuildParametricToneCurve (int contextID, int type, double[] values);

		[DllImport (lcmsLib, EntryPoint = "cmsFreeToneCurve", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsFreeToneCurve (HandleRef handle);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateMultiprofileTransform", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateMultiprofileTransform (HandleRef[] hProfiles,
									 int nProfiles,
									 Format InputFormat,
									 Format OutputFormat,
									 int Intent,
									 uint dwFlags);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateTransform", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateTransform (HandleRef Input, Format InputFormat, HandleRef Output, Format OutputFormat,
			int Intent, uint dwFlags);

		[DllImport (lcmsLib, EntryPoint = "cmsDoTransform", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsDoTransform (HandleRef hTransform, IntPtr InputBuffer, IntPtr OutputBuffer, uint size);

		[DllImport (lcmsLib, EntryPoint = "cmsDeleteTransform", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsDeleteTransform (HandleRef hTransform);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateGrayProfile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateGrayProfile (ref ColorCIExyY white_point, HandleRef transfer_function);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateLabProfile", CallingConvention = CallingConvention.Cdecl)]

		public static extern IntPtr CmsCreateLabProfile (IntPtr foo);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateLabProfile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateLabProfile (out ColorCIExyY WhitePoint);

		[DllImport (lcmsLib, EntryPoint = "cmsCreate_sRGBProfile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateSRGBProfile ();

		[DllImport (lcmsLib, EntryPoint = "_cmsCreateProfilePlaceholder", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateProfilePlaceholder ();

		[DllImport (lcmsLib, EntryPoint = "cmsErrorAction", CallingConvention = CallingConvention.Cdecl)]
		public static extern void CmsErrorAction (int action);

		[DllImport (lcmsLib, EntryPoint = "cmsGetColorSpace", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint CmsGetColorSpace (HandleRef hprofile);

		[DllImport (lcmsLib, EntryPoint = "cmsGetDeviceClass", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint CmsGetDeviceClass (HandleRef hprofile);


		public enum CmsProfileInfo
		{
			Description	= 0,
			Manufacturer	= 1,
			Model		= 2,
			Copyright	= 3
		}

		[DllImport (lcmsLib, EntryPoint = "cmsGetProfileInfo", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CmsGetProfileInfo (HandleRef hprofile,
													  CmsProfileInfo info,
													  string languageCode,
													  string countryCode,
													  [Out, MarshalAsAttribute (UnmanagedType.LPWStr)] StringBuilder buffer,
													  int bufferSize);


		public enum CmsTagSignature
		{
			MediaBlackPoint		= 0x626B7074,
			MediaWhitePoint		= 0x77747074,
			RedColorant		= 0x7258595A,
			GreenColorant		= 0x6758595A,
			BlueColorant		= 0x6258595A
		}


		[DllImport (lcmsLib, EntryPoint = "cmsReadTag", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsReadTag (HandleRef hProfile, CmsTagSignature sig);

		[DllImport (lcmsLib, EntryPoint = "cmsOpenProfileFromMem", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe IntPtr CmsOpenProfileFromMem (byte* data, uint length);

		[DllImport (lcmsLib, EntryPoint = "_cmsSaveProfileToMem", CallingConvention = CallingConvention.Cdecl)]
		public static extern unsafe bool CmsSaveProfileToMem (HandleRef profile, byte* mem, ref uint length);

		[DllImport (lcmsLib, EntryPoint = "cmsOpenProfileFromFile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsOpenProfileFromFile (string ICCProfile, string sAccess);

		[DllImport (lcmsLib, EntryPoint = "cmsCloseProfile", CallingConvention = CallingConvention.Cdecl)]
		public static extern int CmsCloseProfile (HandleRef hprofile);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateRGBProfile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateRGBProfile (out ColorCIExyY whitepoint,
								  out ColorCIExyYTriple primaries,
							  HandleRef[] transfer_function);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateLinearizationDeviceLink", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsCreateLinearizationDeviceLink (IccColorSpace color_space, HandleRef[] tables);

		[DllImport (lcmsLib, EntryPoint = "cmsBuildTabulatedToneCurve16", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsBuildTabulatedToneCurve16 (IntPtr ContextID,
																 int nEntries,
																 [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)]
																 ushort[] values);


		[DllImport (lcmsLib, EntryPoint = "cmsGetToneCurveEstimatedTableEntries", CallingConvention = CallingConvention.Cdecl)]
		public static extern uint CmsGetToneCurveEstimatedTableEntries (HandleRef curve);

		[DllImport (lcmsLib, EntryPoint = "cmsGetToneCurveEstimatedTable", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr CmsGetToneCurveEstimatedTable (HandleRef curve);

		[DllImport ("libfspot", EntryPoint = "f_cmsCreateBCHSWabstractProfile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr FCmsCreateBCHSWabstractProfile (int nLUTPoints,
									 double Exposure,
									 double Bright,
									 double Contrast,
									 double Hue,
									 double Saturation,
									 ref ColorCIExyY src_wp,
									 ref ColorCIExyY dest_wp,
									 HandleRef[] curves);

		[DllImport ("libfspot", EntryPoint = "f_screen_get_profile", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr FScreenGetProfile (IntPtr screen);
	}
}
