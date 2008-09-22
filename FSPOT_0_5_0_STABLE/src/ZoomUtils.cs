using System;
using System.Runtime.InteropServices;

public class ZoomUtils {

	[DllImport ("libfspoteog")]
	static extern void zoom_fit_size (uint dest_width, uint dest_height,
					  uint src_width, uint src_height,
					  bool upscale_smaller,
					  out uint width, out uint height);

	public static void FitToSize (uint dest_width, uint dest_height, uint src_width, uint src_height,
				      bool upscale_smaller, out uint width, out uint height)
	{
		zoom_fit_size (dest_width, dest_height, src_width, src_height, upscale_smaller, out width, out height);
	}


	[DllImport ("libfspoteog")]
	static extern double zoom_fit_scale (uint dest_width, uint dest_height,
					     uint src_width, uint src_height,
					     bool upscale_smaller);

	public static double FitToScale (uint dest_width, uint dest_height, uint src_width, uint src_height, bool upscale_smaller)
	{
		return zoom_fit_scale (dest_width, dest_height, src_width, src_height, upscale_smaller);
	}
}
