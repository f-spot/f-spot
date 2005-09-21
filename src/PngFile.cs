using ICSharpCode.SharpZipLib.Zip.Compression;
using SemWeb;

namespace FSpot.Png {
	public class PngFile : ImageFile, SemWeb.StatementSource {
		System.Collections.ArrayList chunk_list;
		
		public PngFile (string path) : base (path)
		{
			this.path = path;
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
		   
		   xmp is XML:com.adobe.xmp

		   Other keywords may be defined for other purposes. Keywords of general interest can be registered with th
		*/
		public void Select (SemWeb.StatementSink sink)
		{
			// FIXME we should avoid the coversion to and from a string here
			// and make the stream from the deflated data.
			TextChunk xmpchunk = LookupTextChunk ("XML:com.adobe.xmp");
			if (xmpchunk == null)
				xmpchunk = LookupTextChunk ("XMP");
			
			if (xmpchunk != null) {
				System.IO.Stream xmpstream = new System.IO.MemoryStream (xmpchunk.TextData);
				FSpot.Xmp.XmpFile xmp = new FSpot.Xmp.XmpFile (xmpstream);
				xmp.Select (sink);
			}

			string description = LookupText ("Description");
			if (description != null) {
				SinkType (new Literal (description, "x-default", null), "dc:description", "rdf:Alt", sink);
			}

			string title = LookupText ("Title");
			if (title != null) {
				SinkType (new Literal (title, "x-description", null), "dc:title", "rdf:Alt", sink);
			}
			
			string author = LookupText ("Author");
			if (author != null) {
				SinkType (new Literal (author), "dc:creator", "rdf:Seq", sink);
			}

			SinkLiteral ("Comment", "exif:UserComment", sink);
			SinkLiteral ("Software", "xmp:CreatorTool", sink);
			foreach (Chunk c in Chunks) {
				if (c is TimeChunk) {
					TimeChunk tc = c as TimeChunk;
					string date = tc.Time.ToString ("yyyy-MM-ddThh:mm:ss");
					SinkLiteralValue (date, "xmp:ModifyDate", sink);
				}
			}
			
		}
		
		public void SinkLiteralValue (string value, string predicate, StatementSink sink)
		{
			Statement stmt = new Statement ((Entity)"", 
							(Entity)MetadataStore.Namespaces.Resolve (predicate), 
							new Literal (value));
			sink.Add (stmt);
		}

		public void SinkType (Literal value, string predicate, string type, StatementSink sink)
		{
			Entity empty = new Entity (null);
			Statement top = new Statement ("", (Entity)MetadataStore.Namespaces.Resolve (predicate), empty);
			Statement desc = new Statement (empty, 
							(Entity)MetadataStore.Namespaces.Resolve ("rdf:type"), 
							(Entity)MetadataStore.Namespaces.Resolve (type));
			sink.Add (desc);
			Statement literal = new Statement (empty,
							   (Entity)MetadataStore.Namespaces.Resolve ("rdf:li"),
							   value);
			sink.Add (literal);
			sink.Add (top);
		}

		public void SinkLiteral (string keyword, string predicate, StatementSink sink)
		{
			string value = LookupText (keyword);
			if (value != null)
				SinkLiteralValue (value, predicate, sink);
		}

		/*
		public void SinkAltText (string keyword, string predicate, StatementSink sink)
		{
			string value = LookupText (keyword);
			if (value != null) {
				Statement first = new Statement (
				Statement top = new Statement((Entity)""
							      (Entity)MetadataStore.Namespaces.Resolve (predicate),
							      first.Subject);
			}
		}
		*/

		public System.Collections.ArrayList Chunks {
			get {
				if (chunk_list == null) {
					using (System.IO.Stream input = System.IO.File.OpenRead (this.Path)) {
						Load (input);
					}
				}
				
				return chunk_list;
			}
		}

		public class ZtxtChunk : TextChunk {
			//public static string Name = "zTXt";

			protected bool compressed = true;
			public bool Compressed {
				get {
					return compressed;
				}
			}
			
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

				text_data = Chunk.Inflate (data, i, data.Length - i);
			}
		}

		public class TextChunk : Chunk {
			//public static string Name = "tEXt";

			protected string keyword;
			protected string text;
			protected byte [] text_data;
			protected System.Text.Encoding encoding = Latin1;

			public static System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding (28591);
			public TextChunk (string name, byte [] data) : base (name, data) {}

