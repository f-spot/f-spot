//
// BWEditor.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;

using FSpot.Editors;
using FSpot.Resources.Lang;

using Gdk;

using Gtk;

using SerilogTimings;

namespace FSpot.Addins.Editors
{
	class BWEditor : Editor
	{
		public BWEditor () : base (Strings.ConvertToBW, null)
		{
			CanHandleMultiple = false;
			HasSettings = true;
			ApplyLabel = Strings.Apply;
		}

		protected override Pixbuf Process (Pixbuf input, Cms.Profile input_profile)
		{
			using var op = Operation.Begin ($"BWEditor.Process");

			if (input.BitsPerSample != 8) {
				Logger.Log.Warning ("unsupported pixbuf format");
				return (Pixbuf)input.Clone ();
			}
			var output = new Pixbuf (input.Colorspace, input.HasAlpha, input.BitsPerSample, input.Width, input.Height);
			var multiply = new Vector4 ((float)(r.Value / 100.0), (float)(g.Value / 100.0), (float)(b.Value / 100.0), 0);
			Normalize (ref multiply);

			bool has_alpha = input.HasAlpha;
			int chan = input.NChannels;
			int rowstride_in = input.Rowstride;
			int rowstride_out = output.Rowstride;
			Vector4 v_in;
			float[] fcurve = new float[256];
			c.GetVector (fcurve.Length, fcurve);
			byte[] curve = new byte[fcurve.Length];
			for (int i = 0; i < fcurve.Length; i++)
				curve[i] = (byte)fcurve[i];
			unsafe {
				byte* pix_in = (byte*)input.Pixels;
				byte* pix_out = (byte*)output.Pixels;
				for (int i = 0; i < input.Height; i++)
					for (int j = 0; j < input.Width; j++) {
						v_in = new Vector4 (pix_in[i * rowstride_in + j * chan],
									 pix_in[i * rowstride_in + j * chan + 1],
									 pix_in[i * rowstride_in + j * chan + 2],
									 0);
						float val = Desaturate (ref v_in, ref multiply);
						pix_out[i * rowstride_out + j * chan] = curve[unchecked((byte)val)];
						pix_out[i * rowstride_out + j * chan + 1] = curve[unchecked((byte)val)];
						pix_out[i * rowstride_out + j * chan + 2] = curve[unchecked((byte)val)];
						if (has_alpha)
							pix_out[i * rowstride_out + j * chan + 3] = pix_in[i * rowstride_in + j * chan + 3];
					}
			}

			op.Complete ();
			return output;
		}

		static float Desaturate (ref Vector4 input, ref Vector4 chan_multiplier)
		{
			Vector4 temp = input * chan_multiplier; //(r1,g1,b1,0) = (r,g,b,a) * (rx, gx, bx, 0)
			return temp.X + temp.Y + temp.Z;
		}

		static void Normalize (ref Vector4 v)
		{
			float sum = v.X + v.Y + v.Z;
			v /= new Vector4 (sum);
		}

		HScale r, g, b;
		Curve c;

		public override Widget ConfigurationWidget ()
		{
			var h = new VBox ();
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
			var btn = new Button (Gtk.Stock.Refresh);
			btn.Clicked += delegate { UpdatePreview (); };
			h.Add (btn);
			return h;
		}

		void SettingsChanged (object sender, EventArgs e)
		{
			UpdatePreview ();
		}
	}
}
