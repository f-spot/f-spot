//
// CiffImageFile.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@src.gnome.org>
//   Stephen Shaw <sshaw@decriptor.com>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

using Hyena;

namespace FSpot.Imaging
{
	class CiffImageFile : BaseImageFile
	{
		#region private types

		enum Tag
		{
			JpgFromRaw = 0x2007,
		}

		/* See http://www.sno.phy.queensu.ca/~phil/exiftool/canon_raw.html */
		struct Entry
		{
			internal Tag Tag;
			internal uint Size;
			internal uint Offset;

			public Entry (byte[] data, int pos, bool little)
			{
				Tag = (Tag)BitConverter.ToUInt16 (data, pos, little);
				Size = BitConverter.ToUInt32 (data, pos + 2, little);
				Offset = BitConverter.ToUInt32 (data, pos + 6, little);
			}
		}

		class ImageDirectory
		{
			readonly List<Entry> entry_list;
			readonly uint Count;
			readonly bool little;
			readonly uint start;
			readonly long DirPosition;
			readonly Stream stream;

			public ImageDirectory (Stream stream, uint start, long end, bool little)
			{
				this.start = start;
				this.little = little;
				this.stream = stream;

				entry_list = new List<Entry> ();

				stream.Position = end - 4;
				var buf = new byte[10];
				stream.Read (buf, 0, 4);
				uint directory_pos = BitConverter.ToUInt32 (buf, 0, little);
				DirPosition = start + directory_pos;

				stream.Position = DirPosition;
				stream.Read (buf, 0, 2);

				Count = BitConverter.ToUInt16 (buf, 0, little);

				for (int i = 0; i < Count; i++) {
					stream.Read (buf, 0, 10);
					Log.Debug ($"reading {i} {stream.Position}");
					var entry = new Entry (buf, 0, little);
					entry_list.Add (entry);
				}
			}

			public ImageDirectory ReadDirectory (Tag tag)
			{
				foreach (Entry e in entry_list) {
					if (e.Tag == tag) {
						uint subdir_start = start + e.Offset;
						var subdir = new ImageDirectory (stream, subdir_start, subdir_start + e.Size, little);
						return subdir;
					}
				}
				return null;
			}

			public byte[] ReadEntry (int pos)
			{
				Entry e = entry_list[pos];

				stream.Position = start + e.Offset;

				var data = new byte[e.Size];
				stream.Read (data, 0, data.Length);

				return data;
			}

			public byte[] ReadEntry (Tag tag)
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

		#endregion

		ImageDirectory root;
		bool little;
		Stream stream;

		ImageDirectory Root {
			get {
				if (root == null) {
					stream = base.PixbufStream ();
					root = LoadImageDirectory ();
				}
				return root;
			}
		}

		public CiffImageFile (SafeUri uri) : base (uri)
		{
		}

		ImageDirectory LoadImageDirectory ()
		{
			var header = new byte[26];  // the spec reserves the first 26 bytes as the header block
			stream.Read (header, 0, header.Length);

			uint start;

			little = (header[0] == 'I' && header[1] == 'I');

			start = BitConverter.ToUInt32 (header, 2, little);

			// HEAP is the type CCDR is the subtype
			if (System.Text.Encoding.ASCII.GetString (header, 6, 8) != "HEAPCCDR")
				throw new ImageFormatException ("Invalid Ciff Header Block");

			long end = stream.Length;
			return new ImageDirectory (stream, start, end, little);
		}

		public override Stream PixbufStream ()
		{
			byte[] data = GetEmbeddedJpeg ();
			return data != null ? new MemoryStream (data) : DCRawImageFile.RawPixbufStream (Uri);
		}

		byte[] GetEmbeddedJpeg ()
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
