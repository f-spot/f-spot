using FSpot;

namespace FSpot.Tiff {
	public enum TagId : ushort {
		InteroperabilityIndex		= 0x0001,
		InteroperabilityVersion	        = 0x0002,
		
		NewSubfileType                  = 0x00fe,
		SubfileType                     = 0x00ff,
		
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
			T4Options                       = 0x0124,
			T6Options                       = 0x0125,
		ResolutionUnit  		= 0x0128,
		TransferFunction 		= 0x012d,
		Software 			= 0x0131,
		DateTime			= 0x0132,
		Artist				= 0x013b,
		WhitePoint			= 0x013e,
		PrimaryChromaticities		= 0x013f,
			
			HalftoneHints                   = 0x0141,
			// Tiled images
			TileWidth                       = 0x0142,
			TileLength                       = 0x0143,
			TileOffsets                       = 0x0144,
			TileByteCounts                      = 0x0145,

		SubIFDs                         = 0x014a, // TIFF-EP

			// CMYK images
			InkSet                          = 0x014c,
			NumberOfInks                    = 0x014e,
			InkNames                        = 0x014d,
			DotRange                        = 0x0150,
			TargetPrinter                   = 0x0151,
			ExtraSamples                    = 0x0152,
			SampleFormat                    = 0x0153,
			SMinSampleValue                 = 0x0154,
			SMaxSampleValue                 = 0x0155,
		
		TransferRange			= 0x0156,
		
		ClipPath                        = 0x0157, // TIFF PageMaker Technote #2.
		
		JPEGTables                      = 0x015b, // TIFF-EP
		
		JPEGProc			= 0x0200,
		JPEGInterchangeFormat	        = 0x0201,
		JPEGInterchangeFormatLength	= 0x0202,
			JPEGRestartInterval             = 0x0203,
			JPEGLosslessPredictors          = 0x0205,
			JPEGPointTransforms             = 0x0206,
			JPEGQTables                     = 0x0207,
			JPEGDCTables                    = 0x0208,
			JPEGACTables                    = 0x0209,
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
			
			FlashEnergy_TIFFEP                     = 0x920b,// TIFF-EP 
			SpacialFrequencyResponse        = 0x920c,// TIFF-EP 
			Noise                           = 0x920d,// TIFF-EP 
			FocalPlaneXResolution_TIFFEP           = 0x920e,// TIFF-EP 
			FocalPlaneYResolution_TIFFEP           = 0x920f,// TIFF-EP 
			FocalPlaneResolutionUnit_TIFFEP        = 0x9210,// TIFF-EP 
			ImageName                       = 0x9211,// TIFF-EP 
			SecurityClassification          = 0x9212,// TIFF-EP 
		
			ImageHistory                    = 0x9213, // TIFF-EP null separated list

	        SubjectArea			= 0x9214,

			ExposureIndex_TIFFEP                   = 0x9215, // TIFF-EP
			TIFFEPStandardID                = 0x9216, // TIFF-EP
			SensingMethod_TIFFEP                  = 0x9217, // TIFF-EP
			
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

	public enum ExtraSamples {
		Unspecified = 0,
		AssociatedAlpha = 1,
		UnassociatedAlpa = 2
	}

	public enum PhotometricInterpretation : ushort {
		WhiteIsZero = 0,
		BlackIsZero = 1,
		RGB = 2,
		PaletteColor = 3,
		TransparencyMask = 4,
			CMYK = 5,
			YCbCr = 6,
			CIELab = 8,
			CFA = 32803  // ColorFilterArray... the good stuff
	}

	public enum PlanarConfiguration {
		Chunky = 1,
		Planar = 2
	}
	
	public enum Compression {
		Packed = 1,
		Huffman = 2,
		T4 = 3,
		T6 = 4,
		LZW = 5,
		JPEG = 6,
		JPEGStream = 7,  // TIFF-EP stores full jpeg stream 
		PackBits = 32773
	}

	public enum JPEGProc {
		BaselineSequencial = 1,
		LosslessHuffman = 14,
	}

