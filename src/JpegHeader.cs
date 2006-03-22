using System;
using System.IO;
using FSpot.Xmp;

public class JpegHeader : SemWeb.StatementSource {
	public enum JpegMarker {
		Tem = 0x01,
		Rst0 = 0xd0,  // RstN used for resync, ignore
		Rst1 = 0xd1,
		Rst2 = 0xd2,
		Rst3 = 0xd3,
		Rst4 = 0xd4,
		Rst5 = 0xd5,
		Rst6 = 0xd6,
		Rst7 = 0xd7,
		
		Sof0 = 0xc0, // SOFn Start of frame 0-1 common
		Sof1 = 0xc1,
		Sof2 = 0xc2,
		Sof3 = 0xc3,
		
		Dht = 0xc4,  // Define Huffman Table
		
		Sof5 = 0xc5,
		Sof6 = 0xc6,
		Sof7 = 0xc7,
		
		Jpg = 0xc8, // reserved
		
		Sof9 = 0xc9,
		Sof10 = 0xca,
		Sof11 = 0xcb,
		Sof12 = 0xcc,
		Sof13 = 0xcd,
		Sof14 = 0xce,
		Sof15 = 0xcf,

		// These tags all consist of a marker and then a length.
		
		// These are the major structure tags.
		Soi  = 0xd8,  // Start of Image
		Eoi  = 0xd9,  // End of Image
		Sos  = 0xda,  // Start of Scan
		
		Dnl = 0xdc,
		Dri = 0xdd,  // Define restart interval
		Dhp = 0xde,
		Exp = 0xdf, 
		
		Dqt = 0xdb, // Define Quantization Table
		
		// These are the app marker tags that contain the application metadata
		// in its various forms.
		App0 = 0xe0,  // AppN Markers for application data
		App1 = 0xe1,
		App2 = 0xe2,
		App3 = 0xe3,
		App4 = 0xe4,
		App5 = 0xe5,
		App6 = 0xe6,
		App7 = 0xe7,
		App8 = 0xe8,
		App9 = 0xe9,
		App10 = 0xea,
		App11 = 0xeb,
		App12 = 0xec,
		App13 = 0xed,
		App14 = 0xee,
		App15 = 0xef,
		
		Jpg0 = 0xf0,
		Jpg1 = 0xf1,
		Jpg2 = 0xf2,
		Jpg3 = 0xf3,
		Jpg4 = 0xf4,
		Jpg5 = 0xf5,
		Jpg6 = 0xf6,
		Jpg7 = 0xf7,
		Jpg8 = 0xf8,
		Jpg9 = 0xf9,
		Jpg10 = 0xfa,
		Jpg11 = 0xfb,
		Jpg12 = 0xfc,
		Jpg13 = 0xfd,
		
		Com = 0xfe // Comment
	}	

	private System.Collections.ArrayList marker_list = new System.Collections.ArrayList ();	
	private byte [] image_data;

	public class Marker {
		public JpegMarker Type;
		public byte [] Data;
		
		public Marker (JpegMarker type, byte [] data)
		{
			this.Type = type;
			this.Data = data;
		}

		public bool IsApp {
			get {
				return (this.Type >= JpegMarker.App0 && this.Type <= JpegMarker.App15);
			}
		}

		public bool Matches (Signature sig) 
		{
			if (Type == sig.Id) {
				if (sig.Name == null)
					return true;
				
				byte [] name = System.Text.Encoding.ASCII.GetBytes (sig.Name);

				for (int i = 0; i < name.Length; i++)
					if (Data [i] != name [i])
						return false;
				
				return true;
			}
			return false;
		}

		public string GetName ()
		{
			if (!this.IsApp)
				return null;
			
			int j;
			for (j = 0; j < this.Data.Length; j++) {
					if (this.Data [j] == 0x00)
						break;
					
			}
			
			if (j > 0)
				return System.Text.Encoding.ASCII.GetString (this.Data, 0, j);
			else 
				return null;
		}

