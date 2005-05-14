using ICSharpCode.SharpZipLib.Zip.Compression;

namespace FSpot {
	public class PngFile  {
		string Path;
		System.Collections.ArrayList chunk_list;
		
		public PngFile (string path)
		{
			this.Path = path;
			System.IO.Stream input = System.IO.File.Open (this.Path, System.IO.FileMode.Open);
			Load (input);
			input.Close ();
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
		
		public class ZtxtChunk : TextChunk {
			//public static string Name = "zTXt";

			byte compression;
			public byte Compression {
			        get {
					return compression;
				}
				set {
					if (compression != 0)
						throw new System.Exception ("Unknown compression method");
				}
			}

			public ZtxtChunk (string name, byte [] data) : base (name, data) {}
			
			public override void Load (byte [] data) 
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				Compression = data [i++];

				byte [] inflated = Chunk.Inflate (data, i, data.Length - i);
				text = TextChunk.Latin1.GetString (inflated, 0, inflated.Length);
			}

			new public static Chunk Create (string name, byte [] data)
			{
				return new ZtxtChunk (name, data);
			}
		}

		public class TextChunk : Chunk {
			//public static string Name = "tEXt";

			protected string keyword;
			protected string text;
			protected static System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding (28591);
			protected string GetString  (ref int i) 
			{
				for (; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}	
				
				return System.Text.Encoding.ASCII.GetString (data, 0, i);
			}

			public TextChunk (string name, byte [] data) : base (name, data) {}

			public override void Load (byte [] data)
			{
				int i = 0;

				keyword = GetString (ref i);
				i++;
				text = TextChunk.Latin1.GetString (data, i, data.Length - i);
			}

			public static Chunk Create (string name, byte [] data)
			{
				return new ItxtChunk (name, data);
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
 
		public class ItxtChunk : ZtxtChunk{
			//public static string Name = "zTXt";

			string Language;
			bool Compressed;

			public override void Load (byte [] data)
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				Compressed = (data [i++] != 0);
				Compression = data [i++];
				Language = GetString (ref i);
				i++;
				if (Compressed) {
					byte [] inflated = Chunk.Inflate (data, i, data.Length - i);
					text = TextChunk.Latin1.GetString (inflated, 0, inflated.Length);
				} else {
					text = System.Text.Encoding.ASCII.GetString (data, i, data.Length - i);
				}
			}

			public ItxtChunk (string name, byte [] data) : base (name, data) {}

			new protected static Chunk Create (string name, byte [] data)
			{
				return new ItxtChunk ("iTXt", data);
			}
		}

		public class TimeChunk : Chunk {
			//public static string Name = "tIME";

			System.DateTime time;

			public System.DateTime Time {
				get {
					return new System.DateTime (FSpot.BitConverter.ToUInt16 (data, 0, false),
								    data [2], data [3], data [4], data [5], data [6]);

				}
				set {
					byte [] year = BitConverter.GetBytes ((ushort)value.Year, false);
					data [0] = year [0];
					data [1] = year [1];
					data [2] = (byte) value.Month;
					data [3] = (byte) value.Day;
					data [4] = (byte) value.Hour;
					data [6] = (byte) value.Minute;
					data [7] = (byte) value.Second;
				}
			}
			
			public TimeChunk (string name, byte [] data) : base (name, data) {}

			public static Chunk Create (string name, byte [] data)
			{
				TimeChunk chunk = new TimeChunk (name, data);
				return chunk;
			}
		}

		public class Chunk {
			public string Name;
			public byte [] data;

			public byte [] Data {
				get {
					return data;
				}
			}
			
			static Chunk () 
			{
				name_table ["iTXt"] = new ChunkGenerator (ItxtChunk.Create);
				name_table ["tXMP"] = new ChunkGenerator (TextChunk.Create);
				name_table ["tEXt"] = new ChunkGenerator (TextChunk.Create);
				name_table ["zTXt"] = new ChunkGenerator (ZtxtChunk.Create);
				name_table ["tIME"] = new ChunkGenerator (TimeChunk.Create);
			}
			
			public Chunk (string name, byte [] data) 
			{
				this.Name = name;
				this.data = data;
				Load (data);
			}

			public virtual void Load (byte [] data)
			{
				
			}

			public bool Critical {
				get {
					return !System.Char.IsLower (Name, 0);
				}
			}

			public bool Private {
				get {
					return System.Char.IsLower (Name, 1);
				}
			}
			
			public bool Reserved {
				get {
					return System.Char.IsLower (Name, 2);
				}
			}
			
			public bool Safe {
				get {
					return System.Char.IsLower (Name, 3);
				}
			}

			public static Chunk Generate (string name, byte [] data)
			{
				ChunkGenerator gen = (ChunkGenerator) name_table [name];
				
				System.Console.WriteLine ("Looking for {0}", name);
				if (gen != null) {
					System.Console.WriteLine ("found gererator");
					return gen (name, data);
				} else {
					return new Chunk (name, data);
				}
			}

			public static byte [] Inflate (byte [] input, int start, int length)
			{
				System.IO.MemoryStream output = new System.IO.MemoryStream ();
				Inflater inflater = new Inflater ();
				
				inflater.SetInput (input, start, length);
				
				byte [] buf = new byte [1024];
				int inflate_length;
				while ((inflate_length = inflater.Inflate (buf)) > 0) {
					output.Write (buf, 0, inflate_length);
				}
				
				byte [] result = new byte [output.Length];
				output.Position = 0;
				output.Read (result, 0, result.Length);
				output.Close ();
				return result;
			}

			public delegate Chunk ChunkGenerator (string name, byte [] data);
			protected static System.Collections.Hashtable name_table = new System.Collections.Hashtable ();
		}

	        void Load (System.IO.Stream stream)
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

			System.Console.WriteLine ("bleh");
			chunk_list = new System.Collections.ArrayList ();

			while (stream.Read (heading, 0, heading.Length) == heading.Length)
			{
				uint length = BitConverter.ToUInt32 (heading, 0, false);
				string name = System.Text.Encoding.ASCII.GetString (heading, 4, 4);
				byte [] data = new byte [length];
				if (length > 0)
					stream.Read (data, 0, data.Length);

				stream.Read (heading, 0, 4);
				uint crc = BitConverter.ToUInt32 (heading, 0, false);

				Chunk chunk = Chunk.Generate (name, data);
				//if (crc != chunk.Crc ())
				//	throw new System.Exception ("chunk crc check failed");
				
				System.Console.WriteLine ("read one {0}", chunk);
				chunk_list.Add (chunk);
				if (chunk is TextChunk) {
					TextChunk text = (TextChunk) chunk;
					System.Console.WriteLine ("Parsed Text Chunk {0} {1} {2}", 
								  text.Name, text.Keyword, text.Text);
				}
				System.Console.WriteLine ("reading two");
			}
		}
		
		public static void Main (string [] args) 
		{
			PngFile png = new PngFile (args [0]);
		}
	}
}