	public enum SubfileType {
		FullResolution = 1,
		ReducedResolution = 2,
		PageOfMultipage = 3
	}

	[System.Flags]
	public enum NewSubfileType : uint {
		SingleImage = 0,
		ReducedResolutionFlag = 1,
		PageOfMultipageFlag = 1 << 1,
		TransparencyMaskFlag = 1 << 2
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
	
	public class Tag {
		public ushort Id;
		public EntryType Type;
		public int Count;
		public string Name;
		public string Description;
	}

	public class CanonTag : Tag {
		// http://www.gvsoft.homedns.org/exif/makernote-canon.html
		
		public enum CanonId {
			Unknown1           = 0x0000,
			CameraSettings1    = 0x0001,
			Unknown2           = 0x0003,
			CameraSettings2    = 0x0004,
			ImageType          = 0x0006,
			FirmwareVersion    = 0x0007,
			ImageNumber        = 0x0008,
			OwnerName          = 0x0009,
			Unknown3           = 0x000a,
			CameraSerialNumber = 0x000c,
			Unknown4           = 0x000d,
			CustomFunctions    = 0x000f
		}
		
		public CanonTag (CanonId id, EntryType type, int count, string name, string description)
		{
			this.Id = (ushort)id;
			this.Type = type;
			this.Count = count;
			this.Name = name;
			this.Description = description;
		}

		public static System.Collections.Hashtable Tags;

		static CanonTag () {
			CanonTag [] tags = { 
				new CanonTag (CanonId.Unknown1, EntryType.Short, 6, null, null),
				new CanonTag (CanonId.CameraSettings1, EntryType.Short, -1, "Camera Settings 1", "First Canon MakerNote settings section"),
				new CanonTag (CanonId.Unknown2, EntryType.Short, 4, null, null),				
				new CanonTag (CanonId.CameraSettings2, EntryType.Short, -1, "Camera Settings 2", "Second Canon MakerNote settings section"),
				new CanonTag (CanonId.ImageType, EntryType.Ascii, 32, "Image Type", null), // FIXME description
				new CanonTag (CanonId.FirmwareVersion, EntryType.Ascii, 24, "Firmware Version", "Version of the firmware installed on the camera"),
				new CanonTag (CanonId.ImageNumber, EntryType.Long, 1, "Image Number", null), // FIXME description
				new CanonTag (CanonId.OwnerName, EntryType.Long, 32, "Owner Name", "Name of the Camera Owner"), // FIXME description
				new CanonTag (CanonId.Unknown4, EntryType.Short, -1, null, null),				
				new CanonTag (CanonId.CameraSerialNumber, EntryType.Short, 1, "Serial Number", null), //FIXME description
				new CanonTag (CanonId.Unknown4, EntryType.Short, -1, null, null),				
				new CanonTag (CanonId.CustomFunctions, EntryType.Short, -1, "Custom Functions", "Camera Custom Functions")
			};
					 
			foreach (CanonTag tag in tags)
				Tags [tag.Id] = tag;
		}

	}
	
	public enum Endian {
		Big,
		Little
	}

	public class Converter {
		public static uint ReadUInt (System.IO.Stream stream, Endian endian)
		{
			byte [] tmp = new byte [4];

		        if (stream.Read (tmp, 0, tmp.Length) < 4)
				throw new System.Exception ("Short Read");

			return BitConverter.ToUInt32 (tmp, 0, endian == Endian.Little);
		}

		public static ushort ReadUShort (System.IO.Stream stream, Endian endian)
		{
			byte [] tmp = new byte [2];

		        if (stream.Read (tmp, 0, tmp.Length) < 2)
				throw new System.Exception ("Short Read");

			return BitConverter.ToUInt16 (tmp, 0, endian == Endian.Little);
		}
	}

	public class Header {
		public Endian endian;

		private uint directory_offset;
		public ImageDirectory Directory;

		

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
			directory_offset = BitConverter.ToUInt32 (data, 4, endian == Endian.Little);
			
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
		
