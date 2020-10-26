//
// Cr2ImageFile.cs
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

using Hyena;

using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace FSpot.Imaging
{
	class Cr2ImageFile : BaseImageFile
	{
		uint offset;

		public Cr2ImageFile (SafeUri uri) : base (uri)
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
				var entry = structure.GetEntry (0, (ushort)IFDEntryTag.StripOffsets);
				offset = (entry as StripOffsetsIFDEntry).Values[0];
			} catch (Exception e) {
				Log.DebugException (e);
			}
		}

		public override System.IO.Stream PixbufStream ()
		{
			System.IO.Stream file = base.PixbufStream ();
			file.Position = offset;
			return file;
		}
	}
}
