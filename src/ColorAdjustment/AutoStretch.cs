/*
 * AutoStretch.cs
 * 
 * Author
 *   Larry Ewing <lewing@novell.com>
 *   Ruben Vermeersch <ruben@savanne.be>
 *
 * See COPYING for license information
 *
 */
using Cms;
using Gdk;
using System;
using System.Collections.Generic;

namespace FSpot.ColorAdjustment {
	public class AutoStretch : Adjustment {
		public AutoStretch (Pixbuf input, Cms.Profile input_profile) : base (input, input_profile)
		{
		}

		protected override List <Cms.Profile> GenerateAdjustments ()
		{
			List <Cms.Profile> profiles = new List <Cms.Profile> ();
			Histogram hist = new Histogram (Input);
			tables = new GammaTable [3];

			for (int channel = 0; channel < tables.Length; channel++) {
				int high, low;
				hist.GetHighLow (channel, out high, out low);
				System.Console.WriteLine ("high = {0}, low = {1}", high, low);
				tables [channel] = StretchChannel (255, low / 255.0, high / 255.0); 
			}
			profiles.Add (new Cms.Profile (IccColorSpace.Rgb, tables));
			return profiles;
		}

		GammaTable StretchChannel (int count, double low, double high)
		{
			ushort [] entries = new ushort [count];
			for (int i = 0; i < entries.Length; i++) {
				double val = i / (double)entries.Length;
				
				if (high != low) {
					val = Math.Max ((val - low), 0) / (high - low);
				} else {
					val = Math.Max ((val - low), 0);
				}

				entries [i] = (ushort) Math.Min (Math.Round (ushort.MaxValue * val), ushort.MaxValue);
				//System.Console.WriteLine ("val {0}, result {1}", Math.Round (val * ushort.MaxValue), entries [i]);
			}
			return new GammaTable (entries);
		}

		GammaTable [] tables;
	}
}
