/*
 * SoftFocusEditor.cs
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
	// TODO: This had a keybinding e. Maybe we should add it back, but did people even knew it?
	class SoftFocusEditor : Editor
	{
		double radius;
		Scale scale;

		public SoftFocusEditor () : base (Catalog.GetString ("Soft Focus"), "filter-soft-focus") {
			// FIXME: need tooltip Catalog.GetString ("Create a soft focus visual effect")
			HasSettings = true;
        }

		public override Widget ConfigurationWidget ()
		{
			scale = new HScale (0, 1, .01);
			scale.Value = 0.5;
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
				Widgets.SoftFocus soft = new Widgets.SoftFocus (info);
				soft.Radius = radius;

				MemorySurface surface = new MemorySurface (Format.Argb32,
									   input.Width,
									   input.Height);

				Context ctx = new Context (surface);
				soft.Apply (ctx, info.Bounds);
				((IDisposable)ctx).Dispose ();

				result = MemorySurface.CreatePixbuf (surface);
				surface.Flush ();
			}
			return result;
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			radius = scale.Value;
			UpdatePreview ();
		}
	}
}
