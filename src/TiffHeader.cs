public class TiffHeader {
	public ExifData Exif;

	public TiffHeader (string filename) {
		System.IO.FileStream stream = new System.IO.FileStream (filename, System.IO.FileMode.Open);
		byte [] data = new byte [110096];

		// FIXME libexif currently requires that the data you pass to it contains the "Exif\0\0" 
		// which is lame, so we fake it for now.
#if true
		System.Text.Encoding.ASCII.GetBytes ("Exif", 0, 4, data, 0);
		data [4] = 0x00;
		data [5] = 0x00;

		int len = stream.Read (data, 6, 110090);
		Exif = new ExifData (data, (uint)len + 6);
#else
		int len = stream.Read (data, 0, 8192);
		Exif = new ExifData (data, (uint)len);
#endif

		Exif.Assemble ();
	}

	static void Main (string [] args) {
		System.Console.WriteLine ("blah");
		TiffHeader h = new TiffHeader (args [0]);

		System.Console.WriteLine (h.Exif.LookupString (ExifTag.Model));
		System.Console.WriteLine (h.Exif.LookupString (ExifTag.Make));
		foreach (ExifTag tag in h.Exif.Tags) {
			System.Console.WriteLine ("{0} = {1}", 
						  ExifUtil.GetTagTitle (tag), 
						  h.Exif.LookupString (tag));
		}
	}
}
