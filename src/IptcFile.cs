namespace FSpot.Iptc {
#if false
	public enum Format
	{
		Uknown,
		String,
		Numeric,
		Binary,
		Byte,
		Short,
		Int,
		Date,
		Time
	};
	
 #endif
	public enum Record
	{
		Envelope = 1 << 8,
		Application = 2 << 8,
		NewsphotoParameter = 3 << 8,
		NotAllocated1 = 4 << 8,
		NotAllocated2 = 5 << 8,
		AbstractRelationship = 6 << 8,
		PreObjectData = 7 << 8,
		ObjectData = 8 << 8,
		PostObjectData = 9 << 8
	}

	public enum DataSetID
	{
		ModelVersion        = Record.Envelope | 0,
		Destination         = Record.Envelope | 5,
		FileFormat          = Record.Envelope | 20,
		FileFormatVersion   = Record.Envelope | 22,
		ServiceIdentifier   = Record.Envelope | 30,
		EnvelopeNumber      = Record.Envelope | 40,
		ProductID           = Record.Envelope | 50,
		EnvelopPriority     = Record.Envelope | 60,
		DateSent            = Record.Envelope | 70,
		TimeSent            = Record.Envelope | 80,
		CodedCharacterSet   = Record.Envelope | 90,
		UNO                 = Record.Envelope | 100,
		ARMIdentifier       = Record.Envelope | 120,
		ARMVersion          = Record.Envelope | 122,

		RecordVersion            = Record.Application | 0,
		ObjectTypeReference      = Record.Application | 3,
		ObjectAttributeReference = Record.Application | 4,
		ObjectName               = Record.Application | 5,
		EditStatus               = Record.Application | 8,
		Urgency                  = Record.Application | 10,
		SubjectReference         = Record.Application | 12,
		Category                 = Record.Application | 15,
		SupplementalCategory     = Record.Application | 20,
		FixtureIdentifier        = Record.Application | 22,
		Keywords                 = Record.Application | 25,
		ContentLocationCode      = Record.Application | 26,
		ContentLocationName      = Record.Application | 27,
		ReleaseDate              = Record.Application | 30,
		ReleaseTime              = Record.Application | 35,
		ExpirationDate           = Record.Application | 37,
		ExpirationTime           = Record.Application | 38,
		SpecialInstructions      = Record.Application | 40,
		ActionAdvised            = Record.Application | 42,
		ReferenceService         = Record.Application | 45,
		ReferenceDate            = Record.Application | 47,
		ReferenceNumber          = Record.Application | 50,
		DateCreated              = Record.Application | 55,
		TimeCreated              = Record.Application | 60,
		DigitalCreationDate      = Record.Application | 62,
		DigitalCreationTime      = Record.Application | 63,
		OriginatingProgram       = Record.Application | 65,
		ProgramVersion           = Record.Application | 70,
		ObjectCycle              = Record.Application | 75,
		ByLine                   = Record.Application | 80,
		ByLineTitle              = Record.Application | 85,
		City                     = Record.Application | 90,
		Sublocation              = Record.Application | 92,
		ProvinceState            = Record.Application | 95,
		PrimaryLocationCode      = Record.Application | 100,
		PrimaryLocationName      = Record.Application | 101,
		OriginalTransmissionReference = Record.Application | 103,
		Headline                 = Record.Application | 105,
		Credit                   = Record.Application | 110,
		Source                   = Record.Application | 115,
		CopyrightNotice          = Record.Application | 116,
		Contact                  = Record.Application | 118,
		CaptionAbstract          = Record.Application | 120,
		WriterEditor             = Record.Application | 122,
		RasterizedCaption        = Record.Application | 125,
		ImageType                = Record.Application | 130,
		ImageOrientation         = Record.Application | 131,
		LanguageIdentifier       = Record.Application | 135,
		AudioType                = Record.Application | 150,
		AudioSamplingRate        = Record.Application | 151,
		AudioSamplingReduction   = Record.Application | 152,
		AudioDuration            = Record.Application | 153,
		AudioOutcue              = Record.Application | 154,
		ObjectDataPreviewFileFormat = Record.Application | 200,
		ObjectDataPreviewFileFormatVersion  = Record.Application | 201,
		ObjectDataPreviewData    = Record.Application | 202,
		

	}
#if false
	public class DataSetInfo 
	{
		byte RecordNumber;
		byte DataSetNumber;
		string Name;
		string Description;.
		bool Mandatory;
		bool Repeatable;
		uint MinSize;
		uint MaxSize;
		Format Format;
		
		private static DataSetInfo [] datasets = {
			new DataSetInfo (1, 00, Format.Binary, "Model Version", true, false, 2, 2, 
					 Mono.Posix.Catalog.GetString ("IPTC Information Interchange Model (IIM) Version number"));
			new DataSetInfo (1, 05, Format.String, "Destination", false, true, 0, 1024, 
					 Mono.Posix.Catalog.GetString ("OSI Destination routing information"));
			new DataSetInfo (1, 20, Format.Binary, "File Format", true, false, 2, 2, 
					 Mono.Posix.Catalog.GetString ("IPTC file format"));
			new DataSetInfo (1, 30, "Service Identifier", true, false, 0, 10, Mono.Posix.Catalog.GetString ("Identifies the provider and product"));
			new DataSetInfo (1, 40, "Envelope Number", true, false, 8, 8, Mono.Posix.Catalog.GetString ("A unique number")),
			new DataSetInfo (1, 50, "Product I.D.", false, true, 0, 32, Mono.Posix.Catalog.GetString ("A unique number")),
			new DataSetInfo (1, 50, "Product I.D.", false, true, 0, 32, Mono.Posix.Catalog.GetString ("Provides ")),
			
		}

		public DataSetInfo (byte recnum, byte setnum, Format format, string name, bool mandatory, bool repeatable, uint min, uint max, string description)
		{
			RecordNumber = recnum;
			DataSetNumber = setnum;
			
			Name = name;
			Description = description;
			Format = Format;
		        Mandatory = optional;
			Repeatable = repeatable;
			MinSize = min;
			MaxSize = max;
		}
	}
#endif

	public class DataSet 
	{
		public byte RecordNumber;
		public byte DataSetNumber;
		public byte [] Data;
		
		const byte TagMarker = 0x1c;
		const ushort LengthMask = 1 << 15;

		public void Load (System.IO.Stream stream)
		{
			byte [] rec = new byte [5];
			stream.Read (rec, 0, rec.Length);
			if (rec [0] != TagMarker)
				throw new System.Exception (System.String.Format ("Invalid tag marker found {0} != 0x1c", 
							    TagMarker.ToString ("x")));
			
			RecordNumber = rec [1];
			DataSetNumber = rec [2];

			ulong length = FSpot.BitConverter.ToUInt16 (rec, 3, false);			

			if ((length & (LengthMask)) > 0) {
				// Note: if the high bit of the length is set the record is more than 32k long
				// and the length is stored in what would normaly be the record data, so we read
				// that data convert it to a long and continue on.
				ushort lsize = (ushort)((ushort)length & ~LengthMask);
				if (lsize > 8)
					throw new System.Exception ("Wow, that is a lot of data");

				byte [] ldata = new byte [8];
				stream.Read (ldata, 8 - lsize, lsize);
				length = FSpot.BitConverter.ToUInt64 (ldata, 0, false);
			}

			// FIXME if the length is greater than 32768 we re
			Data = new byte [length];
			stream.Read (Data, 0, Data.Length);
		}

		public void Save (System.IO.Stream stream)
		{
			stream.WriteByte (TagMarker);
			stream.WriteByte (RecordNumber);
			stream.WriteByte (DataSetNumber);
		}
	}

	public class IptcFile 
	{
		System.Collections.ArrayList sets = new System.Collections.ArrayList ();
		
		public IptcFile (System.IO.Stream stream)
		{
			Load (stream);
		}
		
		public void Load (System.IO.Stream stream)
		{
			while (stream.Position < stream.Length) {
				DataSet dset = new DataSet ();
				dset.Load (stream);
				DataSetID id = (DataSetID)((int)dset.RecordNumber << 8 | (int)dset.DataSetNumber);
				System.Console.WriteLine ("{0}:{1} - {2}", dset.RecordNumber, dset.DataSetNumber, id.ToString ());
				sets.Add (dset);
			}
		}

		public void Save (System.IO.Stream stream) 
		{
			foreach (DataSet dset in sets) {
				dset.Save (stream);
			}
		}
	}
}