			public override void Load (byte [] data)
			{
				int i = 0;

				keyword = GetString (ref i);
				i++;
				int len = data.Length - i;
				text_data = new byte [len];
				System.Array.Copy (data, i, text_data, 0, len);
			}

			public string Keyword {
				get {
					return keyword;
				}
			}

			public byte [] TextData 
			{
				get {
					return text_data;
				}
			}
			
			public string Text {
				get {
					return encoding.GetString (text_data, 0, text_data.Length);
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
			string LocalizedKeyword;

			public override void Load (byte [] data)
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				compressed = (data [i++] != 0);
				Compression = data [i++];
				Language = GetString (ref i);
				i++;
				LocalizedKeyword = GetString (ref i, System.Text.Encoding.UTF8);
				i++;

				if (Compressed) {
					text_data = Chunk.Inflate (data, i, data.Length - i);
				} else {
					int len = data.Length - i;
					text_data = new byte [len];
					System.Array.Copy (data, i, text_data, 0, len);
				}
			}

			public ItxtChunk (string name, byte [] data) : base (name, data) 
			{
				encoding = System.Text.Encoding.UTF8;
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
				//if (Color != ColorType.Rgb)
				//	throw new System.Exception (System.String.Format ("unsupported {0}", Color));

				this.Compression = (CompressionMethod) data [10];
				if (this.Compression != CompressionMethod.Zlib)
					throw new System.Exception (System.String.Format ("unsupported {0}", Compression));

				Filter = (FilterMethod) data [11];
				if (Filter != FilterMethod.Adaptive)
					throw new System.Exception (System.String.Format ("unsupported {0}", Filter));
					
				Interlace = (InterlaceMethod) data [12];
				//if (Interlace != InterlaceMethod.None)
				//	throw new System.Exception (System.String.Format ("unsupported {0}", Interlace));

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

			public uint GetScanlineLength (int pass)
			{
				uint length = 0;
				if (Interlace == InterlaceMethod.None) {
					int bits = ScanlineComponents * Depth;
					length = (uint) (this.Width * bits / 8);

					// and a byte for the FilterType
					length ++;
				} else {
					throw new System.Exception (System.String.Format ("unsupported {0}", Interlace));
				}

				return length;
			}
		}

		public class Chunk {
			public string Name;
			protected byte [] data;
			protected static System.Collections.Hashtable name_table;

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

				name_table = new System.Collections.Hashtable ();
				name_table ["iTXt"] = typeof (ItxtChunk);
				name_table ["tXMP"] = typeof (ItxtChunk);
				name_table ["tEXt"] = typeof (TextChunk);
				name_table ["zTXt"] = typeof (ZtxtChunk);
				name_table ["tIME"] = typeof (TimeChunk);
				name_table ["iCCP"] = typeof (IccpChunk);
				name_table ["IHDR"] = typeof (IhdrChunk);
			}
			
			public Chunk (string name, byte [] data) 
			{
				this.Name = name;
				this.data = data;
				Load (data);
			}

			
			protected string GetString  (ref int i, System.Text.Encoding enc) 
			{
				for (; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}	
				
				return enc.GetString (data, 0, i);
			}

			protected string GetString  (ref int i) 
			{
				return GetString (ref i, TextChunk.Latin1);
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
				System.Type t = (System.Type) name_table [name];

				Chunk chunk;
				if (t != null)
					chunk = (Chunk) System.Activator.CreateInstance (t, new object[] {name, data});
				else
				        chunk = new Chunk (name, data);

				return chunk;
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

		}

		public class ChunkInflater {
			private Inflater inflater;
			private System.Collections.ArrayList chunks;

			public ChunkInflater ()
			{
				inflater = new Inflater ();
				chunks = new System.Collections.ArrayList ();
			}

			public bool Fill () 
			{
				while (inflater.IsNeedingInput && chunks.Count > 0) {
					inflater.SetInput (((Chunk)chunks[0]).Data);
					//System.Console.WriteLine ("adding chunk {0}", ((Chunk)chunks[0]).Data.Length);
					chunks.RemoveAt (0);
				}
				return true;
			}
			
			public int Inflate (byte [] data, int start, int length)
			{
				int result = 0;
				do {
					Fill ();
					int attempt = length - result;
					result += inflater.Inflate (data, start + result, length - result);
					//System.Console.WriteLine ("Attempting Second after fill Inflate {0} {1} {2}", attempt, result, length - result);
				} while (result < length && chunks.Count > 0);
				
				return result;
			}
		       
			public void Add (Chunk chunk)
			{
				chunks.Add (chunk);
			}
		}

		public class ScanlineDecoder {
			int width;
			int height;
			int row;
			int col;
			ChunkInflater inflater;
			byte [] buffer;

			public ScanlineDecoder (ChunkInflater inflater, uint width, uint height)
			{
				this.inflater = inflater;
				this.row = 0;
				this.height = (int)height;
				this.width = (int)width;
				
				buffer = new byte [width * height];

				Fill ();
			}

			public void Fill () 
			{
				for (; row < height; row ++) { 
					col = inflater.Inflate (buffer, row * width, width);
					
					if (col < width) {
						inflater.Fill ();
						System.Console.WriteLine ("short read missing {0} {1} {2}", width - col, row, height);
					}
				}
			}
			
			private static byte PaethPredict (byte a, byte b, byte c)
			{
				int p = a + b - c;
				int pa = System.Math.Abs (p - a);
				int pb = System.Math.Abs (p - b);
				int pc = System.Math.Abs (p - c);
				if (pa <= pb && pa <= pc)
					return a;
				else if (pb <= pc)
					return b;
				else 
					return c;
			}

			public void ReconstructRow (int row, int channels)
			{
				int offset = row * width;
				FilterType type = (FilterType) buffer [offset];
				byte a = 0;
				byte x;
				byte b;
				byte c = 0;
				
				offset++;
				//buffer [offset++] = 0;
				
				int prev_line;

				//System.Console.WriteLine ("type = {0}", type);
				for (int col = 1; col < this.width;  col++) {
					x = buffer [offset];

					prev_line = offset - width;

					a = col <= channels ? (byte) 0 : (byte) buffer [offset - channels];
					b = (prev_line) < 0 ? (byte) 0 : (byte) buffer [prev_line];
					c = (prev_line) < 0 || (col <= channels) ? (byte) 0 : (byte) buffer [prev_line - channels];

#if false
					switch (type) {
					case FilterType.None:
						break;
					case FilterType.Sub:
						x = (byte) (x + a);
						break;
					case FilterType.Up:
						x = (byte) (x + b);
						break;
					case FilterType.Average:
						x = (byte) (x + ((a + b) >> 1));
						break;
					case FilterType.Paeth:
						x = (byte) (x + PaethPredict (a, b, c));
						break;
					default:					
						throw new System.Exception (System.String.Format ("Invalid FilterType {0}", type));
					}
#else
					if (type == FilterType.Sub) {
						x = (byte) (x + a);
					} else if (type == FilterType.Up) {
						x = (byte) (x + b);
					} else if (type == FilterType.Average) {
						x = (byte) (x + ((a + b) >> 1));
					} else if (type == FilterType.Paeth) {
						int p = a + b - c;
						int pa = System.Math.Abs (p - a);
						int pb = System.Math.Abs (p - b);
						int pc = System.Math.Abs (p - c);
						if (pa <= pb && pa <= pc)
							x = (byte)(x + a);
						else if (pb <= pc)
							x = (byte)(x + b);
						else 
							x = (byte)(x + c);
					}
#endif
					//System.Console.Write ("{0}.", x);
					buffer [offset ++] = x;
				}

			}

			public unsafe void UnpackRGBIndexedLine (Gdk.Pixbuf dest, int line, int depth, byte [] palette, byte [] alpha)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;
				
				pixels += line * dest.Rowstride;
				int channels = dest.NChannels;
				int div = (8 / depth);
				byte mask = (byte)(0xff >> (8 - depth));

				for (int i = 0; i < dest.Width; i++) {
					int val = buffer [pos + i / div];
					int shift = (8 - depth) - (i % div) * depth;

					val = (byte) ((val & (byte)(mask << shift)) >> shift);

					pixels [i * channels] = palette [val * 3];
					pixels [i * channels + 1] = palette [val * 3 + 1];
					pixels [i * channels + 2] = palette [val * 3 + 2];

					if (channels > 3 && alpha != null) 
						pixels [i * channels + 3] = val < alpha.Length ? alpha [val] : (byte)0xff; 
				}
			}

			public unsafe void UnpackRGB16Line (Gdk.Pixbuf dest, int line, int channels)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;
				
				pixels += line * dest.Rowstride;
				
				if (dest.NChannels != channels)
					throw new System.Exception ("bad pixbuf format");

				int i = 0;
				int length = dest.Width * channels;
				while (i < length) {
					pixels [i++] = (byte) (BitConverter.ToUInt16 (buffer, pos, false) >> 8);
					pos += 2;
				}

			}

