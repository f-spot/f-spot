// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace FSpot.Cms.Tests
{
	// An ICC header plus one desc tag, so a catalogue test can name a profile lcms
	// would otherwise report with an empty description.
	static class DescribedProfile
	{
		public static byte[] Output (string colorSpace, string description)
			=> Build ("prtr", colorSpace, description);

		public static byte[] Display (string colorSpace, string description)
			=> Build ("mntr", colorSpace, description);

		static byte[] Build (string deviceClass, string colorSpace, string description)
		{
			byte[] ascii = Encoding.ASCII.GetBytes (description + "\0");
			int descSize = 12 + ascii.Length + 4 + 4 + 2 + 1 + 67;
			int descAligned = (descSize + 3) & ~3;
			int dataAt = 128 + 4 + 12;
			int total = dataAt + descAligned;
			var icc = new byte[total];

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
			U32 (8, 0x02100000);
			Signature (12, deviceClass);
			Signature (16, colorSpace);
			Signature (20, "XYZ ");
			Signature (36, "acsp");
			U32 (68, 0x0000f6d6);
			U32 (72, 0x00010000);
			U32 (76, 0x0000d32d);
			U32 (128, 1);
			Signature (132, "desc");
			U32 (136, (uint)dataAt);
			U32 (140, (uint)descSize);

			Signature (dataAt, "desc");
			U32 (dataAt + 8, (uint)ascii.Length);
			Array.Copy (ascii, 0, icc, dataAt + 12, ascii.Length);
			return icc;
		}
	}
}
