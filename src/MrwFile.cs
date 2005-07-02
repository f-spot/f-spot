using FSpot.Tiff;

namespace FSpot.Mrw {
	// Minolta raw format
	// see http://www.dalibor.cz/minolta/raw_file_format.htm for details

	public class Block {
		protected byte [] name;
		protected uint  Length;
		protected long Start;
		protected System.IO.Stream stream;
		byte [] data;

		public Block (System.IO.Stream stream)
		{
			this.stream = stream;
			Start = stream.Position;
			name = new byte [4];
			byte [] tmp = new byte [8];
			stream.Read (tmp, 0, tmp.Length);
			System.Array.Copy (tmp, name, name.Length);
			System.Console.WriteLine (System.Text.Encoding.ASCII.GetString (name, 1, 3));
			Length = BitConverter.ToUInt32 (tmp, name.Length, false);
			stream.Position = stream.Position + Length;
		}
		
		public byte [] Data {
			get {
				if (data == null)
					data = ReadData ();
				
				return data;
			}
		}

		protected byte [] ReadData ()
		{
			stream.Position = Start + 8;
			byte [] data = new byte [this.Length];
			stream.Read (data, 0, data.Length);

			return data;
		}
	}

	public class PrdBlock : Block {
		public PrdBlock (System.IO.Stream stream) : base (stream)
		{

		}

		public ulong Version {
			get {
				return BitConverter.ToUInt64 (this.Data, 0, false);
			}
		}
		
		public ushort CCDSizeY {
			get {
				return BitConverter.ToUInt16 (this.Data, 8, false);
			}
		}

		public ushort CCDSizeX {
			get {
				return BitConverter.ToUInt16 (this.Data, 10, false);
			}
		}

		public ushort ImageSizeY {
			get {
				return BitConverter.ToUInt16 (this.Data, 12, false);
			}
		}

		public ushort ImageSizeX {
			get {
				return BitConverter.ToUInt16 (this.Data, 14, false);
			}
		}
		
		public byte Depth {
			get {
				return this.Data [16];
			}
		}

		public byte SampleDepth {
			get {
				return this.Data [17];
			}
		}
	}

	internal class TtwBlock : Block {
		FSpot.Tiff.Header header;

		public TtwBlock (System.IO.Stream stream) : base (stream)
		{

		}
		
		public FSpot.Tiff.Header TiffHeader {
			get {
				if (header == null) {
					try {
						System.IO.MemoryStream mem = new System.IO.MemoryStream (this.Data);
						System.Console.WriteLine ("before header");
						header = new Header (mem);
					} catch (System.Exception e) {
						System.Console.WriteLine (e.ToString ());
					}
				}
				
				return header;
			}
		}
	}

	internal class MrmBlock : Block {
		Block [] blocks;

		public MrmBlock (System.IO.Stream stream) : base (stream) {}

		protected void Load ()
		{
			stream.Position = Start + 8;
			blocks = new Block [4];
			
			blocks [0] = new PrdBlock (stream);
			blocks [1] = new TtwBlock (stream);
			blocks [2] = new Block (stream);
			blocks [3] = new Block (stream);
		}

		public Block [] Blocks {
			get {
				if (blocks == null) {
					Load ();
				}

				return blocks;
			}
		}
		
	}
	
	public class MrwFile : ImageFile {
		MrmBlock mrm;
		FSpot.Tiff.Header Header;

		public MrwFile (string path) : base (path)
		{
			LoadBlocks ();
			System.Console.WriteLine ("testing {0}", this.Date ().ToString ());
		}

		public override System.DateTime Date () 
		{
			AsciiEntry e = (AsciiEntry)(this.Header.Directory.Lookup (TagId.DateTime));

			if (e != null)
				return DirectoryEntry.DateTimeFromString (e.StringValue);
			else
				return base.Date ();
		}

		public override System.IO.Stream PixbufStream ()
		{
			return null;
		}
		
		public override Gdk.Pixbuf Load ()
		{
			return DCRawFile.Load (this.Path, null);
		}

		public override Gdk.Pixbuf Load (int width, int height)
		{
			return PixbufUtils.ScaleToMaxSize (this.Load (), width, height);
		}

		protected void LoadBlocks () 
		{
			using (System.IO.Stream file = System.IO.File.OpenRead (this.path)) {
				mrm = new MrmBlock (file);
				System.Console.WriteLine ("here");
				try {
				Header = ((TtwBlock)mrm.Blocks [1]).TiffHeader;
				Header.Dump ();
				} catch (System.Exception e) {
					System.Console.WriteLine (e.ToString ());
				}
					
				System.Console.WriteLine ("here2");
			}
		}
	}

}
