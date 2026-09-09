//
// MinimalIccProfile.cs
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Cms.Tests
{
	// A well-formed ICC profile carrying nothing but a header and an empty tag table.
	// lcms opens it and reports its colour space, but every text tag is missing -- which
	// is what a truncated, stripped or hand-rolled .icc in a scanned directory looks
	// like, and the input the catalogue rules have to survive.
	static class MinimalIccProfile
	{
		// colorSpace is a four-character ICC signature: "RGB ", "GRAY", "CMYK".
		public static byte[] WithColorSpace (string colorSpace)
		{
			var icc = new byte[132];

			void U32 (int at, uint value)
			{
				icc[at] = (byte)(value >> 24);
				icc[at + 1] = (byte)(value >> 16);
				icc[at + 2] = (byte)(value >> 8);
				icc[at + 3] = (byte)value;
			}

			void Signature (int at, string value)
			{
				for (int i = 0; i < 4; i++)
					icc[at + i] = (byte)value[i];
			}

			U32 (0, (uint)icc.Length);
			U32 (8, 0x02100000);        // version 2.1
			Signature (12, "mntr");
			Signature (16, colorSpace);
			Signature (20, "XYZ ");
			Signature (36, "acsp");     // the magic lcms validates
			U32 (68, 0x0000f6d6);       // D50 illuminant, s15Fixed16
			U32 (72, 0x00010000);
			U32 (76, 0x0000d32d);
			U32 (128, 0);               // tag count

			return icc;
		}
	}
}
