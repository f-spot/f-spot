/*
 * FSpot.Editors.TiltEditor.cs
 *
 * Author(s)
 * 	Ruben Vermeersch <ruben@savanne.be>
 *	Stephane Delcroix <stephane@delcroix.org>
 *
 * Copyright (c) 2009 Stephane Delcroix
 * 
 * This is free software. See COPYING for details.
 */

using System;
using Mono.Unix;

using Gdk;
using Gtk;
using Cairo;

using FSpot.Widgets;

namespace FSpot.Editors
{
	// TODO: there were keybindings (left/right) to adjust tilt, maybe they should be added back.
	class TiltEditor : Editor
	{
		double angle;
		Scale scale;

		public TiltEditor () : base (Catalog.GetString ("Straighten"), "align-horizon") {
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


		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
			return ProcessImpl (input, input_profile, false);
		}

		protected override Pixbuf ProcessFast (Pixbuf input, Cms.Profile input_profile)
		{
			return ProcessImpl (input, input_profile, true);
		}


		private Pixbuf ProcessImpl (Pixbuf input, Cms.Profile input_profile, bool fast) {
			Pixbuf result;
			using (ImageInfo info = new ImageInfo (input)) {
				using (MemorySurface surface = new MemorySurface (Format.Argb32,
									   input.Width,
									   input.Height)) {
					using (Context ctx = new Context (surface)) {
						ctx.Matrix = info.Fill (info.Bounds, angle);
						using (SurfacePattern p = new SurfacePattern (info.Surface)) {
							if (fast) 
								p.Filter =  Filter.Fast;
							ctx.Source = p;
							ctx.Paint ();
						}
						result = MemorySurface.CreatePixbuf (surface);
						surface.Flush ();
					}
				}
			}
			return result;
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			angle = scale.Value * Math.PI / -180;
			UpdatePreview ();
		}
	}
}
