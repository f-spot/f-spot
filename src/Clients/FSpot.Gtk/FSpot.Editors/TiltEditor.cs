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
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
			using (var info = new ImageInfo (input)) {
				using var surface = new ImageSurface (Format.Argb32, input.Width, input.Height);
				using var ctx = new Context (surface);
				ctx.Matrix = info.Fill (info.Bounds, angle);
				using (var p = new SurfacePattern (info.Surface)) {
					if (fast)
						p.Filter = Filter.Fast;
					ctx.SetSource (p);
					ctx.Paint ();
				}
				result = surface.ToPixbuf ();
				surface.Flush ();
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