		protected bool has_header;
		protected bool has_footer;

		public ImageDirectory (System.IO.Stream stream, uint start_position, Endian endian)
		{
			this.endian = endian;
			orig_position = start_position;
			Load (stream);
		}
		
		protected void Load (System.IO.Stream stream)
		{
			ReadHeader (stream);			
			ReadEntries (stream);
			ReadFooter (stream);

			LoadEntries (stream);
			LoadNextDirectory (stream);
		}

		public virtual bool ReadHeader (System.IO.Stream stream)
		{
			stream.Seek ((long)orig_position, System.IO.SeekOrigin.Begin);
			return true;
		}

		protected virtual void ReadEntries (System.IO.Stream stream) 
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
				System.Console.WriteLine ("Added Entry {0} {1}", entry.Id.ToString (), entry.Id.ToString ("x"));
				if (entry.Id == TagId.NewSubfileType) {
					
				}
			}
		}

		protected virtual void ReadFooter (System.IO.Stream stream)
		{
			next_directory_offset = Converter.ReadUInt (stream, this.endian);
		}

		protected void LoadEntries (System.IO.Stream stream)
		{
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

		public ImageDirectory NextDirectory {
			get {
				return next_directory;
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
			case TagId.MakerNote:
				return new MakerNoteEntry (input, start, header_endian);
			}
			
			switch (type) {
			case EntryType.Ifd:
				System.Console.WriteLine ("Trying to load {0} {1}", tagid, tagid.ToString ("x"));
				return new SubdirectoryEntry (input, start, header_endian);
			case EntryType.Byte:
				return new ByteEntry (input, start, header_endian);
			case EntryType.Long:
				return new LongEntry (input, start, header_endian);
			case EntryType.Short:
				return new ShortEntry (input, start, header_endian);
			case EntryType.Ascii:
				return new AsciiEntry (input, start, header_endian);
			}

			return new DirectoryEntry (input, start, header_endian);
		}
	}
	       
	public class MakerNoteEntry : SubdirectoryEntry {
		public MakerNoteEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
		
		}

		public override uint GetEntryCount ()
		{
			return 1;
		}

		public override void LoadExternal (System.IO.Stream stream)
		{
		}
	}

	public class SubdirectoryEntry : DirectoryEntry {
		public uint directory_offset;
		ImageDirectory [] Directory;
		
		public SubdirectoryEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (this.GetEntryCount () > 1) {
				System.Console.WriteLine ("Count is greater than 1 ({1}) on Subdirectory {0} interesting", tagid, count);
			}
		}

		public virtual uint GetEntryCount ()
		{
			return count;
		}

