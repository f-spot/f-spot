//
// NativeMethods.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Cms
{
	internal static class NativeMethods
	{
		public const string lcmsLib = "liblcms-2.0.0.dll";

		[DllImport(lcmsLib, EntryPoint = "cmsWhitePointFromTemp")]
		public static extern bool CmsWhitePointFromTemp(int TempSrc,  out ColorCIExyY white_point);

		[DllImport(lcmsLib, EntryPoint = "cmsxyY2XYZ")]
		public static extern void CmsxyY2XYZ (out ColorCIEXYZ dest, ref ColorCIExyY src);

		[DllImport(lcmsLib, EntryPoint = "cmsD50_xyY")]
		public static extern IntPtr CmsD50xyY();

		[DllImport(lcmsLib, EntryPoint = "cmsXYZ2xyY")]
		public static extern void CmsXYZ2xyY (out ColorCIExyY dest, ref ColorCIEXYZ source);
		
		[DllImport(lcmsLib, EntryPoint = "cmsXYZ2Lab")]
		public static extern void CmsXYZ2Lab (ref ColorCIEXYZ wp, out ColorCIELab lab, ref ColorCIEXYZ xyz);

		[DllImport(lcmsLib, EntryPoint = "cmsD50_XYZ")]
		public static extern IntPtr CmsD50XYZ();

		[DllImport(lcmsLib, EntryPoint = "cmsLab2LCh")]
		public static extern void CmsLab2LCh (out ColorCIELCh lch, ref ColorCIELab lab);

		[DllImport(lcmsLib, EntryPoint = "cmsLab2XYZ")]
		public static extern void CmsLab2XYZ (ref ColorCIEXYZ wp, out ColorCIEXYZ xyz, ref ColorCIELab lab);
		
		[DllImport(lcmsLib, EntryPoint = "cmsLCh2Lab")]
		public static extern void CmsLCh2Lab (out ColorCIELab lab, ref ColorCIELCh lch);
		
		[DllImport(lcmsLib, EntryPoint = "cmsBuildGamma")]
		public static extern IntPtr CmsBuildGamma (int contextID, double gamma);

		[DllImport(lcmsLib, EntryPoint = "cmsBuildParametricToneCurve")]
		public static extern IntPtr CmsBuildParametricToneCurve (int contextID, int type, double [] values);

		[DllImport(lcmsLib, EntryPoint = "cmsFreeToneCurve")]
		public static extern void CmsFreeToneCurve (HandleRef handle);

		[DllImport(lcmsLib, EntryPoint = "cmsCreateMultiprofileTransform")]
		public static extern IntPtr CmsCreateMultiprofileTransform (HandleRef [] hProfiles,
								     int nProfiles,
								     Format InputFormat,
								     Format OutputFormat,
								     int Intent,
								     uint dwFlags);

		[DllImport(lcmsLib, EntryPoint = "cmsCreateTransform")]
		public static extern IntPtr CmsCreateTransform(HandleRef Input,
							Format InputFormat,
							HandleRef Output,
							Format OutputFormat,
							int Intent,
							uint dwFlags);

		[DllImport(lcmsLib, EntryPoint = "cmsDoTransform")]
		public static extern void CmsDoTransform (HandleRef hTransform, IntPtr InputBuffer, IntPtr OutputBuffer, uint size);
		
		[DllImport(lcmsLib, EntryPoint = "cmsDeleteTransform")]
		public static extern void CmsDeleteTransform(HandleRef hTransform);
		
		[DllImport (lcmsLib, EntryPoint = "cmsCreateGrayProfile")]
		public static extern IntPtr CmsCreateGrayProfile (ref ColorCIExyY white_point,
							   HandleRef transfer_function);
		
		[DllImport (lcmsLib, EntryPoint = "cmsCreateLabProfile")]
		public static extern IntPtr CmsCreateLabProfile (IntPtr foo);

		[DllImport (lcmsLib, EntryPoint = "cmsCreateLabProfile")]
		public static extern IntPtr CmsCreateLabProfile (out ColorCIExyY WhitePoint);

		[DllImport(lcmsLib, EntryPoint = "cmsCreate_sRGBProfile")]
		public static extern IntPtr CmsCreateSRGBProfile ();

		[DllImport(lcmsLib, EntryPoint = "_cmsCreateProfilePlaceholder")]
		public static extern IntPtr CmsCreateProfilePlaceholder ();

		[DllImport(lcmsLib, EntryPoint = "cmsErrorAction")]
		public static extern void CmsErrorAction (int action);
		
		[DllImport (lcmsLib, EntryPoint = "cmsGetColorSpace")]
		public static extern uint CmsGetColorSpace (HandleRef hprofile);

		[DllImport (lcmsLib, EntryPoint = "cmsGetDeviceClass")]
		public static extern uint CmsGetDeviceClass (HandleRef hprofile);


		public enum CmsProfileInfo
		{
			Description	= 0,
			Manufacturer	= 1,
			Model		= 2,
			Copyright	= 3
		}

		[DllImport(lcmsLib, EntryPoint = "cmsGetProfileInfo")]
		public extern static int CmsGetProfileInfo(HandleRef hprofile,
		                                              CmsProfileInfo info,
		                                              string languageCode,
		                                              string countryCode,
		                                              [Out, MarshalAsAttribute(UnmanagedType.LPWStr)] StringBuilder buffer,
		                                              int bufferSize);


		public enum CmsTagSignature
		{
			MediaBlackPoint		= 0x626B7074,
			MediaWhitePoint		= 0x77747074,
			RedColorant		= 0x7258595A,
			GreenColorant		= 0x6758595A,
			BlueColorant		= 0x6258595A
		}

		[DllImport(lcmsLib, EntryPoint = "cmsReadTag")]
		public extern static IntPtr CmsReadTag(HandleRef hProfile, CmsTagSignature sig);

		[DllImport(lcmsLib, EntryPoint = "cmsOpenProfileFromMem")]
		public static extern unsafe IntPtr CmsOpenProfileFromMem (byte *data, uint length);

		[DllImport(lcmsLib, EntryPoint = "_cmsSaveProfileToMem")]
		public static extern unsafe bool CmsSaveProfileToMem (HandleRef profile, byte *mem, ref uint length);

		[DllImport(lcmsLib, EntryPoint = "cmsOpenProfileFromFile")]
		public static extern IntPtr CmsOpenProfileFromFile (string ICCProfile, string sAccess);

		[DllImport(lcmsLib, EntryPoint = "cmsCloseProfile")]
		public static extern int CmsCloseProfile (HandleRef hprofile);

		[DllImport(lcmsLib, EntryPoint = "cmsCreateRGBProfile")]
		public static extern IntPtr CmsCreateRGBProfile (out ColorCIExyY whitepoint, 
						          out ColorCIExyYTriple primaries,
							  HandleRef [] transfer_function);

		[DllImport(lcmsLib, EntryPoint = "cmsCreateLinearizationDeviceLink")]
		public static extern IntPtr CmsCreateLinearizationDeviceLink (IccColorSpace color_space, HandleRef [] tables);

		[DllImport(lcmsLib, EntryPoint = "cmsBuildTabulatedToneCurve16")]
		public static extern IntPtr CmsBuildTabulatedToneCurve16(IntPtr ContextID,
		                                                         int nEntries,
		                                                         [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)]
		                                                         ushort[] values);


		[DllImport(lcmsLib, EntryPoint = "cmsGetToneCurveEstimatedTableEntries")]
		public static extern uint CmsGetToneCurveEstimatedTableEntries (HandleRef curve);

		[DllImport(lcmsLib, EntryPoint = "cmsGetToneCurveEstimatedTable")]
		public static extern IntPtr CmsGetToneCurveEstimatedTable(HandleRef curve);
		
		[DllImport("libfspot", EntryPoint = "f_cmsCreateBCHSWabstractProfile")]
		public static extern IntPtr FCmsCreateBCHSWabstractProfile(int nLUTPoints,
								     double Exposure,
								     double Bright, 
								     double Contrast,
								     double Hue,
								     double Saturation,
								     ref ColorCIExyY src_wp, 
								     ref ColorCIExyY dest_wp,
								     HandleRef [] tables);
		
		[DllImport ("libfspot", EntryPoint = "f_screen_get_profile")]
		public static extern IntPtr FScreenGetProfile (IntPtr screen);
	}
}
