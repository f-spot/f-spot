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
			public static System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding (28591);
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
				return new TextChunk (name, data);
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
		
		public class IccpChunk : Chunk {
			string keyword;
			byte [] profile;

			public IccpChunk (string name, byte [] data) : base (name, data) {}
			
			public override void Load (byte [] data)
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				int compression = data [i++];
				if (compression != 0)
					throw new System.Exception ("Unknown Compression type");

				profile = Chunk.Inflate (data, i, data.Length - i);
			}

			new public static Chunk Create (string name, byte [] data)
			{
				return new IccpChunk (name, data);
			}

			public string Keyword {
				get {
					return keyword;
				}
			}
			
			public byte [] Profile {
				get {
					return profile;
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
					text = TextChunk.Latin1.GetString (data, i, data.Length - i);
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

		public enum ColorType : byte {
			Gray = 0,
			Rgb = 2,
			Indexed = 3,
			GrayAlpha = 4,	
			RgbA = 6
		};
		
		public enum CompressionMethod : byte {
			Zlib = 0
		};
		
		public enum InterlaceMethod : byte {
			None = 0,
			Adam7 = 1
		};

		public enum FilterMethod : byte {
			Adaptive = 0
		}

		// Filter Types Show up as the first byte of each scanline
		public enum FilterType  {
			None = 0,
			Sub = 1,
			Up = 2,
			Average = 3,
			Paeth = 4
		};

		public class IhdrChunk : Chunk {
			public uint Width;
			public uint Height;
			public byte Depth;
			public ColorType Color;
			public PngFile.CompressionMethod Compression;
			public FilterMethod Filter;
			public InterlaceMethod Interlace;

			public IhdrChunk (string name, byte [] data) : base (name, data) {}
			
			public override void Load (byte [] data)
			{
				Width = BitConverter.ToUInt32 (data, 0, false);
				Height = BitConverter.ToUInt32 (data, 4, false);
				Depth = data [8];
				Color = (ColorType) data [9];
				if (Color != ColorType.Rgb)
					throw new System.Exception (System.String.Format ("unsupported {0}", Color));

				this.Compression = (CompressionMethod) data [10];
				if (this.Compression != CompressionMethod.Zlib)
					throw new System.Exception (System.String.Format ("unsupported {0}", Compression));

				Filter = (FilterMethod) data [11];
				if (Filter != FilterMethod.Adaptive)
					throw new System.Exception (System.String.Format ("unsupported {0}", Filter));
					
				Interlace = (InterlaceMethod) data [12];
				if (Interlace != InterlaceMethod.None)
					throw new System.Exception (System.String.Format ("unsupported {0}", Interlace));

			}

			public int ScanlineComponents {
				get {
					switch (Color) {
					case ColorType.Gray:
					case ColorType.Indexed:
						return 1;
					case ColorType.GrayAlpha:
						return 2;
					case ColorType.Rgb:
						return 3;
					case ColorType.RgbA:
						return 4;
					default:
						throw new System.Exception (System.String.Format ("Unknown format {0}", Color));
					}
				}
			}

			public int GetScanlineLength (int pass)
			{
				int length = 0;
				if (Interlace == InterlaceMethod.None) {
					int bits = ScanlineComponents * Depth;
					length = bits / 8;

					// add a byte if the bits don't fit
					if (bits % 8 > 0)
						length ++;
					// and a byte for the FilterType
					length ++;
				} else {
					throw new System.Exception (System.String.Format ("unsupported {0}", Interlace));
				}

				return length;
			}

			new public static Chunk Create (string name, byte [] data)
			{
				return new IhdrChunk (name, data);
			}
		}

		public class Chunk {
			public string Name;
			protected byte [] data;

			public byte [] Data {
				get {
					return data;
				}
				set {
					Load (value);
				}
			}
			
			static Chunk () 
			{
				name_table ["iTXt"] = new ChunkGenerator (ItxtChunk.Create);
				name_table ["tXMP"] = new ChunkGenerator (TextChunk.Create);
				name_table ["tEXt"] = new ChunkGenerator (TextChunk.Create);
				name_table ["zTXt"] = new ChunkGenerator (ZtxtChunk.Create);
				name_table ["tIME"] = new ChunkGenerator (TimeChunk.Create);
				name_table ["iCCP"] = new ChunkGenerator (IccpChunk.Create);
				name_table ["IHDR"] = new ChunkGenerator (IhdrChunk.Create);
			}
			
			public Chunk (string name, byte [] data) 
			{
				this.Name = name;
				this.data = data;
				Load (data);
			}

			protected string GetString  (ref int i) 
			{
				for (; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}	
				
				return TextChunk.Latin1.GetString (data, 0, i);
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

		public class ChunkInflater {
			private Inflater inflater;
			private System.Collections.ArrayList chunks;

			public bool Fill () 
			{
				if (inflater.IsNeedingInput && chunks.Count > 0) {
					if (chunks.Count > 0) {
						inflater.SetInput (((Chunk)chunks[0]).Data);
						chunks.RemoveAt (0);
						return true;
					} 
					return false;
				}
				return true;
			}
			
			public int Inflate (byte [] data, int start, int length)
			{
				int result = inflater.Inflate (data, start, length);
				if (result < length) {
					Fill ();
					result += inflater.Inflate (data, result, length - result);
				}

				return result;
			}
		       
			public void Add (Chunk chunk)
			{
				chunks.Add (chunk);
				Fill ();
			}
		}
		
		public class ScanlineDecoder {
			int width;
			int height;
			int row;
			int col;
			ChunkInflater inflater;
			byte [] buffer;

			public ScanlineDecoder (ChunkInflater inflater, int width, int height)
			{
				this.inflater = inflater;
				this.row = 0;
				this.height = height;
				this.width = width;
				
				buffer = new byte [width * height];

				Fill ();
			}

			public void Fill () 
			{
				for (; row < height; row ++) { 
					col = inflater.Inflate (buffer, row * height, width);

					if (col < width)
						throw new System.Exception ("Short Read");
				}
			}

			public void Filter ()
			{
				
			}
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

			chunk_list = new System.Collections.ArrayList ();

			for (int i = 0; stream.Read (heading, 0, heading.Length) == heading.Length; i++) {
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
				
				System.Console.Write ("read one {0} {1}", chunk, chunk.Name);
				chunk_list.Add (chunk);

				if (chunk is TextChunk) {
					TextChunk text = (TextChunk) chunk;
					System.Console.Write (" Text Chunk {0} {1}", 
								  text.Keyword, text.Text);
				}

				TimeChunk time = chunk as TimeChunk;
				if (time != null)
					System.Console.Write(" Time {0}", time.Time);

				System.Console.WriteLine ("");
				
				if (chunk.Name == "IEND")
					break;
			}

			
			
		}

		public static void Main (string [] args) 
		{
			foreach (string path in args) {
				try {
					new PngFile (path);
				} catch (System.Exception e) {
					System.Console.WriteLine ("Error loading {0}", path);
					System.Console.WriteLine (e.ToString ());
				}
			}
		}
	}
}
