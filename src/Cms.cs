//
// A very incomplete wrapper for lcms
//

using System;
using System.Runtime.InteropServices;

namespace Cms {
	public enum Format {
		Rgb8  = 262169,
		Rgba8 = 262297,
		Gbr8  = 263193
	}
	

	public enum Intent {
		Perceptual = 0,
		RelativeColorimetric = 1,
		Saturation = 2,
		AbsoluteColorimetric = 3
	}
		
	public class Transform : IDisposable {
		private IntPtr handle;
		public IntPtr Handle {
			get {
				return handle;
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateMultiprofileTransform (IntPtr [] hProfiles,
								     int nProfiles,
								     short InputFormat,
								     short OutputFormat,
								     int Intent,
								     short dwFlags);

		public Transform (Profile [] profiles,
				  Format input_format,
				  Format output_format,
				  Intent intent, short flags)
		{
			IntPtr [] handles = new IntPtr [profiles.Length];
			for (int i = 0; i < profiles.Length; i++) {
				handles [i] = profiles [i].Handle;
			}

			this.handle = cmsCreateMultiprofileTransform (handles, handles.Length, 
								      (short)input_format,
								      (short) output_format, 
								      (int)intent, flags);
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateTransform(IntPtr Input,
							short InputFormat,
							IntPtr Output,
							short OutputFormat,
							int Intent,
							short dwFlags);

		public Transform (Profile input, Format input_format,
				  Profile output, Format output_format,
				  Intent intent, short flags)
		{
			this.handle = cmsCreateTransform (input.Handle, (short)input_format,
							  output.Handle, (short)output_format,
							  (int)intent, (short)flags);
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsDoTransform (IntPtr hTransform, IntPtr InputBuffer, IntPtr OutputBuffer, uint size);
		
		public void Apply (IntPtr input, IntPtr output, uint size)
		{
			cmsDoTransform (this.handle, input, output, size);
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern void cmsDeleteTransform(IntPtr hTransform);
		
		public void Dispose () 
		{
			cmsDeleteTransform (this.handle);
		}
	}

	public class Profile : IDisposable {
		private IntPtr handle;
		public IntPtr Handle {
			get {
				return handle;
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreate_sRGBProfile();
		
		public static Profile CreateSRgb () 
		{
			return new Profile (cmsCreate_sRGBProfile());
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
		static extern int cmsCloseProfile (IntPtr hprofile);
		
		public void Dispose () 
		{
			if (cmsCloseProfile (this.Handle) == 0)
				throw new Exception ("Error closing Handle");
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsOpenProfileFromFile (string ICCProfile, string sAccess);

		public Profile (string path) 
		{
			handle = cmsOpenProfileFromFile (path, "r");
			if (handle == IntPtr.Zero)
				throw new Exception ("Error opening ICC profile in file " + path);
		}
		
#if false
		[DllImport("liblcms-1.0.0.dll")]
		static unsafe extern IntPtr cmsOpenProfileFromMem (byte *data, uint length);

		public unsafe Profile (byte [] data)
		{
			handle = cmsOpenProfileFromMem ((byte *) data, data.Length);
		}
#endif	
		protected Profile (IntPtr handle)
		{
			this.handle = handle;
		}
	}
}
