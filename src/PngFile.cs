namespace FSpot {
	public class PngFile : ImageFile {
		System.Collections.ArrayList chunks;
		

		public PngFile (string path) : base (path) {}

		public string description;
		public string Description {
			get {
				PATH
			}
			set {

			}
		}

		/**
		   Title 	Short (one line) title or caption for image
		   Author 	Name of image's creator
		   Description 	Description of image (possibly long)
		   Copyright 	Copyright notice
		   Creation Time 	Time of original image creation
		   Software 	Software used to create the image
		   Disclaimer 	Legal disclaimer
		   Warning 	Warning of nature of content
		   Source 	Device used to create the image
		   Comment 	Miscellaneous comment
		   
		   xmp is iTXt:XML:com.adobe.xmp

		   Other keywords may be defined for other purposes. Keywords of general interest can be registered with th
		*/
		
		public override Save (Gdk.Pixbuf pixbuf, System.IO.Stream stream)
		{
			
			
		}
		
		public class ZtxtChunk : TextChunk {
			byte Compression;

			public ZtxtChunk (byte [] data)
			{
				int i;
				keyword = GetKeyword (out i);
				i++;
				compression = data [i];
			}


		}
		public class TextChunk : Chunk {
			protected string keyword;
			protected string text;
			
			protected string GetKeyword (out int i) 
			{
				for (int i = 0; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}	
				
				return System.Text.ASCIIEncoding.GetString (data, 0, i);
			}

			public ItxtChunk (byte [] data)
			{
				int i;

				keyword = GetKeyword (out i);
				i++;
				text = System.Text.ASCIIEncoding.GetString (data, i, data.Length - i);
			}

			protected static Create (string name, byte [] data)
			{
				return new ItxtChunk (data);
			}

			public string Keyword {
				get {
					return keyword;
				}
			}

			public string Text {
				get {
					return text;
				}
			}
		}
 
		public class TimeChunk : Chunk {
			System.DateTime time;

			public System.DateTime Time {
				get {
					int year = FSpot.BitConverter.ToUInt16 (data, 0, false);
					return new System.DateTime (FSpot.BitConverter.ToUInt16 (data, 0, false),
								    data [2], data [3], data [4], data [5], data [6]);

				}
				set {
					byte [] year = BitConverter.GetBytes ((ushort)value.Year);
					data [0] = year [0];
					data [1] = year [1];
					data [2] = (byte) value.Month;
					data [3] = (byte) value.Day;
					data [4] = (byte) value.Hour;
					data [6] = (byte) value.Minute;
					data [7] = (byte) value.Sec;
				}
			}
			
			protected static Create (string name, byte [] data)
			{
				TimeChunk chunk = new TimeChunk ();
				chunk.Name = name;
				chunk.data = data;
				return chunk;
			}
		}

		public class ItxtChunk : ZtxtChunk{
			string Language;
			byte Compressed;
			public ItxtChunk (byte [] data)
			{
				int i;
				keyword = GetKeyword (out i);
				i++;
				Compressed = data [i++];
				Compression = data [i++];
				

				text = System.Text.ASCIIEncoding.GetString (data, i, data.Length - i);
			}

			protected static Create (string name, byte [] data)
			{
				return new ItxtChunk (data);
			}

			public string Keyword {
				get {
					return keyword;
				}
			}

			public string Text {
				get {
					return text;
				}
			}
		}

		public class Chunk {
			public string Name;
			public byte [] data;

			public byte [] Data {
				get {
					return Data [];
				}
			}
			
			static Chunk () {
				name_table ["iTXt"] = new ChunkGenerator (ItxtChunk.Create);
				name_table ["tEXt"] = new ChunkGenerator (TextChunk.Create);
				name_table ["tIME"] = new ChunkGenerator (TimeChunk.Create);
			}
			
			public bool Critical {
				get {
					!System.Char.IsLower (Name, 0);
				}
			}

			public bool Private {
				get {
					System.Char.IsLower (Name, 1);
				}
			}

			public bool Reserved {
				get {
					System.Char.IsLower (Name, 2);					
				}
			}

			public bool Safe {
				get {
					System.Char.IsLower (Name, 3);
				}
			}
				

			public Chunk Generate (string name, byte [] data)
			{
				ChunkGenerator gen = name_table [name];
				
				if (gen != null)
					return gen (name, data);
				else
					return new Chunk (name, data);
			}
			
			public uint Crc ()
			{
			}

			public static delegate Chunk ChunkGenerator (string name, byte [] data);
			protected static System.Collections.Hashtable name_table = new System.Collections.Hashtable ();
		}

		public void Load (System.IO.Stream stream)
		{
			byte [] heading = new byte [8];
			stream.Read (heading, 0, heading.Length);

			if (heading [0] != 137 ||
			    heading [1] != 80 ||
			    heading [2] != 78 ||
			    heading [3] != 71 ||
			    heading [4] != 13 ||
			    heading [5] != 10 ||
			    heading [6] != 26 ||
			    heading [7] != 10)
			    throw new System.Exception ("This ain't no png file");
			    
			while (stream.Read (heading, 0, heading.Length) == heading.Length)
			{
				uint length = BitConverter.ToUInt32 (heading, 0, false);
				string name = System.Text.ASCIIEncoding.GetString (heading, 4, 4);
				byte [] data = new byte [length];
				if (length > 0)
					string.Read (data, 0, data.Length);

				stream.Read (heading, 0, 4);
				int crc = BitConverter.ToUInt32 (heading, 0, false);

				Chunk chunk = Chunk.Generate (name, data);
				if (crc != chunk.Crc ())
					throw new System.Exception ("chunk crc check failed");
				
				chunk_list.Add (chunk);
			}
		}
	}
}