			public unsafe void UnpackRGB8Line (Gdk.Pixbuf dest, int line, int channels)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;

				pixels += line * dest.Rowstride;
				if (dest.NChannels != channels)
					throw new System.Exception ("bad pixbuf format");

				System.Runtime.InteropServices.Marshal.Copy (buffer, pos, 
									     (System.IntPtr)pixels, dest.Width * channels);

			}

			public unsafe void UnpackGrayLine (Gdk.Pixbuf dest, int line, int depth, bool alpha)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;
				
				pixels += line * dest.Rowstride;
				int div = (8 / depth);
				byte mask = (byte)(0xff >> (8 - depth));
				int length = dest.Width * (alpha ? 2 : 1);
				
				for (int i = 0; i < length; i++) {
					byte val = buffer [pos + i / div];
					int shift = (8 - depth) - (i % div) * depth;

					if (depth != 8) {
						val = (byte) ((val & (byte)(mask << shift)) >> shift);
						val = (byte) (((val * 0xff) + (mask >> 1)) / mask); 
					}
					
					if (!alpha || i % 2 == 0) {
						pixels [0] = val;
						pixels [1] = val;
						pixels [2] = val;
						pixels += 3;
					} else {
						pixels [0] = val;
						pixels ++;
					}
				}
			}

