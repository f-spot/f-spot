public class JpegHeader {
	public enum JpegMarker {
		// The Following tags are listed as containing the content within the
		// marker itself and the data is stored in the two bytes that would
		// otherwise hold the length.
		Tem = 0x01, // no length, ignore
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
		Sof10 = 0xc10,
		Sof11 = 0xc11,
		Sof12 = 0xc12,
		Sof13 = 0xc13,
		Sof14 = 0xc14,
		Sof15 = 0xc15,

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

	public class Marker {
		public Marker (JpegMarker type, byte [] data, long position)
		{
			this.Type = type;
			this.Data = data;
			this.Position = position;
		}
		
		public JpegMarker Type;
		public byte [] Data;
		public long Position;
	}

	public byte [] GetRawXmp ()
	{
		return this.GetRaw ("http://ns.adobe.com/xap/1.0/");
	}
	
	public byte [] GetRawExif ()
	{
		return this.GetRaw ("Exif");
	}

	public byte [] GetRawJfif ()
	{
		return this.GetRaw ("JFIF");
	}

	public byte [] GetRawIcc ()
	{
		return this.GetRaw ("ICC_PROFILE");
	}

	public byte [] GetRaw (string name)
	{
		Marker m = (Marker)app_marker_hash [name];
		if (m != null)
			return m.Data;
		else 
			return null;
	}

	private void AddNamed (Marker m)
	{
		int j;
		for (j = 0; j < m.Data.Length; j++) {
			if (m.Data [j] == 0x00)
				break;
		}
		
		string header = System.Text.Encoding.ASCII.GetString (m.Data, 0, j);
		app_marker_hash [header] = m;
		System.Console.WriteLine (header);
	}

	System.Collections.Hashtable app_marker_hash = new System.Collections.Hashtable ();
	System.Collections.ArrayList marker_list = new System.Collections.ArrayList ();	

	public JpegHeader (string filename) 
	{
		System.Console.WriteLine ("opening {0}", filename);
		System.IO.FileStream stream = new System.IO.FileStream (filename, System.IO.FileMode.Open);
		byte [] length_data = new byte [2];
		
		if (stream.ReadByte () != 0xff || (JpegMarker)stream.ReadByte () != JpegMarker.Soi)
			throw new System.Exception ("Invalid file Type, not a JPEG file");
		
		bool at_image = false;
		while (!at_image) {
			int i = 0;
			JpegMarker marker;
			
			// 0xff can be used as padding between markers, ignore it all
			// of it for now.
			do {
				marker = (JpegMarker)stream.ReadByte ();
				i++;
			} while (marker == (JpegMarker)0xff);
			

			if (i < 2)
				throw new System.Exception ("Invalid Marker");
			
			long position = stream.Position - 1;

			// FIXME use real byteswapping later
			if (System.BitConverter.IsLittleEndian) {
				length_data [1] = (byte)stream.ReadByte ();
				length_data [0] = (byte)stream.ReadByte ();
			} else {
				length_data [0] = (byte)stream.ReadByte ();
				length_data [1] = (byte)stream.ReadByte ();
			}
			
			ushort length = System.BitConverter.ToUInt16 (length_data, 0);
			System.Console.WriteLine ("Marker {0} Length = {1}", marker.ToString (), length);
			
			if (length < 2)
				throw new System.Exception ("Invalid Marker length");
			
			length -= 2;
			
			switch (marker) {
			case JpegMarker.App0:
			case JpegMarker.App1:
			case JpegMarker.App2:
			case JpegMarker.App3:
			case JpegMarker.App4:
			case JpegMarker.App5:
			case JpegMarker.App6:
			case JpegMarker.App7:
			case JpegMarker.App8:
			case JpegMarker.App9:
			case JpegMarker.App10:
			case JpegMarker.App11:
			case JpegMarker.App12:
			case JpegMarker.App13:
			case JpegMarker.App14:
			case JpegMarker.App15:
				byte [] data = new byte [length];
				if (stream.Read (data, 0, length) != length)
					throw new System.Exception ("Incomplete Marker");

				Marker m = new Marker (marker, data, position);
				marker_list.Add (m);
				this.AddNamed (m);
				break;
			case JpegMarker.Rst0:
			case JpegMarker.Rst1:
			case JpegMarker.Rst2:
			case JpegMarker.Rst3:
			case JpegMarker.Rst4:
			case JpegMarker.Rst5:
			case JpegMarker.Rst6:
			case JpegMarker.Rst7:
			case JpegMarker.Tem:
				// These markers have no data it is in length_data
				
				marker_list.Add (new Marker (marker, length_data, position));
				break;
			default:
				byte [] d = new byte [length];
				if (stream.Read (d, 0, length) != length)
					throw new System.Exception ("Incomplete Marker");
				
				marker_list.Add (new Marker (marker, d, position));
				break;
			}
			
			if (marker == JpegMarker.Sos)
				at_image = true;
		}
	}

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
		if (value != null) {
			ExifData exif = new ExifData (value, (uint)value.Length);
			System.Console.WriteLine (exif.LookupString (ExifTag.Model));
		}

		return 0;
	}

}