		public static Marker Load (System.IO.Stream stream) {
			byte [] raw = new byte [2];
			ushort length;
		       
			if (stream.Length - stream.Position < 2)
				return null;

			// FIXME there is a potential loop here.
			
			raw [0] = (byte)stream.ReadByte ();
			if (raw [0] != 0xff)
				throw new System.Exception (System.String.Format ("Invalid marker found {0}", raw [0]));
			
			JpegMarker id = (JpegMarker) stream.ReadByte ();
			switch (id) {
			case JpegMarker.Soi:
			case JpegMarker.Eoi:
			case JpegMarker.Rst0:
			case JpegMarker.Rst1:
			case JpegMarker.Rst2:
			case JpegMarker.Rst3:
			case JpegMarker.Rst4:
			case JpegMarker.Rst5:
			case JpegMarker.Rst6:
			case JpegMarker.Rst7:
			case JpegMarker.Tem: 
			case (JpegMarker) 0:
				return new Marker (id, null);
			default:
				stream.Read (raw, 0, 2);
				length = FSpot.BitConverter.ToUInt16 (raw, 0, false);
				
				byte [] data = new byte [length - 2];
				stream.Read (data, 0, data.Length);
				return new Marker (id, data);
			}
			
		}

		public void Save (System.IO.Stream stream) {
			/* 
			 * It is possible we should just base this choice off the existance
			 * of this.Data, but I'm not sure so I'll do it this way for now
			 */
			
			switch (this.Type) {
			case JpegMarker.Soi:
			case JpegMarker.Eoi:
				stream.WriteByte (0xff);				
				stream.WriteByte ((byte)this.Type);
				break;
			default:
				stream.WriteByte (0xff);				
				stream.WriteByte ((byte)this.Type);
				ushort length = (ushort)(this.Data.Length + 2);
				
				byte [] len = FSpot.BitConverter.GetBytes (length, false);
				stream.Write (len, 0, len.Length);

				stream.Write (this.Data, 0, this.Data.Length);
				break;
			}
		}
	}

	public static Signature JfifSignature = new Signature (JpegMarker.App0, "JFIF\0");
	public static Signature JfxxSignature = new Signature (JpegMarker.App0, "JFXX\0");
	public static Signature XmpSignature = new Signature (JpegMarker.App1, "http://ns.adobe.com/xap/1.0/\0");
	public static Signature ExifSignature = new Signature (JpegMarker.App1, "Exif\0\0");
	public static Signature IccProfileSignature = new Signature (JpegMarker.App2, "ICC_PROFILE\0");
	public static Signature PhotoshopSignature = new Signature (JpegMarker.App13, "Photoshop 3.0\0");

	public class Signature {
		public JpegMarker Id;
		public string Name;

		public Signature (JpegMarker marker, string name)
		{
			Id = marker;
			Name = name;
		}

		public int WriteName (Stream stream)
		{
			byte [] sig = System.Text.Encoding.ASCII.GetBytes (Name);
			stream.Write (sig, 0, sig.Length);
			return sig.Length;
		}
	}	

	public Marker FindMarker (Signature sig)
	{
		foreach (Marker m in Markers) {
			if (m.Matches (sig))
				return m;
		}

		return null;
	}


	public Marker FindMarker (JpegMarker id, string name)
	{
		return FindMarker (new Signature (id, name));
	}

	public Cms.Profile GetProfile ()
	{
		Marker m = FindMarker (IccProfileSignature);
		string name = IccProfileSignature.Name;
		try {
			if (m != null)
				return new Cms.Profile (m.Data, name.Length, m.Data.Length - name.Length); 
		} catch (System.Exception e) {
			System.Console.WriteLine (e);
		}
		
		FSpot.Tiff.Header exif = GetExifHeader ();
		if (exif != null)
			return exif.Directory.GetProfile ();
		
		return null;
	}
	
	public FSpot.Tiff.Header GetExifHeader ()
	{
		string name = ExifSignature.Name;
		Marker marker = FindMarker (ExifSignature);

		if (marker == null)
			return null;
		
		using (System.IO.Stream exifstream = new System.IO.MemoryStream (marker.Data, name.Length, marker.Data.Length - name.Length, false)) {
			FSpot.Tiff.Header exif = new FSpot.Tiff.Header (exifstream);
			return exif;
		}
	}

	public XmpFile GetXmp ()
	{
		string name = XmpSignature.Name;
		Marker marker = FindMarker (XmpSignature);
		if (marker != null) {
			int len = name.Length;
			//System.Console.WriteLine (System.Text.Encoding.ASCII.GetString (marker.Data, len, 
			//								marker.Data.Length - len));
			using (System.IO.Stream xmpstream = new System.IO.MemoryStream (marker.Data, len, 
											marker.Data.Length - len, false)) {
			
				XmpFile xmp = new XmpFile (xmpstream);					
				return xmp;
			}
		}
		return null;
	}

