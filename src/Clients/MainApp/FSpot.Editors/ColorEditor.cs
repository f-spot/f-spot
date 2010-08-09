/*
 * ColorEditor.cs
 *
 * Author(s)
 *  Larry Ewing <lewing@novell.com>
 *  Ruben Vermeersch <ruben@savanne.be>
 *  Paul Lange <palango@gmx.de>
 *
 * This is free software. See COPYING for details.
 */

using FSpot;
using FSpot.ColorAdjustment;
using Gdk;
using Gtk;
using Mono.Unix;
using System;

using GtkBeans;

namespace FSpot.Editors {
	class ColorEditor : Editor {
		GtkBeans.Builder builder;

		[GtkBeans.Builder.Object] private Gtk.HScale exposure_scale;
		[GtkBeans.Builder.Object] private Gtk.HScale temp_scale;
		[GtkBeans.Builder.Object] private Gtk.HScale temptint_scale;
		[GtkBeans.Builder.Object] private Gtk.HScale brightness_scale;
		[GtkBeans.Builder.Object] private Gtk.HScale contrast_scale;
		[GtkBeans.Builder.Object] private Gtk.HScale hue_scale;
		[GtkBeans.Builder.Object] private Gtk.HScale sat_scale;

		[GtkBeans.Builder.Object] private Gtk.SpinButton exposure_spinbutton;
		[GtkBeans.Builder.Object] private Gtk.SpinButton temp_spinbutton;
		[GtkBeans.Builder.Object] private Gtk.SpinButton temptint_spinbutton;
		[GtkBeans.Builder.Object] private Gtk.SpinButton brightness_spinbutton;
		[GtkBeans.Builder.Object] private Gtk.SpinButton contrast_spinbutton;
		[GtkBeans.Builder.Object] private Gtk.SpinButton hue_spinbutton;
		[GtkBeans.Builder.Object] private Gtk.SpinButton sat_spinbutton;

		public ColorEditor () : base (Catalog.GetString ("Adjust Colors"), "adjust-colors") {
			// FIXME: need tooltip Catalog.GetString ("Adjust the photo colors")
			HasSettings = true;
			ApplyLabel = Catalog.GetString ("Adjust");
		}

		public override Widget ConfigurationWidget () {
			builder = new GtkBeans.Builder (null, "color_editor_prefs_window.ui", null);
			builder.Autoconnect (this);
			AttachInterface ();
			return new VBox (builder.GetRawObject ("color_editor_prefs"));
		}

		private void AttachInterface () {

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

		public void RangeChanged (object sender, EventArgs args) {
			UpdatePreview ();
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile) {
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
