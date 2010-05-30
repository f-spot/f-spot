/*
 * BWEditor.cs
 *
 * Author(s)
 * 	Stephane Delcroix  (stephane@delcroix.org)
 *
 * Copyright (c) 2009 Novell, Inc.
 *
 * This is open source software. See COPYING for details.
 */

using System;
using FSpot;
using FSpot.Editors;
using Gtk;
using Gdk;
using Mono.Unix;
using Mono.Simd;
using Hyena;

namespace BWEditor {
	class BWEditor : Editor {
		public BWEditor () : base (Catalog.GetString ("Convert to B/W"), null) {
			CanHandleMultiple = false;
			HasSettings = true;
			ApplyLabel = Catalog.GetString ("Apply");
		}

		static bool enhanced_support = EnhancedSimdSupport ();
		static bool EnhancedSimdSupport () { //require sse3
			return SimdRuntime.IsMethodAccelerated (typeof (VectorOperations), "HorizontalAdd", new Type[] {typeof (Vector4f), typeof (Vector4f)})
			       && SimdRuntime.IsMethodAccelerated (typeof (Vector4f), "op_Multiply");
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			uint timer = Log.DebugTimerStart ();
			if (input.BitsPerSample != 8) {
				Log.Warning ("unsupported pixbuf format");
				return (Pixbuf)input.Clone ();
			}
			Pixbuf output = new Pixbuf (input.Colorspace, input.HasAlpha, input.BitsPerSample, input.Width, input.Height);
			Vector4f multiply = new Vector4f ((float)(r.Value/100.0), (float)(g.Value/100.0), (float)(b.Value/100.0), 0);
			Normalize (ref multiply);

			bool has_alpha = input.HasAlpha;
			int chan = input.NChannels;
			int rowstride_in = input.Rowstride;
			int rowstride_out = output.Rowstride;
			Vector4f v_in;
			Vector4f v_out = new Vector4f (0);
			float[] fcurve = new float [256];
			c.GetVector (fcurve.Length, fcurve);
			byte[] curve = new byte [fcurve.Length];
			for (int i = 0; i < fcurve.Length; i++)
				curve[i] = (byte)fcurve[i];
			unsafe {
				byte *pix_in = (byte *)input.Pixels;
				byte *pix_out = (byte *)output.Pixels;
				for (int i=0; i < input.Height; i++)
					for (int j=0; j<input.Width; j++) {
						v_in = new Vector4f (pix_in[i*rowstride_in + j*chan],
								     pix_in[i*rowstride_in + j*chan + 1],
								     pix_in[i*rowstride_in + j*chan + 2],
								     0);
						Desaturate (ref v_in, ref multiply, ref v_out);
						pix_out[i*rowstride_out + j*chan] = curve [unchecked ((byte)v_out.X)];
						pix_out[i*rowstride_out + j*chan + 1] = curve [unchecked ((byte)v_out.Y)];
						pix_out[i*rowstride_out + j*chan + 2] = curve [unchecked ((byte)v_out.Z)];
						if (has_alpha)
							pix_out[i*rowstride_out + j*chan + 3] = pix_in[i*rowstride_in + j*chan + 3];
					}
			}
			Log.DebugTimerPrint (timer, "Processing took {0}");
			return output;
		}

		static void Desaturate (ref Vector4f input, ref Vector4f chan_multiplier, ref Vector4f output)
		{
			Vector4f temp = input * chan_multiplier;	//(r1,g1,b1,0) = (r,g,b,a) * (rx, gx, bx, 0)
			temp = temp.HorizontalAdd (temp);		//(r1+g1, b1+0, r1+g1, b1+0)
			output = temp.HorizontalAdd (temp);		//(r1+g1+b1+0, r1+g1+b1+0, ..., ...)
		}

		static void Normalize (ref Vector4f v)
		{
			float sum = v.X + v.Y + v.Z;
			v /= new Vector4f (sum);
		}

		HScale r, g, b;
		Curve c;

		public override Widget ConfigurationWidget ()
		{
			VBox h = new VBox ();
			r = new HScale (0, 100, 1);
			r.ModifyBg (StateType.Selected, new Color (0xff, 0, 0));
			r.Value = 80;
			r.ValueChanged += SettingsChanged;
			h.Add (r);
			g = new HScale (0, 100, 1);
			g.ModifyBg (StateType.Selected, new Color (0, 0xff, 0));
			g.Value = 10;
			g.ValueChanged += SettingsChanged;
			h.Add (g);
			b = new HScale (0, 100, 1);
			b.ModifyBg (StateType.Selected, new Color (0, 0, 0xff));
			b.Value = 10;
			b.ValueChanged += SettingsChanged;
			h.Add (b);
			c = new Curve ();
			c.CurveType = CurveType.Spline;
			c.SetRange (0, 255, 0, 255);
			h.Add (c);
			Button btn = new Button (Gtk.Stock.Refresh);
			btn.Clicked += delegate {UpdatePreview ();};
			h.Add (btn);
			return h;
		}

		void SettingsChanged (object sender, EventArgs e)
		{
			UpdatePreview ();
		}
	}
}