		public override void LoadExternal (System.IO.Stream stream)
		{
			uint entry_count = GetEntryCount ();
			Directory = new ImageDirectory [count];

			base.LoadExternal (stream);

			for (int i = 0; i <  entry_count; i++) {
				directory_offset = BitConverter.ToUInt32 (raw_data, i * 4, endian == Endian.Little);
				System.Console.WriteLine ("Entering Subdirectory {0} at {1}", tagid.ToString (), directory_offset);
				Directory [i] = new ImageDirectory (stream, directory_offset, endian);
				System.Console.WriteLine ("Leaving Subdirectory {0} at {1}", tagid.ToString (), directory_offset);
			}
		}
	}
	
	public class ShortEntry : DirectoryEntry {
		public ShortEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
		}

		public ushort [] ShortValue
		{
			get {
				ushort [] data = new ushort [raw_data.Length];
				for (int i = 0; i < raw_data.Length; i+= 2) {
					data [i] = BitConverter.ToUInt16 (raw_data, i, endian == Endian.Little);
				}
				return data;
			}
		}
	}
	
	public class LongEntry : DirectoryEntry {
		public LongEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (type != EntryType.Long)
				throw new System.Exception (System.String.Format ("Invalid Settings At Birth {0}", tagid));
		}

		public uint [] LongValue
		{
			get {
				uint [] data = new uint [raw_data.Length];
				for (int i = 0; i < raw_data.Length; i+= 4) {
					data [i] = BitConverter.ToUInt32 (raw_data, i, endian == Endian.Little);
				}
				return data;
			}
		}
	}

	public class ByteEntry : DirectoryEntry {
		public ByteEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (type != EntryType.Byte)
				throw new System.Exception ("Invalid Settings At Birth");
		}
	}
	
	public class AsciiEntry : DirectoryEntry {
		public AsciiEntry (byte [] data, int offset, Endian endian) : base (data, offset, endian)
		{
			if (type != EntryType.Ascii)
				throw new System.Exception (System.String.Format ("Invalid Settings At Birth {0}", tagid));
		}

		public string StringValue {
			get {
				return System.Text.Encoding.ASCII.GetString (raw_data);
			}
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
			tagid = (TagId) BitConverter.ToUInt16 (data, start, endian == Endian.Little);
			type = (EntryType) BitConverter.ToUInt16 (data, start + 2, endian == Endian.Little);
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
				raw_data = data;
			}

			switch (this.Id) {
			case TagId.NewSubfileType:
				System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXX new NewSubFileType {0}", (NewSubfileType)((LongEntry)this).LongValue [0]);
				break;
			case TagId.SubfileType:
				System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXX new SubFileType {0}", (SubfileType)((ShortEntry)this).ShortValue [0]);
				break;
			case TagId.Compression:
				System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXX new Compression {0}", (Compression)((ShortEntry)this).ShortValue [0]);
				
				break;
			case TagId.JPEGProc:
				System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXX new JPEGProc {0}", (JPEGProc)((ShortEntry)this).ShortValue [0]);
				
				break;
			case TagId.PhotometricInterpretation:
				System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXX new PhotometricInterpretation {0}", (PhotometricInterpretation)((ShortEntry)this).ShortValue [0]);
				break;
			}
		}

		protected void ParseStream (byte [] data, int start)
		{
			int i = start;

			count = BitConverter.ToUInt32 (data, i, endian == Endian.Little); i += 4;
			int size = (int)count * GetTypeSize ();
			if (size > 4)
				data_offset = BitConverter.ToUInt32 (data, i, endian == Endian.Little);
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
	
		public static System.DateTime DateTimeFromString(string dt)
		 {
			 // Exif DateTime strings are formatted as
			 //      "YYYY:MM:DD HH:MM:SS"

			 string delimiters = " :";
			 string[] dt_data = dt.Split ( delimiters.ToCharArray(), 6 );
			 System.DateTime result;
			 result = new System.DateTime (System.Int32.Parse(dt_data[0]), 
						       System.Int32.Parse(dt_data[1]), 
						       System.Int32.Parse(dt_data[2]),
						       System.Int32.Parse(dt_data[3]), 
						       System.Int32.Parse(dt_data[4]), 
						       System.Int32.Parse(dt_data[5]));
			
			return result;
		}
	
		public void Dump ()
		{
			System.Console.WriteLine ("{0}-{1}({2})", this.tagid, this.type, this.count);
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

	public class TiffFile : ImageFile {
		public Header Header;

		public TiffFile (string path) : base (path)
		{
			try {
				System.IO.Stream input = System.IO.File.OpenRead (path);
				this.Header = new Header (input);
				input.Close ();
				//System.Console.WriteLine (this.Header.Dump ());
			} catch (System.Exception e) {
				System.Console.WriteLine (e.ToString ());
			}
		}

		public override PixbufOrientation GetOrientation ()
		{
			ShortEntry e = (ShortEntry)(this.Header.Directory.Lookup (TagId.Orientation));
			return (PixbufOrientation)(e.ShortValue[0]);
		}

		public override System.DateTime Date ()
		{
			AsciiEntry e = (AsciiEntry)(this.Header.Directory.Lookup (TagId.DateTime));
			System.DateTime time = DirectoryEntry.DateTimeFromString (e.StringValue);
			return time;
		}
	}
}