	public void Select (SemWeb.StatementSink sink)
	{
		FSpot.Tiff.Header exif = GetExifHeader ();
		if (exif != null)
			exif.Select (sink);
		
		XmpFile xmp = GetXmp ();
		if (xmp != null)
			xmp.Select (sink);
		
		string name = PhotoshopSignature.Name;
		JpegHeader.Marker marker = FindMarker (PhotoshopSignature);
		if (marker != null) {
			int len = name.Length;
			using (System.IO.Stream bimstream = new System.IO.MemoryStream (marker.Data, len, marker.Data.Length - len, false)) {
				FSpot.Bim.BimFile bim = new FSpot.Bim.BimFile (bimstream);
				bim.Select (sink);
			}
		}
	}

	public Exif.ExifData Exif {
		get {
			Marker m = FindMarker (ExifSignature);

			if (m  == null)
				return null;

			return new Exif.ExifData (m.Data);
		}
	}

	public void Replace (Signature sig, Marker data)
	{
		bool added = false;
		for (int i = 1; i < Markers.Count; i++) {
			Marker m = (Marker) Markers [i];
			
			if (m.Matches (sig)) {
				
				if (!added) {
					Markers [i] = data;
					added = true;
				} else
					Markers.RemoveAt (i--);
				
			} else if (!m.IsApp || m.Type > sig.Id) {
				if (!added) {
					Markers.Insert (i, data); 
					added = true;
				}
			}
		}

		if (!added)
			throw new System.Exception (String.Format ("unable to replace {0} marker", sig.Name));
	}

	public void SetExif (Exif.ExifData value)
	{
		byte [] raw_data = value.Save ();
		Marker exif = new Marker (ExifSignature.Id, raw_data);

		Replace (ExifSignature, exif);
	}	
	
	public void SetXmp (XmpFile xmp)
	{
		using (MemoryStream stream = new MemoryStream ()) {
			
			XmpSignature.WriteName (stream);
			xmp.Save (stream);
			
			Marker xmp_marker = new Marker (XmpSignature.Id, stream.ToArray ());
			Replace (XmpSignature, xmp_marker);
		}
	}
	
	public System.Collections.ArrayList Markers {
		get {
			return marker_list;
		}
	}

	public void Save (System.IO.Stream stream)
	{
		foreach (Marker marker in marker_list) {
			//System.Console.WriteLine ("saving marker {0} {1}", marker.Type, 
			//			   (marker.Data != null) ? marker.Data.Length .ToString (): "(null)");
			marker.Save (stream);
			if (marker.Type == JpegMarker.Sos)
				stream.Write (ImageData, 0, ImageData.Length);
		}
	}

	public JpegHeader (System.IO.Stream stream)
	{
		Load (stream, false);
	}

	public JpegHeader (System.IO.Stream stream, bool metadata_only)
	{
		Load (stream, metadata_only);
	}

	private void Load (System.IO.Stream stream, bool metadata_only) 
	{
		marker_list.Clear ();
		image_data = null;
		bool at_image = false;

		Marker marker = Marker.Load (stream);
		if (marker.Type != JpegMarker.Soi)
			throw new System.Exception ("This doesn't appear to be a jpeg stream");
		
		this.Markers.Add (marker);
		while (!at_image) {
			marker = Marker.Load (stream);

			if (marker == null)
				break;

			//System.Console.WriteLine ("loaded marker {0} length {1}", marker.Type, marker.Data.Length);

			this.Markers.Add (marker);
			
			if (marker.Type == JpegMarker.Sos) {
				at_image = true;

				if (metadata_only)
					return;
			}
		}

		long image_data_length = stream.Length - stream.Position;
		this.image_data = new byte [image_data_length];

		if (stream.Read (image_data, 0, (int)image_data_length) != image_data_length)
			throw new System.Exception ("truncated image data or something");
	}

	public byte [] ImageData {
		get {
			return image_data;
		}
	}
#if false
	public static int Main (string [] args)
	{
		JpegHeader data = new JpegHeader (args [0]);
		byte [] value = data.GetRawXmp ();

		if (value != null) {
			string xml = System.Text.Encoding.UTF8.GetString (value, 29, value.Length - 29);
			System.Console.WriteLine (xml);
		}
		
		value = data.GetRaw ("ICC_PROFILE");
		if (value != null) {
			System.IO.FileStream stream = new System.IO.FileStream ("profile.icc", System.IO.FileMode.Create);
			stream.Write (value, 12, value.Length - 12);
			stream.Close ();
		}

		value = data.GetRawExif ();
		
		
		//System.IO.Stream ostream = System.IO.File.Open ("/home/lewing/test.jpg", System.IO.FileMode.OpenOrCreate);
		//data.Save (ostream);
		//ostream.Position = 0;
		//data = new JpegHeader (ostream);

		return 0;
	}
#endif
}
