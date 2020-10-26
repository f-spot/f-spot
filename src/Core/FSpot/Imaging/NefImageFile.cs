//
// NefImageFile.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@src.gnome.org>
//
// Copyright (C) 2005-2010 Novell, Inc.
// Copyright (C) 2010 Ruben Vermeersch
// Copyright (C) 2005-2007 Larry Ewing
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Hyena;

using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace FSpot.Imaging
{
	class NefImageFile : BaseImageFile
	{
		byte[] jpeg_data;

		public NefImageFile (SafeUri uri) : base (uri)
		{
		}

		protected override void ExtractMetadata (TagLib.Image.File metadata)
		{
			base.ExtractMetadata (metadata);

			if (metadata == null)
				return;

			try {
				var tag = metadata.GetTag (TagTypes.TiffIFD) as IFDTag;
				var structure = tag.Structure;
				var SubImage1_structure = (structure.GetEntry (0, (ushort)IFDEntryTag.SubIFDs) as SubIFDArrayEntry).Entries[0];
				var entry = SubImage1_structure.GetEntry (0, (ushort)IFDEntryTag.JPEGInterchangeFormat);
				jpeg_data = (entry as ThumbnailDataIFDEntry).Data.Data;
			} catch (Exception e) {
				Log.DebugException (e);
				jpeg_data = null;
			}
		}

		public override Stream PixbufStream ()
		{
			return jpeg_data != null ? new MemoryStream (jpeg_data) : DCRawImageFile.RawPixbufStream (Uri);
		}
	}
}
