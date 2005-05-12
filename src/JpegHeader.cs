public class JpegHeader {
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

	private System.Collections.Hashtable app_marker_hash = new System.Collections.Hashtable ();
	private System.Collections.ArrayList marker_list = new System.Collections.ArrayList ();	
	private byte [] image_data;

	public class Marker {
		public JpegMarker Type;
		public byte [] Data;
		public long Position;
		
		public Marker (JpegMarker type, byte [] data, long position)
		{
			this.Type = type;
			this.Data = data;
			this.Position = position;
		}
		
		public bool IsApp {
			get {
				return (this.Type >= JpegMarker.App0 && this.Type <= JpegMarker.App15);
			}
		}

		public static Marker Load (System.IO.Stream stream) {
			byte [] raw = new byte [2];
			ushort length;
			
			raw [0] = (byte)stream.ReadByte ();
			if (raw [0] != 0xff)
				throw new System.Exception (System.String.Format ("Invalid marker found {0}", raw [0]));
			
			JpegMarker id = (JpegMarker) stream.ReadByte ();
			switch (id) {
			case JpegMarker.Soi:
			case JpegMarker.Eoi:
				return new Marker (id, null, stream.Position);

			/*  These rst* and tem can be skipped but I'm not sure of the circumstances right now */
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
				System.Console.WriteLine ("found marker = {0}, skipping", id);
				return null;
			default:
				stream.Read (raw, 0, 2);
				length = System.BitConverter.ToUInt16 (raw, 0);
				if (System.BitConverter.IsLittleEndian)
					length = (ushort) ((length >> 8) | (ushort) (length << 8));
				
				byte [] data = new byte [length - 2];
				stream.Read (data, 0, data.Length);
				return new Marker (id, data, stream.Position);
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
				
				if (System.BitConverter.IsLittleEndian)
					length = (ushort) ((length >> 8) | (ushort) (length << 8));
				
				stream.WriteByte ((byte)(length & 0x00ff));
				stream.WriteByte ((byte)((length >> 8) & 0x00ff));
				stream.Write (this.Data, 0, this.Data.Length);
				break;
			}
		}
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
		// If JFIF exists there might also be JFIF extensions following it
		// the extension name is "JFXX"
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
		System.Console.WriteLine ("Found {0} marker with header {1}", m.Type, header);
	}

	public void Save (System.IO.Stream stream)
	{
		foreach (Marker marker in marker_list) {
			System.Console.WriteLine ("saving marker {0}", marker.Type);
			marker.Save (stream);
			if (marker.Type == JpegMarker.Sos)
				stream.Write (ImageData, 0, ImageData.Length);
		}
	}

	public JpegHeader (System.IO.Stream stream)
	{
		Load (stream);
	}

	public JpegHeader (string filename) 
	{
		System.Console.WriteLine ("opening {0}", filename);
		System.IO.FileStream stream = new System.IO.FileStream (filename, System.IO.FileMode.Open);
		Load (stream);
	}

	private void Load (System.IO.Stream stream) 
	{
		marker_list.Clear ();
		app_marker_hash.Clear ();
		bool at_image = false;
		Marker marker = Marker.Load (stream);
		if (marker.Type != JpegMarker.Soi)
			throw new System.Exception ("This doesn't appear to be a jpeg stream");
		
		this.marker_list.Add (marker);
		while (!at_image) {
			marker = Marker.Load (stream);

			if (marker == null)
				continue;

			System.Console.WriteLine ("loaded marker {0} length {1}", marker.Type, marker.Data.Length);

			this.marker_list.Add (marker);
			if (marker.IsApp)
				this.AddNamed (marker);
			
			if (marker.Type == JpegMarker.Sos)
				at_image = true;
			
		}
		
		long image_data_length = stream.Length - stream.Position - 2;
		this.image_data = new byte [image_data_length];

		if (stream.Read (image_data, 0, (int)image_data_length) != image_data_length)
			throw new System.Exception ("truncated image data or something");
		
		marker = Marker.Load (stream);

		if (marker.Type != JpegMarker.Eoi)
			throw new System.Exception ("couldn't find eoi marker");
		this.marker_list.Add (marker);
	}

	

	public byte [] ImageData {
		get {
			return image_data;
		}
	}
#if true
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
		
		
		System.IO.Stream ostream = System.IO.File.Open ("/home/lewing/test.jpg", System.IO.FileMode.OpenOrCreate);
		data.Save (ostream);
		ostream.Position = 0;
		
		data = new JpegHeader (ostream);

		return 0;
	}
#endif
}
