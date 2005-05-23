namespace FSpot.Ciff {
	public enum Tag {
		// Byte valuesad
		NullRecord = 0x0000,
		FreeBytes = 0x0001,
		CanonColorInfo1 = 0x0032,

		// ASCII strings
		CanonFileDescription = 0x0805,
		UserComment = 0x0805, // FIXME this is the same as above, is it correct?
		CanonRawMakeModel = 0x080a, 
		CanonFirmwareVersion = 0x080b,
		ComponentVersion = 0x08c,
		ROMOperationMode = 0x08d,
		OwnerName = 0x0810,
		CanonImageType = 0x0815,
		OriginalFileName = 0x0816,
		ThumbnailFileName = 0x0817,
		
		// Short values
		TargetImageType = 0x100a,
		ShutterReleaseMethod = 0x1010,
		ShutterReleaseTiming = 0x1011,
		ReleaseSetting = 0x1016,
		BaseISO = 0x101c,

		Uknown2 = 0x1028,

		FocalLength = 0x1029,
		CanonShotInfo = 0x102a,
		CanonColorInfo2 = 0x102c,
		CanonCameraSettings = 0x102d,
		WhiteSample = 0x1030,
		SensorInfo = 0x1031,
		CanonCustomFunctions = 0x1033,
		CanonPictureInfo = 0x1038,

		Unknown3 = 0x1039,
		Unknown4 = 0x1093,
		Unknown5 = 0x10a8,
		
		WhiteBalanceTable = 0x10a9,
		
		Unknown6 = 0x10aa,

		ColorTemperature = 0x10ae,
		ColorSapce = 0x10b4,
		
		Unknown7 = 0x10b5,
		unknown8 = 0x10c0,
		Unknown9 = 0x10c1,

		ImageFormat = 0x1803,
		RecordID = 0x1804,
		SelfTimerTime = 0x1806,
		TargetDistanceSetting = 0x1807,
		SerialNumber = 0x180b,
		TimeStamp = 0x180e,
		ImageInfo = 0x1810,
		FlashInfo = 0x1813,
		MeasuredEV = 0x1814,
		FileNumber = 0x1817,
		ExposureInfo = 0x1818,
		
		Unknown10 = 0x1834,

		DecoderTable = 0x1835,
		
		Unknown11 = 0x183b,

		// Image Data
		RawData = 0x2005,
		JpgFromRaw = 0x2007,
		ThumbnailImage = 0x2008,

		// Directories
		ImageDescrption = 0x2804,
		CameraObject = 0x2807,
		ShootingRecord = 0x3002,
		MeasuredInfo = 0x3003,
		CameraSpecification = 0x3004,
		ImageProps = 0x300a,
		ExifInformation = 0x300b
	}

	public struct ImageInfo {
		uint ImageWidth;  // Number of horizontal pixels
		uint ImageHeight; // Number of vertical pixels
		float PixelAspectratio;
		int RotationAngle;  // degreess clockwise to rotate (orientation)
		uint ComponentBitDepth; // bits per component
		uint ColorBW; //  byte wise:  0 gray - 1 color ; byte 2 use aspect ratio ; 3 and 4 reserved
	}

	public enum EntryType : ushort {
		Byte = 0x0000,
		Ascii = 0x0800,
		Short = 0x1000,
		Int = 0x1800,
		Struct = 0x2000,
		Directory1 = 0x2800,
		Directory2 = 0x2800,
	}
	
	public enum Mask {
		StorageFormat = 0xc000,
		Type = 0x3800,
		ID = 0x07ff,
	}

	/* See http://www.sno.phy.queensu.ca/~phil/exiftool/canon_raw.html */
	public struct Entry {
		internal Tag Tag;
		internal uint Size;
		internal uint Offset;

		public Entry (byte [] data, int pos, bool little)
		{
			Tag = (Tag) BitConverter.ToUInt16 (data, pos, little);
			Size = BitConverter.ToUInt32 (data, pos + 2, little);
			Offset = BitConverter.ToUInt32 (data, pos + 6, little);
		}	

		public static EntryType GetType (Tag tag) 
		{
			EntryType type = (EntryType) ((ushort)tag & (ushort)Mask.Type);
			return type;
		}
		

		public static bool IsDirectory (Tag tag)
		{
			EntryType type = GetType (tag);
			return (type == EntryType.Directory1 || type == EntryType.Directory2);
		}
	}

	
	public class ImageDirectory {
		System.Collections.ArrayList entry_list;
		uint Count;
		bool little;
		uint start;
		long DirPosition;
		System.IO.Stream stream;

		public ImageDirectory (System.IO.Stream stream, uint start, long end, bool little)
		{
			this.start = start;
			this.little = little;
			this.stream = stream;

			entry_list = new System.Collections.ArrayList ();
			
			stream.Position = end - 4;
			byte [] buf = new byte [10];
			stream.Read (buf, 0, 4);
			uint directory_pos  = BitConverter.ToUInt32 (buf, 0, little);
			DirPosition = start + directory_pos;

			stream.Position = DirPosition;
			stream.Read (buf, 0, 2);

			Count = BitConverter.ToUInt16 (buf, 0, little);
			
			for (int i = 0; i < Count; i++)
			{
				stream.Read (buf, 0, 10);
				System.Console.WriteLine ("reading {0} {1}", i, stream.Position);
				Entry entry = new Entry (buf, 0, little);
				entry_list.Add (entry);
			}
		}			
	      
		public void Dump ()
		{
			System.Console.WriteLine ("Dumping directory with {0} entries", entry_list.Count);
			for (int i = 0; i < entry_list.Count; i++) {
				Entry e = (Entry) entry_list[i];
				System.Console.WriteLine ("\tentry[{0}] = {1}..{5}({4}).{2}-{3}", i, e.Tag, e.Size, e.Offset, e.Tag.ToString ("x"), (uint)e.Tag & ~(uint)Mask.StorageFormat); 
			}
		}

		public ImageDirectory ReadDirectory (Tag tag)
		{
			int pos = 0;
			foreach (Entry e in entry_list) {
				if (e.Tag == tag) {
					uint subdir_start = this.start + e.Offset;
					ImageDirectory subdir = new ImageDirectory (stream, subdir_start, subdir_start + e.Size, little);
					return subdir;
				}
			}
			return null;
		}
		
		public byte [] ReadEntry (int pos)
		{
			Entry e = (Entry) entry_list [pos];

			stream.Position = this.start + e.Offset;			

			byte [] data = new byte [e.Size];
			stream.Read (data, 0, data.Length);

			return data;
		}
		
		public byte [] ReadEntry (Tag tag) 
		{
			int pos = 0;
			foreach (Entry e in entry_list) {
				if (e.Tag == tag)
					return ReadEntry (pos);
				pos++;
			}
			return null;
		}
	}
	
	public class CiffFile : FSpot.ImageFile {
		public ImageDirectory Root;
		System.IO.Stream stream;

		public CiffFile (string path) : base (path)
		{
			System.IO.Stream input = System.IO.File.OpenRead (path);
			this.Load (input);
			this.Dump ();
		}

		public void Load (System.IO.Stream stream) 
		{
			byte [] header = new byte [26];  // the spec reserves the first 26 bytes as the header block
			stream.Read (header, 0, header.Length);

			bool little;
			uint start;
			
			little = (header [0] == 'I' && header [1] == 'I');
			
			start = BitConverter.ToUInt32 (header, 2, little);
			
			// HEAP is the type CCDR is the subtype
			if (System.Text.Encoding.ASCII.GetString (header, 6, 8) != "HEAPCCDR") 
				throw new System.Exception ("Invalid Ciff Header Block");
			
			uint version =  BitConverter.ToUInt32 (header, 14, little);
			
			//
			
			long end = stream.Length;
			Root = new ImageDirectory (stream, start, end, little);
		}

		/*
		public override System.DateTime Date () 
		{

		}
		*/

		public override Gdk.Pixbuf Load ()
		{
			// FIXME this is a hack. No, really, I mean it.
			
			byte [] data = GetEmbeddedJpeg ();
			if (data != null) {
				Gdk.PixbufLoader loader = new Gdk.PixbufLoader ();
				loader.Write (data, (ulong)data.Length);
				Gdk.Pixbuf pixbuf = loader.Pixbuf;
				loader.Close ();
				return pixbuf;
			}
			return null;
		}

		public System.IO.Stream PixbufLoaderStream ()
		{
			System.IO.MemoryStream stream = null; 
			byte [] data = GetEmbeddedJpeg ();
			
			if (data != null)
				stream = new System.IO.MemoryStream (data);
				
			return stream;
		}

		public override Gdk.Pixbuf Load (int width, int height)
		{
			Gdk.Pixbuf full = this.Load ();
			Gdk.Pixbuf scaled  = PixbufUtils.ScaleToMaxSize (full, width, height);
			full.Dispose ();
			return scaled;
		}

		public void Dump ()
		{
			Root.Dump ();
			ImageDirectory props = Root.ReadDirectory (Tag.ImageProps);
			props.Dump ();
			string path = "out2.jpg";
			//System.IO.File.Delete (path);

			/*
			System.IO.Stream output = System.IO.File.Open (path, System.IO.FileMode.OpenOrCreate);
			byte [] data = GetEmbeddedThumbnail ();
			System.Console.WriteLine ("data length {0}", data != null ? data.Length : -1);
			output.Write (data, 0, data.Length);
			output.Close ();
			*/
		}

		public byte [] GetEmbeddedJpeg ()
		{
			return Root.ReadEntry (Tag.JpgFromRaw);
		}

		public byte [] GetEmbeddedThumbnail ()
		{
			return Root.ReadEntry (Tag.ThumbnailImage); 
		}
		
		/*
		public static void Main (string [] args)
		{
			CiffFile ciff = new CiffFile (args [0]);
			ciff.Dump ();
		}
		*/
	}
}
