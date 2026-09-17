//
// ColorEditor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//   Paul Lange <palango@gmx.de>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Ruben Vermeersch
// Copyright (C) 2010 Paul Lange
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using FSpot.Gui;
using FSpot.Resources.Lang;

using Gdk;

using Gtk;

namespace FSpot.Editors
{
	class ColorEditor : Editor
	{
		GtkBeans.Builder builder;

#pragma warning disable 649
		[GtkBeans.Builder.Object] Gtk.HScale exposure_scale;
		[GtkBeans.Builder.Object] Gtk.HScale temp_scale;
		[GtkBeans.Builder.Object] Gtk.HScale temptint_scale;
		[GtkBeans.Builder.Object] Gtk.HScale brightness_scale;
		[GtkBeans.Builder.Object] Gtk.HScale contrast_scale;
		[GtkBeans.Builder.Object] Gtk.HScale hue_scale;
		[GtkBeans.Builder.Object] Gtk.HScale sat_scale;

		[GtkBeans.Builder.Object] Gtk.SpinButton exposure_spinbutton;
		[GtkBeans.Builder.Object] Gtk.SpinButton temp_spinbutton;
		[GtkBeans.Builder.Object] Gtk.SpinButton temptint_spinbutton;
		[GtkBeans.Builder.Object] Gtk.SpinButton brightness_spinbutton;
		[GtkBeans.Builder.Object] Gtk.SpinButton contrast_spinbutton;
		[GtkBeans.Builder.Object] Gtk.SpinButton hue_spinbutton;
		[GtkBeans.Builder.Object] Gtk.SpinButton sat_spinbutton;
#pragma warning restore 649

		public ColorEditor () : base (PhotoEditorCatalog.Color)
		{
			ApplyLabel = Strings.Adjust;
		}

		public override Widget ConfigurationWidget ()
		{
			builder = TranslatedBuilder.FromResource (null, "color_editor_prefs_window.ui");
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

		protected override PhotoEditorContext CreateContext ()
		{
			var context = base.CreateContext ();
			context.ColorExposure = exposure_scale.Value;
			context.ColorBrightness = brightness_scale.Value;
			context.ColorContrast = contrast_scale.Value;
			context.ColorHue = hue_scale.Value;
			context.ColorSaturation = sat_scale.Value;
			context.ColorTemperature = temp_scale.Value;
			context.ColorTint = temptint_scale.Value;
			return context;
		}
	}
}
