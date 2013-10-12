//
// ColorEditor.cs
//
// Author:
//   Stephen Shaw <sshaw@decriptor.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2013 Stephen Shaw
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
// Copyright (C) 2010 Paul Lange
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

using FSpot.ColorAdjustment;

using Gdk;
using Gtk;

using Mono.Unix;

namespace FSpot.Editors
{
	class ColorEditor : Editor
	{
		Builder builder;

		[Builder.Object] HScale exposure_scale;
		[Builder.Object] HScale temp_scale;
		[Builder.Object] HScale temptint_scale;
		[Builder.Object] HScale brightness_scale;
		[Builder.Object] HScale contrast_scale;
		[Builder.Object] HScale hue_scale;
		[Builder.Object] HScale sat_scale;
				  
		[Builder.Object] SpinButton exposure_spinbutton;
		[Builder.Object] SpinButton temp_spinbutton;
		[Builder.Object] SpinButton temptint_spinbutton;
		[Builder.Object] SpinButton brightness_spinbutton;
		[Builder.Object] SpinButton contrast_spinbutton;
		[Builder.Object] SpinButton hue_spinbutton;
		[Builder.Object] SpinButton sat_spinbutton;

		public ColorEditor () : base (Catalog.GetString ("Adjust Colors"), "adjust-colors")
		{
			// FIXME: need tooltip Catalog.GetString ("Adjust the photo colors")
			HasSettings = true;
			ApplyLabel = Catalog.GetString ("Adjust");
		}

		public override Widget ConfigurationWidget ()
		{
			builder = new Builder (null, "color_editor_prefs_window.ui", null);
			builder.Autoconnect (this);
			AttachInterface ();
			return new VBox (builder.GetRawObject ("color_editor_prefs"));
		}

		void AttachInterface ()
		{
			temp_spinbutton.Adjustment.ChangeValue ();
			temptint_spinbutton.Adjustment.ChangeValue ();
			brightness_spinbutton.Adjustment.ChangeValue ();
			contrast_spinbutton.Adjustment.ChangeValue ();
			hue_spinbutton.Adjustment.ChangeValue ();
			sat_spinbutton.Adjustment.ChangeValue ();
			exposure_spinbutton.Adjustment.ChangeValue ();

			temp_scale.Value = 5000;

			exposure_scale.ValueChanged += RangeChanged;
			temp_scale.ValueChanged += RangeChanged;
			temptint_scale.ValueChanged += RangeChanged;
			brightness_scale.ValueChanged += RangeChanged;
			contrast_scale.ValueChanged += RangeChanged;
			hue_scale.ValueChanged += RangeChanged;
			sat_scale.ValueChanged += RangeChanged;
		}

		public void RangeChanged (object sender, EventArgs args)
		{
			UpdatePreview ();
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			Cms.ColorCIEXYZ src_wp;
			Cms.ColorCIEXYZ dest_wp;

			src_wp = Cms.ColorCIExyY.WhitePointFromTemperature (5000).ToXYZ ();
			dest_wp = Cms.ColorCIExyY.WhitePointFromTemperature ((int)temp_scale.Value).ToXYZ ();
			Cms.ColorCIELab dest_lab = dest_wp.ToLab (src_wp);
			dest_lab.a += temptint_scale.Value;
			dest_wp = dest_lab.ToXYZ (src_wp);

			FullColorAdjustment adjust = new FullColorAdjustment (input, input_profile,
					exposure_scale.Value, brightness_scale.Value, contrast_scale.Value,
					hue_scale.Value, sat_scale.Value, src_wp, dest_wp);
			return adjust.Adjust ();
		}
	}
}
