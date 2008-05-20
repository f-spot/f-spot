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
		public static extern IntPtr CmsD50_xyY();

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsXYZ2xyY")]
		public static extern void CmsXYZ2xyY (out ColorCIExyY dest, ref ColorCIEXYZ source);
		
		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsXYZ2Lab")]
		public static extern void CmsXYZ2Lab (ref ColorCIEXYZ wp, out ColorCIELab lab, ref ColorCIEXYZ xyz);

		[DllImport("liblcms-1.0.0.dll", EntryPoint = "cmsD50_XYZ")]
		public static extern IntPtr CmsD50_XYZ();

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
		

		[DllImport ("libfspot")]
		public static extern IntPtr f_cms_gamma_table_new (ushort [] values, int start, int length);

		[DllImport ("libfspot")]
		public static extern IntPtr f_cms_gamma_table_get_values (HandleRef table);

		[DllImport ("libfspot")]
		public static extern int f_cms_gamma_table_get_count (HandleRef table);

	
	}
}
