using FSpot;
using FSpot.Utils;
using System;
using System.IO;
using System.Collections.Generic;
using Hyena;
using TagLib;
using TagLib.Image;
using TagLib.IFD;
using TagLib.IFD.Entries;
using TagLib.IFD.Tags;

namespace FSpot.Imaging.Tiff {
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

	public class DngFile : BaseImageFile {
		uint offset;

		public DngFile (SafeUri uri) : base (uri)
		{
		}

		protected override void ExtractMetadata (TagLib.Image.File metadata)
		{
			try {
				var tag = metadata.GetTag (TagTypes.TiffIFD) as IFDTag;
				var structure = tag.Structure;
				var sub_entries = (structure.GetEntry (0, (ushort) IFDEntryTag.SubIFDs) as SubIFDArrayEntry).Entries;
				var subimage_structure = sub_entries [sub_entries.Length - 1];
				var entry = subimage_structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				offset = (entry as StripOffsetsIFDEntry).Values [0];
			} catch (Exception e) {
				Log.DebugException (e);
			}
			base.ExtractMetadata (metadata);
		}

		public override System.IO.Stream PixbufStream ()
		{
			try {
				System.IO.Stream file = base.PixbufStream ();
				file.Position = offset;
				return file;
			} catch {
				return DCRawFile.RawPixbufStream (Uri);
			}
		}
	}	
	
	public class NefFile : BaseImageFile {
		byte [] jpeg_data;

		public NefFile (SafeUri uri) : base (uri)
		{
		}

		protected override void ExtractMetadata (TagLib.Image.File metadata)
		{
			try {
				var tag = metadata.GetTag (TagTypes.TiffIFD) as IFDTag;
				var structure = tag.Structure;
				var SubImage1_structure = (structure.GetEntry (0, (ushort) IFDEntryTag.SubIFDs) as SubIFDArrayEntry).Entries [0];
				var entry = SubImage1_structure.GetEntry (0, (ushort) IFDEntryTag.JPEGInterchangeFormat);
				jpeg_data = (entry as ThumbnailDataIFDEntry).Data.Data;
			} catch (Exception e) {
				Log.DebugException (e);
				jpeg_data = null;
			}
			base.ExtractMetadata (metadata);
		}

		public override System.IO.Stream PixbufStream ()
		{
			if (jpeg_data != null) {
				return new MemoryStream (jpeg_data);
			} else {
				return DCRawFile.RawPixbufStream (Uri);
			}
		}
	}
		

	public class Cr2File : BaseImageFile {
		uint offset;

		public Cr2File (SafeUri uri) : base (uri)
		{
		}

		protected override void ExtractMetadata (TagLib.Image.File metadata)
		{
			try {
				var tag = metadata.GetTag (TagTypes.TiffIFD) as IFDTag;
				var structure = tag.Structure;
				var entry = structure.GetEntry (0, (ushort) IFDEntryTag.StripOffsets);
				offset = (entry as StripOffsetsIFDEntry).Values [0];
			} catch (Exception e) {
				Log.DebugException (e);
			}
			base.ExtractMetadata (metadata);
		}

		public override System.IO.Stream PixbufStream ()
		{
			System.IO.Stream file = base.PixbufStream ();
			file.Position = offset;
			return file;
		}
	}

}

