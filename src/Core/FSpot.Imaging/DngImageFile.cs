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
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Hyena;
using TagLib;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace FSpot.Imaging
{
	/*
		public Cms.Profile GetProfile ()
		{
			Cms.ColorCIExyY whitepoint = new Cms.ColorCIExyY (0, 0, 0);
			Cms.ColorCIExyYTriple primaries = new Cms.ColorCIExyYTriple (whitepoint, whitepoint, whitepoint);
			Cms.GammaTable [] transfer = null;
			int bits_per_sample = 8;
			double gamma = 2.2;

			foreach (DirectoryEntry e in entries) {
				switch (e.Id) {
				case TagId.InterColorProfile:
					try {
						return new Cms.Profile (e.RawData);
					} catch (System.Exception ex) {
						Log.Exception (ex);
					}
					break;
				case TagId.ColorSpace:
					switch ((ColorSpace)e.ValueAsLong [0]) {
					case ColorSpace.StandardRGB:
						return Cms.Profile.CreateStandardRgb ();
					case ColorSpace.AdobeRGB:
						return Cms.Profile.CreateAlternateRgb ();
					case ColorSpace.Uncalibrated:
						Log.Debug ("Uncalibrated colorspace");
						break;
					}
					break;
				case TagId.WhitePoint:
					Rational [] white = e.RationalValue;
					whitepoint.x = white [0].Value;
					whitepoint.y = white [1].Value;
					whitepoint.Y = 1.0;
					break;
				case TagId.PrimaryChromaticities:
					Rational [] colors = e.RationalValue;
					primaries.Red.x = colors [0].Value;
					primaries.Red.y = colors [1].Value;
					primaries.Red.Y = 1.0;

					primaries.Green.x = colors [2].Value;
					primaries.Green.y = colors [3].Value;
					primaries.Green.Y = 1.0;

					primaries.Blue.x = colors [4].Value;
					primaries.Blue.y = colors [5].Value;
					primaries.Blue.Y = 1.0;
					break;
				case TagId.TransferFunction:
					ushort [] trns = e.ShortValue;
					ushort gamma_count = (ushort) (1 << bits_per_sample);
					Cms.GammaTable [] tables = new Cms.GammaTable [3];
					Log.DebugFormat ("Parsing transfer function: count = {0}", trns.Length);

					// FIXME we should use the TransferRange here
					// FIXME we should use bits per sample here
					for (int c = 0; c < 3; c++) {
						tables [c] = new Cms.GammaTable (trns, c * gamma_count, gamma_count);
					}

					transfer = tables;
					break;
				case TagId.ExifIfdPointer:
					SubdirectoryEntry exif = (SubdirectoryEntry) e;
					DirectoryEntry ee = exif.Directory [0].Lookup ((int)TagId.Gamma);

					if (ee == null)
						break;

					Rational rgamma = ee.RationalValue [0];
					gamma = rgamma.Value;
					break;
				}
			}

			if (transfer == null) {
				Cms.GammaTable basic = new Cms.GammaTable (1 << bits_per_sample, gamma);
				transfer = new Cms.GammaTable [] { basic, basic, basic };
			}

			// if we didn't get a white point or primaries, give up
			if (whitepoint.Y != 1.0 || primaries.Red.Y != 1.0)
				return null;

			return new Cms.Profile (whitepoint, primaries, transfer);
		}
	}*/
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
				var subimage_structure = sub_entries [sub_entries.Length - 1];
				var entry = subimage_structure.GetEntry (0, (ushort)IFDEntryTag.StripOffsets);
				offset = (entry as StripOffsetsIFDEntry).Values [0];
			} catch (Exception e) {
				Log.DebugException (e);
			}
		}

		public override System.IO.Stream PixbufStream ()
		{
			try {
				System.IO.Stream file = base.PixbufStream ();
				file.Position = offset;
				return file;
			} catch {
				return DCRawImageFile.RawPixbufStream (Uri);
			}
		}
	}
}
