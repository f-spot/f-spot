/*
 * TiltEditor.cs
 *
 * Author(s)
 * 	Ruben Vermeersch <ruben@savanne.be>
 *
 * This is free software. See COPYING for details.
 */

using Cairo;

using FSpot.Widgets;

using Gdk;
using Gtk;

using Mono.Unix;

using System;

namespace FSpot.Editors {
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
				MemorySurface surface = new MemorySurface (Format.Argb32,
									   input.Width,
									   input.Height);

				Context ctx = new Context (surface);
				ctx.Matrix = info.Fill (info.Bounds, angle);
				SurfacePattern p = new SurfacePattern (info.Surface);
				if (fast) {
					p.Filter =  Filter.Fast;
				}
				ctx.Source = p;
				ctx.Paint ();
				((IDisposable)ctx).Dispose ();
				p.Destroy ();
				result = MemorySurface.CreatePixbuf (surface);
				surface.Flush ();
			}
			return result;
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			angle = scale.Value * Math.PI / 180;
			UpdatePreview ();
		}
	}
}
