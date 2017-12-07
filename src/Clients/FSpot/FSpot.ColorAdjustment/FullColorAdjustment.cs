//
// FullColorAdjustment.cs
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

using Gdk;

namespace FSpot.ColorAdjustment
{
	public class FullColorAdjustment : Adjustment
	{
		readonly double exposure;
		readonly double brightness;
		readonly double contrast;
		readonly double hue;
		readonly double saturation;

		Cms.ColorCIEXYZ src_wp;
		Cms.ColorCIEXYZ dest_wp;

		public FullColorAdjustment (Pixbuf input, Cms.Profile input_profile,
				double exposure, double brightness, double contrast,
				double hue, double saturation,
				Cms.ColorCIEXYZ src_wp, Cms.ColorCIEXYZ dest_wp)
			: base (input, input_profile)
		{
			this.exposure = exposure;
			this.brightness = brightness;
			this.contrast = contrast;
			this.hue = hue;
			this.saturation = saturation;
			this.src_wp = src_wp;
			this.dest_wp = dest_wp;
		}

		protected override List <Cms.Profile> GenerateAdjustments ()
		{
			var profiles = new List <Cms.Profile> ();
			profiles.Add (InputProfile);
			profiles.Add (Cms.Profile.CreateAbstract (nsteps,
						Math.Pow (Math.Sqrt (2.0), exposure),
						brightness,
						contrast,
						hue,
						saturation,
						null,
						src_wp.ToxyY (),
						dest_wp.ToxyY ()));
			profiles.Add (DestinationProfile);
			return profiles;
		}
	}
}
