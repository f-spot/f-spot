//
// TiltEditor.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
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

using Mono.Unix;

using Gdk;
using Gtk;
using Cairo;

using FSpot.Widgets;

using Pinta.Core;

namespace FSpot.Editors
{
	// TODO: there were keybindings (left/right) to adjust tilt, maybe they should be added back.
	class TiltEditor : Editor
	{
		double angle;
		Scale scale;

		public TiltEditor () : base (Catalog.GetString ("Straighten"), "align-horizon")
		{
			// FIXME: need tooltip Catalog.GetString ("Adjust the angle of the image to straighten the horizon")
			HasSettings = true;
        }

		public override Widget ConfigurationWidget ()
		{
			scale = new HScale (-45, 45, 1);
			scale.Value = 0.0;
			scale.ValueChanged += HandleValueChanged;
			return scale;
		}


		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			return ProcessImpl (input, input_profile, false);
		}

		protected override Pixbuf ProcessFast (Pixbuf input, Cms.Profile input_profile)
		{
			return ProcessImpl (input, input_profile, true);
		}


		Pixbuf ProcessImpl (Pixbuf input, Cms.Profile inputProfile, bool fast)
		{
			Pixbuf result;
			using (ImageInfo info = new ImageInfo (input)) {
				using (ImageSurface surface = new ImageSurface (Format.Argb32,
									   input.Width,
									   input.Height)) {
					using (Context ctx = new Context (surface)) {
						ctx.Matrix = info.Fill (info.Bounds, angle);
						using (SurfacePattern p = new SurfacePattern (info.Surface)) {
							if (fast)
								p.Filter =  Filter.Fast;
							ctx.SetSource(p);
							ctx.Paint ();
						}
						result = surface.ToPixbuf();
                        surface.Flush ();
					}
				}
			}
			return result;
		}

		void HandleValueChanged (object sender, EventArgs args)
		{
			angle = scale.Value * Math.PI / -180;
			UpdatePreview ();
		}
	}
}
