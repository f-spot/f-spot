public class TiffHeader {
	public ExifData Exif;

	public byte [] GetRawExif () {
		return Exif.Data;
	}
	
	// according to http://partners.adobe.com/asn/tech/xmp/pdf/xmpspecification.pdf
	// the xmp data should live in tag id 700
	public byte [] GetRawXmp () {
		if (Exif != null)
			return Exif.LookupData ((ExifTag)0x02bc); 
		else
			return null;
	}

	public TiffHeader (string filename) {
		System.IO.FileStream stream = new System.IO.FileStream (filename, System.IO.FileMode.Open);
		byte [] data = new byte [110096];

		// FIXME libexif currently requires that the data you pass to it contains the "Exif\0\0" 
		// which is lame, so we fake it for now.
#if true
		System.Text.Encoding.ASCII.GetBytes ("Exif\0\0", 0, 6, data, 0);
		int len = stream.Read (data, 6, data.Length - 6);
		Exif = new ExifData (data, (uint)len + 6);
#else
		int len = stream.Read (data, 0, data.Length);
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

		byte [] value = h.GetRawXmp ();
		if (value != null) {
			System.Console.WriteLine ("XMP length={0}", value.Length);
			System.IO.FileStream stream = new System.IO.FileStream ("file.xmp", System.IO.FileMode.Create);
			stream.Write (value, 0, value.Length );
			stream.Close ();

			string xml = System.Text.Encoding.UTF8.GetString (value, 0, value.Length);
			System.Console.WriteLine (xml);
		}
	}
}
