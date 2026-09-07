//
// DngImageFile.cs
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
	class DngImageFile : BaseImageFile
	{
		uint offset;

		public DngImageFile (SafeUri uri) : base (uri)
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
				var sub_entries = (structure.GetEntry (0, (ushort)IFDEntryTag.SubIFDs) as SubIFDArrayEntry).Entries;
				var subimage_structure = sub_entries[sub_entries.Length - 1];
				var entry = subimage_structure.GetEntry (0, (ushort)IFDEntryTag.StripOffsets);
				offset = (entry as StripOffsetsIFDEntry).Values[0];
			} catch (Exception e) {
				Logger.Log.Debug (e, "");
			}
		}

		public override System.IO.Stream OpenImageStream ()
		{
			try {
				System.IO.Stream file = base.OpenImageStream ();
				file.Position = offset;
				return file;
			} catch {
				return DCRawImageFile.OpenRawImageStream (Uri);
			}
		}
	}
}
