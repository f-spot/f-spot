namespace Tiff {
	public enum TagId {
		InteroperabilityIndex		= 0x0001,
		InteroperabilityVersion	        = 0x0002,
		
		NewSubFileType                  = 0x00fe, // TIFF-EP
		
		ImageWidth 			= 0x0100,
		ImageLength 			= 0x0101,
		BitsPersample 	         	= 0x0102,
		Compression 			= 0x0103,
		PhotometricInterpretation 	= 0x0106,
		FillOrder 			= 0x010a,
		DocumentName 			= 0x010d,
		ImageDescription 		= 0x010e,
		Make 				= 0x010f,
		Model 				= 0x0110,
		StripOffsets 			= 0x0111,
		Orientation 			= 0x0112,
		SamplesPerPixel 		= 0x0115,
		RowsPerStrip    		= 0x0116,
		StripByteCounts 		= 0x0117,
		XResolution 			= 0x011a,
		YResolution 			= 0x011b,
		PlanarConfiguration 		= 0x011c,
		ResolutionUnit  		= 0x0128,
		TransferFunction 		= 0x012d,
		Software 			= 0x0131,
		DateTime			= 0x0132,
		Artist				= 0x013b,
		WhitePoint			= 0x013e,
		PrimaryChromaticities		= 0x013f,

		SubIFDs                         = 0x014a, // TIFF-EP
		
		TransferRange			= 0x0156,
		
		ClipPath                        = 0x0157, // TIFF PageMaker Technote #2.
		
		JPEGTables                      = 0x015b, // TIFF-EP
		
		JPEGProc			= 0x0200,
		JPEGInterchangeFormat	        = 0x0201,
		JPEGInterchangeFormatLength	= 0x0202,
		YCBCRCoefficients		= 0x0211,
		YCBCRSubSampling		= 0x0212,
		YCBCRPositioning		= 0x0213,
		ReferenceBlackWhite		= 0x0214,
		RelatedImageFileFormat   	= 0x1000,
		RelatedImageWidth		= 0x1001,
		RelatedImageLength		= 0x1002,
		CFARepeatPatternDim		= 0x828d,
		CFAPattern			= 0x828e,
		BatteryLevel			= 0x828f,
		Copyright			= 0x8298,
		ExposureTime			= 0x829a,
		FNumber 			= 0x829d,
		IPTCNAA	        		= 0x83bb,
		ExifIfdPointer      		= 0x8769,
		InterColorProfile		= 0x8773,
		ExposureProgram 		= 0x8822,
		SpectralSensitivity		= 0x8824,
		GPSInfoIfdPointer		= 0x8825,
		ISOSpeedRatings	        	= 0x8827,
		OECF				= 0x8828,
		ExifVersion			= 0x9000,
		DateTimeOriginal		= 0x9003,
		DateTimeDigitized		= 0x9004,
		ComponentsConfiguration	        = 0x9101,
		CompressedBitsPerPixel	        = 0x9102,
		ShutterSpeedValue		= 0x9201,
		ApertureValue			= 0x9202,
		BrightnessValue  		= 0x9203,
		ExposureBiasValue		= 0x9204,
		MaxApertureValue		= 0x9205,
		SubjectDistance 		= 0x9206,
		MeteringMode			= 0x9207,
		LightSource			= 0x9208,
		Flash				= 0x9209,
		FocalLength			= 0x920a,
		
		ImageHistory                    = 0x9212, // TIFF-EP null separated list

		SubjectArea			= 0x9214,
		MakerNote			= 0x927c,
		UserComment			= 0x9286,
		SubSecTime			= 0x9290,
		SubSecTimeOriginal		= 0x9291,
		SubSecTimeDigitized		= 0x9292,
		FlashPixVersion 		= 0xa000,
		ColorSpace			= 0xa001,
		PixelXDimension 		= 0xa002,
		PixelYDimension 		= 0xa003,
		RelatedSoundFile		= 0xa004,
		InteroperabilityIfdPointer	= 0xa005,
		FlashEnergy			= 0xa20b,
		SpatialFrequencyResponse	= 0xa20c,
		FocalPlaneXResolution	        = 0xa20e,
		FocalPlaneYResolution	        = 0xa20f,
		FocalPlaneResolutionUnit	= 0xa210,
		SubjectLocation 		= 0xa214,
		ExposureIndex			= 0xa215,
		SensingMethod			= 0xa217,
		FileSource			= 0xa300,
		SceneType			= 0xa301,
		NewCFAPattern		        = 0xa302,
		CustomRendered  		= 0xa401,
		ExposureMode			= 0xa402,
		WhiteBalance			= 0xa403,
		DigitalZoomRatio		= 0xa404,
		FocalLengthIn35mmFilm	        = 0xa405,
		SceneCaptureType		= 0xa406,
		GainControl			= 0xa407,
		Contrast			= 0xa408,
		Saturation			= 0xa409,
		Sharpness			= 0xa40a,
		DeviceSettingDescription	= 0xa40b,
		SubjectDistanceRange		= 0xa40c,
		ImageUniqueId   		= 0xa420,

		// The Following IDs are not described the EXIF spec

		// The XMP spec declares that XMP data should live 0x2bc when
		// embedded in tiff images.
		XMP                             = 0x02bc,
		
		// from the dng spec
		DNGVersion                      = 0xc612, // Ifd0
		DNGBackwardVersion              = 0xc613, // Ifd0
		UniqueCameraModel               = 0xc614, // Ifd0
		LocalizedCameraModel            = 0xc615, // Ifd0
		CFAPlaneColor                   = 0xc616, // RawIfd
		CFALayout                       = 0xc617, // RawIfd
		LinearizationTable              = 0xc618, // RawIfd
		BlackLevelRepeatDim             = 0xc619, // RawIfd
		BlackLevel                      = 0xc61a, // RawIfd
		BlackLevelDeltaH                = 0xc61b, // RawIfd
		BlackLevelDeltaV                = 0xc61c, // RawIfd
		WhiteLevel                      = 0xc61d, // RawIfd
		DefaultScale                    = 0xc61e, // RawIfd		
		DefaultCropOrigin               = 0xc61f, // RawIfd
		DefaultCropSize                 = 0xc620, // RawIfd
		ColorMatrix1                    = 0xc621, // Ifd0
		ColorMatrix2                    = 0xc622, // Ifd0
		CameraCalibration1              = 0xc623, // Ifd0
		CameraCalibration2              = 0xc624, // Ifd0
		ReductionMatrix1                = 0xc625, // Ifd0
		ReductionMatrix2                = 0xc626, // Ifd0
		AnalogBalance                   = 0xc627, // Ifd0
		AsShotNetural                   = 0xc628, // Ifd0
		AsShotWhiteXY                   = 0xc629, // Ifd0
		BaselineExposure                = 0xc62a, // Ifd0
		BaselineNoise                   = 0xc62b, // Ifd0
		BaselineSharpness               = 0xc62c, // Ifd0
		BayerGreeSpit                   = 0xc62d, // Ifd0
		LinearResponseLimit             = 0xc62e, // Ifd0
		CameraSerialNumber              = 0xc62f, // Ifd0
		LensInfo                        = 0xc630, // Ifd0
		ChromaBlurRadius                = 0xc631, // RawIfd
		AntiAliasStrength               = 0xc632, // RawIfd
		DNGPrivateData                  = 0xc634, // Ifd0
		
		MakerNoteSafety                 = 0xc635, // Ifd0

		// The Spec says BestQualityScale is 0xc635 but it appears to be wrong
		//BestQualityScale                = 0xc635, // RawIfd 
		BestQualityScale                = 0xc63c, // RawIfd  this looks like the correct value

		CalibrationIlluminant1          = 0xc65a, // Ifd0
		CalibrationIlluminant2          = 0xc65b, // Ifd0
		

		// Print Image Matching data
		PimIfdPointer                   = 0xc4a5
	}
	
	public enum EntryType {
		Byte = 1,
		Ascii,
		Short,
		Long,
		Rational,
		SByte,
		Undefined,
		SShort,
		SLong,
		SRational,
		Float,
		Double,
		Ifd // TIFF-EP - TIFF PageMaker TechnicalNote 2
	}
	
	public struct Tag {
		TagId id;
		EntryType type;
		int Count;
		string location;
	}
	
	public enum Endian {
		Little,
		Big
	}

	public class Converter {
		private static unsafe void PutBytes (byte *dest, byte *src, int count, Endian endian)
		{
			int i = 0;
			if (System.BitConverter.IsLittleEndian == (endian == Endian.Little)) {
				for (i = 0; i < count; i++) {
					//System.Console.WriteLine ("Copying normal byte [{0}]= {1}", i, src[i]);
					dest [i] = src [i];
				}
			} else {
				for (i = 0; i < count; i++) {
					//System.Console.WriteLine ("Copying swapped byte [{0}]= {1}", i, src[i]);
					dest [i] = src [count - i -1];  
				}
			}
		}

		private static unsafe void PutBytes (byte *dest, byte [] src, int start, int count, Endian endian)
		{
			int i = 0;
			if (System.BitConverter.IsLittleEndian == (endian == Endian.Little)) {
				for (i = 0; i < count; i++) {
					//System.Console.WriteLine ("Copying normal byte [{0}]= {1}", i, src[i]);
					dest [i] = src [start + i];
				}
			} else {
				for (i = 0; i < count; i++) {
					//System.Console.WriteLine ("Copying swapped byte [{0}]= {1}", i, src[i]);
					dest [i] = src [start + count - i -1];  
				}
			}
		}

		public static uint ReadUInt (System.IO.Stream stream, Endian endian)
		{
			byte [] tmp = new byte [4];

		        if (stream.Read (tmp, 0, tmp.Length) < 4)
				throw new System.Exception ("Short Read");

			return ToUInt (tmp, 0, endian);
		}

		public static uint ToUInt (byte [] src, int start, Endian endian)
		{
			if (start + src.Length < 4)
				throw new System.Exception ("Invalid Length");

			unsafe {
				uint value;
				PutBytes ((byte *)&value, src, start, 4, endian);
				return value;
			}
		}

		public static unsafe uint ToUInt (byte *src, Endian endian)
		{ 
			uint value;
			PutBytes ((byte *)&value, src, 4, endian);
			return value;
		}

		public static ushort ReadUShort (System.IO.Stream stream, Endian endian)
		{
			byte [] tmp = new byte [2];

		        if (stream.Read (tmp, 0, tmp.Length) < 2)
				throw new System.Exception ("Short Read");
			
			return ToUShort (tmp, 0, endian);
		}

		public static ushort ToUShort (byte [] src, int start, Endian endian)
		{
			if (start + src.Length < 2)
				throw new System.Exception ("Invalid Length");

			unsafe {
				ushort value;
				PutBytes ((byte *)&value, src, start, 2, endian);
				return value;
			}
		}

		public static unsafe ushort ToUShort (byte *src, Endian endian)
		{
			ushort value;
			PutBytes ((byte *)&value, src, 2, endian);
			return value;
		}
	}

	public class Header {
		public Endian endian;

		private uint directory_offset;
		ImageDirectory Directory;

		

		public Header (System.IO.Stream stream)
		{
			byte [] data = new byte [8];
			stream.Read (data, 0, data.Length);
			if (data [0] == 'M' && data [1] == 'M' && data [2] == 0 && data [3] == 42)
				endian = Endian.Big;
			else if (data [0] == 'I' && data [1] == 'I' && data [2] == 42 && data [3] == 0)
				endian = Endian.Little;
			else
				throw new System.Exception ("Invalid Tiff Header Block");

			System.Console.WriteLine ("Converting Something");
			directory_offset = Converter.ToUInt (data, 4, endian);
			
			if (directory_offset < 8)
				throw new System.Exception ("Invalid IFD0 Offset [" + directory_offset.ToString () + "]"); 
			
			System.Console.WriteLine ("Reading First IFD");
			Directory = new ImageDirectory (stream, directory_offset, endian); 
		}

		public string Dump ()
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();
			builder.Append (System.String.Format ("Header [{0}]\n", endian.ToString ()));
			builder.Append (System.String.Format ("|-{0}", Directory.Dump ()));
			return builder.ToString ();
		}
	}
	

	public class ImageDirectory {
		protected Endian endian;
		protected ushort num_entries;
		protected System.Collections.ArrayList entries;
		protected uint orig_position;

		protected uint next_directory_offset;
		ImageDirectory next_directory;
		

		public ImageDirectory (System.IO.Stream stream, uint directory_offset, Endian endian)
		{
			stream.Seek ((long)directory_offset, System.IO.SeekOrigin.Begin);

			this.endian = endian;
			orig_position = directory_offset;
			
			LoadEntries (stream);
			LoadNextDirectory (stream);
		}
		
		public ImageDirectory NextDirectory {
			get {
				return next_directory;
			}
		}

		protected void LoadEntries (System.IO.Stream stream) 
		{
			num_entries = Converter.ReadUShort (stream, endian);
			System.Console.WriteLine ("reading {0} entries", num_entries);

			entries = new System.Collections.ArrayList (num_entries);
			int entry_length = num_entries * 12;
			byte [] content = new byte [entry_length];
			
			if (stream.Read (content, 0, content.Length) < content.Length)
				throw new System.Exception ("Short Read");
			
			for (int pos = 0; pos < entry_length; pos += 12) {
				DirectoryEntry entry = EntryFactory.CreateEntry (this, content, pos, this.endian);
				entries.Add (entry);		
				System.Console.WriteLine ("Added Entry {0}", entry.Id.ToString ());
			}

			next_directory_offset = Converter.ReadUInt (stream, this.endian);

			foreach (DirectoryEntry entry in entries) {
				entry.LoadExternal (stream);
			}
		}
		
		protected void LoadNextDirectory (System.IO.Stream stream)
		{
			System.Console.WriteLine ("next_directory_offset = {0}", next_directory_offset);
			try {
				if (next_directory_offset != 0)
					next_directory = new ImageDirectory (stream, next_directory_offset, this.endian);
			} catch (System.Exception e) {
				System.Console.WriteLine ("Error loading directory {0}", e.ToString ());
				next_directory = null;
				next_directory_offset = 0;
			}		
		}

		public DirectoryEntry Lookup (TagId id) 
		{
			foreach (DirectoryEntry entry in entries)
				if (entry.Id == id)
					return entry;

			
			return null;
		}
		
		public string Dump ()
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder ();
			builder.Append ("Dummping IFD");
			foreach (DirectoryEntry entry in entries) {
				builder.Append (entry.ToString ()+ "\n");
				if (entry is SubdirectoryEntry) {
					builder.Append ("Found SUBDIRECTORYENTRY\n");
				}
			}
			
			if (next_directory != null) {
				builder.Append ("Dummping Next IFD");
				builder.Append (next_directory.Dump ());
			}

			return builder.ToString ();
		}
	}
	
	public class DNFPrivateDirectory {
		

	}
	
	public class EntryFactory {
		//public delegate DirectoryEntry ConstructorFunc (byte [], Endian endian);
		//public static System.Collections.Hashtable ctors = new System.Collections.Hashtable ();
		
		public static DirectoryEntry CreateEntry (ImageDirectory parent, byte [] input, int start, Endian header_endian)
		{
			TagId tagid;
			EntryType type;

			DirectoryEntry.ParseHeader (input, start, out tagid, out type, header_endian);
			//ConstructorFunc ctor = ctors[tagid];			
			//if (ctor == null) {
			//	return ctor (input, header_endian);				
			//}
			
			switch (tagid) {
			case TagId.ExifIfdPointer:
			case TagId.GPSInfoIfdPointer:
			case TagId.InteroperabilityIfdPointer:
			case TagId.SubIFDs:
				return new SubdirectoryEntry (input, start, header_endian);
				//case TagId.MakerNote:
				//return new MakerNoteEntry (input, start, header_endian);
				//case TagId.PimIfdPointer:
				//return new 
			}
			
			switch (type) {
			case EntryType.Ifd:
				return new SubdirectoryEntry (input, start, header_endian);
			case EntryType.Byte:
				return new ByteEntry (input, start, header_endian);
			case EntryType.Long:
				return new LongEntry (input, start, header_endian);
			}

			return new DirectoryEntry (input, start, header_endian);
		}
	}
		
	public class SubdirectoryEntry : LongEntry {
		public uint directory_offset;
		ImageDirectory Directory;
		
		public SubdirectoryEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (count != 1)
				throw new System.Exception ("Invalid Settings At Birth");
		}

		public override void LoadExternal (System.IO.Stream stream)
		{
			directory_offset = Converter.ToUInt (raw_data, 0, endian);
			System.Console.WriteLine ("Entering Subdirectory {0} at {1}", tagid.ToString (), directory_offset);
			Directory = new ImageDirectory (stream, directory_offset, endian);
			System.Console.WriteLine ("Leaving Subdirectory {0} at {1}", tagid.ToString (), directory_offset);
		}
	}
	
	public class LongEntry : DirectoryEntry {
		public LongEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (type != EntryType.Long)
				throw new System.Exception ("Invalid Settings At Birth");
		}
	}

	public class ByteEntry : DirectoryEntry {
		public ByteEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (type != EntryType.Byte)
				throw new System.Exception ("Invalid Settings At Birth");
		}
	}

	public class DirectoryEntry {
		protected TagId  tagid;
		protected EntryType type;
		protected uint count;
		protected uint offset_origin;
		protected uint data_offset;

		protected byte [] raw_data;
		protected Endian endian;

		public TagId Id {
			get {
				return tagid;
			}
		}

		public void SetOrigin (uint pos)
		{
			offset_origin = pos;
		}

		public uint Position
		{
			get {
				return offset_origin + data_offset;
			}
		}

		public virtual int GetTypeSize ()
		{
			return GetTypeSize (type);
		}

		public static int GetTypeSize (EntryType type)
		{
			switch (type) {
			case EntryType.Byte:
			case EntryType.SByte:
			case EntryType.Undefined:
			case EntryType.Ascii:
				return 1;
			case EntryType.Short:
			case EntryType.SShort:
				return 2;
			case EntryType.Long:
			case EntryType.SLong:
			case EntryType.Float:
				return 4;
			case EntryType.Double:
			case EntryType.Rational:
			case EntryType.SRational:
				return 8;
			default:
				return 1;
			}
		}

		public static int ParseHeader (byte [] data, int start, out TagId tagid, out EntryType type, Endian endian)
		{
			tagid = (TagId) Converter.ToUShort (data, start, endian);
			type = (EntryType) Converter.ToUShort (data, start + 2, endian);
			return 4;
		}
		
		public DirectoryEntry (byte [] data, int start, Endian endian)
		{
			this.endian = endian;

			start += ParseHeader (data, start, out this.tagid, out this.type, endian);
			ParseStream (data, start);
		}

		public virtual void LoadExternal (System.IO.Stream stream)
		{
			if (data_offset != 0) {
				stream.Seek ((long)Position, System.IO.SeekOrigin.Begin);
				byte [] data = new byte [count * GetTypeSize ()];
				if (stream.Read (data, 0, data.Length) < data.Length)
					throw new System.Exception ("Short Read");
			}
		}

		protected void ParseStream (byte [] data, int start)
		{
			int i = start;

			count = Converter.ToUInt (data, i, endian); i += 4;
			int size = (int)count * GetTypeSize ();
			if (size > 4)
				data_offset = Converter.ToUInt (data, i, endian);
			else {
				data_offset = 0;
				raw_data = new byte [size];
				System.Array.Copy (data, i, raw_data, 0, size);
			}
		}
		
		public void SetData (string value)
		{
			int len = System.Text.Encoding.UTF8.GetByteCount (value);
			byte [] tmp = new byte [len + 1];
			System.Text.Encoding.UTF8.GetBytes (value, 0, value.Length, tmp, 0);
			tmp[len] = 0;
			System.Console.WriteLine ("value = {0} len = {1}", value, len);
			SetData (tmp);
		}
		
		public void SetData (byte [] data)
		{
			raw_data = data;
			count = (uint)raw_data.Length / (uint)GetTypeSize ();
		}
		
		public byte [] RawData
		{
			get { 
				return raw_data;
			}
		}
	}
}
