using System.Runtime.InteropServices;
using System;
using Gdk;

public class JpegUtils {
//	[DllImport ("libfspot")]
//	static extern IntPtr f_load_scaled_jpeg (string path,
//						 int target_width,
//						 int target_height,
//						 out int original_width_return,
//						 out int original_height_return);
//
//	/* FIXME */
//	[DllImport("libgobject-2.0-0.dll")]
//	static extern void g_object_unref (IntPtr raw);
//
//	public static Pixbuf LoadScaled (string path, int target_width, int target_height,
//					 out int original_width, out int original_height)
//	{
//		Pixbuf pixbuf = new Pixbuf (f_load_scaled_jpeg (path, target_width, target_height,
//								out original_width, out original_height));
//		g_object_unref (pixbuf.Handle);
//		return pixbuf;
//	}

//	public static Pixbuf LoadScaled (string path, int target_width, int target_height)
//	{
//		int unused;
//		return LoadScaled (path, target_width, target_height, out unused, out unused);
//	}

	[DllImport ("libfspot")]
	static extern void f_save_jpeg_exif (string path, HandleRef data);

	public static void SaveExif (string path, Exif.ExifData data)
	{
		f_save_jpeg_exif (path, data.Handle);
	}		

	[DllImport ("libfspot")]
	static extern void f_get_jpeg_size (string path, out int width_return, out int height_return);

	public static void GetSize (string path, out int width_return, out int height_return)
	{
		f_get_jpeg_size (path, out width_return, out height_return);
	}

	public enum TransformType {
		Rotate90,
		Rotate180,
		Rotate270,
		FlipH,
		FlipV
	};

	[DllImport ("libfspot")]
	static extern bool f_transform_jpeg (string source_path, string destination_path, TransformType transform,
					     out string error_message_return);

	public static void Transform (string source_path, string destination_path, TransformType transform)
	{
		string error_message;

		if (! f_transform_jpeg (source_path, destination_path, transform, out error_message))
			throw new Exception (error_message);
	}
}
