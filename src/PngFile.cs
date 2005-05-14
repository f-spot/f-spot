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
		
		public class TextChunk : Chunk {
			string keyword;
			string text;
			
			public ItxtChunk (byte [] data)
			{
				int i;
				for (int i = 0; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}

				keyword = System.Text.ASCIIEncoding.GetString (data, 0, i);
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
 
		public class ItxtChunk : Chunk{
			string keyword;
			string text;

			public ItxtChunk (byte [] data)
			{
				int i;
				for (int i = 0; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}

				keyword = System.Text.ASCIIEncoding.GetString (data, 0, i);
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

		public class Chunk {
			public string Name;
			public byte [] Data;
			
			static Chunk () {
				name_table ["iTXt"] = new ChunkGenerator (ItxtChunk.Create);
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
