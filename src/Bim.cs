namespace FSpot.Bim {
	public enum EntryType : ushort {
		IPTC = 0x0404
	}

	/*
	  From what I can see it looks like these resources are a just
	  a list of records starting with a 8Bim\0 followed by a 16 bit
	  value that is the record type then a 8 bit offset and 32bit length. IPTC data is
	  type 0x0404, I don't know any other types at the moment.
	*/

	public class Entry
	{
		public ushort  Type;
		public byte [] Data;

		public Entry ()
		{

		}

		public int Load (System.IO.Stream stream)
		{
			byte [] header = new byte [6];
			
			stream.Read (header, 0, header.Length);
			if (System.Text.Encoding.ASCII.GetString (header, 0, 4) != "8BIM")
				throw new System.Exception ("missing header");
			
			Type = FSpot.BitConverter.ToUInt16 (header, 4, false);

			if (Type == (ushort)FSpot.Bim.EntryType.IPTC)
				System.Console.WriteLine ("found iptc data");

		        int offset = stream.ReadByte ();
			offset += ((offset +  1) & 1);
		       
			stream.Position += offset;
			stream.Read (header, 0, 4);
			uint length = FSpot.BitConverter.ToUInt32 (header, 0, false);

			Data = new byte [length];
			stream.Read (Data, 0, Data.Length);

			if (Data.Length % 2 > 0)
				stream.ReadByte ();

			return header.Length + Data.Length;
		}
		
		public int Save (System.IO.Stream stream) 
		{
			//stream.Write (System.Text.Encoding.ASCII.GetBytes ("8BIM"));
			//stream.Write (FSpot.BitConverter.GetBytes (Type, false));
			//stream.Write (FSpot.BitConverter.GetBytes ((uint)Data.Length, false));
			//stream.Write (Data);

			return 10 + Data.Length;
		}
	}

	public class BimFile
	{
		System.Collections.ArrayList entries = new System.Collections.ArrayList ();
		
		public BimFile (System.IO.Stream stream)
		{
			Load (stream);
		}
		
		public Entry FindEntry (EntryType type)
		{
			foreach (Entry current in entries)
				if (current.Type == (ushort)type)
					return current;

			return null;
		}

		public void Load (System.IO.Stream stream)
		{
			while (stream.Position < stream.Length)
			{
				System.Console.WriteLine ("read");
				Entry current = new Entry ();
				current.Load (stream);
				entries.Add (current);
			}
		}

		public void Save (System.IO.Stream stream)
		{
			foreach (Entry e in entries) {
				e.Save (stream);
			}
		}
	}
}



