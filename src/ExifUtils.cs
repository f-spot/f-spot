using System;

public class ExifUtils {

	public struct ExposureInfo {
		public string ApertureValue;
		public string ExposureTime;
		public string IsoSpeed;
		public string DateTime;
	}

	enum MakerType {
		Unknown,
		Canon,
		Nikon
	};

	public static ExposureInfo GetExposureInfo (string path)
	{
		Exif.ExifData exif_data = new Exif.ExifData (path);

#if UNSED_CODE
		MakerType maker = MakerType.Unknown;
		string maker_tag_value = exif_data.LookupString (ExifTag.Make);
		if (maker_tag_value != null) {
			if (maker_tag_value.ToLower () == "nikon")
				maker = MakerType.Nikon;
			else if (maker_tag_value.ToLower () == "canon")
				maker = MakerType.Canon;
		}
#endif

		ExposureInfo info = new ExposureInfo ();
		info.ApertureValue = exif_data.LookupFirstValue (Exif.Tag.ApertureValue);
		info.ExposureTime = exif_data.LookupFirstValue (Exif.Tag.ExposureTime);
		info.DateTime = exif_data.LookupFirstValue (Exif.Tag.DateTimeOriginal);
		info.IsoSpeed = exif_data.LookupFirstValue (Exif.Tag.ISOSpeedRatings);

// FIXME not sure why, this doesn't work.
#if BROKEN_CODE
		// Use the maker note to figure out the ISO speed rating if it's not in the standard spot.
		if (info.IsoSpeed == null && exif_data.LookupData (ExifTag.MakerNote) != null) {
			switch (maker) {
			case MakerType.Canon:
				byte [] maker_note = exif_data.LookupData (ExifTag.MakerNote);
				ExifData maker_ifd = new ExifData (maker_note_copy, (uint) maker_note_copy.Length);
				byte [] data = maker_ifd.LookupData ((ExifTag) 0x1);

				if (data.Length > 0x10) {
					switch (data [0x10]) {
					case 0xf: info.IsoSpeed = "AUTO"; break;
					case 0x10: info.IsoSpeed = "ISO 50"; break;
					case 0x11: info.IsoSpeed = "ISO 100"; break;
					case 0x12: info.IsoSpeed = "ISO 200"; break;
					case 0x13: info.IsoSpeed = "ISO 400"; break;
					case 0x14: info.IsoSpeed = "ISO 800"; break;
					case 0x15: info.IsoSpeed = "ISO 1600"; break;
					case 0x16: info.IsoSpeed = "ISO 3200"; break;
					}
				}
				break;

			// FIXME: Add support for other MakerNotes, see:
			// http://www.fifi.org/doc/jhead/exif-e.html#APP1
			} 
		}
#endif

		return info;
	}
}
