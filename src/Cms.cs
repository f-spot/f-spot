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
		private HandleRef handle;
		public HandleRef Handle {
			get {
				return handle;
			}
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateMultiprofileTransform (HandleRef [] hProfiles,
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
			HandleRef [] handles = new HandleRef [profiles.Length];
			for (int i = 0; i < profiles.Length; i++) {
				handles [i] = profiles [i].Handle;
			}

			this.handle = new HandleRef (this, cmsCreateMultiprofileTransform (handles, handles.Length, 
											   (short)input_format,
											   (short) output_format, 
											   (int)intent, flags));
		}

		[DllImport("liblcms-1.0.0.dll")]
		static extern IntPtr cmsCreateTransform(HandleRef Input,
							short InputFormat,
							HandleRef Output,
							short OutputFormat,
							int Intent,
							short dwFlags);

		public Transform (Profile input, Format input_format,
				  Profile output, Format output_format,
				  Intent intent, short flags)
		{
			this.handle = new HandleRef (this, cmsCreateTransform (input.Handle, (short)input_format,
									       output.Handle, (short)output_format,
									       (int)intent, (short)flags));
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
		
		private void Cleanup () {
			cmsDeleteTransform (this.handle);
		}
		
		~Transform () 
		{
			Cleanup ();
		}
	       
	}

	public class Profile : IDisposable {
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
		static extern IntPtr cmsOpenProfileFromMem (byte [] data, uint length);

		public unsafe Profile (byte [] data)
		{
			this.handle = new HandleRef (this, cmsOpenProfileFromMem (data, (uint)data.Length));
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
