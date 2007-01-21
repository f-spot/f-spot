/*
 * TiltEditor.cs
 *
 * Copyright 2007 Novell Inc.
 *
 * Author
 *   Larry Ewing <lewing@novell.com>
 *
 * See COPYING for license information.
 *
 */
using System;
using Gtk;
using FSpot;
using Cairo;

namespace FSpot.Editors {
	public class Tilt : EffectEditor {
		Scale scale;

		public Tilt (PhotoImageView view) : base (view)
		{
		}

		protected override void SetView (PhotoImageView view)
		{
			base.SetView (view);
			effect = new Widgets.Tilt (info);
		}

		protected override Widget CreateControls ()
		{
			scale = new HScale (-45, 45, 1);
			scale.ValueChanged += HandleValueChanged;
			scale.WidthRequest = 250;

			return scale;
		}

		private void HandleValueChanged (object sender, System.EventArgs args)
		{
			((Widgets.Tilt)effect).Angle = scale.Value * Math.PI / 180;
			view.QueueDraw ();
		}
	}
}
