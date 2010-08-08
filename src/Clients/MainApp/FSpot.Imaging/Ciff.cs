using System;
using FSpot.Utils;
using Hyena;
using TagLib.Image;

namespace FSpot.Imaging.Ciff {
	internal enum Tag {
		JpgFromRaw = 0x2007,
	}

	public enum EntryType : ushort {
		Byte = 0x0000,
		Ascii = 0x0800,
		Short = 0x1000,
		Int = 0x1800,
		Struct = 0x2000,
		Directory1 = 0x2800,
		Directory2 = 0x2800,
	}

	public enum Mask {
		Type = 0x3800,
	}

	/* See http://www.sno.phy.queensu.ca/~phil/exiftool/canon_raw.html */
	internal struct Entry {
		internal Tag Tag;
		internal uint Size;
		internal uint Offset;

		public Entry (byte [] data, int pos, bool little)
		{
			Tag = (Tag) BitConverter.ToUInt16 (data, pos, little);
			Size = BitConverter.ToUInt32 (data, pos + 2, little);
			Offset = BitConverter.ToUInt32 (data, pos + 6, little);
		}
	}

	class ImageDirectory {
		System.Collections.ArrayList entry_list;
		uint Count;
		bool little;
		uint start;
		long DirPosition;
		System.IO.Stream stream;

		public ImageDirectory (System.IO.Stream stream, uint start, long end, bool little)
		{
			this.start = start;
			this.little = little;
			this.stream = stream;

			entry_list = new System.Collections.ArrayList ();

			stream.Position = end - 4;
			byte [] buf = new byte [10];
			stream.Read (buf, 0, 4);
			uint directory_pos  = BitConverter.ToUInt32 (buf, 0, little);
			DirPosition = start + directory_pos;

			stream.Position = DirPosition;
			stream.Read (buf, 0, 2);

			Count = BitConverter.ToUInt16 (buf, 0, little);

			for (int i = 0; i < Count; i++)
			{
				stream.Read (buf, 0, 10);
				Log.DebugFormat ("reading {0} {1}", i, stream.Position);
				Entry entry = new Entry (buf, 0, little);
				entry_list.Add (entry);
			}
		}

		public ImageDirectory ReadDirectory (Tag tag)
		{
			foreach (Entry e in entry_list) {
				if (e.Tag == tag) {
					uint subdir_start = this.start + e.Offset;
					ImageDirectory subdir = new ImageDirectory (stream, subdir_start, subdir_start + e.Size, little);
					return subdir;
				}
			}
			return null;
		}

		public byte [] ReadEntry (int pos)
		{
			Entry e = (Entry) entry_list [pos];

			stream.Position = this.start + e.Offset;

			byte [] data = new byte [e.Size];
			stream.Read (data, 0, data.Length);

			return data;
		}

		public byte [] ReadEntry (Tag tag)
		{
			int pos = 0;
			foreach (Entry e in entry_list) {
				if (e.Tag == tag)
					return ReadEntry (pos);
				pos++;
			}
			return null;
		}
	}

	public class CiffFile : BaseImageFile {
		ImageDirectory root;
		bool little;
		System.IO.Stream stream;

		ImageDirectory Root {
			get {
				if (root == null) {
					stream = PixbufStream ();
					root = Load (stream);
				}

			        return root;
			}
		}

		public CiffFile (SafeUri uri) : base (uri)
		{
		}

		private ImageDirectory Load (System.IO.Stream stream)
		{
			byte [] header = new byte [26];  // the spec reserves the first 26 bytes as the header block
			stream.Read (header, 0, header.Length);

			uint start;

			little = (header [0] == 'I' && header [1] == 'I');

			start = BitConverter.ToUInt32 (header, 2, little);

			// HEAP is the type CCDR is the subtype
			if (System.Text.Encoding.ASCII.GetString (header, 6, 8) != "HEAPCCDR")
				throw new ImageFormatException ("Invalid Ciff Header Block");

			long end = stream.Length;
			return new ImageDirectory (stream, start, end, little);
		}

		public override System.IO.Stream PixbufStream ()
		{
			byte [] data = GetEmbeddedJpeg ();

			if (data != null)
				return new System.IO.MemoryStream (data);
			else
				return DCRawFile.RawPixbufStream (Uri);
		}

		private byte [] GetEmbeddedJpeg ()
		{
			return Root.ReadEntry (Tag.JpgFromRaw);
		}

		protected override void Close ()
		{
			if (stream != null) {
				stream.Close ();
				stream = null;
			}
		}
	}
}
