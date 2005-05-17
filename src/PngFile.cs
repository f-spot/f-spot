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
				//if (Color != ColorType.Rgb)
				//	throw new System.Exception (System.String.Format ("unsupported {0}", Color));

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
					//System.Console.WriteLine ("found generator");
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

		private class ScanlineImage {
			int Width;
			int Height;
			byte [] Data;
			int Level;

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
				
				//System.Console.WriteLine ("type = {0}", type);
				for (int col = 1; col < this.width;  col++) {
					x = buffer [offset];

					int prev_line = offset - width;

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

						//x = (byte) (x + PaethPredict (a, b, c));
					}
#endif
					//System.Console.Write ("{0}.", x);
					buffer [offset ++] = x;
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
				while (i < dest.Width * channels) {
					pixels [i++] = (byte) (BitConverter.ToUInt16 (buffer, pos, false) >> 8);
					pos += 2;
				}

			}

			public unsafe void UnpackRGB8Line (Gdk.Pixbuf dest, int line, int channels)
			{
				int pos = line * width + 1;
				int length = width - 1;
				byte * pixels = (byte *) dest.Pixels;
				pixels += line * dest.Rowstride;
				if (dest.NChannels != channels)
					throw new System.Exception ("bad pixbuf format");

				System.Runtime.InteropServices.Marshal.Copy (buffer, pos, 
									     (System.IntPtr)pixels, dest.Width * channels);

			}

			public unsafe void UnpackGray8Line (Gdk.Pixbuf dest, int line)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;

				pixels += line * dest.Rowstride;

				if (dest.NChannels != 3)
					throw new System.Exception ("bad pixbuf format");

				int i = 0;
				while (i < dest.Width * 3) {
					pixels [i++] = buffer [pos];
					pixels [i++] = buffer [pos];
					pixels [i++] = buffer [pos];
					pos ++;
				}
			}
			
			public unsafe void UnpackGray16Line (Gdk.Pixbuf dest, int line)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;

				pixels += line * dest.Rowstride;

				if (dest.NChannels != 3)
					throw new System.Exception ("bad pixbuf format");

				int i = 0;
				while (i < dest.Width * 3) {
					byte val = (byte) (BitConverter.ToUInt16 (buffer, pos, false) >> 8);
					pixels [i++] = val;
					pixels [i++] = val;
					pixels [i++] = val;
					pos += 2;
				}
			}
			
			public unsafe void UnpackGray4Line (Gdk.Pixbuf dest, int line)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;
				
				pixels += line * dest.Rowstride;
				
				int i = 0;
				while (i < dest.Width) {
					byte val;
					val = buffer [pos + i / 2];
					val = (byte) ((i % 2 > 0) ? (val & 0x0f) : ((val & 0xf0) >> 4));
					val = (byte)  (((val * 0xff) + 8) / 0x0f); 

					pixels [i * 3 + 0] = val;
					pixels [i * 3 + 1] = val;
					pixels [i * 3 + 2] = val;
					i++;
				}
			}			
			
			public unsafe void UnpackGray1Line (Gdk.Pixbuf dest, int line)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;
				
				pixels += line * dest.Rowstride;
				
				int i = 0;
				while (i < dest.Width) {
					byte val = buffer [pos + i / 8];
				        int shift = 7 - (i % 8);

					val = (byte)((val & (1 << shift)) > 0 ? 0xff : 0x00);
					
				        
					pixels [i * 3 + 0] = val;
					pixels [i * 3 + 1] = val;
					pixels [i * 3 + 2] = val;
					i++;
				}
			}

			public unsafe void UnpackGray2Line (Gdk.Pixbuf dest, int line)
			{
				int pos = line * width + 1;
				byte * pixels = (byte *) dest.Pixels;
				
				pixels += line * dest.Rowstride;
				
				int i = 0;
				while (i < dest.Width) {
					byte val;
					val = buffer [pos + i / 4];
					switch (i % 4) {
					case 0:
						val = (byte) (val & 0x03);
						break;
					case 1:
						val = (byte) ((val & 0x0C) >> 2);
						break;
					case 2:
						val = (byte) ((val & 0x30) >> 4);
						break;
					case 3:
						val = (byte) ((val & 0xC0) >> 6);
						break;
					}						
						
					val = (byte) ((((double)val * 0xff)/ 0x03) + 0.5); 
					
					pixels [i * 3 + 0] = val;
					pixels [i * 3 + 1] = val;
					pixels [i * 3 + 2] = val;
					i++;
				}
			}
		}
		
		public Gdk.Pixbuf GetPixbuf ()
		{
			ChunkInflater ci = new ChunkInflater ();
			foreach (Chunk chunk in chunk_list) {
				if (chunk.Name == "IDAT")
					ci.Add (chunk);
			}

			IhdrChunk ihdr = (IhdrChunk) chunk_list [0];
			//System.Console.WriteLine ("Attempting to to inflate image {0}.{1}({2}, {3})", ihdr.Color, ihdr.Depth, ihdr.Width, ihdr.Height);
			ScanlineDecoder decoder = new ScanlineDecoder (ci, ihdr.GetScanlineLength (0), ihdr.Height);
			decoder.Fill ();
			//Gdk.Pixbuf pixbuf = decoder.GetPixbuf ();

			//System.Console.WriteLine ("XXXXXXXXXXXXXXXXXXXXXXXXXXX Inflate ############################");

			bool alpha = (ihdr.Color == ColorType.GrayAlpha || ihdr.Color == ColorType.RgbA);

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
				case ColorType.Gray:
					switch (ihdr.Depth) {
					case 16:
						decoder.ReconstructRow (line, 2);
						decoder.UnpackGray16Line (pixbuf, line);
						break;
					case 8:
						decoder.ReconstructRow (line, 1);
						decoder.UnpackGray8Line (pixbuf, line);
						break;
					case 4:
						decoder.ReconstructRow (line, 1);
						decoder.UnpackGray4Line (pixbuf, line);
						break;
					case 2:
						decoder.ReconstructRow (line, 1);
						decoder.UnpackGray2Line (pixbuf, line);
						break;
					case 1:
						decoder.ReconstructRow (line, 1);
						decoder.UnpackGray1Line (pixbuf, line);
						break;
					default:
						throw new System.Exception (System.String.Format ("Unhandled Depth {0}.{1}", ihdr.Color, ihdr.Depth));
					}
					break;
				default:
					throw new System.Exception (System.String.Format ("unhandled color type {0}", ihdr.Color));
				}
			}
			return pixbuf;
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
				
				//System.Console.Write ("read one {0} {1}", chunk, chunk.Name);
				chunk_list.Add (chunk);

				if (chunk is TextChunk) {
					TextChunk text = (TextChunk) chunk;
					//System.Console.Write (" Text Chunk {0} {1}", 
					//		      text.Keyword, text.Text);
				}

				TimeChunk time = chunk as TimeChunk;
				//if (time != null)
				//	System.Console.Write(" Time {0}", time.Time);

				//System.Console.WriteLine ("");
				
				if (chunk.Name == "IEND")
					break;
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
	}
}
