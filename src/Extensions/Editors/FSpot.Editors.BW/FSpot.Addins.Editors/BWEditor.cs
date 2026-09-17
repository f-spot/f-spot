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

using FSpot.Editors;
using FSpot.Resources.Lang;

using Gdk;

using Gtk;

namespace FSpot.Addins.Editors
{
	class BWEditor : Editor
	{
		public BWEditor () : base (PhotoEditorCatalog.BW)
		{
			ApplyLabel = Strings.Apply;
		}

		protected override PhotoEditorContext CreateContext ()
		{
			var context = base.CreateContext ();
			context.BwRed = (float)r.Value;
			context.BwGreen = (float)g.Value;
			context.BwBlue = (float)b.Value;
			float[] fcurve = new float[256];
			c.GetVector (fcurve.Length, fcurve);
			var curve = new byte[fcurve.Length];
			for (int i = 0; i < fcurve.Length; i++)
				curve[i] = (byte)fcurve[i];
			context.BwCurve = curve;
			return context;
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
