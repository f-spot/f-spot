//
// Adjustment.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Larry Ewing <lewing@novell.com>
//
// Copyright (C) 2007-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
// Copyright (C) 2007 Larry Ewing
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

using System.Collections.Generic;

using Cms;

using Gdk;

namespace FSpot.ColorAdjustment {
	public abstract class Adjustment {
		protected int nsteps = 20;
		private Intent intent = Intent.Perceptual;

		// This is the input pixbuf, on which the adjustment will be performed.
		protected readonly Gdk.Pixbuf Input;

		private Profile input_profile;
		public Profile InputProfile {
			get {
				if (input_profile == null)
					input_profile = Profile.CreateStandardRgb ();

				return input_profile;
			}
			set { input_profile = value; }
		}

		private Profile destination_profile;
		public Profile DestinationProfile {
			get {
				if (destination_profile == null)
					destination_profile = InputProfile;

				return destination_profile;
			}
			set { destination_profile = value; }
		}

		public Adjustment (Pixbuf input, Profile inputProfile)
		{
			Input = input;
			InputProfile = inputProfile;
		}

		protected abstract List <Profile> GenerateAdjustments ();

		public Pixbuf Adjust ()
		{
			Gdk.Pixbuf final = new Gdk.Pixbuf (Gdk.Colorspace.Rgb,
							   false, 8,
							   Input.Width,
							   Input.Height);
			Profile [] list = GenerateAdjustments ().ToArray ();

			if (Input.HasAlpha) {
				Gdk.Pixbuf input_copy = (Gdk.Pixbuf)Input.Clone ();
				Pixbuf alpha = PixbufUtils.Flatten (Input);
				Transform transform = new Transform (list,
								     PixbufUtils.PixbufCmsFormat (alpha),
								     PixbufUtils.PixbufCmsFormat (final),
								     intent, 0x0000);
				PixbufUtils.ColorAdjust (alpha, final, transform);
				PixbufUtils.ReplaceColor (final, input_copy);
				alpha.Dispose ();
				final.Dispose ();
				final = input_copy;
			} else {
				Transform transform = new Transform (list,
									     PixbufUtils.PixbufCmsFormat (Input),
									     PixbufUtils.PixbufCmsFormat (final),
									     intent, 0x0000);

				PixbufUtils.ColorAdjust (Input, final, transform);
			}

			return final;
		}
	}
}
