using System.Runtime.InteropServices;
using System;
using Gdk;

namespace FSpot.Imaging {
public class JpegUtils {

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
}
