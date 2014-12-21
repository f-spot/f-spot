//
// AutoStretch.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
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

		protected override List <Profile> GenerateAdjustments ()
		{
			var profiles = new List <Profile> ();
			var hist = new Histogram (Input);
			tables = new ToneCurve [3];

			for (int channel = 0; channel < tables.Length; channel++) {
				int high, low;
				hist.GetHighLow (channel, out high, out low);
				Log.DebugFormat ("high = {0}, low = {1}", high, low);
				tables [channel] = StretchChannel (255, low / 255.0, high / 255.0);
			}
			profiles.Add (new Profile (IccColorSpace.Rgb, tables));
			return profiles;
		}

		ToneCurve StretchChannel (int count, double low, double high)
		{
			var entries = new ushort [count];
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
			return new ToneCurve (entries);
		}

		ToneCurve [] tables;
	}
}