			public unsafe void UnpackGray16Line (Gdk.Pixbuf dest, int line, bool alpha)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;

				pixels += line * dest.Rowstride;

				int i = 0;
				while (i < dest.Width) {
					byte val = (byte) (BitConverter.ToUInt16 (buffer, pos, false) >> 8);
					pixels [0] = val;
					pixels [1] = val;
					pixels [2] = val;
					if (alpha) {
						pos += 2;
						pixels [3] = (byte)(BitConverter.ToUInt16 (buffer, pos, false) >> 8);
					}
					pos += 2;
					pixels += dest.NChannels;
					i++;
				}
			}

			
		}
		
		public Gdk.Pixbuf GetPixbuf ()
		{
			ChunkInflater ci = new ChunkInflater ();
			Chunk palette = null;
			Chunk transparent = null;

			foreach (Chunk chunk in Chunks) {
				if (chunk.Name == "IDAT")
					ci.Add (chunk);
				else if (chunk.Name == "PLTE") 
					palette = chunk;
				else if (chunk.Name == "tRNS")
					transparent = chunk;
			}

			IhdrChunk ihdr = (IhdrChunk) Chunks [0];
			System.Console.WriteLine ("Attempting to to inflate image {0}.{1}({2}, {3})", ihdr.Color, ihdr.Depth, ihdr.Width, ihdr.Height);
			ScanlineDecoder decoder = new ScanlineDecoder (ci, ihdr.GetScanlineLength (0), ihdr.Height);
			decoder.Fill ();
			//Gdk.Pixbuf pixbuf = decoder.GetPixbuf ();

			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXX Inflate ############################");

			bool alpha = (ihdr.Color == ColorType.GrayAlpha || ihdr.Color == ColorType.RgbA || transparent != null);

			Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, 
							    alpha, 8, (int)ihdr.Width, (int)ihdr.Height);
			
			for (int line = 0; line < ihdr.Height; line++) {
				switch (ihdr.Color) {
				case ColorType.Rgb:
					if (ihdr.Depth == 16) {
						decoder.ReconstructRow (line, 6);
						decoder.UnpackRGB16Line (pixbuf, line, 3);
					} else {
						decoder.ReconstructRow (line, 3);
						decoder.UnpackRGB8Line (pixbuf, line, 3);
					}
					break;
				case ColorType.RgbA:
					if (ihdr.Depth == 16) {
						decoder.ReconstructRow (line, 8);
						decoder.UnpackRGB16Line (pixbuf, line, 4);						
					} else {
						decoder.ReconstructRow (line, 4);
						decoder.UnpackRGB8Line (pixbuf, line, 4);
					}
					break;
				case ColorType.GrayAlpha:
					switch (ihdr.Depth) {
					case 16:
						decoder.ReconstructRow (line, 4);
						decoder.UnpackGray16Line (pixbuf, line, true);
						break;
					default:
						decoder.ReconstructRow (line, 2);
						decoder.UnpackGrayLine (pixbuf, line, ihdr.Depth, true);
						break;
					}
					break;
				case ColorType.Gray:
					switch (ihdr.Depth) {
					case 16:
						decoder.ReconstructRow (line, 2);
						decoder.UnpackGray16Line (pixbuf, line, false);
						break;
					default:
						decoder.ReconstructRow (line, 1);
						decoder.UnpackGrayLine (pixbuf, line, ihdr.Depth, false);
						break;
					}
					break;
				case ColorType.Indexed:
					decoder.ReconstructRow (line, 1);
					decoder.UnpackRGBIndexedLine (pixbuf, 
								      line, 
								      ihdr.Depth, 
								      palette.Data, 
								      transparent != null ? transparent.Data : null);
					break;
				default:
					throw new System.Exception (System.String.Format ("unhandled color type {0}", ihdr.Color));
				}
			}
			return pixbuf;
		}

		/*
		public override Gdk.Pixbuf Load ()
		{
			return this.GetPixbuf ();
		}
		*/

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
			    throw new System.Exception ("Invalid PNG magic number");

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
				
				//System.Console.Write ("read one {0} {1}", chunk, chunk.Name);
				chunk_list.Add (chunk);

