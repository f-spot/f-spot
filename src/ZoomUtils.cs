using System;

public class ZoomUtils {
	public static double FitToScale (uint dest_width, uint dest_height, uint src_width, uint src_height, bool upscale_smaller)
	{
		if (src_width == 0 || src_height == 0)
			return 1.0;

		if (dest_width == 0 || dest_height == 0)
			return 0.0;

		if (src_width <= dest_width && src_height <= dest_height && !upscale_smaller)
			return 1.0;

		return Math.Min ((double)dest_width / src_width, (double)dest_height / src_height);
	}
}
