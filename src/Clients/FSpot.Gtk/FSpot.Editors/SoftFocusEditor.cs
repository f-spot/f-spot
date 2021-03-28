//
// SoftFocusEditor.cs
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

using Mono.Unix;

using Cairo;

using Gdk;
using Gtk;

using FSpot.Widgets;

using Pinta.Core;

namespace FSpot.Editors
{
	// TODO: This had a keybinding e. Maybe we should add it back, but did people even knew it?
	class SoftFocusEditor : Editor
	{
		double radius;
		Scale scale;

		public SoftFocusEditor () : base (Catalog.GetString ("Soft Focus"), "filter-soft-focus")
		{
			// FIXME: need tooltip Catalog.GetString ("Create a soft focus visual effect")
			HasSettings = true;
		}

		public override Widget ConfigurationWidget ()
		{
			scale = new HScale (0, 1, .01) {
				Value = 0.5
			};
			scale.ValueChanged += HandleValueChanged;
			return scale;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile inputProfile)
		{
			return ProcessImpl (input, inputProfile, false);
		}

		protected override Pixbuf ProcessFast (Pixbuf input, Cms.Profile input_profile)
		{
			return ProcessImpl (input, input_profile, true);
		}


		Pixbuf ProcessImpl (Pixbuf input, Cms.Profile inputProfile, bool fast)
		{
			Pixbuf result;
			using (var info = new ImageInfo (input)) {
				using var soft = new SoftFocus (info) {
					Radius = radius
				};

				using var surface = new ImageSurface (Format.Argb32, input.Width, input.Height);
				using (var ctx = new Context (surface)) {
					soft.Apply (ctx, info.Bounds);
				}

				result = surface.ToPixbuf ();
				surface.Flush ();
			}
			return result;
		}

		void HandleValueChanged (object sender, System.EventArgs args)
		{
			radius = scale.Value;
			UpdatePreview ();
		}
	}
}
