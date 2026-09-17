// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Cms;
using FSpot.Imaging;

namespace FSpot.ColorAdjustment
{
	public class AutoStretch : Adjustment
	{
		public AutoStretch (ImageBuffer input, Profile inputProfile) : base (input, inputProfile)
		{
		}

		protected override List<Profile> GenerateAdjustments ()
		{
			var values = RgbHistogram.CountRgb (Input);
			var tables = new ToneCurve[3];
			for (int channel = 0; channel < tables.Length; channel++) {
				RgbHistogram.GetHighLow (values, channel, out var high, out var low);
				tables[channel] = StretchChannel (255, low / 255.0, high / 255.0);
			}

			return new List<Profile> { new Profile (IccColorSpace.Rgb, tables) };
		}

		static ToneCurve StretchChannel (int count, double low, double high)
		{
			var entries = new ushort[count];
			for (int i = 0; i < entries.Length; i++) {
				double val = i / (double)entries.Length;
				if (high != low)
					val = Math.Max ((val - low), 0) / (high - low);
				else
					val = Math.Max ((val - low), 0);
				entries[i] = (ushort)Math.Min (Math.Round (ushort.MaxValue * val), ushort.MaxValue);
			}

			return new ToneCurve (entries);
		}
	}
}
