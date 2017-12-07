//
// SepiaTone.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
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

using FSpot.Cms;

using Gdk;

namespace FSpot.ColorAdjustment
{
	public class SepiaTone : Adjustment
	{
		public SepiaTone (Pixbuf input, Profile inputProfile) : base (input, inputProfile)
		{
		}

		protected override List <Profile> GenerateAdjustments ()
		{
			var profiles = new List <Profile> ();
			profiles.Add (InputProfile);
			profiles.Add (Profile.CreateAbstract (nsteps,
								  1.0,
								  0.0,
								  0.0,
								  0.0,
								  -100.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.D50));

			profiles.Add (Profile.CreateAbstract (nsteps,
								  1.0,
								  32.0,
								  0.0,
								  0.0,
								  0.0,
								  null,
								  ColorCIExyY.D50,
								  ColorCIExyY.WhitePointFromTemperature (9934)));
			profiles.Add (DestinationProfile);
			return profiles;
		}
	}
}
