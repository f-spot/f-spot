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

		public string Name {
			get {
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
				return new Marker (id, null);

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
				
				length = FSpot.BitConverter.Swap (length, false);
				
				stream.WriteByte ((byte)(length & 0x00ff));
				stream.WriteByte ((byte)((length >> 8) & 0x00ff));
				stream.Write (this.Data, 0, this.Data.Length);
				break;
			}
		}
	}

	public Marker FindMarker (JpegMarker id, string name)
	{
		foreach (Marker m in Markers) {
			if (m.Type == id && m.Name == name)
				return m;
		}
		return null;
	}
	
	public Exif.ExifData Exif {
		get {
			Marker m = FindMarker (JpegMarker.App1, "Exif");

			if (m  == null)
				return null;

			return new Exif.ExifData (m.Data);
		}
		set {
			Marker m;
			while ((m = FindMarker (JpegMarker.App1, "Exif")) != null)
				this.Markers.Remove (m);

			byte [] raw_data = value.Save ();
			for (int i = 1; i < Markers.Count; i++) {
				m = (Marker) this.Markers [i];

				if (m.IsApp && m.Type == JpegMarker.App0)
					continue;
				
				this.Markers.Insert (i, new Marker (JpegMarker.App1, raw_data));
				return;
			}
		}
	}

	/* JFIF JFXX ICC_PROFILE http://ns.adobe.com/xap/1.0/ */
	
	public System.Collections.ArrayList Markers {
		get {
			return marker_list;
		}
	}

	public void Save (System.IO.Stream stream)
	{
		foreach (Marker marker in marker_list) {
			System.Console.WriteLine ("saving marker {0} {1}", marker.Type, 
						   (marker.Data != null) ? marker.Data.Length .ToString (): "(null)");
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
		image_data = null;
		bool at_image = false;

		Marker marker = Marker.Load (stream);
		if (marker.Type != JpegMarker.Soi)
			throw new System.Exception ("This doesn't appear to be a jpeg stream");
		
		this.Markers.Add (marker);
		while (!at_image) {
			marker = Marker.Load (stream);

			if (marker == null)
				continue;

			System.Console.WriteLine ("loaded marker {0} length {1}", marker.Type, marker.Data.Length);

			this.Markers.Add (marker);
			
			if (marker.Type == JpegMarker.Sos)
				at_image = true;
			
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
