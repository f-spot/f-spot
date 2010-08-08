using System;
using System.Runtime.InteropServices;

namespace Cms
{
	internal static class NativeMethods
	{
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsWhitePointFromTemp")]
		public static extern bool CmsWhitePointFromTemp(int TempSrc,  out ColorCIExyY white_point);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsxyY2XYZ")]
		public static extern void CmsxyY2XYZ (out ColorCIEXYZ dest, ref ColorCIExyY src);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsD50_xyY")]
		public static extern IntPtr CmsD50xyY();

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsXYZ2xyY")]
		public static extern void CmsXYZ2xyY (out ColorCIExyY dest, ref ColorCIEXYZ source);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsXYZ2Lab")]
		public static extern void CmsXYZ2Lab (ref ColorCIEXYZ wp, out ColorCIELab lab, ref ColorCIEXYZ xyz);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsD50_XYZ")]
		public static extern IntPtr CmsD50XYZ();

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsLab2LCh")]
		public static extern void CmsLab2LCh (out ColorCIELCh lch, ref ColorCIELab lab);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsLab2XYZ")]
		public static extern void CmsLab2XYZ (ref ColorCIEXYZ wp, out ColorCIEXYZ xyz, ref ColorCIELab lab);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsLCh2Lab")]
		public static extern void CmsLCh2Lab (out ColorCIELab lab, ref ColorCIELCh lch);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsBuildGamma")]
		public static extern IntPtr CmsBuildGamma (int entry_count, double gamma);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsBuildParametricGamma")]
		public static extern IntPtr CmsBuildParametricGamma (int entry_count, int type, double [] values);
		
//		[DllImport("liblcms-1.0.0.dll")]
//		public static extern IntPtr cmsAllocGamma (int entry_count);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsFreeGamma")]
		public static extern void CmsFreeGamma (HandleRef handle);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsCreateMultiprofileTransform")]
		public static extern IntPtr CmsCreateMultiprofileTransform (HandleRef [] hProfiles,
								     int nProfiles,
								     Format InputFormat,
								     Format OutputFormat,
								     int Intent,
								     uint dwFlags);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsCreateTransform")]
		public static extern IntPtr CmsCreateTransform(HandleRef Input,
							Format InputFormat,
							HandleRef Output,
							Format OutputFormat,
							int Intent,
							uint dwFlags);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsDoTransform")]
		public static extern void CmsDoTransform (HandleRef hTransform, IntPtr InputBuffer, IntPtr OutputBuffer, uint size);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsDeleteTransform")]
		public static extern void CmsDeleteTransform(HandleRef hTransform);
		
		[DllImport ("liblcms-1.0.0.dll", EntryPoint = "cmsCreateGrayProfile")]
		public static extern IntPtr CmsCreateGrayProfile (ref ColorCIExyY white_point,
							   HandleRef transfer_function);
		
		[DllImport ("liblcms-1.0.0.dll", EntryPoint = "cmsCreateLabProfile")]
		public static extern IntPtr CmsCreateLabProfile (IntPtr foo);

		[DllImport ("liblcms-1.0.0.dll", EntryPoint = "cmsCreateLabProfile")]
		public static extern IntPtr CmsCreateLabProfile (out ColorCIExyY WhitePoint);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsCreate_sRGBProfile")]
		public static extern IntPtr CmsCreateSRGBProfile ();

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "_cmsCreateProfilePlaceholder")]
		public static extern IntPtr CmsCreateProfilePlaceholder ();

//		[DllImport("liblcms-1.0.0.dll")]
//		public static extern IntPtr cmsCreateBCHSWabstractProfile(int nLUTPoints,
//								   double Bright, 
//								   double Contrast,
//								   double Hue,
//								   double Saturation,
//								   int TempSrc, 
//								   int TempDest);
	
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsErrorAction")]
		public static extern void CmsErrorAction (int action);
		
		[DllImport ("liblcms-1.0.0.dll", EntryPoint = "cmsGetColorSpace")]
		public static extern uint CmsGetColorSpace (HandleRef hprofile);

		[DllImport ("liblcms-1.0.0.dll", EntryPoint = "cmsGetDeviceClass")]
		public static extern uint CmsGetDeviceClass (HandleRef hprofile);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsTakeProductDesc")]
		public extern static IntPtr CmsTakeProductDesc (HandleRef handle);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsTakeProductName")]
		public extern static IntPtr CmsTakeProductName (HandleRef handle);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsTakeModel")]
		public extern static IntPtr CmsTakeModel (HandleRef handle);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsTakeColorants")]
		public extern static bool CmsTakeColorants (out ColorCIEXYZTriple colors, HandleRef handle);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsTakeMediaBlackPoint")]
		public extern static bool CmsTakeMediaBlackPoint (out ColorCIEXYZ black, HandleRef handle);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsTakeMediaWhitePoint")]
		public extern static bool CmsTakeMediaWhitePoint (out ColorCIEXYZ wp, HandleRef handle);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsOpenProfileFromMem")]
		public static extern unsafe IntPtr CmsOpenProfileFromMem (byte *data, uint length);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "_cmsSaveProfileToMem")]
		public static extern unsafe bool CmsSaveProfileToMem (HandleRef profile, byte *mem, ref uint length);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsOpenProfileFromFile")]
		public static extern IntPtr CmsOpenProfileFromFile (string ICCProfile, string sAccess);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsCloseProfile")]
		public static extern int CmsCloseProfile (HandleRef hprofile);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsCreateRGBProfile")]
		public static extern IntPtr CmsCreateRGBProfile (out ColorCIExyY whitepoint, 
						          out ColorCIExyYTriple primaries,
							  HandleRef [] gamma_table);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsCreateLinearizationDeviceLink")]
		public static extern IntPtr CmsCreateLinearizationDeviceLink (IccColorSpace color_space, HandleRef [] tables);
		
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

		[DllImport ("libfspot", EntryPoint = "f_cms_gamma_table_new")]
		public static extern IntPtr FCmsGammaTableNew (ushort [] values, int start, int length);

		[DllImport ("libfspot", EntryPoint = "f_cms_gamma_table_get_values")]
		public static extern IntPtr FCmsGammaTableGetValues (HandleRef table);

		[DllImport ("libfspot", EntryPoint = "f_cms_gamma_table_get_count")]
		public static extern int FCmsGammaTableGetCount (HandleRef table);	
	}
}
