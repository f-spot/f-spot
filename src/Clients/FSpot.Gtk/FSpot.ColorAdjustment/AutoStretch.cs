//
// AutoStretch.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

using FSpot.Cms;

using Gdk;

using Hyena;

namespace FSpot.ColorAdjustment
{
	public class AutoStretch : Adjustment
	{
		public AutoStretch (Pixbuf input, Profile inputProfile) : base (input, inputProfile)
		{
		}

		protected override List<Profile> GenerateAdjustments ()
		{
			var profiles = new List<Profile> ();
			var hist = new Histogram (Input);
			tables = new ToneCurve[3];

			for (int channel = 0; channel < tables.Length; channel++) {
				hist.GetHighLow (channel, out var high, out var low);
				Log.Debug ($"high = {high}, low = {low}");
				tables[channel] = StretchChannel (255, low / 255.0, high / 255.0);
			}
			profiles.Add (new Profile (IccColorSpace.Rgb, tables));
			return profiles;
		}

		ToneCurve StretchChannel (int count, double low, double high)
		{
			var entries = new ushort[count];
			for (int i = 0; i < entries.Length; i++) {
				double val = i / (double)entries.Length;

				if (high != low) {
					val = Math.Max ((val - low), 0) / (high - low);
				} else {
					val = Math.Max ((val - low), 0);
				}

				entries[i] = (ushort)Math.Min (Math.Round (ushort.MaxValue * val), ushort.MaxValue);
				//System.Console.WriteLine ("val {0}, result {1}", Math.Round (val * ushort.MaxValue), entries [i]);
			}
			return new ToneCurve (entries);
		}

		ToneCurve[] tables;
	}
}
