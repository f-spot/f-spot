//
// PulsingButton.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theatrics;

namespace Hyena.Widgets
{
	public class PulsingButton : Button
	{
		static Stage<PulsingButton> default_stage;
		public static Stage<PulsingButton> DefaultStage {
			get {
				if (default_stage == null) {
					default_stage = new Stage<PulsingButton> ();
					default_stage.DefaultActorDuration = 1250;
				}

				return default_stage;
			}
		}

		Pulsator<PulsingButton> pulsator = new Pulsator<PulsingButton> ();

		public Stage<PulsingButton> Stage {
			get { return pulsator.Stage; }
			set { pulsator.Stage = value; }
		}

		public PulsingButton () : base ()
		{
			Setup ();
		}

		public PulsingButton (string stock_id) : base (stock_id)
		{
			Setup ();
		}

		public PulsingButton (Widget widget) : base (widget)
		{
			Setup ();
		}

		protected PulsingButton (IntPtr raw) : base (raw)
		{
			Setup ();
		}

		void Setup ()
		{
			Stage = DefaultStage;
			pulsator.Target = this;
			pulsator.Pulse += delegate { QueueDraw (); };
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (!pulsator.IsPulsing) {
				return base.OnExposeEvent (evnt);
			}

			Cairo.Context cr = Gdk.CairoHelper.Create (GdkWindow);

			double x = Allocation.X + Allocation.Width / 2;
			double y = Allocation.Y + Allocation.Height / 2;
			double r = Math.Min (Allocation.Width, Allocation.Height) / 2;
			double alpha = Choreographer.Compose (pulsator.Percent, Easing.Sine);

			Cairo.Color color = CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Selected));
			var fill = new Cairo.RadialGradient (x, y, 0, x, y, r);
			color.A = alpha;
			fill.AddColorStop (0, color);
			fill.AddColorStop (0.5, color);
			color.A = 0;
			fill.AddColorStop (1, color);

			cr.Arc (x, y, r, 0, 2 * Math.PI);
			cr.SetSource (fill);
			cr.Fill ();
			fill.Dispose ();

			CairoExtensions.DisposeContext (cr);
			return base.OnExposeEvent (evnt);
		}

		public void StartPulsing ()
		{
			if (IsMapped && Sensitive) {
				pulsator.StartPulsing ();
			}
		}

		public void StopPulsing ()
		{
			pulsator.StopPulsing ();
		}

		protected override void OnStateChanged (StateType previous_state)
		{
			base.OnStateChanged (previous_state);
			if (State == StateType.Insensitive) {
				StopPulsing ();
			}
		}
	}
}