#if TEST_METADATA				
				if (chunk is TextChunk) {
					TextChunk text = (TextChunk) chunk;
					System.Console.Write (" Text Chunk {0} {1}", 
							      text.Keyword, "", text.Text);
				}

				TimeChunk time = chunk as TimeChunk;
				if (time != null)
					System.Console.Write(" Time {0}", time.Time);
#endif
				//System.Console.WriteLine ("");
				
				if (chunk.Name == "IEND")
					break;
			}
		}

		public string LookupText (string keyword)
		{
			TextChunk chunk = LookupTextChunk (keyword);
			if (chunk != null)
				return chunk.Text;

			return null;
		}

		public TextChunk LookupTextChunk (string keyword)
		{
			foreach (Chunk chunk in Chunks) {
				TextChunk text = chunk as TextChunk;
				if (text != null && text.Keyword == keyword)
					return text;
			}
			return null;	
		}


		public override string Description {
			get {
				string description = LookupText ("Description");

				if (description != null)
					return description;
				else
					return LookupText ("Comment");
			}
		}

		public override System.DateTime Date {
			get {
				// FIXME: we should first try parsing the
				// LookupText ("Creation Time") as a valid date

				foreach (Chunk chunk in Chunks) {
					TimeChunk time = chunk as TimeChunk;
					if (time != null)
						return time.Time.ToUniversalTime ();
				}
				return base.Date;
			}
		}

#if false
		public class ImageFile {
			string Path;
			public ImageFile (string path)
			{
				this.Path = path;
			}
		}


		public static void Main (string [] args) 
		{
			System.Collections.ArrayList failed = new System.Collections.ArrayList ();
			Gtk.Application.Init ();
			foreach (string path in args) {
				Gtk.Window win = new Gtk.Window (path);
				Gtk.HBox box = new Gtk.HBox ();
				box.Spacing = 12;
				win.Add (box);
				Gtk.Image image;
				image = new Gtk.Image ();

				System.DateTime start = System.DateTime.Now;
				System.TimeSpan one = start - start;
				System.TimeSpan two = start - start;
				try {
					start = System.DateTime.Now;
					image.Pixbuf = new Gdk.Pixbuf (path);
					one = System.DateTime.Now - start;
				}  catch (System.Exception e) {
				}
				box.PackStart (image);

				image = new Gtk.Image ();
				try {
					start = System.DateTime.Now;
					PngFile png = new PngFile (path);
					image.Pixbuf = png.GetPixbuf ();
					two = System.DateTime.Now - start;
				} catch (System.Exception e) {
					failed.Add (path);
					//System.Console.WriteLine ("Error loading {0}", path);
					System.Console.WriteLine (e.ToString ());
				}

				System.Console.WriteLine ("{2} Load Time {0} vs {1}", one.TotalMilliseconds, two.TotalMilliseconds, path); 
				box.PackStart (image);
				win.ShowAll ();
			}
			
			System.Console.WriteLine ("{0} Failed to Load", failed.Count);
			foreach (string fail_path in failed) {
				System.Console.WriteLine (fail_path);
			}

			Gtk.Application.Run ();
		}
#endif
	}
}
